using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Synnotech.MsSqlServer.Tests;

public static class DropAndCreateDatabaseTests
{
    [SkippableFact]
    public static async Task DropAndRecreateExistingDatabase()
    {
        var connectionString = TestSettings.GetConnectionStringOrSkip();
        await Database.TryCreateDatabaseAsync(connectionString);

        var result = await Database.DropAndCreateDatabaseAsync(connectionString);

        result.Should().BeTrue();
    }

    [SkippableFact]
    public static async Task CreateNonExistentDatabase()
    {
        var connectionString = TestSettings.GetConnectionStringOrSkip();
        await Database.TryDropDatabaseAsync(connectionString);

        var result = await Database.DropAndCreateDatabaseAsync(connectionString);

        result.Should().BeFalse();
    }
}