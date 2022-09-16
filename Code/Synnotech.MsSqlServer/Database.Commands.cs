using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;

namespace Synnotech.MsSqlServer;

public static partial class Database
{
    /// <summary>
    /// Executes the specified SQL against the database that is targeted by the connection string.
    /// The underlying SQL command will be called with ExecuteNonQueryAsync.
    /// You can use the optional delegate to configure the command (to e.g. provide parameters).
    /// </summary>
    /// <param name="connectionString">The connection string that identifies the target database.</param>
    /// <param name="sql">The SQL statements that should be executed against the database.</param>
    /// <param name="configureCommand">
    /// The delegate that allows you to further configure the SQL command (optional).
    /// You will probably want to use this to add parameters to the command.
    /// </param>
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
        connectionString.MustNotBeNullOrWhiteSpace();
        sql.MustNotBeNullOrWhiteSpace();

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
    /// <param name="openConnection">The SQL connection to the target database. It must already be open.</param>
    /// <param name="sql">The SQL statement that should be executed.</param>
    /// <param name="configureCommand">The delegate that allows you to further configure the SQL command (optional). You will probably want to use this to add parameters to the command.</param>
    /// <param name="transactionLevel">
    /// The value indicating whether the command is executed within a transaction (optional).
    /// If the value is not null, a transaction with the specified level will be created.
    /// </param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="openConnection" /> or <paramref name="sql" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sql" /> is an empty string or contains only white space.</exception>
    /// <exception cref="SqlException">Thrown when any I/O errors with MS SQL Server occur.</exception>
    public static Task<int> ExecuteNonQueryAsync(this SqlConnection openConnection,
                                                 string sql,
                                                 Action<SqlCommand>? configureCommand = null,
                                                 IsolationLevel? transactionLevel = null,
                                                 CancellationToken cancellationToken = default)
    {
        openConnection.MustNotBeNull();
        sql.MustNotBeNullOrWhiteSpace();

        var command = openConnection.CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;
        configureCommand?.Invoke(command);

        return transactionLevel == null ?
                   command.ExecuteNonQueryAndDisposeAsync(cancellationToken) :
                   openConnection.ExecuteNonQueryWithTransactionAsync(command, transactionLevel.Value, cancellationToken);
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
    /// Executes the specified SQL against the database that is targeted by the connection string.
    /// The underlying SQL command will be called with ExecuteScalarAsync.
    /// You can use the optional delegate to configure the command (to e.g. provide parameters).
    /// </summary>
    /// <param name="connectionString">The connection string that identifies the target database.</param>
    /// <param name="sql">The SQL statements that should be executed against the database.</param>
    /// <param name="configureCommand">
    /// The delegate that allows you to further configure the SQL command (optional).
    /// You will probably want to use this to add parameters to the command.
    /// </param>
    /// <param name="transactionLevel">
    /// The value indicating whether the command is executed within a transaction (optional).
    /// If the value is not null, a transaction with the specified level will be created.
    /// </param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <typeparam name="T">The type that the scalar result should be cast to.</typeparam>
    /// <returns>The scalar return</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionString"/> or <paramref name="sql"/> are null.</exception>
    /// <exception cref="ArgumentException">Thrown when the <paramref name="connectionString" /> is invalid.</exception>
    /// <exception cref="SqlException">Thrown when any I/O errors with MS SQL Server occur.</exception>
    public static async Task<T> ExecuteScalarAsync<T>(string connectionString,
                                                      string sql,
                                                      Action<SqlCommand>? configureCommand = null,
                                                      IsolationLevel? transactionLevel = null,
                                                      CancellationToken cancellationToken = default)
    {
#if NETSTANDARD2_0
        using var connection =
#else
        await using var connection =
#endif
            await OpenConnectionAsync(connectionString, cancellationToken);

        return await connection.ExecuteScalarAsync<T>(sql, configureCommand, transactionLevel, cancellationToken);
    }

    /// <summary>
    /// Executes the specified SQL against the database that is targeted by the connection string.
    /// You can use the optional delegate to configure the command (to e.g. provide parameters).
    /// </summary>
    /// <param name="openConnection">The SQL connection to the target database. This connection must already be open.</param>
    /// <param name="sql">The SQL statement that should be executed.</param>
    /// <param name="configureCommand">
    /// The delegate that allows you to further configure the SQL command (optional).
    /// You will probably want to use this to add parameters to the command.</param>
    /// <param name="transactionLevel">
    /// The value indicating whether the command is executed within a transaction (optional).
    /// If the value is not null, a transaction with the specified level will be created.
    /// </param>
    /// <param name="cancellationToken">The cancellation instruction (optional).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="openConnection" /> or <paramref name="sql" /> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sql" /> is an empty string or contains only white space.</exception>
    /// <exception cref="SqlException">Thrown when any I/O errors with MS SQL Server occur.</exception>
    public static Task<T> ExecuteScalarAsync<T>(this SqlConnection openConnection,
                                                string sql,
                                                Action<SqlCommand>? configureCommand = null,
                                                IsolationLevel? transactionLevel = null,
                                                CancellationToken cancellationToken = default)
    {
        openConnection.MustNotBeNull();
        sql.MustNotBeNullOrWhiteSpace();

        var command = openConnection.CreateCommand();
        command.CommandText = sql;
        configureCommand?.Invoke(command);

        return transactionLevel is null ?
                   command.ExecuteScalarAndDisposeAsync<T>(cancellationToken) :
                   openConnection.ExecuteScalarCommandWithTransactionAndDisposeAsync<T>(command, cancellationToken);
    }

    private static async Task<T> ExecuteScalarAndDisposeAsync<T>(this SqlCommand command, CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        using (command)
#else
        await using (command)
#endif
        {
            return (T) await command.ExecuteScalarAsync(cancellationToken);
        }
    }

    private static async Task<T> ExecuteScalarCommandWithTransactionAndDisposeAsync<T>(this SqlConnection connection, SqlCommand command, CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        using var transaction =
#else
        await using var transaction =
#endif
            connection.BeginTransaction();

        command.Transaction = transaction;

#if NETSTANDARD2_0
        using (command)
#else
        await using (command)
#endif
        {
            return (T) await command.ExecuteScalarAsync(cancellationToken);
        }
    }
}