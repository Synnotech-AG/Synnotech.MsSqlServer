using System;
using Light.GuardClauses;
using Light.GuardClauses.Exceptions;

namespace Synnotech.MsSqlServer
{
    /// <summary>
    /// Provides methods to escape strings to avoid SQL Injection attacks.
    /// </summary>
    public static class SqlEscaping
    {
        /// <summary>
        /// Checks and normalizes the specified database name. This method searches for invalid characters, trims the name if necessary
        /// and pads the name with brackets if it collides with one of the SQL Server reserved keywords.
        /// This method is implemented according to https://docs.microsoft.com/en-us/sql/relational-databases/databases/database-identifiers?view=sql-server-ver15#rules-for-regular-identifiers.
        /// </summary>
        /// <param name="databaseName"></param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="databaseName" /> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="databaseName" /> is an empty string or contains only white space, when it has more than 123 characters
        /// after being trimmed, or when it contains invalid characters.
        /// </exception>
        public static string CheckAndNormalizeDatabaseName(string databaseName)
        {
            databaseName.MustNotBeNullOrEmpty(nameof(databaseName));

            var span = databaseName.AsSpan().Trim();
            if (span.IsEmpty)
                Throw.WhiteSpaceString(databaseName, nameof(databaseName));
            if (span.Length > 123)
                Throw.Argument(nameof(databaseName), $"The specified database name \"{span.ToString()}\" is too long. The maximum length is restricted to 123 characters.");

            var firstCharacter = span[0];
            if (!Advanced.IsValidFirstCharacterForDatabaseName(firstCharacter))
                Throw.Argument(nameof(databaseName), $"The specified database name \"{span.ToString()}\" does not start with a character or an underscore '_'.");

            if (span.Length == 1)
                return databaseName.Length == 1 ? databaseName : span.ToString();

            for (var i = 1; i < span.Length; i++)
            {
                var character = span[i];
                if (!Advanced.IsValidSubsequentIdentifierCharacter(character))
                    Throw.Argument(nameof(databaseName), $"The specified database name \"{span.ToString()}\" contains invalid characters. It must start with a character or an underscore '_', and continue with characters consisting of letters, digits, or the signs '@', '$', '#', or the underscore'_'.");
            }

            if (databaseName.Length != span.Length)
                databaseName = span.ToString();

            if (SqlKeywords.IsKeyword(databaseName))
                databaseName = Advanced.PadWithBrackets(databaseName);

            return databaseName;
        }

        /// <summary>
        /// Provides advanced helper functions for escaping strings for T-SQL.
        /// </summary>
        public static class Advanced
        {
            /// <summary>
            /// Checks if the specified character is a valid first character for a database name. The
            /// character must either be a character or an underscore '_'.
            /// This method is implemented according to https://docs.microsoft.com/en-us/sql/relational-databases/databases/database-identifiers?view=sql-server-ver15#rules-for-regular-identifiers.
            /// </summary>
            /// <param name="character">The character to be checked.</param>
            public static bool IsValidFirstCharacterForDatabaseName(char character) =>
                character.IsLetter() || character == '_';

            /// <summary>
            /// Checks if the specified character is either a letter or a digit, the "@" sign, the "$" sign, the "#" sign, or the underscore "_".
            /// This method is implemented according to https://docs.microsoft.com/en-us/sql/relational-databases/databases/database-identifiers?view=sql-server-ver15#rules-for-regular-identifiers.
            /// </summary>
            /// <param name="character">The character to be checked.</param>
            public static bool IsValidSubsequentIdentifierCharacter(char character)
            {
                switch (character)
                {
                    case '@':
                    case '$':
                    case '#':
                    case '_':
                        return true;
                    default:
                        return character.IsLetterOrDigit();
                }
            }

            /// <summary>
            /// Creates a new string that encapsulates the specified identifier with brackets,
            /// e.g. "foo" will become "[foo]". This is usually used to escape identifier names
            /// that would collide with a SQL Server reserved keyword.
            /// This method uses a stack-allocated span to create the new string to avoid
            /// allocations. Please ensure that your identifier is not too long before
            /// calling this method.
            /// </summary>
            /// <param name="identifier">The identifier that needs to be escaped with brackets.</param>
            /// <exception cref="ArgumentNullException">Thrown when <paramref name="identifier" /> is null.</exception>
            /// <exception cref="ArgumentException">Thrown when <paramref name="identifier" /> is an empty string</exception>
            public static string PadWithBrackets(string identifier)
            {
                identifier.MustNotBeNullOrEmpty(nameof(identifier));

                Span<char> span = stackalloc char[identifier.Length + 2];
                span[0] = '[';
                span[span.Length - 1] = ']';
                identifier.AsSpan().CopyTo(span.Slice(1));
                return span.ToString();
            }
        }
    }
}