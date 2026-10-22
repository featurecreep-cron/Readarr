using Dapper;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Instrumentation;
using NzbDrone.Core.Tags;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore
{
    [TestFixture]
    public class SchemaVerifierFixture : DbTest<SchemaVerifier, Tag>
    {
        private void Execute(string sql)
        {
            using var connection = Mocker.Resolve<IMainDatabase>().OpenConnection();
            connection.Execute(sql);
        }

        [Test]
        public void should_find_no_problems_in_a_migrated_database()
        {
            Subject.FindProblems(MigrationType.Main).Should().BeEmpty();
        }

        [Test]
        public void should_report_a_missing_column()
        {
            Execute("ALTER TABLE \"Config\" DROP COLUMN \"Value\"");

            Subject.FindProblems(MigrationType.Main).Should().Equal("column Config.Value is missing");
        }

        [Test]
        public void should_report_a_missing_table()
        {
            Execute("DROP TABLE \"Tags\"");

            Subject.FindProblems(MigrationType.Main).Should().Equal("table Tags is missing");
        }
    }

    [TestFixture]
    public class SchemaVerifierLogFixture : DbTest<SchemaVerifier, Log>
    {
        protected override MigrationType MigrationType => MigrationType.Log;

        [Test]
        public void should_find_no_problems_in_a_migrated_log_database()
        {
            Subject.FindProblems(MigrationType.Log).Should().BeEmpty();
        }
    }
}
