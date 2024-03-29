# Synnotech.MsSqlServer
*Provides common functionality for database access to MS SQL Server.*

[![Synnotech Logo](synnotech-large-logo.png)](https://www.synnotech.de/)

[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/Synnotech-AG/Synnotech.MsSqlServer/blob/main/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-4.1.0-blue.svg?style=for-the-badge)](https://www.nuget.org/packages/Synnotech.MsSqlServer/)

# How to Install

Synnotech.MsSqlServer is compiled against [.NET Standard 2.0 and 2.1](https://docs.microsoft.com/en-us/dotnet/standard/net-standard) and thus supports all major platforms like .NET 5, .NET Core, .NET Framework 4.6.1 or newer, Mono, Xamarin, UWP, or Unity.

Synnotech.MsSqlServer is available as a [NuGet package](https://www.nuget.org/packages/Synnotech.MsSqlServer/) and can be installed via:

- **Package Reference in csproj**: `<PackageReference Include="Synnotech.MsSqlServer" Version="4.1.0" />`
- **dotnet CLI**: `dotnet add package Synnotech.MsSqlServer`
- **Visual Studio Package Manager Console**: `Install-Package Synnotech.MsSqlServer`

# What does Synnotech.MsSqlServer offer you?

## Async Sessions with ADO.NET

As of version 2.0.0, Synnotech.MsSqlServer implements the `IAsyncSession` and `IAsyncReadOnlySession` from [Synnotech.DatabaseAbstractions](https://github.com/synnotech-AG/synnotech.DatabaseAbstractions). These allow you to make direct ADO.NET requests via a `SqlConnection` and `SqlCommand` through the aforementioned abstractions. All async methods have full support for cancellation tokens.

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

namespace MyTestProject;

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
```

A detailed description on how to set this up can be found [here](https://github.com/Synnotech-AG/Synnotech.Xunit#testsettingsjson).

## Easily Execute a SQL Command

You can use `Database.ExecuteNonQueryAsync`, `Database.ExecuteScalarAsync`, and `Database.ExecuteReaderAsync` to quickly apply SQL statements to the target database. This is most useful in integration tests where you want to apply a single SQL script to bring the database in a certain state before the actual test exercises the database.

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

`ExecuteScalarAsync` and `ExecuteReaderAsync` work in a similar fashion. Here is an example for reading data from a SQL query:

```csharp
public static class ExecuteReaderAsyncTests
{
    [SkippableFact]
    public static async Task ReadFromDatabase()
    {
        var connectionString = TestSettings.GetConnectionStringOrSkip();
        await Database.DropAndCreateDatabaseAsync(connectionString);
        await Database.ExecuteNonQueryAsync(connectionString, Scripts.SimpleDatabase);

        var persons = await Database.ExecuteReaderAsync(connectionString,
                                                        Scripts.GetPersons,
                                                        DeserializePersons);

        var expectedPersons = new List<Person>
        {
            new () { Id = 1, Name = "John Doe", Age = 42 },
            new () { Id = 2, Name = "Helga Orlowski", Age = 29 },
            new () { Id = 3, Name = "Bruno Hitchens", Age = 37 }
        };
        persons.Should().Equal(expectedPersons);
    }

    private static async Task<List<Person>> DeserializePersons(SqlDataReader reader, CancellationToken cancellationToken)
    {
        var persons = new List<Person>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32(0);
            var name = reader.GetString(1);
            var age = reader.GetInt32(2);
            persons.Add(new () { Id = id, Name = name, Age = age });
        }

        return persons;
    }
}
```

Finally, every of these methods supports cancellation tokens.

## Detaching and Attaching Databases

In scenarios where you want to process the MDF and LDF files of a SQL Server database directly (e.g. when moving to another server), you usually have to detach and attach the corresponding files. This can be easily done with the `Database.DetachDatabaseAsync` and `Database.AttachDatabaseAsync` methods.

```csharp
// The following statement detaches the database and returns a struct that contains
// information about the database name and the associated physical file paths
// (usually one MDF and one LDF file).
var databaseFilesInfo = await Database.DetachDatabaseAsync(connectionString);

// You can now access the physical files of a database, to e.g. move them to another
// server, ZIPPING them for redistribution, etc. In this example, we copy them to another
// folder:
foreach (var fileInfo in databaseInfo.Files)
{
    var sourceFileName = Path.GetFileName(fileInfo.PhysicalFilePath);
    var targetFilePath = Path.Combine(targetFolder, sourceFileName);
    File.Copy(fileInfo.PhysicalFilePath, targetFilePath);
}

// Afterwards, you can re-attach the database. If you want to change the
// server, simply provide another connection string.
await Database.AttachDatabaseAsync(connectionString, databaseFilesInfo);
```

## Check if a Database Exists

In some scenarios, you want to quickly check if a database exists. You can use the `Database.CheckIfDatabaseExistsAsync` method for that:

```csharp
var doesDatabaseExist = await Database.CheckIfDatabaseExistsAsync(connectionString, cancellationToken);
if (doesDatabaseExist)
    // you could create a backup here before updating the database
```

There is also an overload that uses an already open `SqlConnection` to perform this query.

## Database Name Escaping

For most things, the `SqlCommand` provides parameters that will be properly escaped when executing queries or DML statements. However, DDL statements usually do not support parameters.You can simply create a `DatabaseName` instance from a string which will automatically check and trim the database name. You can then use the raw name e.g. in T-SQL strings by calling `ToString`, or use `Database.Identifier` to include the potentially escaped name directly in T-SQL scripts.

```csharp
public static Task DropDatabaseAsync(string databaseName)
{
    // This will trim and check the database name. It will throw if the value is invalid.
    var parsedName = new DatabaseName(databaseName);

    // You can then safely use the parsed name in your dynamically created DDL statements.
    // Use parsedName directly in T-SQL strings, and use .Identifier to get a potentially
    // escaped version of the database name for direct use in T-SQL scripts.
    var sql = $@"
IF DB_ID('{parsedName}') IS NOT NULL
    DROP DATABASE {parsedName.Identifier}";

    return Database.ExecuteNonQueryAsync(sql);
}
```

You can also manually escape database identifiers by using the `SqlEscaping.CheckAndNormalizeDatabaseName` function (which ensures that the database name is compatible with the [official rules of SQL Server](https://docs.microsoft.com/en-us/sql/relational-databases/databases/database-identifiers?view=sql-server-ver15#rules-for-regular-identifiers)).

## Migration Guide

### From 1.1.0 to 2.0.0

Version 2.0.0 uses [System.Data.SqlClient](https://www.nuget.org/packages/System.Data.SqlClient/) instead of [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/). This allows you to use spatial types and `SqlHierarchyId` in .NET Core and .NET 5/6 projects via [dotmortem.Microsoft.SqlServer.Types](https://github.com/dotMorten/Microsoft.SqlServer.Types). You simply need to recompile to make this work.

### From 2.0.0 to 3.0.0

Version 3.0.0 adds optional retry parameters to methods where it makes sense (e.g. `DropAndCreateDatabaseAsync`). The default is three retries with an interval of 750ms between each try. You can customize the behavior by setting the `retryCount` and `intervalBetweenRetriesInMilliseconds` parameters. If you want to process the caught exceptions, use the `processException` parameter where you can pass a delegate. This delegate is always called, even when the exception is rethrown.

Additionally, the `DatabaseName` structure now allows non-standard names for databases. Names that contain spaces and/or hyphens are now allowed. You must distinguish between the normal `ToString` call which returns the raw database name for T-SQL strings, and the `DatabaseName.Identifier` property which applies brackets to the identifier if necessary (see section Database Name Escaping).

Simply recompiling with the new version is enough to properly upgrade to the new package version.

### From 3.x to 4.0.0

Version 4.0.0 removes the `IInitializeAsync` interface and reuses the one from [Synnotech.Core](https://github.com/Synnotech-AG/Synnotech.core). The `SessionFactory<T>` now derives from `GenericAsyncFactory<T>` from Synnotech.Core to reuse its functionality. `AddSessionFactory` now relies internally on `ContainerSettingsContext` of Synnotech.Core to determine if create-session delegates should be registered.

You probably won't notice any of these changes - a simple recompile should be enough after updating your NuGet package reference.