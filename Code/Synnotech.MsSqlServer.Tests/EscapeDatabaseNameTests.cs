using System;
using FluentAssertions;
using Synnotech.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Synnotech.MsSqlServer.Tests;

public sealed class EscapeDatabaseNameTests
{
    public EscapeDatabaseNameTests(ITestOutputHelper output) => Output = output;

    private ITestOutputHelper Output { get; }

    [Theory]
    [ClassData(typeof(ValidDatabaseIdentifiers))]
    public static void ValidDatabaseNames(string validName, string expectedName) =>
        SqlEscaping.CheckAndNormalizeDatabaseName(validName).Should().Be(expectedName);

    [Theory]
    [ClassData(typeof(InvalidDatabaseIdentifiers))]
    public void InvalidDatabaseNames(string invalidName)
    {
        Action act = () => SqlEscaping.CheckAndNormalizeDatabaseName(invalidName);

        var exception = act.Should().Throw<ArgumentException>().Which;
        exception.ParamName.Should().Be("databaseName");
        exception.ShouldBeWrittenTo(Output);
    }
}