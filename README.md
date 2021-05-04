# Synnotech.MsSqlServer
*Provides common functionality for database access to MS SQL Server.*

[![Synnotech Logo](synnotech-large-logo.png)](https://www.synnotech.de/)

[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/Synnotech-AG/Synnotech.MsSqlServer/blob/main/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-1.0.0-blue.svg?style=for-the-badge)](https://www.nuget.org/packages/Synnotech.MsSqlServer/)

# How to Install

Synnotech.MsSqlServer is compiled against [.NET Standard 2.0 and 2.1](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) and thus supports all major plattforms like .NET 5, .NET Core, .NET Framework 4.6.1 or newer, Mono, Xamarin, UWP, or Unity.

Synnotech.MsSqlServer is available as a [NuGet package](https://www.nuget.org/packages/Synnotech.MsSqlServer/) and can be installed via:

- **Package Reference in csproj**: `<PackageReference Include="Synnotech.MsSqlServer" Version="1.0.0" />`
- **dotnet CLI**: `dotnet add package Synnotech.MsSqlServer`
- **Visual Studio Package Manager Console**: `Install-Package Synnotech.MsSqlServer`

# What does Synnotech.MsSqlServer offer you?

## Easily Create or Drop Databases

The `Database` class offers several methods to you to easily create or drop databases. This feature should be mostly used in automated integration tests, but can also be used e.g. at app startup to ensure that a specific database is created.

The corresponding methods are

- `DropAndCreateDatabaseAsync`: creates a database, or drops and recreates it when it already exists. Most useful in testing scenarios when you want to have a fresh database at the beginning of a test, but want to leave it intact for inspection after the test has finished. All existing connections to the target database will be terminated before it is dropped.
- `TryCreateDatabaseAsync`: creates a new database if it does not exist yet.
- `TryDropDatabaseAsync`: drops a database if it exists. All existing connections to the target database will be terminated before it is dropped.

For all these methods, you will provide the connection string for the target database. All these methods will connect to the "master" database of the corresponding SQL server to execute the corresponding `CREATE DATABASE` or `DROP DATABASE` statements - so be sure that the credentials provided in the connection string have enough privileges to execute these statements.

We encourage you to combine this package with [Synnotech.Xunit](https://github.com/Synnotech-AG/Synnotech.Xunit) and [Xunit.SkippableFact](https://github.com/AArnott/Xunit.SkippableFact). A typical scenario in a test project might look like this:

In testsettings.json:
```jsonc
{
    "database": {
        "areTestsEnabled": false, // tests are turned off by default
        "connectionString": "Server=(localdb)\\MsSqlLocalDb; Database=IntegrationTests; Integrated Security=True"
    }
}
```

In testsettings.Development.json:
```jsonc
{
    // Devs can use this file to individually configure the tests for their dev machine.
    // This file should be ignored by your version control system.
    "database": {
        "areTestsEnabled": true,
        "connectionString": "Server=MyLocalInstance; Database=IntegrationTests; Integrated Security=True"
    }
}
```

Your test code:
```csharp
using Synnotech.Xunit
using Xunit;

namespace MyTestProject
{
    public class MyDatabaseIntegrationTests
    {
        [SkippableFact]
        public async Task RunAllMigrations()
        {
            Skip.IfNot(TestSettings.Configuration.GetValue<bool>("database:areTestsEnabled"));
            
            var connectionString = TestSettings.Configuration["database:connectionString"] ??
                throw new InvalidOperationException("You must set connectionString when areTestsEnabled is set to true");           
            await Database.DropAndCreateDatabaseAsync(connectionString);
            
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            
            // Execute all your migrations and check if they were applied successfully
        }
    }
}
```

A detailed description on how to set this up can be found [here](https://github.com/Synnotech-AG/Synnotech.Xunit#testsettingsjson).

## Database Name Escaping

For most things, the `SqlCommand` provides parameters that will be properly escaped when executing for queries or DML statements. However, DDL statements usually do not support parameters. You can manually escape database identifiers by using the `SqlEscaping.CheckAndNormalizeDatabaseName` function. Alternatively, you can use the `DatabaseName` struct to encapsulate an escaped database identifier. The identifiers are escaped according to the [official rules of SQL Server](https://docs.microsoft.com/en-us/sql/relational-databases/databases/database-identifiers?view=sql-server-ver15#rules-for-regular-identifiers).
