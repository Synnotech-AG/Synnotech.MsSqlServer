using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Synnotech.MsSqlServer.Tests.SqlScripts;
using Xunit;

namespace Synnotech.MsSqlServer.Tests;

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