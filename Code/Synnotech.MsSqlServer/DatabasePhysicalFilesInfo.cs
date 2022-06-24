namespace Synnotech.MsSqlServer;

/// <summary>
/// Represents a data structure that encapsulates a database name and
/// the physical file paths of the MDF and the LDF file.
/// </summary>
/// <param name="DatabaseName">The name of the database.</param>
/// <param name="MdfFilePath">The physical file path to the MDF file.</param>
/// <param name="LdfFilePath">The physical file path to the LDF file.</param>
public readonly record struct DatabasePhysicalFilesInfo(DatabaseName DatabaseName,
                                                        string MdfFilePath,
                                                        string LdfFilePath);