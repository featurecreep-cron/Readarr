using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class DatabaseSchemaCheckFixture : CoreTest<DatabaseSchemaCheck>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>()))
                  .Returns<string, Dictionary<string, object>>((key, args) => $"{key}: {args["problems"]}");
        }

        private void GivenProblems(params string[] problems)
        {
            Mocker.GetMock<IVerifySchema>()
                  .Setup(s => s.FindProblems())
                  .Returns(problems.ToList());
        }

        [Test]
        public void should_be_ok_when_the_schema_matches()
        {
            GivenProblems();

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_be_an_error_when_the_schema_does_not_match()
        {
            GivenProblems("column Tags.Label is missing");

            Subject.Check().ShouldBeError("DatabaseSchemaCheckMessage: column Tags.Label is missing");

            ExceptionVerification.ExpectedErrors(1);
        }

        [Test]
        public void should_only_show_the_first_few_problems()
        {
            GivenProblems(Enumerable.Range(1, 8).Select(i => $"table T{i} is missing").ToArray());

            Subject.Check().ShouldBeError("DatabaseSchemaCheckMessage: table T1 is missing; table T2 is missing; table T3 is missing; table T4 is missing; table T5 is missing; and 3 more");

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
