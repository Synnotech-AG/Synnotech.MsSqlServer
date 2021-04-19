using System.Threading.Tasks;
using FluentAssertions;
using Synnotech.Xunit;
using Xunit;

namespace Synnotech.MsSqlServer.Tests
{
    [TestCaseOrderer(TestOrderer.TypeName, TestOrderer.AssemblyName)]
    public static class TryDropDatabaseTests
    {
        [SkippableFact]
        public static async Task DropExistingDatabase()
        {
            var connectionString = TestSettings.TryGetConnectionString();
            await Database.TryCreateDatabaseAsync(connectionString);

            await Database.TryDropDatabaseAsync(connectionString);
        }

        [SkippableFact]
        public static async Task NonExistentDatabase()
        {
            var connectionString = TestSettings.TryGetConnectionString();
            await Database.TryDropDatabaseAsync(connectionString);

            await Database.TryDropDatabaseAsync(connectionString);
        }
    }
}