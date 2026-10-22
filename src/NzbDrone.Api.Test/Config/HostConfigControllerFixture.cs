using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;
using NzbDrone.Test.Common;
using Readarr.Api.V1.Config;

namespace NzbDrone.Api.Test.Config
{
    [TestFixture]
    public class HostConfigControllerFixture : TestBase<HostConfigController>
    {
        private Dictionary<string, object> _savedFile;
        private Dictionary<string, object> _savedDb;

        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<IConfigFileProvider>().SetupGet(s => s.SslCertPassword).Returns("cert-secret");
            Mocker.GetMock<IConfigService>().SetupGet(s => s.ProxyPassword).Returns("proxy-secret");
            Mocker.GetMock<IUserService>().Setup(s => s.FindUser()).Returns(new User { Username = "admin", Password = "hash" });

            Mocker.GetMock<IConfigFileProvider>()
                  .Setup(s => s.SaveConfigDictionary(It.IsAny<Dictionary<string, object>>()))
                  .Callback<Dictionary<string, object>>(d => _savedFile = d);

            Mocker.GetMock<IConfigService>()
                  .Setup(s => s.SaveConfigDictionary(It.IsAny<Dictionary<string, object>>()))
                  .Callback<Dictionary<string, object>>(d => _savedDb = d);
        }

        [Test]
        public void should_not_return_secrets()
        {
            var resource = Subject.GetHostConfig();

            resource.Password.Should().Be("********");
            resource.SslCertPassword.Should().Be("********");
            resource.ProxyPassword.Should().Be("********");
        }

        [Test]
        public void saving_masked_secrets_should_keep_the_stored_values()
        {
            var resource = Subject.GetHostConfig();

            Subject.SaveHostConfig(resource);

            _savedFile["SslCertPassword"].Should().Be("cert-secret");
            _savedDb["ProxyPassword"].Should().Be("proxy-secret");
            Mocker.GetMock<IUserService>().Verify(v => v.Upsert("admin", null), Times.Once());
        }

        [Test]
        public void saving_a_new_password_should_set_it()
        {
            var resource = Subject.GetHostConfig();
            resource.Password = "new-password";

            Subject.SaveHostConfig(resource);

            Mocker.GetMock<IUserService>().Verify(v => v.Upsert("admin", "new-password"), Times.Once());
        }
    }
}
