using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Light.GuardClauses;
using Synnotech.Core.Initialization;
using Synnotech.DatabaseAbstractions;

namespace Synnotech.MsSqlServer;

/// <summary>
/// Represents a factory that instantiates a session and optionally initializes it
/// in an asynchronous fashion when the session implements <see cref="IInitializeAsync" />.
/// </summary>
/// <typeparam name="T">The abstraction that your session implements.</typeparam>
public sealed class SessionFactory<T> : GenericAsyncFactory<T>, ISessionFactory<T>
{
    /// <summary>
    /// Initializes a new instance of <see cref="SessionFactory{TAbstraction}" />.
    /// </summary>
    /// <param name="getSession">The delegate that resolves the session instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="getSession" /> is null.</exception>
    public SessionFactory(Func<T> getSession) : base(getSession) { }

    /// <summary>
    /// Creates a new data connection, opens a connection to the target database asynchronously
    /// and starts a transaction. The data connection is then passed to a new session instance.
    /// </summary>
    /// <param name="cancellationToken">The token to cancel this asynchronous operation (optional).</param>
    /// <exception cref="SqlException">Thrown when an SQL error occurred when opening the session or starting the transaction.</exception>
    public ValueTask<T> OpenSessionAsync(CancellationToken cancellationToken = default) =>
        CreateAsync(cancellationToken);
}