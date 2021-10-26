using System.Threading.Tasks;
using Synnotech.MsSqlServer.Tests.SqlScripts;
using Xunit;

namespace Synnotech.MsSqlServer.Tests
{
    public static class ExecuteNonQueryAsyncTests
    {
        [SkippableFact]
        public static async Task ExecuteNonQuery()
        {
            var connectionString = TestSettings.GetConnectionStringOrSkip();
            await Database.DropAndCreateDatabaseAsync(connectionString);
            await Database.ExecuteNonQueryAsync(connectionString, Scripts.SimpleDatabaseScript);
        }
    }
}