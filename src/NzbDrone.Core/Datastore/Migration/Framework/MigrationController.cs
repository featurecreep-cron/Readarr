using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Generators;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using NLog.Extensions.Logging;

namespace NzbDrone.Core.Datastore.Migration.Framework
{
    public interface IMigrationController
    {
        void Migrate(string connectionString, MigrationContext migrationContext, DatabaseType databaseType);
    }

    public class MigrationController : IMigrationController
    {
        // Upstream Readarr's last migration. Readarr forks each numbered their first migration after it 41,
        // for different changes, so from here on a migration is only ours if its name matches too.
        private const long LastUpstreamMigration = 40;

        private readonly Logger _logger;

        public MigrationController(Logger logger)
        {
            _logger = logger;
        }

        public void Migrate(string connectionString, MigrationContext migrationContext, DatabaseType databaseType)
        {
            var sw = Stopwatch.StartNew();

            _logger.Info("*** Migrating {0} ***", connectionString);

            var db = databaseType switch
            {
                DatabaseType.SQLite => ProcessorIdConstants.SQLite,
                DatabaseType.PostgreSQL => ProcessorIdConstants.PostgreSQL,
                _ => throw new NotImplementedException($"Unknown database type: {databaseType}")
            };

            var serviceProvider = new ServiceCollection()
                .AddLogging(b => b.AddNLog())
                .AddFluentMigratorCore()
                .Configure<RunnerOptions>(cfg => cfg.IncludeUntaggedMaintenances = true)
                .ConfigureRunner(builder => builder
                    .AddPostgres15_0()
                    .AddNzbDroneSQLite()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(Assembly.GetExecutingAssembly()).For.All())
                .Configure<TypeFilterOptions>(opt => opt.Namespace = "NzbDrone.Core.Datastore.Migration")
                .Configure<ProcessorOptions>(opt =>
                {
                    opt.PreviewOnly = false;
                    opt.Timeout = TimeSpan.FromMinutes(5);
                })
                .Configure<SelectingProcessorAccessorOptions>(cfg =>
                {
                    cfg.ProcessorId = db;
                })
                .Configure<SelectingGeneratorAccessorOptions>(cfg =>
                {
                    cfg.GeneratorId = db;
                })
                .BuildServiceProvider();

            using (var scope = serviceProvider.CreateScope())
            {
                var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();

                VerifyAppliedMigrations(runner, scope.ServiceProvider.GetRequiredService<IProcessorAccessor>().Processor);

                MigrationContext.Current = migrationContext;

                if (migrationContext.DesiredVersion.HasValue)
                {
                    runner.MigrateUp(migrationContext.DesiredVersion.Value);
                }
                else
                {
                    runner.MigrateUp();
                }

                MigrationContext.Current = null;
            }

            sw.Stop();

            _logger.Debug("Took: {0}", sw.Elapsed);
        }

        // A database another Readarr fork or a newer Readarr has migrated has a schema this version doesn't know.
        // Migrating it would run our migrations over theirs, so refuse instead.
        private static void VerifyAppliedMigrations(IMigrationRunner runner, IMigrationProcessor processor)
        {
            if (!processor.TableExists(null, "VersionInfo"))
            {
                return;
            }

            var known = runner.MigrationLoader.LoadMigrations();
            var unknown = new List<string>();

            foreach (DataRow row in processor.Read("SELECT \"Version\", \"Description\" FROM \"VersionInfo\"").Tables[0].Rows)
            {
                var version = Convert.ToInt64(row["Version"]);
                var description = row["Description"] as string;

                if (!known.TryGetValue(version, out var migration) ||
                    (version > LastUpstreamMigration && description != null && description != (migration.Description ?? migration.Migration.GetType().Name)))
                {
                    unknown.Add($"{version} {description}");
                }
            }

            if (unknown.Any())
            {
                throw new UnknownMigrationException(
                    "The database has migrations this version of Readarr doesn't have: " + string.Join(", ", unknown) +
                    ". It was last used by another Readarr fork or by a newer Readarr. Restore a backup made by this version, or use the version that made it.");
            }
        }
    }
}
