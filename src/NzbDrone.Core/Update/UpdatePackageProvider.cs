using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;

namespace NzbDrone.Core.Update
{
    public interface IUpdatePackageProvider
    {
        UpdatePackage GetLatestUpdate(string branch, Version currentVersion);
        List<UpdatePackage> GetRecentUpdates(string branch, Version currentVersion, Version previousVersion = null);
    }

    public class UpdatePackageProvider : IUpdatePackageProvider
    {
        private static readonly Regex TagRegex = new(@"^v(?<version>\d+\.\d+\.\d+)$", RegexOptions.Compiled);
        private static readonly Regex SectionRegex = new(@"^##\s+(?<title>.+)$", RegexOptions.Compiled);
        private static readonly Regex ChangeRegex = new(@"^-\s+(?<text>.+?)(?:\s+\([0-9a-f]{7,40}\))?$", RegexOptions.Compiled);

        private readonly IHttpClient _httpClient;
        private readonly IHttpRequestBuilderFactory _requestBuilder;

        public UpdatePackageProvider(IHttpClient httpClient, IReadarrCloudRequestBuilder requestBuilder)
        {
            _httpClient = httpClient;
            _requestBuilder = requestBuilder.Releases;
        }

        public UpdatePackage GetLatestUpdate(string branch, Version currentVersion)
        {
            return GetPackages(branch)
                .Where(p => p.Version > currentVersion)
                .MaxBy(p => p.Version);
        }

        public List<UpdatePackage> GetRecentUpdates(string branch, Version currentVersion, Version previousVersion)
        {
            return GetPackages(branch)
                .Where(p => p.Version <= currentVersion)
                .OrderByDescending(p => p.Version)
                .ToList();
        }

        private IEnumerable<UpdatePackage> GetPackages(string branch)
        {
            var request = _requestBuilder.Create()
                                         .Resource("releases")
                                         .AddQueryParam("per_page", 30)
                                         .Build();

            var releases = Json.Deserialize<List<GitHubRelease>>(_httpClient.Get(request).Content);
            var runtime = GetRuntimeIdentifier();

            foreach (var release in releases.Where(r => !r.Draft && !r.Prerelease))
            {
                var tag = TagRegex.Match(release.TagName ?? string.Empty);
                var asset = release.Assets?.FirstOrDefault(a => a.Name.EndsWith($".{runtime}.tar.gz") || a.Name.EndsWith($".{runtime}.zip"));

                // An asset without a digest can't be verified after download, so it isn't offered.
                if (!tag.Success || asset?.Digest == null || !asset.Digest.StartsWith("sha256:"))
                {
                    continue;
                }

                yield return new UpdatePackage
                {
                    Version = new Version(tag.Groups["version"].Value),
                    ReleaseDate = release.PublishedAt,
                    FileName = asset.Name,
                    Url = asset.BrowserDownloadUrl,
                    Hash = asset.Digest.Substring("sha256:".Length),
                    Branch = branch,
                    Changes = ParseChanges(release.Body)
                };
            }
        }

        private static UpdateChanges ParseChanges(string body)
        {
            var changes = new UpdateChanges();
            List<string> section = null;

            foreach (var line in (body ?? string.Empty).Split('\n').Select(l => l.Trim()))
            {
                var heading = SectionRegex.Match(line);

                if (heading.Success)
                {
                    section = heading.Groups["title"].Value switch
                    {
                        "New" => changes.New,
                        "Fixed" => changes.Fixed,
                        _ => null
                    };

                    continue;
                }

                var change = ChangeRegex.Match(line);

                if (section != null && change.Success)
                {
                    section.Add(change.Groups["text"].Value);
                }
            }

            return changes;
        }

        private static string GetRuntimeIdentifier()
        {
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

            return $"{os}-{arch}";
        }

        private class GitHubRelease
        {
            [JsonProperty("tag_name")]
            public string TagName { get; set; }

            public string Body { get; set; }
            public bool Draft { get; set; }
            public bool Prerelease { get; set; }

            [JsonProperty("published_at")]
            public DateTime PublishedAt { get; set; }

            public List<GitHubAsset> Assets { get; set; }
        }

        private class GitHubAsset
        {
            public string Name { get; set; }
            public string Digest { get; set; }

            [JsonProperty("browser_download_url")]
            public string BrowserDownloadUrl { get; set; }
        }
    }
}
