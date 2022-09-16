using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Synnotech.Core.Initialization;
using Synnotech.DatabaseAbstractions;

namespace Synnotech.MsSqlServer;

/// <summary>
/// <para>
/// Represents an asynchronous session to MS SQL Server via a SqlConnection.
/// </para>
/// <para>
/// This session wraps a transaction which is started by calling <see cref="IInitializeAsync.InitializeAsync" /> (this is usually done
/// when you instantiate the session via <see cref="ISessionFactory{TSessionAbstraction}" />).
/// </para>
/// <para>
/// Calling <see cref="SaveChangesAsync" /> will commit the transaction.
/// Disposing the session will implicitly roll-back the transaction if SaveChangesAsync was not called beforehand.
/// </para>
/// <para>
/// BEWARE: you must not derive from this class and introduce other references to disposable objects.
/// Only the SqlConnection and the transaction will be disposed.
/// </para>
/// </summary>
public abstract class AsyncSession : AsyncReadOnlySession, IAsyncSession
{
    /// <summary>
    /// Initializes a new instance of <see cref="AsyncSession" />.
    /// </summary>
    /// <param name="sqlConnection">The SqlConnection used for database access.</param>
    /// <param name="transactionLevel">
    /// The isolation level for the transaction (optional). The default value is <see cref="IsolationLevel.Serializable" />.
    /// When this value is set to <see cref="IsolationLevel.Unspecified" />, no transaction will be started.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sqlConnection" /> is null.</exception>
    protected AsyncSession(SqlConnection sqlConnection, IsolationLevel transactionLevel = IsolationLevel.Serializable)
        : base(sqlConnection, transactionLevel) { }

    /// <summary>
    /// Commits the underlying transaction if possible.
    /// </summary>
#if NETSTANDARD2_0
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Transaction?.Commit();
        return Task.CompletedTask;
    }
#else
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Transaction != null ? Transaction.CommitAsync(cancellationToken) : Task.CompletedTask;
#endif
}