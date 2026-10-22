using System;
using Dapper;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Datastore.Migration.Framework;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Migration
{
    [TestFixture]
    public class UnknownMigrationFixture : DbTest
    {
        private void GivenAppliedMigration(long version, string description)
        {
            using var connection = Db.OpenConnection();
            connection.Execute("INSERT INTO \"VersionInfo\" (\"Version\", \"AppliedOn\", \"Description\") VALUES (@version, @appliedOn, @description)",
                new { version, appliedOn = DateTime.UtcNow, description });
        }

        private void Migrate()
        {
            var connection = Mocker.Resolve<IConnectionStringFactory>().MainDbConnection;

            Mocker.Resolve<MigrationController>().Migrate(connection.ConnectionString, new MigrationContext(MigrationType.Main), connection.DatabaseType);
        }

        [Test]
        public void should_migrate_a_database_that_only_has_our_migrations()
        {
            Assert.DoesNotThrow(Migrate);
        }

        [Test]
        public void should_refuse_a_database_whose_migration_has_our_number_but_another_name()
        {
            using (var connection = Db.OpenConnection())
            {
                connection.Execute("UPDATE \"VersionInfo\" SET \"Description\" = 'last_remote_search_time' WHERE \"Version\" = 41");
            }

            Assert.Throws<UnknownMigrationException>(Migrate)
                .Message.Should().Contain("41 last_remote_search_time");
        }

        [Test]
        public void should_refuse_a_database_with_a_migration_from_a_newer_version()
        {
            GivenAppliedMigration(999, "future_migration");

            Assert.Throws<UnknownMigrationException>(Migrate);
        }
    }
}
