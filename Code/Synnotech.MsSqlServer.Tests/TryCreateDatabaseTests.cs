using System.Threading.Tasks;
using Synnotech.Xunit;
using Xunit;

namespace Synnotech.MsSqlServer.Tests
{
    [TestCaseOrderer(TestOrderer.TypeName, TestOrderer.AssemblyName)]
    public static class TryCreateDatabaseTests
    {
        [SkippableFact]
        public static async Task CreateDatabaseThatDoesNotExist()
        {
            var connectionString = TestSettings.TryGetConnectionString();
            await Database.TryDropDatabaseAsync(connectionString);

            await Database.TryCreateDatabaseAsync(connectionString);
        }

        [SkippableFact]
        public static async Task DoNotCreateWhenDatabaseAlreadyExists()
        {
            var connectionString = TestSettings.TryGetConnectionString();
            await Database.TryCreateDatabaseAsync(connectionString);

            await Database.TryCreateDatabaseAsync(connectionString);
        }
    }
}