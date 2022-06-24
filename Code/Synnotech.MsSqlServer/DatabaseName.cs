using System;

namespace Synnotech.MsSqlServer;

/// <summary>
/// Represents a string value that is a valid SQL Server database identifier. This is
/// done by calling <see cref="SqlEscaping.CheckAndNormalizeDatabaseName" />. The check is done
/// according to the rules of https://docs.microsoft.com/en-us/sql/relational-databases/databases/database-identifiers?view=sql-server-ver15#rules-for-regular-identifiers.
/// </summary>
public readonly struct DatabaseName : IEquatable<DatabaseName>
{
    private readonly string _databaseName;

    /// <summary>
    /// Initializes a new instance of <see cref="DatabaseName" />.
    /// </summary>
    /// <param name="databaseName">The name of the database.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="databaseName" /> is null.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="databaseName" /> is an empty string or contains only white space, when it has more than 123 characters
    /// after being trimmed, or when it contains invalid characters.
    /// </exception>
    public DatabaseName(string databaseName)
    {
        _databaseName = SqlEscaping.CheckAndNormalizeDatabaseName(databaseName);
    }

    /// <summary>
    /// Checks if the specified instance is equal to this one.
    /// </summary>
    public bool Equals(DatabaseName other) =>
        StringComparer.CurrentCultureIgnoreCase.Equals(_databaseName, other._databaseName);

    /// <summary>
    /// Checks if the specified object is an instance of <see cref="DatabaseName" />
    /// and equal to this instance.
    /// </summary>
    public override bool Equals(object @object) =>
        @object is DatabaseName databaseName && Equals(databaseName);

    /// <summary>
    /// Returns the hash code of the database name.
    /// </summary>
    public override int GetHashCode() => StringComparer.CurrentCultureIgnoreCase.GetHashCode(_databaseName);

    /// <summary>
    /// Returns the database name.
    /// </summary>
    public override string ToString() => _databaseName;

    /// <summary>
    /// Compares two <see cref="DatabaseName" /> instances for equality.
    /// </summary>
    public static bool operator ==(DatabaseName x, DatabaseName y) => x.Equals(y);

    /// <summary>
    /// Compares two <see cref="DatabaseName" /> instances for inequality.
    /// </summary>
    public static bool operator !=(DatabaseName x, DatabaseName y) => !x.Equals(y);

    /// <summary>
    /// Implicitly converts a string to a database name.
    /// </summary>
    public static implicit operator DatabaseName(string databaseName) => new (databaseName);
}