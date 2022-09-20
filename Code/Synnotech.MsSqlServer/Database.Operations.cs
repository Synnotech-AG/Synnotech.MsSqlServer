using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using Light.GuardClauses.Exceptions;

namespace Synnotech.MsSqlServer;

/// <summary>
/// Provides helper methods for creating and dropping MS SQL databases.
/// </summary>
public static partial class Database
{
    /// <summary>
    /// <para>
    /// Tries to create the database the specified connection string points to. If
    /// the target database already exists, nothing will be done.
    /// This method will connect to the default database that is configured for the user on the target
    /// SQL server to do this - please ensure that the credentials in the connection string
    /// have enough privileges to perform this operation.
    /// </para>
    /// <para>
    /// This method implements an automatic retry-strategy. It tries for three times and
    /// waits for 750ms between each try. You can adjust the <paramref name="retryCount" />
    /// and <paramref name="intervalBetweenRetriesInMilliseconds" /> parameters to adjust this behavior.
    /// Furthermore, if you want to process the caught exceptions (e.g. for logging), you
    /// can assign the <paramref name="processException" /> delegate. If you want to cancel
    /// early, pass a corresponding <paramref name="cancellationToken" /> that times out
    /// after a certain amount of time.
    /// </para>
    /// </summary>
    /// <param name="connectionString">The connection string that identifies the target database.</param>
    /// <param name="retryCount">The number of retries this method will attempt to create the database (optional). The default value is 3.</param>
    /// <param name="intervalBetweenRetriesInMilliseconds">
    /// The number of milliseconds the method will wait (using Task.Delay) after an exception has occurred (optional).
    /// The default value is 750ms.
    /// </param>
    /// <param name="processException">
    /// The delegate that is called when an exception occurred (optional).
    /// You would usually use this delegate to log the exception.
    /// </param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <returns>True when the database was created, otherwise false.</returns>
    /// <exception cref="KeyNotFoundException">Invalid key name within the connection string.</exception>
    /// <exception cref="FormatException">Invalid value within the connection string (specifically, when a Boolean or numeric value was expected but not supplied).</exception>
    /// <exception cref="ArgumentException">The supplied connectionString is not valid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="retryCount" /> is less than 0 or
    /// when <paramref name="intervalBetweenRetriesInMilliseconds" /> is less than or equal to 0.
    /// </exception>
    /// <exception cref="SqlException">Thrown when the connection to the master database fails or when the command fails to execute and the retry count exceeds.</exception>
    public static async Task<bool> TryCreateDatabaseAsync(string connectionString,
                                                          int retryCount = 3,
                                                          int intervalBetweenRetriesInMilliseconds = 750,
                                                          Action<SqlException>? processException = null,
                                                          CancellationToken cancellationToken = default)
    {
        var (defaultConnectionString, databaseName) = connectionString.PrepareDefaultConnectionAndDatabaseName();

#if NETSTANDARD2_0
        using var defaultConnection =
#else
        await using var defaultConnection =
#endif
            await OpenConnectionAsync(defaultConnectionString, cancellationToken);

        return await defaultConnection.TryCreateDatabaseAsync(databaseName,
                                                              retryCount,
                                                              intervalBetweenRetriesInMilliseconds,
                                                              processException,
                                                              cancellationToken);
    }

    /// <summary>
    /// <para>
    /// Tries to drop the database the specified connection string points to. All existing
    /// connections to the database will be terminated. If the database does not exist, nothing
    /// will happen.
    /// This method will connect to the the default database that is configured for the user on the target
    /// SQL server to do this - please ensure that the credentials in the connection string
    /// have enough privileges to perform this operation.
    /// </para>
    /// <para>
    /// This method implements an automatic retry-strategy. It tries for three times and
    /// waits for 750ms between each try. You can adjust the <paramref name="retryCount" />
    /// and <paramref name="intervalBetweenRetriesInMilliseconds" /> parameters to adjust this behavior.
    /// Furthermore, if you want to process the caught exceptions (e.g. for logging), you
    /// can assign the <paramref name="processException" /> delegate. If you want to cancel
    /// early, pass a corresponding <paramref name="cancellationToken" /> that times out
    /// after a certain amount of time.
    /// </para>
    /// </summary>
    /// <param name="connectionString">The connection string that identifies the target database.</param>
    /// <param name="retryCount">The number of retries this method will attempt to drop the database (optional). The default value is 3.</param>
    /// <param name="intervalBetweenRetriesInMilliseconds">
    /// The number of milliseconds the method will wait (using Task.Delay) after an exception has occurred (optional).
    /// The default value is 750ms.
    /// </param>
    /// <param name="processException">
    /// The delegate that is called when an exception occurred (optional).
    /// You would usually use this delegate to log the exception.
    /// </param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <returns>True when the database was dropped, otherwise false.</returns>
    /// <exception cref="KeyNotFoundException">Invalid key name within the connection string.</exception>
    /// <exception cref="FormatException">Invalid value within the connection string (specifically, when a Boolean or numeric value was expected but not supplied).</exception>
    /// <exception cref="ArgumentException">The supplied connectionString is not valid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="retryCount" /> is less than 0 or
    /// when <paramref name="intervalBetweenRetriesInMilliseconds" /> is less than or equal to 0.
    /// </exception>
    /// <exception cref="SqlException">Thrown when the connection to the master database fails or when the command fails to execute and the retry count exceeds.</exception>
    public static async Task<bool> TryDropDatabaseAsync(string connectionString,
                                                        int retryCount = 3,
                                                        int intervalBetweenRetriesInMilliseconds = 750,
                                                        Action<SqlException>? processException = null,
                                                        CancellationToken cancellationToken = default)
    {
        var (defaultConnectionString, databaseName) = connectionString.PrepareDefaultConnectionAndDatabaseName();
#if NETSTANDARD2_0
        using var defaultConnection =
#else
        await using var defaultConnection =
#endif
            await OpenConnectionAsync(defaultConnectionString, cancellationToken);

        return await defaultConnection.TryDropDatabaseAsync(databaseName,
                                                            retryCount,
                                                            intervalBetweenRetriesInMilliseconds,
                                                            processException,
                                                            cancellationToken);
    }

    /// <summary>
    /// <para>
    /// Creates the database for the specified connection string. If it already exists, the
    /// database will be dropped and recreated. Connections to the existing database
    /// will be terminated. This method will connect to the default database that is configured for the user on the target
    /// SQL server to do this - please ensure that the credentials in the connection string
    /// have enough privileges to perform this operation.
    /// </para>
    /// <para>
    /// This method implements an automatic retry-strategy. It tries for three times and
    /// waits for 750ms between each try. You can adjust the <paramref name="retryCount" />
    /// and <paramref name="intervalBetweenRetriesInMilliseconds" /> parameters to adjust this behavior.
    /// Furthermore, if you want to process the caught exceptions (e.g. for logging), you
    /// can assign the <paramref name="processException" /> delegate. If you want to cancel
    /// early, pass a corresponding <paramref name="cancellationToken" /> that times out
    /// after a certain amount of time.
    /// </para>
    /// </summary>
    /// <param name="connectionString">The connection string that identifies the target database.</param>
    /// <param name="retryCount">The number of retries this method will attempt to drop and create the database (optional). The default value is 3.</param>
    /// <param name="intervalBetweenRetriesInMilliseconds">
    /// The number of milliseconds the method will wait (using Task.Delay) after an exception has occurred (optional).
    /// The default value is 750ms.
    /// </param>
    /// <param name="processException">
    /// The delegate that is called when an exception occurred (optional).
    /// You would usually use this delegate to log the exception.
    /// </param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <returns>True when the database was dropped before creation, else false.</returns>
    /// <exception cref="KeyNotFoundException">Invalid key name within the connection string.</exception>
    /// <exception cref="FormatException">Invalid value within the connection string (specifically, when a Boolean or numeric value was expected but not supplied).</exception>
    /// <exception cref="ArgumentException">The supplied connectionString is not valid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="retryCount" /> is less than 0 or
    /// when <paramref name="intervalBetweenRetriesInMilliseconds" /> is less than or equal to 0.
    /// </exception>
    /// <exception cref="SqlException">Thrown when the connection to the master database fails or when the command fails to execute and the retry count exceeds.</exception>
    public static async Task<bool> DropAndCreateDatabaseAsync(string connectionString,
                                                              int retryCount = 3,
                                                              int intervalBetweenRetriesInMilliseconds = 750,
                                                              Action<SqlException>? processException = null,
                                                              CancellationToken cancellationToken = default)
    {
        var (defaultConnectionString, databaseName) = connectionString.PrepareDefaultConnectionAndDatabaseName();
#if NETSTANDARD2_0
        using var defaultConnection =
#else
        await using var defaultConnection =
#endif
            await OpenConnectionAsync(defaultConnectionString, cancellationToken);

        return await defaultConnection.DropAndCreateDatabaseAsync(databaseName,
                                                                  retryCount,
                                                                  intervalBetweenRetriesInMilliseconds,
                                                                  processException,
                                                                  cancellationToken);
    }

    private static (string connectionStringToMaster, string databaseName) PrepareDefaultConnectionAndDatabaseName(this string connectionString)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = connectionStringBuilder.InitialCatalog;
        connectionStringBuilder.InitialCatalog = string.Empty;
        return (connectionStringBuilder.ConnectionString, databaseName);
    }

    /// <summary>
    /// <para>
    /// Executes a T-SQL command (non-query) that kills all active connections to the database
    /// with the specified name. It is safe to run this command when the target database does
    /// not exist.
    /// </para>
    /// <para>
    /// Prefer <see cref="SetSingleUserAsync" /> over this method as the latter only terminates
    /// user sessions. SQL server system sessions might still be present and prevent e.g. dropping
    /// the database after this method returned.
    /// </para>
    /// </summary>
    /// <param name="openConnection">
    /// The SQL connection that will be used to execute the command.
    /// It must target a database that is fit to kill the connections
    /// of the target database and it must already be open.
    /// </param>
    /// <param name="databaseName">The name of the target database.</param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="openConnection" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseName" /> is the default instance.</exception>
    /// <exception cref="SqlException">Thrown when the command fails to execute.</exception>
    public static Task KillAllDatabaseConnectionsAsync(this SqlConnection openConnection,
                                                       DatabaseName databaseName,
                                                       CancellationToken cancellationToken = default)
    {
        openConnection.MustNotBeNull();
        databaseName.MustNotBeDefault();

        // This statement concatenates strings of the form "kill <session_id>;".
        var sql = $@"
DECLARE @kill varchar(1000) = '';
SELECT @kill = @kill + 'kill ' + CONVERT(varchar(5), session_id) + ';'
FROM sys.dm_exec_sessions
WHERE database_id = db_id('{databaseName.ToString()}') AND
      is_user_process = 1;

EXEC(@kill);";
        return openConnection.ExecuteNonQueryAsync(sql, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Executes a T-SQL command (non-query) that changes the state of a database to only allow
    /// a single user to be connected at the same time.
    /// </summary>
    /// <param name="openConnection">
    /// The SQL connection that will be used to execute the command.
    /// It must target a database that is fit to manipulate the state of
    /// the target database and it must already be open.
    /// </param>
    /// <param name="databaseName">The name of the target database.</param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="openConnection" /> is null.</exception>
    /// <exception cref="ArgumentDefaultException">Thrown when <paramref name="databaseName" /> is the default instance.</exception>
    /// <exception cref="SqlException">Thrown when the command fails to execute.</exception>
    public static Task SetSingleUserAsync(this SqlConnection openConnection,
                                          DatabaseName databaseName,
                                          CancellationToken cancellationToken = default)
    {
        openConnection.MustNotBeNull();
        databaseName.MustNotBeDefault();

        var sql = $"ALTER DATABASE {databaseName.Identifier} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;";
        return openConnection.ExecuteNonQueryAsync(sql, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// <para>
    /// Execute a T-SQL command (non-query) that creates a new database. If the database
    /// already exists, it will be dropped and recreated.
    /// </para>
    /// <para>
    /// This method implements an automatic retry-strategy. It tries for three times and
    /// waits for 750ms between each try. You can adjust the <paramref name="retryCount" />
    /// and <paramref name="intervalBetweenRetriesInMilliseconds" /> parameters to adjust this behavior.
    /// Furthermore, if you want to process the caught exceptions (e.g. for logging), you
    /// can assign the <paramref name="processException" /> delegate. If you want to cancel
    /// early, pass a corresponding <paramref name="cancellationToken" /> that times out
    /// after a certain amount of time.
    /// </para>
    /// </summary>
    /// <param name="openConnection">
    /// The SQL connection that will be used to execute the command.
    /// It must target a database that is fit to drop and create other databases and
    /// it must already be open.
    /// </param>
    /// <param name="databaseName">The name of the target database.</param>
    /// <param name="retryCount">The number of retries this method will attempt to drop and create the database (optional). The default value is 3.</param>
    /// <param name="intervalBetweenRetriesInMilliseconds">
    /// The number of milliseconds the method will wait (using Task.Delay) after an exception has occurred (optional).
    /// The default value is 750ms.
    /// </param>
    /// <param name="processException">
    /// The delegate that is called when an exception occurred (optional).
    /// You would usually use this delegate to log the exception.
    /// </param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <returns>True when the database was dropped before creation, else false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="openConnection" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseName" /> is the default instance.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="retryCount" /> is less than 0 or
    /// when <paramref name="intervalBetweenRetriesInMilliseconds" /> is less than or equal to 0.
    /// </exception>
    /// <exception cref="SqlException">Thrown when the command fails to execute, and the retry count exceeds.</exception>
    public static async Task<bool> DropAndCreateDatabaseAsync(this SqlConnection openConnection,
                                                              DatabaseName databaseName,
                                                              int retryCount = 3,
                                                              int intervalBetweenRetriesInMilliseconds = 750,
                                                              Action<SqlException>? processException = null,
                                                              CancellationToken cancellationToken = default)
    {
        openConnection.MustNotBeNull();
        databaseName.MustNotBeDefault();
        retryCount.MustBeGreaterThanOrEqualTo(0);
        intervalBetweenRetriesInMilliseconds.MustBeGreaterThan(0);

        var databaseIdentifier = databaseName.Identifier;
        var sql = $@"
DECLARE @DbId INT;

SELECT @DbId = DB_ID('{databaseName.ToString()}');

IF @DbId IS NOT NULL
BEGIN
    ALTER DATABASE {databaseIdentifier} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE {databaseIdentifier};    
END;

CREATE DATABASE {databaseIdentifier};

SELECT @DbId;";

        var numberOfTries = 1;
        while (true)
        {
            try
            {
                var result = await openConnection.ExecuteScalarAsync<int?>(sql, cancellationToken: cancellationToken);
                return result is not null;
            }
            catch (SqlException exception)
            {
                processException?.Invoke(exception);
                if (numberOfTries++ > retryCount)
                    throw;

                await Task.Delay(intervalBetweenRetriesInMilliseconds, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Executes a T-SQL command (non-query) that drops a database if it exists.
    /// </summary>
    /// <param name="openConnection">
    /// The SQL connection that will be used to execute the command.
    /// It must target a database that is fit to drop the target database and
    /// it must already be open.
    /// </param>
    /// <param name="databaseName">The name of the target database.</param>
    /// <param name="retryCount">The number of retries this method will attempt to drop the database (optional). The default value is 3.</param>
    /// <param name="intervalBetweenRetriesInMilliseconds">
    /// The number of milliseconds the method will wait (using Task.Delay) after an exception has occurred (optional).
    /// The default value is 750ms.
    /// </param>
    /// <param name="processException">
    /// The delegate that is called when an exception occurred (optional).
    /// You would usually use this delegate to log the exception.
    /// </param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <returns>True when the database was dropped, otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="openConnection" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseName" /> is the default instance.</exception>
    /// <exception cref="SqlException">Thrown when the command fails to execute, and the retry count exceeds.</exception>
    public static async Task<bool> TryDropDatabaseAsync(this SqlConnection openConnection,
                                                        DatabaseName databaseName,
                                                        int retryCount = 3,
                                                        int intervalBetweenRetriesInMilliseconds = 750,
                                                        Action<SqlException>? processException = null,
                                                        CancellationToken cancellationToken = default)
    {
        openConnection.MustNotBeNull();
        databaseName.MustNotBeDefault();
        retryCount.MustBeGreaterThanOrEqualTo(0);
        intervalBetweenRetriesInMilliseconds.MustBeGreaterThan(0);

        var databaseIdentifier = databaseName.Identifier;
        var sql = $@"
DECLARE @DbId INT;

SELECT @DbId = DB_ID('{databaseName.ToString()}');

IF @DbId IS NOT NULL
BEGIN
    ALTER DATABASE {databaseIdentifier} SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE {databaseIdentifier};    
END;

SELECT @DbId;";

        var numberOfTries = 1;
        while (true)
        {
            try
            {
                var result = await openConnection.ExecuteScalarAsync<int?>(sql, cancellationToken: cancellationToken);
                return result is not null;
            }
            catch (SqlException exception)
            {
                processException?.Invoke(exception);
                if (numberOfTries++ > retryCount)
                    throw;

                await Task.Delay(intervalBetweenRetriesInMilliseconds, cancellationToken);
            }
        }
    }

    /// <summary>
    /// <para>
    /// Executes a T-SQL command (non-query) that creates a database if it does not exist.
    /// </para>
    /// <para>
    /// This method implements an automatic retry-strategy. It tries for three times and
    /// waits for 750ms between each try. You can adjust the <paramref name="retryCount" />
    /// and <paramref name="intervalBetweenRetriesInMilliseconds" /> parameters to adjust this behavior.
    /// Furthermore, if you want to process the caught exceptions (e.g. for logging), you
    /// can assign the <paramref name="processException" /> delegate. If you want to cancel
    /// early, pass a corresponding <paramref name="cancellationToken" /> that times out
    /// after a certain amount of time.
    /// </para>
    /// </summary>
    /// <param name="openConnection">
    /// The SQL connection that will be used to execute the command.
    /// It must target a database that is fit to create the target database and
    /// it must already be open.
    /// </param>
    /// <param name="databaseName">The name of the target database.</param>
    /// <param name="retryCount">The number of retries this method will attempt to create the database (optional). The default value is 3.</param>
    /// <param name="intervalBetweenRetriesInMilliseconds">
    /// The number of milliseconds the method will wait (using Task.Delay) after an exception has occurred (optional).
    /// The default value is 750ms.
    /// </param>
    /// <param name="processException">
    /// The delegate that is called when an exception occurred (optional).
    /// You would usually use this delegate to log the exception.
    /// </param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <returns>True when the database was created, otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="openConnection" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseName" /> is the default instance.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="retryCount" /> is less than 0 or
    /// when <paramref name="intervalBetweenRetriesInMilliseconds" /> is less than or equal to 0.
    /// </exception>
    /// <exception cref="SqlException">Thrown when the command fails to execute, and the retry count exceeds.</exception>
    public static async Task<bool> TryCreateDatabaseAsync(this SqlConnection openConnection,
                                                          DatabaseName databaseName,
                                                          int retryCount = 3,
                                                          int intervalBetweenRetriesInMilliseconds = 750,
                                                          Action<SqlException>? processException = null,
                                                          CancellationToken cancellationToken = default)
    {
        openConnection.MustNotBeNull();
        databaseName.MustNotBeDefault();
        retryCount.MustBeGreaterThanOrEqualTo(0);
        intervalBetweenRetriesInMilliseconds.MustBeGreaterThan(0);

        var databaseIdentifier = databaseName.Identifier;
        var sql = $@"
DECLARE @DbId INT;

SELECT @DbId = DB_ID('{databaseName.ToString()}');

IF @DbId IS NULL
    CREATE DATABASE {databaseIdentifier};
SELECT @DbId;";

        var numberOfTries = 1;
        while (true)
        {
            try
            {
                var result = await openConnection.ExecuteScalarAsync<int?>(sql, cancellationToken: cancellationToken);
                return result is null;
            }
            catch (SqlException exception)
            {
                processException?.Invoke(exception);
                if (numberOfTries++ > retryCount)
                    throw;

                await Task.Delay(intervalBetweenRetriesInMilliseconds, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Executes a T-SQL query that checks if the database targeted by the specified connection
    /// string exists.
    /// </summary>
    /// <param name="connectionString">The connection string that identifies the target database.</param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <returns>True if the database exists, else false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="connectionString" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionString" /> contains no database/initial catalog.</exception>
    /// <exception cref="SqlException">Thrown when any I/O error occurs with SQL Server.</exception>
    public static async Task<bool> CheckIfDatabaseExistsAsync(string connectionString,
                                                              CancellationToken cancellationToken = default)
    {
        var (defaultConnection, databaseName) = PrepareDefaultConnectionAndDatabaseName(connectionString);
#if NETSTANDARD2_0
        using var connection =
#else
        await using var connection =
#endif
            await OpenConnectionAsync(defaultConnection, cancellationToken);

        return await connection.CheckIfDatabaseExistsAsync(databaseName, cancellationToken);
    }

    /// <summary>
    /// Executes a T-SQL query that checks if a database with the specified database name exists.
    /// </summary>
    /// <param name="openConnection">
    /// The SQL connection that will be used to execute the command.
    /// It must target a database that is fit to call the T-SQL DB_ID function
    /// with your specified database name.
    /// </param>
    /// <param name="databaseName">The name of the target database.</param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <returns>True if the database exists, else false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="openConnection" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="databaseName" /> is the default instance.</exception>
    /// <exception cref="SqlException">Thrown when any I/O error occurs with SQL Server.</exception>
    public static async Task<bool> CheckIfDatabaseExistsAsync(this SqlConnection openConnection,
                                                              DatabaseName databaseName,
                                                              CancellationToken cancellationToken = default)
    {
        openConnection.MustNotBeNull();
        databaseName.MustNotBeDefault();

        var sql = $"SELECT DB_ID('{databaseName.ToString()}');";
        var result = await openConnection.ExecuteScalarAsync<short?>(sql, cancellationToken: cancellationToken);
        return result is not null;
    }

    /// <summary>
    /// Creates a new <see cref="SqlConnection" /> and opens it asynchronously.
    /// </summary>
    /// <param name="connectionString">The connection string that identifies the target database.</param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionString" /> is invalid.</exception>
    /// <exception cref="SqlException">Thrown when the connection cannot be established properly to Microsoft SQL Server.</exception>
    public static async Task<SqlConnection> OpenConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}