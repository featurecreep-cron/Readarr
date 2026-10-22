using System;
using System.Net;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MediaCoverTests
{
    [TestFixture]
    public class MediaCoverProxyFixture : CoreTest<MediaCoverProxy>
    {
        private static readonly byte[] Image = { 1, 2, 3 };

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<ICacheManager>(new CacheManager());

            Mocker.GetMock<IConfigService>()
                  .SetupGet(s => s.MetadataSource)
                  .Returns("http://192.168.1.10:8788");

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Get(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), Image));
        }

        private string Register(string url)
        {
            var proxied = Subject.RegisterUrl(url);
            return proxied.Split('/')[2];
        }

        private void GivenRedirect(string from, string to)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Get(It.Is<HttpRequest>(r => r.Url.FullUri == from)))
                  .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader { { "Location", to } }, Array.Empty<byte>(), HttpStatusCode.Found));
        }

        [TestCase("http://93.184.215.14/cover.jpg")]
        [TestCase("http://192.168.1.10:8788/cover.jpg")]
        public void should_fetch_a_public_or_metadata_server_cover(string url)
        {
            Subject.GetImage(Register(url)).Should().Equal(Image);
        }

        [TestCase("http://127.0.0.1:8787/api/v1/system/status")]
        [TestCase("http://localhost/cover.jpg")]
        [TestCase("http://10.0.0.1/cover.jpg")]
        [TestCase("http://169.254.169.254/latest/meta-data/")]
        [TestCase("http://[::1]/cover.jpg")]
        [TestCase("http://0.0.0.0/cover.jpg")]
        public void should_refuse_a_local_cover(string url)
        {
            Assert.Throws<NzbDroneClientException>(() => Subject.GetImage(Register(url)));

            Mocker.GetMock<IHttpClient>().Verify(v => v.Get(It.IsAny<HttpRequest>()), Times.Never());
        }

        [Test]
        public void should_refuse_a_cover_that_is_not_http()
        {
            Assert.Catch(() => Subject.GetImage(Register("file:///etc/passwd")));

            Mocker.GetMock<IHttpClient>().Verify(v => v.Get(It.IsAny<HttpRequest>()), Times.Never());
        }

        [Test]
        public void should_refuse_a_redirect_to_a_local_address()
        {
            GivenRedirect("http://93.184.215.14/cover.jpg", "http://127.0.0.1:8787/api/v1/config");

            Assert.Throws<NzbDroneClientException>(() => Subject.GetImage(Register("http://93.184.215.14/cover.jpg")));

            Mocker.GetMock<IHttpClient>().Verify(v => v.Get(It.Is<HttpRequest>(r => r.Url.Host == "127.0.0.1")), Times.Never());
        }

        [Test]
        public void should_follow_a_redirect_to_a_public_address()
        {
            GivenRedirect("http://93.184.215.14/cover.jpg", "http://93.184.215.15/cover.jpg");

            Subject.GetImage(Register("http://93.184.215.14/cover.jpg")).Should().Equal(Image);
        }
    }
}
