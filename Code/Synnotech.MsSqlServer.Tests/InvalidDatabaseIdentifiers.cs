using Xunit;

namespace Synnotech.MsSqlServer.Tests;

public sealed class InvalidDatabaseIdentifiers : TheoryData<string>
{
    public InvalidDatabaseIdentifiers()
    {
        Add("%ABC"); // Invalid first character
        Add("!AB");
        Add("DatabaseName'; DROP DATABASE FOO; --"); // SQL Injection Attack
        Add(string.Empty); // Empty String
        Add(null!); // null
        Add("\t\r\n"); // white space
        Add("Invalid Name"); // White space in between
        Add("Other$Invalid§Special?Characters"); // White space in between
    }
}