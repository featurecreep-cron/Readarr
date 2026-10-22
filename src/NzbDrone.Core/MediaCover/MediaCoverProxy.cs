using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Exceptions;

namespace NzbDrone.Core.MediaCover
{
    public interface IMediaCoverProxy
    {
        string RegisterUrl(string url);

        string GetUrl(string hash);
        byte[] GetImage(string hash);
    }

    public class MediaCoverProxy : IMediaCoverProxy
    {
        private const int MaxRedirects = 5;

        private readonly IHttpClient _httpClient;
        private readonly IConfigFileProvider _configFileProvider;
        private readonly IConfigService _configService;
        private readonly ICached<string> _cache;

        public MediaCoverProxy(IHttpClient httpClient, IConfigFileProvider configFileProvider, IConfigService configService, ICacheManager cacheManager)
        {
            _httpClient = httpClient;
            _configFileProvider = configFileProvider;
            _configService = configService;
            _cache = cacheManager.GetCache<string>(GetType());
        }

        public string RegisterUrl(string url)
        {
            if (url.IsNullOrWhiteSpace())
            {
                return null;
            }

            var hash = url.SHA256Hash();

            _cache.Set(hash, url, TimeSpan.FromHours(24));

            _cache.ClearExpired();

            var fileName = Path.GetFileName(url);
            return _configFileProvider.UrlBase + @"/MediaCoverProxy/" + hash + "/" + fileName;
        }

        public string GetUrl(string hash)
        {
            var result = _cache.Find(hash);

            if (result == null)
            {
                throw new KeyNotFoundException("Url no longer in cache");
            }

            return result;
        }

        public byte[] GetImage(string hash)
        {
            var url = new HttpUri(GetUrl(hash));

            // Redirects are followed here rather than by the client so every hop is checked.
            for (var i = 0; i <= MaxRedirects; i++)
            {
                EnsureAllowed(url);

                var request = new HttpRequest(url.FullUri) { AllowAutoRedirect = false };
                var response = _httpClient.Get(request);

                if (!response.HasHttpRedirect)
                {
                    return response.ResponseData;
                }

                url += new HttpUri(response.Headers.GetSingleValue("Location"));
            }

            throw new NzbDroneClientException(HttpStatusCode.BadGateway, "Too many redirects fetching cover");
        }

        // Cover urls come from metadata, so they mustn't be a way to reach the user's own network.
        // The configured metadata server is the exception: a self-hosted one may serve its covers locally.
        private void EnsureAllowed(HttpUri url)
        {
            if (url.Scheme != "http" && url.Scheme != "https")
            {
                throw new NzbDroneClientException(HttpStatusCode.Forbidden, "Cover url must be http or https");
            }

            var metadataSource = _configService.MetadataSource;

            if (metadataSource.IsNotNullOrWhiteSpace() && new HttpUri(metadataSource).Host.Equals(url.Host, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var addresses = IPAddress.TryParse(url.Host.Trim('[', ']'), out var address) ? new[] { address } : Dns.GetHostAddresses(url.Host);

            if (addresses.Any(a => a.IsLocalAddress() || a.Equals(IPAddress.Any) || a.Equals(IPAddress.IPv6Any)))
            {
                throw new NzbDroneClientException(HttpStatusCode.Forbidden, "Cover url points to a local address");
            }
        }
    }
}
