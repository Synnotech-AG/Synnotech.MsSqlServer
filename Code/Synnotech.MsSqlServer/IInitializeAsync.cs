using System.Threading;
using System.Threading.Tasks;

namespace Synnotech.MsSqlServer;

/// <summary>
/// Represents the abstraction of an object that needs to be initialized asynchronously.
/// </summary>
public interface IInitializeAsync
{
    /// <summary>
    /// Gets the value indicating whether <see cref="InitializeAsync" /> was already called.
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Runs asynchronous initialization logic.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}