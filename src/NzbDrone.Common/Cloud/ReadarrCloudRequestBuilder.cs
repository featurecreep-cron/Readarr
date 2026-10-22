using NzbDrone.Common.Http;

namespace NzbDrone.Common.Cloud
{
    public interface IReadarrCloudRequestBuilder
    {
        IHttpRequestBuilderFactory Releases { get; }
        IHttpRequestBuilderFactory Metadata { get; }
    }

    public class ReadarrCloudRequestBuilder : IReadarrCloudRequestBuilder
    {
        public ReadarrCloudRequestBuilder()
        {
            Releases = new HttpRequestBuilder("https://api.github.com/repos/featurecreep-cron/Readarr/")
                .Accept(HttpAccept.Json)
                .CreateFactory();

            Metadata = new HttpRequestBuilder("https://api.bookinfo.club/v1/{route}")
                .CreateFactory();
        }

        public IHttpRequestBuilderFactory Releases { get; }

        public IHttpRequestBuilderFactory Metadata { get; }
    }
}
