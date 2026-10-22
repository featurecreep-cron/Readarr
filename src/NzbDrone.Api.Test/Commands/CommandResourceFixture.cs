using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Serializer;
using NzbDrone.Test.Common;
using Readarr.Api.V1.Commands;

namespace NzbDrone.Api.Test.Commands
{
    [TestFixture]
    public class CommandResourceFixture : TestBase
    {
        [Test]
        public void should_ignore_body_from_the_request()
        {
            var resource = STJson.Deserialize<CommandResource>("{\"name\":\"RssSync\",\"body\":{\"name\":\"Backup\",\"sendUpdatesToClient\":true}}");

            resource.Name.Should().Be("RssSync");
            resource.Body.Should().BeNull();
        }
    }
}
