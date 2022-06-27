using System.Collections.Generic;

namespace Synnotech.MsSqlServer;

/// <summary>
/// Represents a data structure that encapsulates a database name and
/// the physical file paths of the MDF and the LDF file.
/// </summary>
/// <param name="DatabaseName">The name of the database.</param>
/// <param name="FilePaths">The list of physical files that belong to the database.</param>
public readonly record struct DatabasePhysicalFilesInfo(DatabaseName DatabaseName,
                                                        List<DatabaseFileInfo> FilePaths);

/// <summary>
/// Represents information about a single physical file that belongs to a SQL Server database.
/// </summary>
/// <param name="Type">The type of the file. Typical values are ROWS for MDF files or LOG for LDF files.</param>
/// <param name="PhysicalFilePath">The path to the physical file.</param>
public readonly record struct DatabaseFileInfo(string Type, string PhysicalFilePath);