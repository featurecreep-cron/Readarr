using System.IO;
using System.Text;
using FluentAssertions;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;
using NzbDrone.Common;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.ProviderTests.DiskProviderTests
{
    [TestFixture]
    public class ArchiveProviderFixture : TestBase<ArchiveService>
    {
        [Test]
        public void Should_extract_to_correct_folder()
        {
            var destinationFolder = new DirectoryInfo(GetTempFilePath());
            var testArchive = OsInfo.IsWindows ? "TestArchive.zip" : "TestArchive.tar.gz";

            Subject.Extract(GetTestPath("Files/" + testArchive), destinationFolder.FullName);

            destinationFolder.Exists.Should().BeTrue();
            destinationFolder.GetDirectories().Should().HaveCount(1);
            destinationFolder.GetDirectories("*", SearchOption.AllDirectories).Should().HaveCount(3);
            destinationFolder.GetFiles("*.*", SearchOption.AllDirectories).Should().HaveCount(6);
        }

        [Test]
        public void should_not_extract_an_entry_outside_the_destination()
        {
            var root = new DirectoryInfo(GetTempFilePath());
            var destination = Path.Combine(root.FullName, "destination");
            var archive = Path.Combine(root.FullName, "evil.zip");
            Directory.CreateDirectory(destination);

            using (var zip = new ZipOutputStream(File.Create(archive)))
            {
                zip.PutNextEntry(new ZipEntry("../escaped.txt"));
                zip.Write(Encoding.UTF8.GetBytes("escaped"));
                zip.CloseEntry();
            }

            Assert.Throws<IOException>(() => Subject.Extract(archive, destination));

            File.Exists(Path.Combine(root.FullName, "escaped.txt")).Should().BeFalse();
        }
    }
}
