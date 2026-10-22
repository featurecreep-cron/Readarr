using System.Data.SQLite;
using System.IO;
using Dapper;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class ForkDatabaseFixture : CoreTest<MigrationController>
    {
        private string GivenDatabase(string fixture)
        {
            var path = Path.Combine(TempFolder, fixture + ".db");
            var connectionString = new SQLiteConnectionStringBuilder { DataSource = path }.ConnectionString;

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                connection.Execute(File.ReadAllText(GetTestPath(Path.Combine("Files", "ForkDatabases", fixture + ".sql"))));
            }

            return connectionString;
        }

        private void Migrate(string connectionString)
        {
            Subject.Migrate(connectionString, new MigrationContext(MigrationType.Main), DatabaseType.SQLite);
        }

        [Test]
        public void should_migrate_a_readarr_0_4_19_database()
        {
            var connectionString = GivenDatabase("readarr-0.4.19");

            Migrate(connectionString);

            var database = new MainDatabase(new Database("readarr", () =>
            {
                var connection = new SQLiteConnection(connectionString);
                connection.Open();
                return connection;
            }));

            new SchemaVerifier(database, null, null).FindProblems(MigrationType.Main).Should().BeEmpty();
        }

        [TestCase("faustvii", "41 EditionBookIdMonitoredIndex")]
        [TestCase("ricetim", "41 add_kca_to_author_metadata")]
        [TestCase("bookshelf-softcover", "41 bookfile_last_remote_search")]
        [TestCase("bookshelf-hardcover", "41 bookfile_last_remote_search")]
        public void should_refuse_a_database_migrated_by_a_fork(string fixture, string migration)
        {
            var connectionString = GivenDatabase(fixture);

            Assert.Throws<UnknownMigrationException>(() => Migrate(connectionString))
                  .Message.Should().Contain(migration);
        }
    }
}
