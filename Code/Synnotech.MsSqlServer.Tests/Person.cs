using System.Collections.Generic;
using System.Data.SqlClient;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Synnotech.MsSqlServer.Tests;

public sealed class Person
{
    public int Id { get; init; }

    public string Name { get; set; } = string.Empty;

    public int Age { get; set; }
}

public static class PersonExtensions
{
    public static async Task<List<Person>> DeserializePersonsAsync(this SqlDataReader reader)
    {
        var persons = new List<Person>();
        if (!reader.HasRows)
            return persons;

        var (idOrdinal, nameOrdinal, ageOrdinal) = reader.GetOrdinals();

        while (await reader.ReadAsync())
        {
            var person = reader.DeserializeRow(idOrdinal, nameOrdinal, ageOrdinal);
            persons.Add(person);
        }

        return persons;
    }

    public static async Task<Person?> DeserializePersonAsync(this SqlDataReader reader)
    {
        if (!reader.HasRows)
            return null;

        var (idOrdinal, nameOrdinal, ageOrdinal) = reader.GetOrdinals();

        if (!await reader.ReadAsync())
            throw new SerializationException("The reader did not advance to the person to be deserialized");

        var person = reader.DeserializeRow(idOrdinal, nameOrdinal, ageOrdinal);
        return person;
    }

    private static (int idOrdinal, int nameOrdinal, int ageOrdinal) GetOrdinals(this SqlDataReader reader)
    {
        var idOrdinal = reader.GetOrdinal(nameof(Person.Id));
        var nameOrdinal = reader.GetOrdinal(nameof(Person.Name));
        var ageOrdinal = reader.GetOrdinal(nameof(Person.Age));
        return (idOrdinal, nameOrdinal, ageOrdinal);
    }

    private static Person DeserializeRow(this SqlDataReader reader, int idOrdinal, int nameOrdinal, int ageOrdinal)
    {
        var id = reader.GetInt32(idOrdinal);
        var name = reader.GetString(nameOrdinal);
        var age = reader.GetInt32(ageOrdinal);
        var person = new Person { Id = id, Name = name, Age = age };
        return person;
    }
}