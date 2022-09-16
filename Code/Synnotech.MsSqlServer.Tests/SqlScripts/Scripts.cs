using Light.EmbeddedResources;

namespace Synnotech.MsSqlServer.Tests.SqlScripts;

public static class Scripts
{
    public static string GetPersons => GetScript("GetPersons.sql");
    public static string SimpleDatabase => GetScript("SimpleDatabase.sql");
    public static string UpdatePerson => GetScript("UpdatePerson.sql");
    public static string GetPerson => GetScript("GetPerson.sql");
    public static string GetPersonCount => GetScript("GetPersonCount.sql");
    
    private static string GetScript(string name) => typeof(Scripts).GetEmbeddedResource(name);
}