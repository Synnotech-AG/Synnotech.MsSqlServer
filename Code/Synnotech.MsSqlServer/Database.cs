using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;

namespace Synnotech.MsSqlServer;

/// <summary>
/// Provides helper methods for creating and dropping MS SQL databases.
/// </summary>
public static class Database
{
    /// <summary>
    /// <para>
    /// Tries to create the database the specified connection string points to. If
    /// the target database already exists, nothing will be done.
    /// This method will connect to the "master" database of the target
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
        var (connectionStringToMaster, databaseName) = connectionString.PrepareMasterConnectionAndDatabaseName();

#if NETSTANDARD2_0
        using var connectionToMaster =
#else
        await using var connectionToMaster =
#endif
            await OpenConnectionAsync(connectionStringToMaster, cancellationToken);

        return await connectionToMaster.TryCreateDatabaseAsync(databaseName,
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
    /// This method will connect to the "master" database of the target
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
        var (connectionStringToMaster, databaseName) = connectionString.PrepareMasterConnectionAndDatabaseName();
#if NETSTANDARD2_0
        using var connectionToMaster =
#else
        await using var connectionToMaster =
#endif
            await OpenConnectionAsync(connectionStringToMaster, cancellationToken);

        await connectionToMaster.KillAllDatabaseConnectionsAsync(databaseName, cancellationToken);
        return await connectionToMaster.TryDropDatabaseAsync(databaseName,
                                                             retryCount,
                                                             intervalBetweenRetriesInMilliseconds,
                                                             processException,
                                                             cancellationToken);
    }

    /// <summary>
    /// <para>
    /// Creates the database for the specified connection string. If it already exists, the
    /// database will be dropped and recreated. Connections to the existing database
    /// will be terminated. This method will connect to the "master" database of the target
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
    /// <exception cref="KeyNotFoundException">Invalid key name within the connection string.</exception>
    /// <exception cref="FormatException">Invalid value within the connection string (specifically, when a Boolean or numeric value was expected but not supplied).</exception>
    /// <exception cref="ArgumentException">The supplied connectionString is not valid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="retryCount" /> is less than 0 or
    /// when <paramref name="intervalBetweenRetriesInMilliseconds" /> is less than or equal to 0.
    /// </exception>
    /// <exception cref="SqlException">Thrown when the connection to the master database fails or when the command fails to execute and the retry count exceeds.</exception>
    public static async Task DropAndCreateDatabaseAsync(string connectionString,
                                                        int retryCount = 3,
                                                        int intervalBetweenRetriesInMilliseconds = 750,
                                                        Action<SqlException>? processException = null,
                                                        CancellationToken cancellationToken = default)
    {
        var (connectionStringToMaster, databaseName) = connectionString.PrepareMasterConnectionAndDatabaseName();
#if NETSTANDARD2_0
        using var connectionToMaster =
#else
        await using var connectionToMaster =
#endif
            await OpenConnectionAsync(connectionStringToMaster, cancellationToken);

        await connectionToMaster.KillAllDatabaseConnectionsAsync(databaseName, cancellationToken);
        await connectionToMaster.DropAndCreateDatabaseAsync(databaseName, retryCount, intervalBetweenRetriesInMilliseconds, processException, cancellationToken);
    }

    private static (string connectionStringToMaster, string databaseName) PrepareMasterConnectionAndDatabaseName(this string connectionString)
    {
        var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
        string databaseName = connectionStringBuilder.InitialCatalog;
        connectionStringBuilder.InitialCatalog = "master";
        return (connectionStringBuilder.ConnectionString, databaseName);
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionToMaster" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseName" /> is the default instance.</exception>
    /// <exception cref="SqlException">Thrown when the command fails to execute.</exception>
    public static Task KillAllDatabaseConnectionsAsync(this SqlConnection connectionToMaster, DatabaseName databaseName, CancellationToken cancellationToken = default)
    {
        connectionToMaster.MustNotBeNull(nameof(connectionToMaster));
        databaseName.MustNotBeDefault(nameof(databaseName));

        // This statement concatenates strings of the form "kill <session_id>;".
        var sql = $@"
DECLARE @kill varchar(1000) = '';
SELECT @kill = @kill + 'kill ' + CONVERT(varchar(5), session_id) + ';'
FROM sys.dm_exec_sessions
WHERE database_id = db_id('{databaseName}') AND
      is_user_process = 1;

EXEC(@kill);";
        return connectionToMaster.ExecuteNonQueryAsync(sql, cancellationToken: cancellationToken);
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
    /// <param name="connectionToMaster">
    /// The SQL connection that will be used to execute the command.
    /// It must target the master database of a SQL server.
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionToMaster" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseName" /> is the default instance.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="retryCount" /> is less than 0 or
    /// when <paramref name="intervalBetweenRetriesInMilliseconds" /> is less than or equal to 0.
    /// </exception>
    /// <exception cref="SqlException">Thrown when the command fails to execute, and the retry count exceeds.</exception>
    public static async Task DropAndCreateDatabaseAsync(this SqlConnection connectionToMaster,
                                                        DatabaseName databaseName,
                                                        int retryCount = 3,
                                                        int intervalBetweenRetriesInMilliseconds = 750,
                                                        Action<SqlException>? processException = null,
                                                        CancellationToken cancellationToken = default)
    {
        connectionToMaster.MustNotBeNull();
        databaseName.MustNotBeDefault();
        retryCount.MustBeGreaterThanOrEqualTo(0);
        intervalBetweenRetriesInMilliseconds.MustBeGreaterThan(0);

        // Issue #5: https://github.com/Synnotech-AG/Synnotech.MsSqlServer/issues/5
        // One user reported that a system process was attached to a database whose connections were killed.
        // These system process sessions cannot be killed. I introduced a retry strategy simply in the hope
        // of the system process disconnecting quickly enough so that a subsequent call to DROP Database and
        // CREATE DATABASE will succeed.
        var databaseIdentifier = databaseName.Identifier;
        var sql = $@"
IF DB_ID('{databaseName}') IS NOT NULL
DROP DATABASE {databaseIdentifier};

CREATE DATABASE {databaseIdentifier};";

        var numberOfTries = 1;
        while (true)
        {
            try
            {
                await connectionToMaster.ExecuteNonQueryAsync(sql, cancellationToken: cancellationToken);
                return;
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
    /// <param name="connectionToMaster">
    /// The SQL connection that will be used to execute the command.
    /// It must target the master database of a SQL server.
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionToMaster" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseName" /> is the default instance.</exception>
    /// <exception cref="SqlException">Thrown when the command fails to execute, and the retry count exceeds.</exception>
    public static async Task<bool> TryDropDatabaseAsync(this SqlConnection connectionToMaster,
                                                        DatabaseName databaseName,
                                                        int retryCount = 3,
                                                        int intervalBetweenRetriesInMilliseconds = 750,
                                                        Action<SqlException>? processException = null,
                                                        CancellationToken cancellationToken = default)
    {
        connectionToMaster.MustNotBeNull();
        databaseName.MustNotBeDefault();
        retryCount.MustBeGreaterThanOrEqualTo(0);
        intervalBetweenRetriesInMilliseconds.MustBeGreaterThan(0);

        var databaseIdentifier = databaseName.Identifier;
        var sql = $@"
IF DB_ID('{databaseName}') IS NOT NULL
DROP DATABASE {databaseIdentifier};
";

        var numberOfTries = 1;
        while (true)
        {
            try
            {
                var result = await connectionToMaster.ExecuteNonQueryAsync(sql, cancellationToken: cancellationToken);
                return result == 1;
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
    /// <param name="connectionToMaster">
    /// The SQL connection that will be used to execute the command.
    /// It must target the master database of a SQL server.
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionToMaster" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="databaseName" /> is the default instance.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="retryCount" /> is less than 0 or
    /// when <paramref name="intervalBetweenRetriesInMilliseconds" /> is less than or equal to 0.
    /// </exception>
    /// <exception cref="SqlException">Thrown when the command fails to execute, and the retry count exceeds.</exception>
    public static async Task<bool> TryCreateDatabaseAsync(this SqlConnection connectionToMaster,
                                                          DatabaseName databaseName,
                                                          int retryCount = 3,
                                                          int intervalBetweenRetriesInMilliseconds = 750,
                                                          Action<SqlException>? processException = null,
                                                          CancellationToken cancellationToken = default)
    {
        connectionToMaster.MustNotBeNull(nameof(connectionToMaster));
        databaseName.MustNotBeDefault(nameof(databaseName));
        retryCount.MustBeGreaterThanOrEqualTo(0);
        intervalBetweenRetriesInMilliseconds.MustBeGreaterThan(0);

        var databaseIdentifier = databaseName.Identifier;
        var sql = $@"
IF DB_ID('{databaseName}') IS NULL
CREATE DATABASE {databaseIdentifier};
";

        var numberOfTries = 1;
        while (true)
        {
            try
            {
                var result = await connectionToMaster.ExecuteNonQueryAsync(sql, cancellationToken: cancellationToken);
                return result == 1;
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
    /// Executes the specified SQL against the database that is targeted by the connection string.
    /// The underlying SQL command will be called with ExecuteNonQueryAsync.
    /// You can use the optional delegate to configure the command (to e.g. provide parameters).
    /// </summary>
    /// <param name="connectionString">The connection string that identifies the target database.</param>
    /// <param name="sql">The SQL statements that should be executed against the database.</param>
    /// <param name="configureCommand">The delegate that allows you to further configure the SQL command (optional). You will probably want to use this to add parameters to the command.</param>
    /// <param name="transactionLevel">
    /// The value indicating whether the command is executed within a transaction (optional).
    /// If the value is not null, a transaction with the specified level will be created.
    /// </param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionString" /> or <paramref name="sql" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when either <paramref name="connectionString" /> or <paramref name="sql" /> is an empty string or contains only white space.</exception>
    /// <exception cref="SqlException">Thrown when any I/O errors with MS SQL Server occur.</exception>
    public static async Task<int> ExecuteNonQueryAsync(string connectionString,
                                                       string sql,
                                                       Action<SqlCommand>? configureCommand = null,
                                                       IsolationLevel? transactionLevel = null,
                                                       CancellationToken cancellationToken = default)
    {
        connectionString.MustNotBeNullOrWhiteSpace(nameof(connectionString));
        sql.MustNotBeNullOrWhiteSpace(nameof(sql));

#if NETSTANDARD2_0
        using var connection =
#else
        await using var connection =
#endif
            await OpenConnectionAsync(connectionString, cancellationToken);

        return await connection.ExecuteNonQueryAsync(sql, configureCommand, transactionLevel, cancellationToken);
    }

    /// <summary>
    /// Executes the specified SQL against the database that is targeted by the connection string.
    /// You can use the optional delegate to configure the command (to e.g. provide parameters).
    /// </summary>
    /// <param name="connection">The SQL connection to the target database.</param>
    /// <param name="sql">The SQL statement that should be executed.</param>
    /// <param name="configureCommand">The delegate that allows you to further configure the SQL command (optional). You will probably want to use this to add parameters to the command.</param>
    /// <param name="transactionLevel">
    /// The value indicating whether the command is executed within a transaction (optional).
    /// If the value is not null, a transaction with the specified level will be created.
    /// </param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection" /> or <paramref name="sql" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sql" /> is an empty string or contains only white space.</exception>
    /// <exception cref="SqlException">Thrown when any I/O errors with MS SQL Server occur.</exception>
    public static Task<int> ExecuteNonQueryAsync(this SqlConnection connection,
                                                 string sql,
                                                 Action<SqlCommand>? configureCommand = null,
                                                 IsolationLevel? transactionLevel = null,
                                                 CancellationToken cancellationToken = default)
    {
        connection.MustNotBeNull(nameof(connection));
        sql.MustNotBeNullOrWhiteSpace(nameof(sql));

        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        configureCommand?.Invoke(command);

        return transactionLevel == null ?
                   command.ExecuteNonQueryAndDisposeAsync(cancellationToken) :
                   connection.ExecuteNonQueryWithTransactionAsync(command, transactionLevel.Value, cancellationToken);
    }

    private static async Task<int> ExecuteNonQueryAndDisposeAsync(this SqlCommand command, CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        using (command)
#else
        await using (command)
#endif
        {
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<int> ExecuteNonQueryWithTransactionAsync(this SqlConnection connection,
                                                                       SqlCommand command,
                                                                       IsolationLevel transactionLevel,
                                                                       CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        using var transaction = connection.BeginTransaction(transactionLevel);
#else
        await using var transaction = (SqlTransaction) await connection.BeginTransactionAsync(transactionLevel, cancellationToken);
#endif
        command.Transaction = transaction;
        return await command.ExecuteNonQueryAndDisposeAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a new <see cref="SqlConnection" /> and opens it asynchronously.
    /// </summary>
    /// <param name="connectionString">The connection string that identifies the target database.</param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString" /> is invalid.</exception>
    /// <exception cref="SqlException">Thrown when the connection cannot be established properly to Microsoft SQL Server.</exception>
    public static async Task<SqlConnection> OpenConnectionAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    /// <summary>
    /// <para>
    /// Detaches the database specified in the connection string and returns you information
    /// about the physical file locations of the database.
    /// </para>
    /// <para>
    /// This method will connect to the "master" database of the target
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
    /// <param name="retryCount">The number of retries this method will attempt to detach the database (optional). The default value is 3.</param>
    /// <param name="intervalBetweenRetriesInMilliseconds">
    /// The number of milliseconds the method will wait (using Task.Delay) after an exception has occurred (optional).
    /// The default value is 750ms.
    /// </param>
    /// <param name="processException">
    /// The delegate that is called when an exception occurred (optional).
    /// You would usually use this delegate to log the exception.
    /// </param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <returns>A struct containing information about the database name and paths to the MDF, LDF and other files that belong to the database.</returns>
    /// <exception cref="KeyNotFoundException">Invalid key name within the connection string.</exception>
    /// <exception cref="FormatException">Invalid value within the connection string (specifically, when a Boolean or numeric value was expected but not supplied).</exception>
    /// <exception cref="ArgumentException">The supplied connectionString is not valid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="retryCount" /> is less than 0 or
    /// when <paramref name="intervalBetweenRetriesInMilliseconds" /> is less than or equal to 0.
    /// </exception>
    /// <exception cref="SqlException">Thrown when the command fails to execute, and the retry count exceeds.</exception>
    public static async Task<DatabasePhysicalFilesInfo> DetachDatabaseAsync(this string connectionString,
                                                                            int retryCount = 3,
                                                                            int intervalBetweenRetriesInMilliseconds = 750,
                                                                            Action<SqlException>? processException = null,
                                                                            CancellationToken cancellationToken = default)
    {
        var (masterConnectionString, databaseName) = PrepareMasterConnectionAndDatabaseName(connectionString);

        var info = await GetPhysicalFilesInfoAsync(connectionString, databaseName, cancellationToken);

#if NETSTANDARD2_0
        using var masterConnection =
#else
        await using var masterConnection =
#endif
            await OpenConnectionAsync(masterConnectionString, cancellationToken);

        await masterConnection.KillAllDatabaseConnectionsAsync(databaseName, cancellationToken);
        await masterConnection.DetachDatabaseAsync(databaseName,
                                                   retryCount,
                                                   intervalBetweenRetriesInMilliseconds,
                                                   processException,
                                                   cancellationToken);
        return info;
    }

    /// <summary>
    /// <para>
    /// Detaches the database specified in the connection string and returns you information
    /// about the physical file locations of the database. This method will only work on
    /// databases that have a single MDF and a single LDF file (which is the default for SQL Server
    /// database). If you have a database with several data and/or log files, this method will throw.
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
    /// <param name="connectionToMaster">The SQL connection that is connected to the master database of the SQL server. This connection must already be opened.</param>
    /// <param name="databaseName">The name of the database that should be detached.</param>
    /// <param name="retryCount">The number of retries this method will attempt to detach the database (optional). The default value is 3.</param>
    /// <param name="intervalBetweenRetriesInMilliseconds">
    /// The number of milliseconds the method will wait (using Task.Delay) after an exception has occurred (optional).
    /// The default value is 750ms.
    /// </param>
    /// <param name="processException">
    /// The delegate that is called when an exception occurred (optional).
    /// You would usually use this delegate to log the exception.
    /// </param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <returns>A struct containing information about the database name and paths to the MDF and LDF file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the connection is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the database name is the default instance of the <see cref="DatabaseName" /> struct.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="retryCount" /> is less than 0 or
    /// when <paramref name="intervalBetweenRetriesInMilliseconds" /> is less than or equal to 0.
    /// </exception>
    /// <exception cref="SqlException">Thrown when the connection is not open or the command fails to execute, and the retry count exceeds.</exception>
    public static async Task DetachDatabaseAsync(this SqlConnection connectionToMaster,
                                                 DatabaseName databaseName,
                                                 int retryCount = 3,
                                                 int intervalBetweenRetriesInMilliseconds = 750,
                                                 Action<SqlException>? processException = null,
                                                 CancellationToken cancellationToken = default)
    {
        connectionToMaster.MustNotBeNull();
        databaseName.MustNotBeDefault();
        retryCount.MustBeGreaterThanOrEqualTo(0);
        intervalBetweenRetriesInMilliseconds.MustBeGreaterThan(0);

        var sql = $@"EXEC sp_detach_db N'{databaseName}', 'true'";

        var numberOfTries = 1;
        while (true)
        {
            try
            {
                var result = await connectionToMaster.ExecuteNonQueryAsync(sql, cancellationToken: cancellationToken);
                Debug.Assert(result == 0, "sp_detach_db returns 0 on success.");
                return;
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
    /// Returns information about the physical files of a database.
    /// </summary>
    /// <param name="connectionString">The connection string that identifies the target database.</param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <returns>A struct containing information about the database name and paths to the MDF, LDF and other files that belong to the database.</returns>
    /// <exception cref="KeyNotFoundException">Invalid key name within the connection string.</exception>
    /// <exception cref="FormatException">Invalid value within the connection string (specifically, when a Boolean or numeric value was expected but not supplied).</exception>
    /// <exception cref="ArgumentException">The supplied connectionString is not valid.</exception>
    /// <exception cref="SqlException">Thrown when the command to retrieve the physical file names fails.</exception>
    public static Task<DatabasePhysicalFilesInfo> GetPhysicalFilesInfoAsync(this string connectionString,
                                                                            CancellationToken cancellationToken = default)
    {
        var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(connectionString);
        DatabaseName databaseName = sqlConnectionStringBuilder.InitialCatalog;
        return connectionString.GetPhysicalFilesInfoAsync(databaseName, cancellationToken);
    }

    private static async Task<DatabasePhysicalFilesInfo> GetPhysicalFilesInfoAsync(this string connectionString,
                                                                                   DatabaseName databaseName,
                                                                                   CancellationToken cancellationToken = default)
    {
        #if NETSTANDARD2_0
            using var connection = 
        #else
        await using var connection =
#endif
            await OpenConnectionAsync(connectionString, cancellationToken);

#if NETSTANDARD2_0
        using var command =
#else
        await using var command = 
#endif
            connection.CreateCommand();
        command.CommandText = @"
SELECT type_desc Type,
       physical_name PhysicalFilePath
FROM sys.database_files;";

#if NETSTANDARD2_0
        using var reader =
#else
        await using var reader =
#endif
            await command.ExecuteReaderAsync(CommandBehavior.SingleResult, cancellationToken);

        var databaseFiles = new List<DatabaseFileInfo>(2);
        while (await reader.ReadAsync(cancellationToken))
        {
            var type = reader.GetString(0);
            var filePath = reader.GetString(1);

            var fileInfo = new DatabaseFileInfo(type, filePath);
            databaseFiles.Add(fileInfo);
        }

        return new DatabasePhysicalFilesInfo(databaseName, databaseFiles);
    }
}