using NUnit.Framework;

namespace NzbDrone.Test.Common.Categories
{
    // Needs the metadata server Readarr used until 2025, which is gone. CI excludes these until the tests
    // run against a metadata service again.
    public class MetadataServerTestAttribute : CategoryAttribute
    {
        public MetadataServerTestAttribute()
            : base("MetadataServer")
        {
        }
    }
}
