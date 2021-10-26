using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synnotech.DatabaseAbstractions;
using Synnotech.MsSqlServer.Tests.SqlScripts;
using Xunit;

namespace Synnotech.MsSqlServer.Tests
{
    public static class AsyncReadOnlySessionTests
    {
        [Fact]
        public static void MustImplementIAsyncReadOnlySession() =>
            typeof(AsyncReadOnlySession).Should().Implement<IAsyncReadOnlySession>();

        [Fact]
        public static void MustBeAbstract() =>
            typeof(AsyncReadOnlySession).Should().BeAbstract();

        [SkippableFact]
        public static async Task LoadPersons()
        {
            var connectionString = TestSettings.GetConnectionStringOrSkip();

            await Database.DropAndCreateDatabaseAsync(connectionString);
            await Database.ExecuteNonQueryAsync(connectionString, Scripts.SimpleDatabaseScript);


            await using var container = new ServiceCollection().AddSqlConnection(connectionString)
                                                               .AddSessionFactoryFor<IGetPersonsSession, SqlGetPersonsSession>()
                                                               .BuildServiceProvider();

            var sessionFactory = container.GetRequiredService<ISessionFactory<IGetPersonsSession>>();
            await using var session = await sessionFactory.OpenSessionAsync();
            var persons = await session.GetPersonsAsync();

            var expectedPersons = new List<Person>
            {
                new () { Id = 1, Name = "John Doe", Age = 42 },
                new () { Id = 2, Name = "Helga Orlowski", Age = 29 },
                new () { Id = 3, Name = "Bruno Hitchens", Age = 37 }
            };
            persons.Should().BeEquivalentTo(expectedPersons);
        }

        private interface IGetPersonsSession : IAsyncReadOnlySession
        {
            Task<List<Person>> GetPersonsAsync();
        }

        // ReSharper disable once ClassNeverInstantiated.Local -- the session is instantiated by the DI container
        private sealed class SqlGetPersonsSession : AsyncReadOnlySession, IGetPersonsSession
        {
            public SqlGetPersonsSession(SqlConnection sqlConnection) : base(sqlConnection) { }

            public async Task<List<Person>> GetPersonsAsync()
            {
                await using var command = SqlConnection.CreateCommand();
                command.CommandText = Scripts.GetPersonsScript;
                await using var reader = await command.ExecuteReaderAsync();
                return await reader.DeserializePersonsAsync();
            }
        }
    }
}