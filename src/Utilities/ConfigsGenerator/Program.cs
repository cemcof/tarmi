namespace ConfigsGenerator;

internal static class Program
{
    public static void Main()
    {
        ProdConfigGenerator.Generate();
        DevConfigGenerator.Generate();
        HoldersConfigGenerator.Generate();
    }
}
