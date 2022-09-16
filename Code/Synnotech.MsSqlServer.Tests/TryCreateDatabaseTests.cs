using System.Threading.Tasks;
using FluentAssertions;
using Synnotech.Xunit;
using Xunit;

namespace Synnotech.MsSqlServer.Tests;

public static class TryCreateDatabaseTests
{
    [SkippableFact]
    public static async Task CreateDatabaseThatDoesNotExist()
    {
        var connectionString = TestSettings.GetConnectionStringOrSkip();
        await Database.TryDropDatabaseAsync(connectionString);

        var result = await Database.TryCreateDatabaseAsync(connectionString);

        result.Should().BeTrue();
    }

    [SkippableFact]
    public static async Task DoNotCreateWhenDatabaseAlreadyExists()
    {
        var connectionString = TestSettings.GetConnectionStringOrSkip();
        await Database.TryCreateDatabaseAsync(connectionString);

        var result = await Database.TryCreateDatabaseAsync(connectionString);

        result.Should().BeFalse();
    }
}