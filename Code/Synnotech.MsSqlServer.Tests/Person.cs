using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Synnotech.MsSqlServer.Tests
{
    public sealed class Person
    {
        public int Id { get; init; }

        public string Name { get; set; } = string.Empty;

        public int Age { get; set; }
    }

    public static class PersonExtensions
    {
        public static async Task<List<Person>> DeserializePersons(this SqlDataReader reader)
        {
            var idOrdinal = reader.GetOrdinal(nameof(Person.Id));
            var nameOrdinal = reader.GetOrdinal(nameof(Person.Name));
            var ageOrdinal = reader.GetOrdinal(nameof(Person.Age));

            var persons = new List<Person>();
            while (await reader.ReadAsync())
            {
                var id = reader.GetInt32(idOrdinal);
                var name = reader.GetString(nameOrdinal);
                var age = reader.GetInt32(ageOrdinal);
                var person = new Person { Id = id, Name = name, Age = age };
                persons.Add(person);
            }

            return persons;
        }
    }
}