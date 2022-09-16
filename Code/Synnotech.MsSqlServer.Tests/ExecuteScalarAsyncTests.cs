using System.Threading.Tasks;
using FluentAssertions;
using Synnotech.MsSqlServer.Tests.SqlScripts;
using Xunit;

namespace Synnotech.MsSqlServer.Tests;

public static class ExecuteScalarAsyncTests
{
    [SkippableFact]
    public static async Task ExecuteScalar()
    {
        var connectionString = TestSettings.GetConnectionStringOrSkip();
        await Database.DropAndCreateDatabaseAsync(connectionString);
        await Database.ExecuteNonQueryAsync(connectionString, Scripts.SimpleDatabase);

        var count = await Database.ExecuteScalarAsync<int>(connectionString, Scripts.GetPersonCount);

        count.Should().Be(3);
    }
}