using Light.GuardClauses.Exceptions;
using Microsoft.Extensions.Configuration;
using Xunit;
using SynnotechTestSettings = Synnotech.Xunit.TestSettings;

namespace Synnotech.MsSqlServer.Tests
{
    public static class TestSettings
    {
        public static string GetConnectionStringOrSkip()
        {
            Skip.IfNot(SynnotechTestSettings.Configuration.GetValue<bool>("database:areTestsEnabled"));
            return SynnotechTestSettings.Configuration["database:connectionString"] ??
                   throw new InvalidConfigurationException("You must set database:connectionString when database:areTestsEnabled is set to true");
        }
    }
}