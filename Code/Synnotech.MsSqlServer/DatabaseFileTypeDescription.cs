namespace Synnotech.MsSqlServer;

/// <summary>
/// Provides constants for the different type descriptions (column type_desc) of
/// the SQL Server view sys.database_files
/// </summary>
public static class DatabaseFileTypeDescription
{
    /// <summary>
    /// Gets the "ROWS" constant that identifies data files (MDF or NDF).
    /// </summary>
    public const string Rows = "ROWS";

    /// <summary>
    /// Gets the "LOG" constant that identifies SQL Server log files (LDF).
    /// </summary>
    public const string Log = "LOG";

    /// <summary>
    /// Gets the "FILESTREAM" constant that identifies SQL Server file stream files.
    /// </summary>
    public const string FileStream = "FILESTREAM";

    /// <summary>
    /// Gets the "FULLTEXT" constant that identifies SQL Server full text search index files.
    /// </summary>
    public const string FullText = "FULLTEXT";
}