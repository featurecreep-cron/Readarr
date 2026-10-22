using System;
using System.Net.Http;
using NLog;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.HealthCheck.Checks
{
    public class SystemTimeCheck : HealthCheckBase
    {
        private readonly IHttpClient _client;
        private readonly IHttpRequestBuilderFactory _cloudRequestBuilder;
        private readonly Logger _logger;

        public SystemTimeCheck(IHttpClient client, IReadarrCloudRequestBuilder cloudRequestBuilder, ILocalizationService localizationService, Logger logger)
            : base(localizationService)
        {
            _client = client;
            _cloudRequestBuilder = cloudRequestBuilder.Releases;
            _logger = logger;
        }

        public override HealthCheck Check()
        {
            var request = _cloudRequestBuilder.Create().Build();
            request.Method = HttpMethod.Head;

            var response = _client.Execute(request);
            var date = response.Headers.GetSingleValue("Date");

            if (date == null)
            {
                return new HealthCheck(GetType());
            }

            var serverTime = HttpHeader.ParseDateTime(date);
            var systemTime = DateTime.UtcNow;

            // +/- more than 1 day
            if (Math.Abs(serverTime.ToUniversalTime().Subtract(systemTime).TotalDays) >= 1)
            {
                _logger.Error("System time mismatch. SystemTime: {0} Expected Time: {1}. Update system time", systemTime, serverTime.ToUniversalTime());
                return new HealthCheck(GetType(), HealthCheckResult.Error, _localizationService.GetLocalizedString("SystemTimeCheckMessage"), "#system-time-off");
            }

            return new HealthCheck(GetType());
        }
    }
}
