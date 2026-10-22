using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore
{
    public interface IVerifySchema
    {
        List<string> FindProblems();
        List<string> FindProblems(MigrationType migrationType);
    }

    // Checks that every table and column the repositories read and write exists in the live database.
    // A database migrated by another fork, or edited by hand, can be missing them without the migration
    // history showing it, and then fails one query at a time.
    public class SchemaVerifier : IVerifySchema
    {
        // Which database a table lives in, taken from the database its repository is given.
        private static readonly Lazy<Dictionary<Type, MigrationType>> DatabaseByModel = new(FindDatabaseByModel);

        private readonly IMainDatabase _mainDatabase;
        private readonly ILogDatabase _logDatabase;
        private readonly ICacheDatabase _cacheDatabase;

        public SchemaVerifier(IMainDatabase mainDatabase, ILogDatabase logDatabase, ICacheDatabase cacheDatabase)
        {
            _mainDatabase = mainDatabase;
            _logDatabase = logDatabase;
            _cacheDatabase = cacheDatabase;
        }

        public List<string> FindProblems()
        {
            return Enum.GetValues<MigrationType>().SelectMany(FindProblems).ToList();
        }

        public List<string> FindProblems(MigrationType migrationType)
        {
            var problems = new List<string>();
            IDatabase database = migrationType switch
            {
                MigrationType.Log => _logDatabase,
                MigrationType.Cache => _cacheDatabase,
                _ => _mainDatabase
            };

            foreach (var (type, table) in TableMapping.Mapper.TableMap.Where(t => DatabaseFor(t.Key) == migrationType).OrderBy(t => t.Value))
            {
                var liveColumns = GetColumns(database, table);

                if (liveColumns.Count == 0)
                {
                    problems.Add($"table {table} is missing");
                    continue;
                }

                problems.AddRange(GetMappedColumns(type)
                    .Where(column => !liveColumns.Contains(column))
                    .Select(column => $"column {table}.{column} is missing"));
            }

            return problems;
        }

        private static MigrationType DatabaseFor(Type model)
        {
            return DatabaseByModel.Value.TryGetValue(model, out var migrationType) ? migrationType : MigrationType.Main;
        }

        private static Dictionary<Type, MigrationType> FindDatabaseByModel()
        {
            var result = new Dictionary<Type, MigrationType>();

            foreach (var repository in typeof(SchemaVerifier).Assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
            {
                var model = GetRepositoryModel(repository);

                if (model == null)
                {
                    continue;
                }

                var parameters = repository.GetConstructors().SelectMany(c => c.GetParameters()).Select(p => p.ParameterType).ToList();

                if (parameters.Contains(typeof(ILogDatabase)))
                {
                    result[model] = MigrationType.Log;
                }
                else if (parameters.Contains(typeof(ICacheDatabase)))
                {
                    result[model] = MigrationType.Cache;
                }
            }

            return result;
        }

        private static Type GetRepositoryModel(Type type)
        {
            for (var current = type.BaseType; current != null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(BasicRepository<>))
                {
                    return current.GetGenericArguments()[0];
                }
            }

            return null;
        }

        private static IEnumerable<string> GetMappedColumns(Type type)
        {
            var excluded = TableMapping.Mapper.ExcludeProperties(type).Select(x => x.Name).ToHashSet();

            return type.GetProperties()
                       .Where(x => x.IsMappableProperty() && !excluded.Contains(x.Name))
                       .Select(x => x.Name);
        }

        private static HashSet<string> GetColumns(IDatabase database, string table)
        {
            var sql = database.DatabaseType == DatabaseType.PostgreSQL
                ? "SELECT column_name FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = @table"
                : "SELECT name FROM pragma_table_info(@table)";

            using var connection = database.OpenConnection();

            return connection.Query<string>(sql, new { table }).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
