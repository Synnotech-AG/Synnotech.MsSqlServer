﻿using System;
using System.Collections.Generic;

namespace Synnotech.MsSqlServer;

/// <summary>
/// Provides possibilities to check if an identifier is a reserved SQL keyword.
/// </summary>
public static class SqlKeywords
{
    private static readonly HashSet<string> KeywordsLookup;

    static SqlKeywords()
    {
        var allKeywords = CreateAllKeywords();
        KeywordsLookup = new HashSet<string>(allKeywords, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the specified identifier is a reserved keyword in SQL Server.
    /// This method compares identifiers using the Ordinal-Ignore-Case rule.
    /// </summary>
    public static bool IsKeyword(string identifier) =>
        KeywordsLookup.Contains(identifier);

    private static string[] CreateAllKeywords() =>
        new[]
        {
            "ABSOLUTE",
            "ACTION",
            "ADA",
            "ADD",
            "ADMIN",
            "AFTER",
            "AGGREGATE",
            "ALIAS",
            "ALL",
            "ALLOCATE",
            "ALTER",
            "AND",
            "ANY",
            "ARE",
            "ARRAY",
            "AS",
            "ASC",
            "ASENSITIVE",
            "ASSERTION",
            "ASYMMETRIC",
            "AT",
            "ATOMIC",
            "AUTHORIZATION",
            "AVG",
            "BACKUP",
            "BEFORE",
            "BEGIN",
            "BETWEEN",
            "BINARY",
            "BIT",
            "BIT_LENGTH",
            "BLOB",
            "BOOLEAN",
            "BOTH",
            "BREADTH",
            "BREAK",
            "BROWSE",
            "BULK",
            "BY",
            "CALL",
            "CALLED",
            "CARDINALITY",
            "CASCADE",
            "CASCADED",
            "CASE",
            "CAST",
            "CATALOG",
            "CHAR",
            "CHAR_LENGTH",
            "CHARACTER",
            "CHARACTER_LENGTH",
            "CHECK",
            "CHECKPOINT",
            "CLASS",
            "CLOB",
            "CLOSE",
            "CLUSTERED",
            "COALESCE",
            "COLLATE",
            "COLLATION",
            "COLLECT",
            "COLUMN",
            "COMMIT",
            "COMPLETION",
            "COMPUTE",
            "CONDITION",
            "CONNECT",
            "CONNECTION",
            "CONSTRAINT",
            "CONSTRAINTS",
            "CONSTRUCTOR",
            "CONTAINS",
            "CONTAINSTABLE",
            "CONTINUE",
            "CONVERT",
            "CORR",
            "CORRESPONDING",
            "COUNT",
            "COVAR_POP",
            "COVAR_SAMP",
            "CREATE",
            "CROSS",
            "CUBE",
            "CUME_DIST",
            "CURRENT",
            "CURRENT_CATALOG",
            "CURRENT_DATE",
            "CURRENT_DEFAULT_TRANSFORM_GROUP",
            "CURRENT_PATH",
            "CURRENT_ROLE",
            "CURRENT_SCHEMA",
            "CURRENT_TIME",
            "CURRENT_TIMESTAMP",
            "CURRENT_TRANSFORM_GROUP_FOR_TYPE",
            "CURRENT_USER",
            "CURSOR",
            "CYCLE",
            "DATA",
            "DATABASE",
            "DATE",
            "DAY",
            "DBCC",
            "DEALLOCATE",
            "DEC",
            "DECIMAL",
            "DECLARE",
            "DEFAULT",
            "DEFERRABLE",
            "DEFERRED",
            "DELETE",
            "DENY",
            "DEPTH",
            "DEREF",
            "DESC",
            "DESCRIBE",
            "DESCRIPTOR",
            "DESTROY",
            "DESTRUCTOR",
            "DETERMINISTIC",
            "DIAGNOSTICS",
            "DICTIONARY",
            "DISCONNECT",
            "DISK",
            "DISTINCT",
            "DISTRIBUTED",
            "DOMAIN",
            "DOUBLE",
            "DROP",
            "DUMP",
            "DYNAMIC",
            "EACH",
            "ELEMENT",
            "ELSE",
            "END",
            "END-EXEC",
            "EQUALS",
            "ERRLVL",
            "ESCAPE",
            "EVERY",
            "EXCEPT",
            "EXCEPTION",
            "EXEC",
            "EXECUTE",
            "EXISTS",
            "EXIT",
            "EXTERNAL",
            "EXTRACT",
            "FALSE",
            "FETCH",
            "FILE",
            "FILLFACTOR",
            "FILTER",
            "FIRST",
            "FLOAT",
            "FOR",
            "FOREIGN",
            "FORTRAN",
            "FOUND",
            "FREE",
            "FREETEXT",
            "FREETEXTTABLE",
            "FROM",
            "FULL",
            "FULLTEXTTABLE",
            "FUNCTION",
            "FUSION",
            "GENERAL",
            "GET",
            "GLOBAL",
            "GO",
            "GOTO",
            "GRANT",
            "GROUP",
            "GROUPING",
            "HAVING",
            "HOLD",
            "HOLDLOCK",
            "HOST",
            "HOUR",
            "IDENTITY",
            "IDENTITY_INSERT",
            "IDENTITYCOL",
            "IF",
            "IGNORE",
            "IMMEDIATE",
            "IN",
            "INCLUDE",
            "INDEX",
            "INDICATOR",
            "INITIALIZE",
            "INITIALLY",
            "INNER",
            "INOUT",
            "INPUT",
            "INSENSITIVE",
            "INSERT",
            "INT",
            "INTEGER",
            "INTERSECT",
            "INTERSECTION",
            "INTERVAL",
            "INTO",
            "IS",
            "ISOLATION",
            "ITERATE",
            "JOIN",
            "KEY",
            "KILL",
            "LANGUAGE",
            "LARGE",
            "LAST",
            "LATERAL",
            "LEADING",
            "LEFT",
            "LESS",
            "LEVEL",
            "LIKE",
            "LIKE_REGEX",
            "LIMIT",
            "LINENO",
            "LN",
            "LOAD",
            "LOCAL",
            "LOCALTIME",
            "LOCALTIMESTAMP",
            "LOCATOR",
            "LOWER",
            "MAP",
            "MATCH",
            "MAX",
            "MEMBER",
            "MERGE",
            "METHOD",
            "MIN",
            "MINUTE",
            "MOD",
            "MODIFIES",
            "MODIFY",
            "MODULE",
            "MONTH",
            "MULTISET",
            "NAMES",
            "NATIONAL",
            "NATURAL",
            "NCHAR",
            "NCLOB",
            "NEW",
            "NEXT",
            "NO",
            "NOCHECK",
            "NONCLUSTERED",
            "NONE",
            "NORMALIZE",
            "NOT",
            "NULL",
            "NULLIF",
            "NUMERIC",
            "OBJECT",
            "OCCURRENCES_REGEX",
            "OCTET_LENGTH",
            "OF",
            "OFF",
            "OFFSETS",
            "OLD",
            "ON",
            "ONLY",
            "OPEN",
            "OPENDATASOURCE",
            "OPENQUERY",
            "OPENROWSET",
            "OPENXML",
            "OPERATION",
            "OPTION",
            "OR",
            "ORDER",
            "ORDINALITY",
            "OUT",
            "OUTER",
            "OUTPUT",
            "OVER",
            "OVERLAPS",
            "OVERLAY",
            "PAD",
            "PARAMETER",
            "PARAMETERS",
            "PARTIAL",
            "PARTITION",
            "PASCAL",
            "PATH",
            "PERCENT",
            "PERCENT_RANK",
            "PERCENTILE_CONT",
            "PERCENTILE_DISC",
            "PIVOT",
            "PLAN",
            "POSITION",
            "POSITION_REGEX",
            "POSTFIX",
            "PRECISION",
            "PREFIX",
            "PREORDER",
            "PREPARE",
            "PRESERVE",
            "PRIMARY",
            "PRINT",
            "PRIOR",
            "PRIVILEGES",
            "PROC",
            "PROCEDURE",
            "PUBLIC",
            "RAISERROR",
            "RANGE",
            "READ",
            "READS",
            "READTEXT",
            "REAL",
            "RECONFIGURE",
            "RECURSIVE",
            "REF",
            "REFERENCES",
            "REFERENCING",
            "REGR_AVGX",
            "REGR_AVGY",
            "REGR_COUNT",
            "REGR_INTERCEPT",
            "REGR_R2",
            "REGR_SLOPE",
            "REGR_SXX",
            "REGR_SXY",
            "REGR_SYY",
            "RELATIVE",
            "RELEASE",
            "REPLICATION",
            "RESTORE",
            "RESTRICT",
            "RESULT",
            "RETURN",
            "RETURNS",
            "REVERT",
            "REVOKE",
            "RIGHT",
            "ROLE",
            "ROLLBACK",
            "ROLLUP",
            "ROUTINE",
            "ROW",
            "ROWCOUNT",
            "ROWGUIDCOL",
            "ROWS",
            "RULE",
            "SAVE",
            "SAVEPOINT",
            "SCHEMA",
            "SCOPE",
            "SCROLL",
            "SEARCH",
            "SECOND",
            "SECTION",
            "SECURITYAUDIT",
            "SELECT",
            "SEMANTICKEYPHRASETABLE",
            "SEMANTICSIMILARITYDETAILSTABLE",
            "SEMANTICSIMILARITYTABLE",
            "SENSITIVE",
            "SEQUENCE",
            "SESSION",
            "SESSION_USER",
            "SET",
            "SETS",
            "SETUSER",
            "SHUTDOWN",
            "SIMILAR",
            "SIZE",
            "SMALLINT",
            "SOME",
            "SPACE",
            "SPECIFIC",
            "SPECIFICTYPE",
            "SQL",
            "SQLCA",
            "SQLCODE",
            "SQLERROR",
            "SQLEXCEPTION",
            "SQLSTATE",
            "SQLWARNING",
            "START",
            "STATE",
            "STATEMENT",
            "STATIC",
            "STATISTICS",
            "STDDEV_POP",
            "STDDEV_SAMP",
            "STRUCTURE",
            "SUBMULTISET",
            "SUBSTRING",
            "SUBSTRING_REGEX",
            "SUM",
            "SYMMETRIC",
            "SYSTEM",
            "SYSTEM_USER",
            "TABLE",
            "TABLESAMPLE",
            "TEMPORARY",
            "TERMINATE",
            "TEXTSIZE",
            "THAN",
            "THEN",
            "TIME",
            "TIMESTAMP",
            "TIMEZONE_HOUR",
            "TIMEZONE_MINUTE",
            "TO",
            "TOP",
            "TRAILING",
            "TRAN",
            "TRANSACTION",
            "TRANSLATE",
            "TRANSLATE_REGEX",
            "TRANSLATION",
            "TREAT",
            "TRIGGER",
            "TRIM",
            "TRUE",
            "TRUNCATE",
            "TRY_CONVERT",
            "TSEQUAL",
            "UESCAPE",
            "UNDER",
            "UNION",
            "UNIQUE",
            "UNKNOWN",
            "UNNEST",
            "UNPIVOT",
            "UPDATE",
            "UPDATETEXT",
            "UPPER",
            "USAGE",
            "USE",
            "USER",
            "USING",
            "VALUE",
            "VALUES",
            "VAR_POP",
            "VAR_SAMP",
            "VARCHAR",
            "VARIABLE",
            "VARYING",
            "VIEW",
            "WAITFOR",
            "WHEN",
            "WHENEVER",
            "WHERE",
            "WHILE",
            "WIDTH_BUCKET",
            "WINDOW",
            "WITH",
            "WITHIN",
            "WITHIN GROUP",
            "WITHOUT",
            "WORK",
            "WRITE",
            "WRITETEXT",
            "XMLAGG",
            "XMLATTRIBUTES",
            "XMLBINARY",
            "XMLCAST",
            "XMLCOMMENT",
            "XMLCONCAT",
            "XMLDOCUMENT",
            "XMLELEMENT",
            "XMLEXISTS",
            "XMLFOREST",
            "XMLITERATE",
            "XMLNAMESPACES",
            "XMLPARSE",
            "XMLPI",
            "XMLQUERY",
            "XMLSERIALIZE",
            "XMLTABLE",
            "XMLTEXT",
            "XMLVALIDATE",
            "YEAR",
            "ZONE"
        };
}