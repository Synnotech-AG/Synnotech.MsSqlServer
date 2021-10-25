using System.Data;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
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

        [SkippableFact]
        public static async Task UpdatePerson()
        {
            var connectionString = TestSettings.GetConnectionStringOrSkip();

            await Database.DropAndCreateDatabaseAsync(connectionString);
            await Database.ExecuteNonQueryAsync(connectionString, Scripts.SimpleDatabaseScript);

            await using var container = new ServiceCollection().AddSqlConnection(connectionString)
                                                               .AddSessionFactoryFor<IUpdatePersonSession, SqlUpdatePersonSession>()
                                                               .BuildServiceProvider();

            var sessionFactory = container.GetRequiredService<ISessionFactory<IUpdatePersonSession>>();
            await using var session = await sessionFactory.OpenSessionAsync();
            var person = new Person { Id = 1, Name = "Jane Doe", Age = 24 };
            await session.UpdatePersonAsync(person);
            await session.SaveChangesAsync();
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
    }
}