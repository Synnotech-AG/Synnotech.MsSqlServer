using Xunit;

namespace Synnotech.MsSqlServer.Tests
{
    public sealed class ValidDatabaseIdentifiers : TheoryData<string, string>
    {
        public ValidDatabaseIdentifiers()
        {
            Add("A", "A");
            Add("B", "B");
            Add("  C", "C");
            Add("D\t", "D");
            Add("\r\ne\t", "e");
            Add("Foo", "Foo");
            Add("Update", "[Update]");
            Add("Table", "[Table]");
            Add("Table2016", "Table2016");
            Add("IUseÜmläuts", "IUseÜmläuts");
            Add("_My$Table#With@All_AllowedSigns123", "_My$Table#With@All_AllowedSigns123");
        }
    }
}