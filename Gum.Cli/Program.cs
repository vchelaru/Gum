using System.CommandLine;
using Gum.Cli.Commands;

namespace Gum.Cli;

/// <summary>
/// Entry point for the Gum CLI.
/// </summary>
public class Program
{
    public static int Main(string[] args)
    {
        var rootCommand = new RootCommand("gumcli - create projects, check for errors, and generate code.");

        rootCommand.AddCommand(NewCommand.Create());
        rootCommand.AddCommand(CheckCommand.Create());
        rootCommand.AddCommand(CodegenCommand.Create());

        return rootCommand.Invoke(args);
    }
}
