using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using Synnotech.Core.Initialization;
using Synnotech.DatabaseAbstractions;

namespace Synnotech.MsSqlServer;

/// <summary>
/// <para>
/// Represents an asynchronous database session to MS SQL Server via SqlConnection. This session
/// is only used to read data (i.e. no data is inserted, updated, or deleted), thus SaveChangesAsync
/// is not available. No transaction is needed while this session is active (but you can optionally create one).
/// </para>
/// <para>
/// The connection is opened and the optional transaction is started by calling
/// <see cref="IInitializeAsync.InitializeAsync" /> (this is usually done when you instantiate the session
/// via <see cref="ISessionFactory{TSessionAbstraction}" />).
/// </para>
/// <para>
/// Disposing the session will implicitly roll-back the transaction if SaveChangesAsync was not called beforehand.
/// </para>
/// <para>
/// Beware: you must not derive from this class and introduce other references to disposable objects.
/// Only the <see cref="SqlConnection" /> and the optional <see cref="Transaction" /> will be disposed.
/// </para>
/// </summary>
public abstract class AsyncReadOnlySession : IAsyncReadOnlySession, IInitializeAsync
{
    /// <summary>
    /// Initializes a new instance of <see cref="AsyncReadOnlySession" />.
    /// </summary>
    /// <param name="sqlConnection">The SqlConnection used for database access.</param>
    /// <param name="transactionLevel">
    /// The isolation level for the transaction (optional). The default value is <see cref="IsolationLevel.Unspecified" />.
    /// When this value is set to <see cref="IsolationLevel.Unspecified" />, no transaction will be started.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sqlConnection" /> is null.</exception>
    protected AsyncReadOnlySession(SqlConnection sqlConnection, IsolationLevel transactionLevel = IsolationLevel.Unspecified)
    {
        SqlConnection = sqlConnection.MustNotBeNull(nameof(sqlConnection));
        TransactionLevel = transactionLevel;
    }

    /// <summary>
    /// Gets the SqlConnection.
    /// </summary>
    protected SqlConnection SqlConnection { get; }

    /// <summary>
    /// Gets the isolation level of the transaction.
    /// </summary>
    protected IsolationLevel TransactionLevel { get; }

    /// <summary>
    /// Gets the transaction that was initialized in InitializeAsync.
    /// </summary>
    protected SqlTransaction? Transaction { get; private set; }

    /// <summary>
    /// Disposes of the transaction and the SqlConnection.
    /// </summary>
    public void Dispose()
    {
        Transaction?.Dispose();
        SqlConnection.Dispose();
    }

    /// <summary>
    /// Disposes of the transaction and the SqlConnection.
    /// </summary>
#if NETSTANDARD2_0
    public ValueTask DisposeAsync()
    {
        Dispose();
        return default;
    }
#else
        public async ValueTask DisposeAsync()
        {
            if (Transaction != null)
                await Transaction.DisposeAsync();
            await SqlConnection.DisposeAsync();
        }
#endif

    bool IInitializeAsync.IsInitialized => (int) SqlConnection.State > 0 && (TransactionLevel == IsolationLevel.Unspecified || Transaction != null);

    async Task IInitializeAsync.InitializeAsync(CancellationToken cancellationToken)
    {
        await SqlConnection.OpenAsync(cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        if (TransactionLevel != IsolationLevel.Unspecified)
            Transaction = SqlConnection.BeginTransaction(TransactionLevel);
    }

    /// <summary>
    /// Creates a command and attaches the current transaction to it if possible.
    /// </summary>
    protected SqlCommand CreateCommand()
    {
        var sqlCommand = SqlConnection.CreateCommand();
        if (Transaction != null)
            sqlCommand.Transaction = Transaction;
        return sqlCommand;
    }
}