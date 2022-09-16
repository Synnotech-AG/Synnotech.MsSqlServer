using System.Threading.Tasks;
using FluentAssertions;
using Synnotech.Xunit;
using Xunit;

namespace Synnotech.MsSqlServer.Tests;

public static class TryDropDatabaseTests
{
    [SkippableFact]
    public static async Task DropExistingDatabase()
    {
        var connectionString = TestSettings.GetConnectionStringOrSkip();
        await Database.TryCreateDatabaseAsync(connectionString);

        var result = await Database.TryDropDatabaseAsync(connectionString);

        result.Should().BeTrue();
    }

    [SkippableFact]
    public static async Task NonExistentDatabase()
    {
        var connectionString = TestSettings.GetConnectionStringOrSkip();
        await Database.TryDropDatabaseAsync(connectionString);

        var result = await Database.TryDropDatabaseAsync(connectionString);

        result.Should().BeFalse();
    }
}