using System.Threading.Tasks;
using Light.EmbeddedResources;
using Xunit;

namespace Synnotech.MsSqlServer.Tests
{
    public static class ExecuteNonQueryAsyncTests
    {
        [SkippableFact]
        public static async Task ExecuteNonQuery()
        {
            var connectionString = TestSettings.TryGetConnectionString();
            await Database.DropAndCreateDatabaseAsync(connectionString);
            await Database.ExecuteNonQueryAsync(connectionString, typeof(ExecuteNonQueryAsyncTests).GetEmbeddedResource("SimpleDatabase.sql"));
        }
    }
}