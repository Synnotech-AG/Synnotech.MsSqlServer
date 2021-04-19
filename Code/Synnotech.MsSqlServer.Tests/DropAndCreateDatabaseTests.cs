using System.Threading.Tasks;
using Synnotech.Xunit;
using Xunit;

namespace Synnotech.MsSqlServer.Tests
{
    [TestCaseOrderer(TestOrderer.TypeName, TestOrderer.AssemblyName)]
    public static class DropAndCreateDatabaseTests
    {
        [SkippableFact]
        public static async Task DropAndRecreateExistingDatabase()
        {
            var connectionString = TestSettings.TryGetConnectionString();
            await Database.TryCreateDatabaseAsync(connectionString);

            await Database.DropAndCreateDatabaseAsync(connectionString);
        }

        [SkippableFact]
        public static async Task CreateNonExistentDatabase()
        {
            var connectionString = TestSettings.TryGetConnectionString();
            await Database.TryDropDatabaseAsync(connectionString);

            await Database.DropAndCreateDatabaseAsync(connectionString);
        }
    }
}