using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace Synnotech.MsSqlServer.Tests;

public static class AttachAndDetachTests
{
    [SkippableFact]
    public static async Task DetachCopyAttach()
    {
        var connectionString = TestSettings.GetConnectionStringOrSkip();
        await Database.DropAndCreateDatabaseAsync(connectionString);
        const string databaseSetup = @"
CREATE TABLE Foo (
    Id INT IDENTITY(1, 1) CONSTRAINT PK_Foo PRIMARY KEY,
    Value NVARCHAR(100) NOT NULL
);

INSERT INTO Foo (Value) VALUES ('Bar');
INSERT INTO Foo (Value) VALUES ('Baz');";
        await Database.ExecuteNonQueryAsync(connectionString, databaseSetup);

        var databaseInfo = await Database.DetachDatabaseAsync(connectionString);
        var temporaryFolder = Path.GetTempPath();
        var now = DateTime.UtcNow;
        var subFolder = Path.Combine(temporaryFolder, $"{now:yyyyMMdd-HHmmss} CopyDetachedDatabaseTests");
        Directory.CreateDirectory(subFolder);

        foreach (var fileInfo in databaseInfo.Files)
        {
            var sourceFileName = Path.GetFileName(fileInfo.PhysicalFilePath);
            var targetFilePath = Path.Combine(subFolder, sourceFileName);
            File.Copy(fileInfo.PhysicalFilePath, targetFilePath);
        }

        await Database.AttachDatabaseAsync(connectionString, databaseInfo);

        Directory.Delete(subFolder, true);
    }
}