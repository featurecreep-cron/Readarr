using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Options;
using NzbDrone.Core.Authentication;
using NzbDrone.Core.Configuration;
using NzbDrone.Test.Common;
using Readarr.Http.Authentication;

namespace NzbDrone.Api.Test.Authentication
{
    [TestFixture]
    public class AuthenticationServiceFixture : TestBase<AuthenticationService>
    {
        private readonly User _user = new User { Username = "user" };

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<ICacheManager>(new CacheManager());

            Mocker.GetMock<IOptions<AuthOptions>>()
                  .SetupGet(s => s.Value)
                  .Returns(new AuthOptions());

            Mocker.GetMock<IConfigFileProvider>()
                  .SetupGet(s => s.AuthenticationMethod)
                  .Returns(AuthenticationType.Forms);

            Mocker.GetMock<IUserService>()
                  .Setup(s => s.FindUser("user", "right"))
                  .Returns(_user);
        }

        private static HttpRequest RequestFrom(string ip)
        {
            var context = new DefaultHttpContext();
            context.Connection.RemoteIpAddress = IPAddress.Parse(ip);

            return context.Request;
        }

        private void FailLogins(string ip, int count)
        {
            for (var i = 0; i < count; i++)
            {
                Subject.Login(RequestFrom(ip), "user", "wrong").Should().BeNull();
            }
        }

        [Test]
        public void should_lock_out_after_five_failed_attempts()
        {
            FailLogins("192.168.1.2", 5);

            Subject.Login(RequestFrom("192.168.1.2"), "user", "right").Should().BeNull();
            Subject.GetLockoutEndTime(RequestFrom("192.168.1.2")).Should().NotBeNull();

            ExceptionVerification.ExpectedWarns(6);
        }

        [Test]
        public void should_not_lock_out_before_five_failed_attempts()
        {
            FailLogins("192.168.1.2", 4);

            Subject.Login(RequestFrom("192.168.1.2"), "user", "right").Should().Be(_user);

            ExceptionVerification.ExpectedWarns(4);
        }

        [Test]
        public void should_reset_failed_attempts_after_a_successful_login()
        {
            FailLogins("192.168.1.2", 4);
            Subject.Login(RequestFrom("192.168.1.2"), "user", "right").Should().Be(_user);
            FailLogins("192.168.1.2", 4);

            Subject.Login(RequestFrom("192.168.1.2"), "user", "right").Should().Be(_user);

            ExceptionVerification.ExpectedWarns(8);
        }

        [Test]
        public void should_only_lock_out_the_failing_address()
        {
            FailLogins("192.168.1.2", 5);

            Subject.Login(RequestFrom("192.168.1.3"), "user", "right").Should().Be(_user);
            Subject.GetLockoutEndTime(RequestFrom("192.168.1.3")).Should().BeNull();

            ExceptionVerification.ExpectedWarns(5);
        }
    }
}
