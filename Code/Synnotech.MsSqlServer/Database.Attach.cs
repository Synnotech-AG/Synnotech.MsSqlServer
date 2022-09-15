using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using Light.GuardClauses.Exceptions;

namespace Synnotech.MsSqlServer;

public static partial class Database
{
    /// <summary>
    /// <para>
    /// Attaches a database to a SQL server. The <paramref name="connectionString" /> can either
    /// point to the master database of the SQL server (in this case the name of the new database will
    /// be retrieved from the <paramref name="databaseFilesInfo" /> structure), or directly to the
    /// database to be attached (in this case, the name in the connection string is preferred).
    /// </para>
    /// <para>
    /// This method will connect to the default database that is configured for the user on the target
    /// SQL server to do this - please ensure that the credentials in the connection string
    /// have enough privileges to perform this operation.
    /// </para>
    /// </summary>
    /// <param name="connectionString">The connection string that identifies the target database or the master database.</param>
    /// <param name="databaseFilesInfo">The information about the physical files of the database.</param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <exception cref="KeyNotFoundException">Invalid key name within the connection string.</exception>
    /// <exception cref="FormatException">Invalid value within the connection string (specifically, when a Boolean or numeric value was expected but not supplied).</exception>
    /// <exception cref="ArgumentException">The supplied connectionString is not valid.</exception>
    /// <exception cref="ArgumentDefaultException">Thrown when <paramref name="databaseFilesInfo" /> is the default instance.</exception>
    /// <exception cref="SqlException">Thrown when the connection to the master database cannot be established or when any SQL command fails.</exception>
    public static async Task AttachDatabaseAsync(string connectionString,
                                                 DatabasePhysicalFilesInfo databaseFilesInfo,
                                                 CancellationToken cancellationToken = default)
    {
        databaseFilesInfo.MustNotBeDefault();

        var (defaultConnectionString, databaseNameFromConnectionString) = PrepareDefaultConnectionAndDatabaseName(connectionString);
        var databaseName = SelectDatabaseName(databaseNameFromConnectionString, databaseFilesInfo.DatabaseName);

#if NETSTANDARD2_0
        using var defaultConnection =
#else
        await using var defaultConnection =
#endif
            await OpenConnectionAsync(defaultConnectionString, cancellationToken);

        await defaultConnection.AttachDatabaseAsync(databaseName, databaseFilesInfo.Files, cancellationToken);
    }

    /// <summary>
    /// Attaches a database to a SQL server.
    /// </summary>
    /// <param name="connectionToMaster">The SQL connection that is connected to the master (or default) database of the SQL server. This connection must already be opened.</param>
    /// <param name="databaseName">The name of the database that should be attached.</param>
    /// <param name="databaseFiles">The physical paths to the database files that will be used to attach the database.</param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="connectionToMaster" /> or <paramref name="databaseFiles" /> are null.</exception>
    /// <exception cref="ArgumentDefaultException">Thrown when <paramref name="databaseName" /> is the default instance.</exception>
    /// <exception cref="EmptyCollectionException">Thrown when <paramref name="databaseFiles" /> contains no element.</exception>
    /// <exception cref="SqlException">Thrown when the connection to the master database is not open or when any SQL command fails.</exception>
    public static Task AttachDatabaseAsync(this SqlConnection connectionToMaster,
                                           DatabaseName databaseName,
                                           List<DatabaseFileInfo> databaseFiles,
                                           CancellationToken cancellationToken = default)
    {
        connectionToMaster.MustNotBeNull();
        databaseName.MustNotBeDefault();
        databaseFiles.MustNotBeNullOrEmpty();

        var sql = CreateAttachDatabaseStatement(databaseName, databaseFiles);
        return connectionToMaster.ExecuteNonQueryAsync(sql, cancellationToken: cancellationToken);
    }

    private static string CreateAttachDatabaseStatement(DatabaseName databaseName, List<DatabaseFileInfo> databaseFiles)
    {
        var stringBuilder = new StringBuilder().Append("CREATE DATABASE ")
                                               .Append(databaseName.Identifier)
                                               .AppendLine(" ON");
        for (var i = 0; i < databaseFiles.Count; i++)
        {
            var fileInfo = databaseFiles[i];
            stringBuilder.Append("    (FILENAME = N'")
                         .Append(fileInfo.PhysicalFilePath)
                         .Append("')");
            if (i < databaseFiles.Count - 1)
                stringBuilder.Append(',');

            stringBuilder.AppendLine();
        }

        stringBuilder.AppendLine("FOR ATTACH;");

        return stringBuilder.ToString();
    }

    private static DatabaseName SelectDatabaseName(string nameFromConnectionString, DatabaseName nameFromDatabaseFiles)
    {
        if ("master".Equals(nameFromConnectionString, StringComparison.OrdinalIgnoreCase) || nameFromConnectionString.IsNullOrWhiteSpace())
            return nameFromDatabaseFiles;

        return nameFromConnectionString;
    }
}