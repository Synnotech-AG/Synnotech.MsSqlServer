# Synnotech.MsSqlServer
*Provides common functionality for database access to MS SQL Server.*

[![Synnotech Logo](synnotech-large-logo.png)](https://www.synnotech.de/)

[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/Synnotech-AG/Synnotech.MsSqlServer/blob/main/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-2.0.0-blue.svg?style=for-the-badge)](https://www.nuget.org/packages/Synnotech.MsSqlServer/)

# How to Install

Synnotech.MsSqlServer is compiled against [.NET Standard 2.0 and 2.1](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) and thus supports all major plattforms like .NET 5, .NET Core, .NET Framework 4.6.1 or newer, Mono, Xamarin, UWP, or Unity.

Synnotech.MsSqlServer is available as a [NuGet package](https://www.nuget.org/packages/Synnotech.MsSqlServer/) and can be installed via:

- **Package Reference in csproj**: `<PackageReference Include="Synnotech.MsSqlServer" Version="2.0.0" />`
- **dotnet CLI**: `dotnet add package Synnotech.MsSqlServer`
- **Visual Studio Package Manager Console**: `Install-Package Synnotech.MsSqlServer`

# What does Synnotech.MsSqlServer offer you?

## Async Sessions with ADO.NET

As of version 2.0.0, Synnotech.MsSqlServer implements the `IAsyncSession` and `IAsyncReadOnlySession` from [Synnotech.DatabaseAbstractions](https://github.com/synnotech-AG/synnotech.DatabaseAbstractions). These allow you to make direct ADO.NET requests via a `SqlConnection` and `SqlCommand` through the forementioned abstractions. All async methods have full support for cancellation tokens.

Consider the following abstraction for database access:

```csharp
public interface IGetContactSession : IAsyncReadOnlySession
{
    Task<Contact?> GetContactAsync(int id);
}
```

To implement this interface easily, you can derive from `AsyncReadOnlySession`:

```csharp
public sealed class SqlGetContactSession : AsyncReadOnlySession, IGetContactSession
{
    public SqlGetContactSession(SqlConnection sqlConnection) : base(sqlConnection) { }

    public async Task<Contact?> GetContactAsync(int id)
    {
        // The following line will create a SqlCommand and automatically
        // attach the current transaction to it (if a transaction is present).
        await using var command = CreateCommand(); 

        // We encourage you to use Light.EmbeddedResources and save your SQL
        // queries as embedded SQL files to your assembly.
        command.CommandText = SqlScripts.GetScript("GetContact.sql");
        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

        await using var reader = await command.ExecuteReaderAsync();
        return await DeserializeContactAsync(reader);
    }

    private async Task<Contact?> DeserializeContactAsync(SqlDataReader reader)
    {
        if (!reader.HasRows)
            return null;

        var idOrdinal = reader.GetOrdinal(nameof(Contact.Id));
        var nameOrdinal = reader.GetOrdinal(nameof(Contact.Name));
        var emailOrdinal = reader.GetOrdinal(nameof(Contact.Email));

        if (!await reader.ReadAsync())
            throw new SerializationException("The reader could not advance to the single row");

        var id = reader.GetInt32(idOrdinal);
        var name = reader.GetString(nameOrdinal);
        var email = reader.GetString(emailOrdinal);
        return new Contact { Id = id, Name = name, Email = email };
    }
}
```

Your SQL script to get a person can be stored in a dedicated SQL file that can be embedded in your assembly. You can use [Light.EmbeddedResources](https://github.com/feO2x/Light.EmbeddedResources) to easily access them:

```csharp
public static class Sqlcripts
{
    public static string GetScript(string name) => typeof(Sqlcripts).GetEmbeddedResource(name);
}
```

```sql
SELECT *
FROM Contacts
WHERE [Id] = @Id;
```

You can then add your sessions to your DI container by calling the `AddSessionFactoryFor` extension method:

```csharp
services.AddSessionFactoryFor<IGetContactSession, SqlGetContactSession>();
```

Be sure that a `SqlConnection` is already registered with the DI Container. You can use the `AddSqlConnection` extension method for that.

To call your session, e.g. in an ASP.NET Core MVC controller, you simply instantiate the session via the factory:

```csharp
[ApiController]
[Route("api/contacts")]
public sealed class GetContactController : ControllerBase
{
    public GetContactController(ISessionFactory<IGetContactSession> sessionFactory) =>
        SessionFactory = sessionFactory;

    private ISessionFactory<IGetContactSession> SessionFactory { get; }

    [HttpGet("{id}")]
    public async Task<ActionResult<Contact>> GetContact(int id)
    {
        // The following call will open the session asynchronously and start
        // a transaction (if necessary), all in one go.
        await using var session = await SessionFactory.OpenSessionAsync();

        var contact = await session.GetContactAsync(id);
        if (contact == null)
            return NotFound();
        return contact();
    }
}
```

If you want to manipulate data, then simply derive from `AsyncSession` instead. This gives you an additional `SaveChangesAsync` method that allows you to commit the internal transaction.

Please be aware: Synnotech.DatabaseAbstractions does not support nested transactions. If you need them, you must create your own abstraction for it. However, we generally recommend to not use nested transactions, but use sequential transactions (e.g. when performaing batch operations).

## Easily Open SQL Connections

You can use the `Database.OpenConnectionAsync` method to create a new `SqlConnection` instance and open it asynchronously in one go:

```csharp
await using var connection = await Database.OpenConnectionAsync(connectionString);
```

You can supply an optional `CancellationToken` if you want to.

## Easily Create or Drop Databases

The `Database` class offers several methods to you to easily create or drop databases. This feature should be mostly used in automated integration tests, but can also be used e.g. at app startup to ensure that a specific database is created.

The corresponding methods are

- `DropAndCreateDatabaseAsync`: creates a database, or drops and recreates it when it already exists. Most useful in testing scenarios when you want to have a fresh database at the beginning of a test, but want to leave it intact for inspection after the test has finished. All existing connections to the target database will be terminated before it is dropped.
- `TryCreateDatabaseAsync`: creates a new database if it does not exist yet.
- `TryDropDatabaseAsync`: drops a database if it exists. All existing connections to the target database will be terminated before it is dropped.

For all these methods, you will provide the connection string for the target database. All these methods will connect to the "master" database of the corresponding SQL server to execute the corresponding `CREATE DATABASE` or `DROP DATABASE` statements - so be sure that the credentials provided in the connection string have enough privileges to execute these statements. All methods support `CancellationToken`.

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
using Synnotech.MsSqlServer;
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
            
            await using var connection = await Database.OpenConnectionAsync(connectionString);
            
            // Execute all your migrations and check if they were applied successfully
        }
    }
}
```

A detailed description on how to set this up can be found [here](https://github.com/Synnotech-AG/Synnotech.Xunit#testsettingsjson).

## Easily Execute a SQL Command

You can use the two overloads of `Database.ExecuteNonQueryAsync` to quickly apply SQL statements to the target database. This is most useful in integration tests where you want to apply a single SQL script to bring the database in a certain state before the actual test exercises the database.

```csharp
public class MyTestClass
{
    [SkippableFact]
    public async Task MyIntegrationTest()
    {
        var connectionString = TestSettings.GetConnectionStringOrSkip();
        await Database.DropAndCreateDatabaseAsync(connectionString);
        await Database.ExecuteNonQueryAsync(connectionString, this.GetEmbeddedResource("SimpleDatabase.sql"))

        // actual test code comes here
    }
}
```

In the example above, we use [Synnotech.Xunit](https://github.com/Synnotech-AG/Synnotech.Xunit) and [Xunit.SkippableFact](https://github.com/AArnott/Xunit.SkippableFact) to skip the test or get the target connection string from testsettings.json. We then (re-)create the target database and execute the script `SimpleDatabase.sql` against it. The script is an embedded resource and retrieved using [Light.EmbeddedResources](https://github.com/feO2x/Light.EmbeddedResources).

As you can see, you can simply pass in a connection string. If you already have an open connection, then you can also call an extension method with the same name on it:

```csharp
await using var connection = Database.OpenConnectionAsync(connectionString);
await connection.ExecuteNonQueryAsync(
    this.GetEmbeddedResource("MySqlScript.sql"),
    command => command.Parameters.AddWithValue("@CompanyId", companyId)
);
```

The example above also illustrates how you can adjust the command via the optional `configureCommand` delegate. You will most likely use it to set parameters on the SQL command.

Besides that, you can also execute the command within a dedicated transaction using the optional `transactionLevel` parameter:

```csharp
await connection.ExecuteNonQueryAsync(
    this.GetEmbeddedResource("MySqlScript.sql"),
    transactionLevel: IsolationLevel.Serializable
);
```

If you created your own transaction, you must add it to the command manually using `configureCommand`:

```csharp
await using var connection = await Database.OpenConnectionAsync(connectionString);
await using var myTransaction = connection.BeginTransaction();
await connection.ExecuteNonQueryAsync(
    this.GetEmbeddedResource("MyScript"),
    command => command.Transaction = myTransaction
);
```

Finally, both of the overloads support `CancellationToken`.

## Database Name Escaping

For most things, the `SqlCommand` provides parameters that will be properly escaped when executing queries or DML statements. However, DDL statements usually do not support parameters. You can manually escape database identifiers by using the `SqlEscaping.CheckAndNormalizeDatabaseName` function. Alternatively, you can use the `DatabaseName` struct to encapsulate an escaped database identifier. The identifiers are escaped according to the [official rules of SQL Server](https://docs.microsoft.com/en-us/sql/relational-databases/databases/database-identifiers?view=sql-server-ver15#rules-for-regular-identifiers).

## Migration Guide

### From 1.1.0 to 2.0.0

Version 2.0.0 uses [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) instead of [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/). This allows you to use spatial types and `SqlHierarchyId` in .NET Core and .NET 5/6 projects via [dotmortem.Microsoft.SqlServer.Types](https://github.com/dotMorten/Microsoft.SqlServer.Types). You simply need to recompile to make this work.