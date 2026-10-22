using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books.Calibre;
using NzbDrone.Core.MediaFiles.BookImport;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.RootFolders;

namespace NzbDrone.Core.MediaFiles
{
    public interface IUpgradeMediaFiles
    {
        BookFileMoveResult UpgradeBookFile(BookFile bookFile, LocalBook localBook, bool copyOnly = false);
    }

    public class UpgradeMediaFileService : IUpgradeMediaFiles
    {
        private const string AsideFolderName = ".readarr-upgrade";

        private readonly IRecycleBinProvider _recycleBinProvider;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMetadataTagService _metadataTagService;
        private readonly IMoveBookFiles _bookFileMover;
        private readonly IDiskProvider _diskProvider;
        private readonly IRootFolderService _rootFolderService;
        private readonly ICalibreProxy _calibre;
        private readonly Logger _logger;

        public UpgradeMediaFileService(IRecycleBinProvider recycleBinProvider,
                                       IMediaFileService mediaFileService,
                                       IMetadataTagService metadataTagService,
                                       IMoveBookFiles bookFileMover,
                                       IDiskProvider diskProvider,
                                       IRootFolderService rootFolderService,
                                       ICalibreProxy calibre,
                                       Logger logger)
        {
            _recycleBinProvider = recycleBinProvider;
            _mediaFileService = mediaFileService;
            _metadataTagService = metadataTagService;
            _bookFileMover = bookFileMover;
            _diskProvider = diskProvider;
            _rootFolderService = rootFolderService;
            _calibre = calibre;
            _logger = logger;
        }

        public BookFileMoveResult UpgradeBookFile(BookFile bookFile, LocalBook localBook, bool copyOnly = false)
        {
            var moveFileResult = new BookFileMoveResult();
            var existingFiles = localBook.Book.BookFiles.Value;

            var rootFolderPath = _diskProvider.GetParentFolder(localBook.Author.Path);
            var rootFolder = _rootFolderService.GetBestRootFolder(rootFolderPath);
            var isCalibre = rootFolder.IsCalibreLibrary && rootFolder.CalibreSettings != null;

            var settings = rootFolder.CalibreSettings;

            // If there are existing book files and the root folder is missing, throw, so the old file isn't left behind during the import process.
            if (existingFiles.Any() && !_diskProvider.FolderExists(rootFolderPath))
            {
                throw new RootFolderNotFoundException($"Root folder '{rootFolderPath}' was not found.");
            }

            if (!isCalibre)
            {
                // The existing files are moved aside rather than deleted, so a failed move or copy can put them back.
                // They go to the recycle bin only once the new file is in place.
                var setAside = SetAside(existingFiles.Where(f => IsInAuthorFolder(f, localBook)).ToList());

                try
                {
                    if (copyOnly)
                    {
                        moveFileResult.BookFile = _bookFileMover.CopyBookFile(bookFile, localBook);
                    }
                    else
                    {
                        moveFileResult.BookFile = _bookFileMover.MoveBookFile(bookFile, localBook);
                    }
                }
                catch
                {
                    Restore(setAside);
                    throw;
                }

                foreach (var file in existingFiles)
                {
                    bookFile.CalibreId = file.CalibreId;

                    if (!IsInAuthorFolder(file, localBook))
                    {
                        _logger.Warn("Not removing {0} after upgrading {1}: it is outside the author folder {2}", file.Path, localBook.Book, localBook.Author.Path);
                    }
                    else if (setAside.TryGetValue(file, out var asidePath))
                    {
                        _logger.Debug("Removing existing book file: {0}", file);
                        _recycleBinProvider.DeleteFile(asidePath, rootFolderPath.GetRelativePath(_diskProvider.GetParentFolder(file.Path)));
                    }

                    moveFileResult.OldFiles.Add(file);
                    _mediaFileService.Delete(file, DeleteMediaFileReason.Upgrade);
                }

                DeleteAsideFolders(setAside);

                _metadataTagService.WriteTags(bookFile, true);
            }
            else
            {
                foreach (var file in existingFiles)
                {
                    bookFile.CalibreId = file.CalibreId;

                    if (_diskProvider.FileExists(file.Path))
                    {
                        var existing = _calibre.GetBook(file.CalibreId, settings);
                        var existingFormats = existing.Formats.Keys;
                        _logger.Debug($"Removing existing formats {existingFormats.ConcatToString()} from calibre");
                        _calibre.RemoveFormats(file.CalibreId, existingFormats, settings);
                    }

                    moveFileResult.OldFiles.Add(file);
                    _mediaFileService.Delete(file, DeleteMediaFileReason.Upgrade);
                }

                var source = bookFile.Path;

                moveFileResult.BookFile = _calibre.AddAndConvert(bookFile, settings);

                if (!copyOnly)
                {
                    _diskProvider.DeleteFile(source);
                }
            }

            return moveFileResult;
        }

        // A file the database links to this book but which lives outside its author's folder may belong to something else,
        // for example after a wrong match. It's left where it is.
        private static bool IsInAuthorFolder(BookFile file, LocalBook localBook)
        {
            return localBook.Author.Path.IsParentPath(file.Path);
        }

        private Dictionary<BookFile, string> SetAside(List<BookFile> files)
        {
            var setAside = new Dictionary<BookFile, string>();

            try
            {
                foreach (var file in files.Where(f => _diskProvider.FileExists(f.Path)))
                {
                    var asideFolder = Path.Combine(_diskProvider.GetParentFolder(file.Path), AsideFolderName);
                    var asidePath = Path.Combine(asideFolder, Path.GetFileName(file.Path));

                    _diskProvider.CreateFolder(asideFolder);
                    _diskProvider.MoveFile(file.Path, asidePath, true);
                    setAside.Add(file, asidePath);
                }
            }
            catch
            {
                Restore(setAside);
                throw;
            }

            return setAside;
        }

        private void Restore(Dictionary<BookFile, string> setAside)
        {
            foreach (var (file, asidePath) in setAside)
            {
                try
                {
                    _diskProvider.MoveFile(asidePath, file.Path, true);
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Couldn't put {0} back after a failed upgrade; it is at {1}", file.Path, asidePath);
                }
            }

            DeleteAsideFolders(setAside);
        }

        private void DeleteAsideFolders(Dictionary<BookFile, string> setAside)
        {
            foreach (var folder in setAside.Values.Select(_diskProvider.GetParentFolder).Distinct())
            {
                if (_diskProvider.FolderExists(folder) && _diskProvider.FolderEmpty(folder))
                {
                    _diskProvider.DeleteFolder(folder, false);
                }
            }
        }
    }
}
