using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Lifecycle;
using NzbDrone.Core.Localization;

namespace NzbDrone.Core.HealthCheck.Checks
{
    [CheckOn(typeof(ApplicationStartedEvent))]
    public class DatabaseSchemaCheck : HealthCheckBase
    {
        private const int MaxProblemsShown = 5;

        private readonly IVerifySchema _schemaVerifier;
        private readonly Logger _logger;

        public DatabaseSchemaCheck(IVerifySchema schemaVerifier, ILocalizationService localizationService, Logger logger)
            : base(localizationService)
        {
            _schemaVerifier = schemaVerifier;
            _logger = logger;
        }

        public override bool CheckOnSchedule => false;

        public override HealthCheck Check()
        {
            var problems = _schemaVerifier.FindProblems();

            if (problems.Count == 0)
            {
                return new HealthCheck(GetType());
            }

            _logger.Error("The database doesn't match what this version expects: {0}", string.Join("; ", problems));

            var shown = string.Join("; ", problems.Take(MaxProblemsShown));

            if (problems.Count > MaxProblemsShown)
            {
                shown += $"; and {problems.Count - MaxProblemsShown} more";
            }

            return new HealthCheck(GetType(),
                HealthCheckResult.Error,
                _localizationService.GetLocalizedString("DatabaseSchemaCheckMessage", new Dictionary<string, object> { { "problems", shown } }),
                "#database-schema-does-not-match");
        }
    }
}
