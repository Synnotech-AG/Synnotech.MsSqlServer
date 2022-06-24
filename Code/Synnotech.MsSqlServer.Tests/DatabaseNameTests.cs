using System;
using FluentAssertions;
using Synnotech.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Synnotech.MsSqlServer.Tests;

public sealed class DatabaseNameTests
{
    public DatabaseNameTests(ITestOutputHelper output) => Output = output;

    private ITestOutputHelper Output { get; }

    [Theory]
    [ClassData(typeof(InvalidDatabaseIdentifiers))]
    public void ArgumentExceptionOnInvalidName(string invalidName)
    {
        // ReSharper disable once ObjectCreationAsStatement
        Action act = () => new DatabaseName(invalidName);

        var exception = act.Should().Throw<ArgumentException>().Which;
        exception.ParamName.Should().Be("databaseName");
        exception.ShouldBeWrittenTo(Output);
    }

    [Theory]
    [ClassData(typeof(ValidDatabaseIdentifiers))]
    public static void ValidDatabaseNameMustBeRetrievable(string validName, string expectedName)
    {
        var databaseName = new DatabaseName(validName);

        databaseName.ToString().Should().Be(expectedName);
    }

    [Theory]
    [InlineData("Foo", "Foo")]
    [InlineData("bar", "BAR")]
    [InlineData("Baz", "baz")]
    [InlineData("UPDATE", "update")]
    public static void TwoInstancesWithSameNameMustBeEqual(string firstName, string secondName)
    {
        var first = new DatabaseName(firstName);
        var second = new DatabaseName(secondName);

        (first == second).Should().BeTrue();
        (second == first).Should().BeTrue();
        (first != second).Should().BeFalse();
        (second != first).Should().BeFalse();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Theory]
    [InlineData("Foo", "Bar")]
    [InlineData("UPDATE", "Table")]
    public static void TwoInstancesWithDifferentNamesMustNotBeEqual(string firstName, string secondName)
    {
        var first = new DatabaseName(firstName);
        var second = new DatabaseName(secondName);

        (first == second).Should().BeFalse();
        (second == first).Should().BeFalse();
        (first != second).Should().BeTrue();
        (second != first).Should().BeTrue();
        first.GetHashCode().Should().NotBe(second.GetHashCode());
    }

    [Theory]
    [ClassData(typeof(ValidDatabaseIdentifiers))]
    public static void ImplicitConversionFromString(string name, string expectedName)
    {
        DatabaseName databaseName = name;

        databaseName.ToString().Should().Be(expectedName);
    }
}