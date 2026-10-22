using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Datastore.Migration.Framework
{
    public class UnknownMigrationException : ReadarrStartupException
    {
        public UnknownMigrationException(string message)
            : base(message)
        {
        }
    }
}
