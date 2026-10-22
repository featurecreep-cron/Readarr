using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Disk;

namespace NzbDrone.Common.Test.DiskTests
{
    [TestFixture]
    [Platform("Linux")]
    public class SystemFoldersFixture
    {
        [TestCase("/usr/bin")]
        [TestCase("/usr/sbin")]
        [TestCase("/usr/local/bin")]
        [TestCase("/usr/local/sbin")]
        public void should_treat_binary_folders_as_system_folders(string folder)
        {
            SystemFolders.GetSystemFolders().Should().Contain(folder);
        }
    }
}
