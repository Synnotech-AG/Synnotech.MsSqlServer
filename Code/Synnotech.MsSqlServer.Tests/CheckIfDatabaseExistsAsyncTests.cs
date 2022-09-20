using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Synnotech.MsSqlServer.Tests;

public static class CheckIfDatabaseExistsAsyncTests
{
    [SkippableFact]
    public static async Task DatabaseDoesNotExist()
    {
        var connectionString = TestSettings.GetConnectionStringOrSkip();
        await Database.TryDropDatabaseAsync(connectionString);

        var result = await Database.CheckIfDatabaseExistsAsync(connectionString);

        result.Should().BeFalse();
    }

    [SkippableFact]
    public static async Task DatabaseExists()
    {
        var connectionString = TestSettings.GetConnectionStringOrSkip();
        await Database.DropAndCreateDatabaseAsync(connectionString);

        var result = await Database.CheckIfDatabaseExistsAsync(connectionString);

        result.Should().BeTrue();
    }
}