using System;
using System.Runtime.InteropServices;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Update;

namespace NzbDrone.Core.Test.UpdateTests
{
    public class UpdatePackageProviderFixture : CoreTest<UpdatePackageProvider>
    {
        private string _runtime;

        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<IReadarrCloudRequestBuilder>(new ReadarrCloudRequestBuilder());

            var os = OsInfo.Os switch
            {
                Os.Windows => "win",
                Os.Osx => "osx",
                Os.LinuxMusl => "linux-musl",
                Os.Bsd => "freebsd",
                _ => "linux"
            };

            var arch = RuntimeInformation.OSArchitecture switch
            {
                Architecture.Arm64 => "arm64",
                Architecture.Arm => "arm",
                Architecture.X86 => "x86",
                _ => "x64"
            };

            _runtime = $"{os}-{arch}";
        }

        private void GivenReleases(string json)
        {
            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Get(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => new HttpResponse(r, new HttpHeader(), json));
        }

        private string Release(string tag, string runtime, string digest = "sha256:abc123", bool prerelease = false)
        {
            return $@"{{
                ""tag_name"": ""{tag}"",
                ""draft"": false,
                ""prerelease"": {prerelease.ToString().ToLowerInvariant()},
                ""published_at"": ""2026-10-20T10:00:00Z"",
                ""body"": ""## New\n\n- Audiobooks get their own profiles (1234abc)\n\n## Fixed\n\n- Import no longer crashes (5678def)\n\n## Dependencies\n\n- MailKit 4.18.1 -> 4.19.0 (9abcdef)\n"",
                ""assets"": [
                    {{
                        ""name"": ""Readarr.main.{tag.TrimStart('v')}.{runtime}.tar.gz"",
                        ""digest"": {(digest == null ? "null" : $@"""{digest}""")},
                        ""browser_download_url"": ""https://example.com/{tag}/{runtime}.tar.gz""
                    }}
                ]
            }}";
        }

        [Test]
        public void should_offer_the_newest_release_for_this_runtime()
        {
            GivenReleases($"[{Release("v0.6.0", _runtime)}, {Release("v0.5.1", _runtime)}]");

            var update = Subject.GetLatestUpdate("develop", new Version(0, 5, 0, 12));

            update.Should().NotBeNull();
            update.Version.Should().Be(new Version(0, 6, 0));
            update.Url.Should().Be($"https://example.com/v0.6.0/{_runtime}.tar.gz");
            update.Hash.Should().Be("abc123");
            update.Branch.Should().Be("develop");
        }

        [Test]
        public void should_not_offer_the_release_that_is_running()
        {
            GivenReleases($"[{Release("v0.5.0", _runtime)}]");

            Subject.GetLatestUpdate("develop", new Version(0, 5, 0, 12)).Should().BeNull();
        }

        [Test]
        public void should_not_offer_a_release_without_an_asset_for_this_runtime()
        {
            GivenReleases($"[{Release("v0.6.0", "other-runtime")}]");

            Subject.GetLatestUpdate("develop", new Version(0, 5, 0)).Should().BeNull();
        }

        [Test]
        public void should_not_offer_a_release_whose_asset_has_no_digest()
        {
            GivenReleases($"[{Release("v0.6.0", _runtime, digest: null)}]");

            Subject.GetLatestUpdate("develop", new Version(0, 5, 0)).Should().BeNull();
        }

        [Test]
        public void should_not_offer_a_prerelease()
        {
            GivenReleases($"[{Release("v0.6.0", _runtime, prerelease: true)}]");

            Subject.GetLatestUpdate("develop", new Version(0, 5, 0)).Should().BeNull();
        }

        [Test]
        public void should_read_new_and_fixed_changes_from_the_release_notes()
        {
            GivenReleases($"[{Release("v0.6.0", _runtime)}]");

            var update = Subject.GetLatestUpdate("develop", new Version(0, 5, 0));

            update.Changes.New.Should().Equal("Audiobooks get their own profiles");
            update.Changes.Fixed.Should().Equal("Import no longer crashes");
        }

        [Test]
        public void should_list_recent_updates_up_to_the_running_version()
        {
            GivenReleases($"[{Release("v0.6.0", _runtime)}, {Release("v0.5.1", _runtime)}, {Release("v0.5.0", _runtime)}]");

            var recent = Subject.GetRecentUpdates("develop", new Version(0, 5, 1, 30), null);

            recent.Should().HaveCount(2);
            recent[0].Version.Should().Be(new Version(0, 5, 1));
            recent[1].Version.Should().Be(new Version(0, 5, 0));
        }
    }
}
