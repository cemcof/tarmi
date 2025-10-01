namespace ConfigsGenerator;

internal class Program
{
    public static void Main()
    {
        ProdConfigGenerator.Generate();
        DevConfigGenerator.Generate();
        HoldersConfigGenerator.Generate();
    }
}
