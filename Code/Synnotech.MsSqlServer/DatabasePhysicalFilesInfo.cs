using System.Collections.Generic;
using Light.GuardClauses;

namespace Synnotech.MsSqlServer;

/// <summary>
/// Represents a data structure that encapsulates a database name and
/// the physical file paths of the MDF and the LDF file.
/// </summary>
/// 
public readonly record struct DatabasePhysicalFilesInfo
{
    /// <summary>
    /// Initializes a new instance of <see cref="DatabasePhysicalFilesInfo" />.
    /// </summary>
    /// <param name="databaseName">The name of the database.</param>
    /// <param name="files">The list of physical files that belong to the database.</param>
    public DatabasePhysicalFilesInfo(DatabaseName databaseName,
                                     List<DatabaseFileInfo> files)
    {
        DatabaseName = databaseName.MustNotBeDefault();
        Files = files.MustNotBeNullOrEmpty();
    }

    /// <summary>
    /// Gets the name of the database.
    /// </summary>
    public DatabaseName DatabaseName { get; }

    /// <summary>
    /// Gets the list of files that belong to the database.
    /// </summary>
    public List<DatabaseFileInfo> Files { get; }
}