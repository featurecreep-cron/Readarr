using System.IO;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Backup;
using NzbDrone.Test.Common;
using Readarr.Http.Frontend.Mappers;

namespace NzbDrone.Api.Test.FrontendTests
{
    [TestFixture]
    public class BackupFileMapperFixture : TestBase<BackupFileMapper>
    {
        private string _backupFolder;

        [SetUp]
        public void Setup()
        {
            _backupFolder = Path.Combine(TempFolder, "Backups");

            Mocker.GetMock<IBackupService>()
                  .Setup(s => s.GetBackupFolder())
                  .Returns(_backupFolder);
        }

        [Test]
        public void should_map_a_backup_inside_the_backup_folder()
        {
            Subject.Map("/backup/scheduled/readarr_backup_v0.5.0.1_2026.10.22_10.00.00.zip")
                .Should().Be(Path.Combine(_backupFolder, "scheduled", "readarr_backup_v0.5.0.1_2026.10.22_10.00.00.zip"));
        }

        [TestCase("/backup/../config.xml")]
        [TestCase("/backup/scheduled/../../readarr.db")]
        public void should_not_map_a_path_outside_the_backup_folder(string resourceUrl)
        {
            Subject.Map(resourceUrl).Should().BeNull();
        }
    }
}
