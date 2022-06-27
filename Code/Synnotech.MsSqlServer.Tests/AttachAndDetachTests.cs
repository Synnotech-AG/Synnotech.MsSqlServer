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