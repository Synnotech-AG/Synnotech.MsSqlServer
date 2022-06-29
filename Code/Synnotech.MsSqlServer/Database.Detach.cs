using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;

namespace Synnotech.MsSqlServer;

public static partial class Database
{
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
    public static async Task<DatabasePhysicalFilesInfo> DetachDatabaseAsync(string connectionString,
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

        await masterConnection.SetSingleUserAsync(databaseName, cancellationToken);
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
    /// about the physical file locations of the database.
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

        var sql = $@"EXEC sp_detach_db N'{databaseName}', 'true';";

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