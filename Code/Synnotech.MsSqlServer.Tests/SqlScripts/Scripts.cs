using Light.EmbeddedResources;

namespace Synnotech.MsSqlServer.Tests.SqlScripts
{
    public static class Scripts
    {
        public static string GetScript(string name) => typeof(Scripts).GetEmbeddedResource(name);

        public static string GetPersonsScript => GetScript("GetPersons.sql");

        public static string SimpleDatabaseScript => GetScript("SimpleDatabase.sql");
    }
}