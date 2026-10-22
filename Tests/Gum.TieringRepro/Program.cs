namespace Gum.TieringRepro;

public static class Program
{
    public static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--child-micro")
        {
            return Micro.Run();
        }
        return Runner.Run(args);
    }
}
