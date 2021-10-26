using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synnotech.DatabaseAbstractions;
using Synnotech.MsSqlServer.Tests.SqlScripts;
using Xunit;

namespace Synnotech.MsSqlServer.Tests
{
    public static class AsyncSessionTests
    {
        [Fact]
        public static void MustImplementIAsyncSession() =>
            typeof(AsyncSession).Should().Implement<IAsyncSession>();

        [Fact]
        public static void MustBeAbstract() =>
            typeof(AsyncSession).Should().BeAbstract();

        [SkippableFact]
        public static async Task UpdatePerson()
        {
            var connectionString = TestSettings.GetConnectionStringOrSkip();

            await Database.DropAndCreateDatabaseAsync(connectionString);
            await Database.ExecuteNonQueryAsync(connectionString, Scripts.SimpleDatabaseScript);

            await using var container = new ServiceCollection().AddSqlConnection(connectionString)
                                                               .AddSessionFactoryFor<IUpdatePersonSession, SqlUpdatePersonSession>()
                                                               .AddSessionFactoryFor<IGetPersonSession, SqlGetPersonSession>()
                                                               .BuildServiceProvider();

            var updateSessionFactory = container.GetRequiredService<ISessionFactory<IUpdatePersonSession>>();
            await using var updateSession = await updateSessionFactory.OpenSessionAsync();
            var updatedPerson = new Person { Id = 1, Name = "Jane Doe", Age = 24 };
            await updateSession.UpdatePersonAsync(updatedPerson);
            await updateSession.SaveChangesAsync();

            var getSessionFactory = container.GetRequiredService<ISessionFactory<IGetPersonSession>>();
            await using var getSession = await getSessionFactory.OpenSessionAsync();
            var loadedPerson = await getSession.GetPersonAsync(1);
            loadedPerson.Should().BeEquivalentTo(updatedPerson);
        }

        private interface IUpdatePersonSession : IAsyncSession
        {
            Task UpdatePersonAsync(Person person);
        }

        // ReSharper disable once ClassNeverInstantiated.Local -- the session is instantiated by the DI container
        private sealed class SqlUpdatePersonSession : AsyncSession, IUpdatePersonSession
        {
            public SqlUpdatePersonSession(SqlConnection sqlConnection)
                : base(sqlConnection) { }


            public async Task UpdatePersonAsync(Person person)
            {
                await using var command = CreateCommand();
                command.CommandText = Scripts.UpdatePersonScript;
                command.Parameters.Add("@Id", SqlDbType.Int).Value = person.Id;
                command.Parameters.Add("@Name", SqlDbType.NVarChar).Value = person.Name;
                command.Parameters.Add("@Age", SqlDbType.Int).Value = person.Age;
                await command.ExecuteNonQueryAsync();
            }
        }

        private interface IGetPersonSession : IAsyncReadOnlySession
        {
            Task<Person?> GetPersonAsync(int id);
        }


        // ReSharper disable once ClassNeverInstantiated.Local -- the session is instantiated by the DI container
        private sealed class SqlGetPersonSession : AsyncReadOnlySession, IGetPersonSession
        {
            public SqlGetPersonSession(SqlConnection sqlConnection) : base(sqlConnection) { }

            public async Task<Person?> GetPersonAsync(int id)
            {
                await using var command = CreateCommand();
                command.CommandText = Scripts.GetPersonScript;
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                await using var reader = await command.ExecuteReaderAsync();
                return await reader.DeserializePersonAsync();
            }
        }
    }
}