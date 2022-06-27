using System;
using Light.GuardClauses;

namespace Synnotech.MsSqlServer;

/// <summary>
/// Represents information about a single physical file that belongs to a SQL Server database.
/// </summary>
public readonly record struct DatabaseFileInfo
{
    /// <summary>
    /// Initializes a new instance of <see cref="DatabaseFileInfo" />.
    /// </summary>
    /// <param name="type">
    /// The type of the file. Typical values are ROWS for MDF files or LOG for LDF files.
    /// See <see cref="DatabaseFileTypeDescription" /> for the different constants of this value.
    /// </param>
    /// <param name="physicalFilePath">The path to the physical file.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> or <paramref name="physicalFilePath" /> are null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> or <paramref name="physicalFilePath" /> contain only white space or are empty strings.</exception>
    public DatabaseFileInfo(string type, string physicalFilePath)
    {
        Type = type.MustNotBeNullOrWhiteSpace();
        PhysicalFilePath = physicalFilePath.MustNotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Gets the type of the of the file. See <see cref="DatabaseFileTypeDescription" /> for the different constants of this value.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the path of the database file.
    /// </summary>
    public string PhysicalFilePath { get; }
}