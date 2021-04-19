using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using Microsoft.Data.SqlClient;

namespace Synnotech.MsSqlServer
{
    /// <summary>
    /// Provides helper methods for creating and dropping MS SQL databases.
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// Tries to create the database the specified connection string points to. If
        /// the target database already exists, nothing will be done.
        /// This method will connect to the "master" database of the target
        /// SQL server to do this - please ensure that the credentials in the connection string
        /// have enough privileges to perform this operation.
        /// </summary>
        /// <param name="connectionString">The connection string that identifies the target database.</param>
        /// <returns>True when the database was dropped, otherwise false.</returns>
        /// <exception cref="KeyNotFoundException">Invalid key name within the connection string.</exception>
        /// <exception cref="FormatException">Invalid value within the connection string (specifically, when a Boolean or numeric value was expected but not supplied).</exception>
        /// <exception cref="ArgumentException">The supplied connectionString is not valid.</exception>
        /// <exception cref="SqlException">Thrown when the connection to the master database fails or when the command fails to execute.</exception>
        public static async Task TryCreateDatabaseAsync(string connectionString)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
            DatabaseName targetDatabaseName = connectionStringBuilder.InitialCatalog;
            connectionStringBuilder.InitialCatalog = "master";

#if NETSTANDARD2_0
            using var connectionToMaster =
#else
            await using var connectionToMaster =
#endif
                new SqlConnection(connectionStringBuilder.ConnectionString);

            await connectionToMaster.OpenAsync();
            await connectionToMaster.TryCreateDatabaseAsync(targetDatabaseName);
        }

        /// <summary>
        /// Tries to drop the database the specified connection string points to. All existing
        /// connections to the database will be terminated. If the database does not exist, nothing
        /// will happen.
        /// This method will connect to the "master" database of the target
        /// SQL server to do this - please ensure that the credentials in the connection string
        /// have enough privileges to perform this operation.
        /// </summary>
        /// <param name="connectionString">The connection string that identifies the target database.</param>
        /// <returns>True when the database was dropped, otherwise false.</returns>
        /// <exception cref="KeyNotFoundException">Invalid key name within the connection string.</exception>
        /// <exception cref="FormatException">Invalid value within the connection string (specifically, when a Boolean or numeric value was expected but not supplied).</exception>
        /// <exception cref="ArgumentException">The supplied connectionString is not valid.</exception>
        /// <exception cref="SqlException">Thrown when the connection to the master database fails or when the command fails to execute.</exception>
        public static async Task TryDropDatabaseAsync(string connectionString)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
            DatabaseName targetDatabaseName = connectionStringBuilder.InitialCatalog;
            connectionStringBuilder.InitialCatalog = "master";
#if NETSTANDARD2_0
            using var connectionToMaster =
#else
            await using var connectionToMaster =
#endif
                new SqlConnection(connectionStringBuilder.ConnectionString);

            await connectionToMaster.OpenAsync();
            await connectionToMaster.KillAllDatabaseConnectionsAsync(targetDatabaseName);
            await connectionToMaster.TryDropDatabaseAsync(targetDatabaseName);
        }

        /// <summary>
        /// Creates the database for the specified connection string. If it already exists, the
        /// database will be dropped and recreated. Connections to the existing database
        /// will be terminated. This method will connect to the "master" database of the target
        /// SQL server to do this - please ensure that the credentials in the connection string
        /// have enough privileges to perform this operation.
        /// </summary>
        /// <param name="connectionString">The connection string that identifies the target database.</param>
        /// <exception cref="KeyNotFoundException">Invalid key name within the connection string.</exception>
        /// <exception cref="FormatException">Invalid value within the connection string (specifically, when a Boolean or numeric value was expected but not supplied).</exception>
        /// <exception cref="ArgumentException">The supplied connectionString is not valid.</exception>
        /// <exception cref="SqlException">Thrown when the connection to the master database fails.</exception>
        public static async Task DropAndCreateDatabaseAsync(string connectionString)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
            DatabaseName targetDatabaseName = connectionStringBuilder.InitialCatalog;
            connectionStringBuilder.InitialCatalog = "master";
#if NETSTANDARD2_0
            using var connectionToMaster =
#else
            await using var connectionToMaster =
#endif
                new SqlConnection(connectionStringBuilder.ConnectionString);

            await connectionToMaster.OpenAsync();
            await connectionToMaster.KillAllDatabaseConnectionsAsync(targetDatabaseName);
            await connectionToMaster.DropAndCreateDatabaseAsync(targetDatabaseName);
        }

        /// <summary>
        /// Execute a T-SQL command (non-query) that kills all active connections to the database
        /// with the specified name. It is safe to run this command when the target database does
        /// not exist.
        /// </summary>
        /// <param name="connectionToMaster">
        /// The SQL connection that will be used to execute the command.
        /// It must target the master database of a SQL server.
        /// </param>
        /// <param name="databaseName">The name of the target database.</param>
        /// <param name="cancellationToken">The cancellation instruction (optional).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionToMaster"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="databaseName"/> is the default instance.</exception>
        /// <exception cref="SqlException">Thrown when the command fails to execute.</exception>
        public static async Task KillAllDatabaseConnectionsAsync(this SqlConnection connectionToMaster, DatabaseName databaseName, CancellationToken cancellationToken = default)
        {
            connectionToMaster.MustNotBeNull(nameof(connectionToMaster));
            databaseName.MustNotBeDefault(nameof(databaseName));

#if NETSTANDARD2_0
            using var command = connectionToMaster.CreateCommand();
#else
            await using var command = connectionToMaster.CreateCommand();
#endif

            command.CommandType = CommandType.Text;
            command.CommandText = $@"
DECLARE @kill varchar(1000) = '';
SELECT @kill = @kill + 'kill ' + CONVERT(varchar(5), session_id) + ';'
FROM sys.dm_exec_sessions
WHERE database_id = db_id('{databaseName}');

EXEC(@kill);
";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// Execute a T-SQL command (non-query) that creates a new database. If the database
        /// already exists, it will be dropped and recreated.
        /// </summary>
        /// <param name="connectionToMaster">
        /// The SQL connection that will be used to execute the command.
        /// It must target the master database of a SQL server.
        /// </param>
        /// <param name="databaseName">The name of the target database.</param>
        /// <param name="cancellationToken">The cancellation instruction (optional).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionToMaster"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="databaseName"/> is the default instance.</exception>
        /// <exception cref="SqlException">Thrown when the command fails to execute.</exception>
        public static async Task DropAndCreateDatabaseAsync(this SqlConnection connectionToMaster, DatabaseName databaseName, CancellationToken cancellationToken = default)
        {
            connectionToMaster.MustNotBeNull(nameof(connectionToMaster));
            databaseName.MustNotBeDefault(nameof(databaseName));

#if NETSTANDARD2_0
            using var command = connectionToMaster.CreateCommand();
#else
            await using var command = connectionToMaster.CreateCommand();
#endif
            command.CommandType = CommandType.Text;
            command.CommandText = $@"
IF DB_ID('{databaseName}') IS NOT NULL
DROP DATABASE {databaseName};

CREATE DATABASE {databaseName};
";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// Executes a T-SQL command (non-query) that drops a database if it exists.
        /// </summary>
        /// <param name="connectionToMaster">
        /// The SQL connection that will be used to execute the command.
        /// It must target the master database of a SQL server.
        /// </param>
        /// <param name="databaseName">The name of the target database.</param>
        /// <param name="cancellationToken">The cancellation instruction (optional).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionToMaster"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="databaseName"/> is the default instance.</exception>
        /// <exception cref="SqlException">Thrown when the command fails to execute.</exception>
        public static async Task TryDropDatabaseAsync(this SqlConnection connectionToMaster, DatabaseName databaseName, CancellationToken cancellationToken = default)
        {
            connectionToMaster.MustNotBeNull(nameof(connectionToMaster));
            databaseName.MustNotBeDefault(nameof(databaseName));

#if NETSTANDARD2_0
            using var command = connectionToMaster.CreateCommand();
#else
            await using var command = connectionToMaster.CreateCommand();
#endif
            command.CommandType = CommandType.Text;
            command.CommandText = $@"
IF DB_ID('{databaseName}') IS NOT NULL
DROP DATABASE {databaseName};
";
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>
        /// Executes a T-SQL command (non-query) that creates a database if it does not exist.
        /// </summary>
        /// <param name="connectionToMaster">
        /// The SQL connection that will be used to execute the command.
        /// It must target the master database of a SQL server.
        /// </param>
        /// <param name="databaseName">The name of the target database.</param>
        /// <param name="cancellationToken">The cancellation instruction (optional).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionToMaster"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="databaseName"/> is the default instance.</exception>
        /// <exception cref="SqlException">Thrown when the command fails to execute.</exception>
        public static async Task TryCreateDatabaseAsync(this SqlConnection connectionToMaster, DatabaseName databaseName, CancellationToken cancellationToken = default)
        {
            connectionToMaster.MustNotBeNull(nameof(connectionToMaster));
            databaseName.MustNotBeDefault(nameof(databaseName));

#if NETSTANDARD2_0
            using var command = connectionToMaster.CreateCommand();
#else
            await using var command = connectionToMaster.CreateCommand();
#endif
            command.CommandType = CommandType.Text;
            command.CommandText = $@"
IF DB_ID('{databaseName}') IS NULL
CREATE DATABASE {databaseName};
";

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
