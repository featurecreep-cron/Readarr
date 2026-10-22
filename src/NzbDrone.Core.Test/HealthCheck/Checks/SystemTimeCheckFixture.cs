using System;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Cloud;
using NzbDrone.Common.Http;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class SystemTimeCheckFixture : CoreTest<SystemTimeCheck>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.SetConstant<IReadarrCloudRequestBuilder>(new ReadarrCloudRequestBuilder());
        }

        private void GivenServerTime(DateTime? dateTime)
        {
            var headers = new HttpHeader();

            if (dateTime.HasValue)
            {
                headers.Add("Date", dateTime.Value.ToString("R"));
            }

            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>()))
                  .Returns("System time is off by more than 1 day. Scheduled tasks may not run correctly until the time is corrected");

            Mocker.GetMock<IHttpClient>()
                  .Setup(s => s.Execute(It.IsAny<HttpRequest>()))
                  .Returns<HttpRequest>(r => new HttpResponse(r, headers, Array.Empty<byte>()));
        }

        [Test]
        public void should_not_return_error_when_system_time_is_close_to_server_time()
        {
            GivenServerTime(DateTime.UtcNow);

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_error_when_system_time_is_more_than_one_day_from_server_time()
        {
            GivenServerTime(DateTime.UtcNow.AddDays(2));

            Subject.Check().ShouldBeError();
            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_not_return_error_when_server_sends_no_date()
        {
            GivenServerTime(null);

            Subject.Check().ShouldBeOk();
        }
    }
}
