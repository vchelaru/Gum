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
        var rootCommand = new RootCommand("Gum CLI - create projects and check for errors.");

        rootCommand.AddCommand(NewCommand.Create());
        rootCommand.AddCommand(CheckCommand.Create());

        return rootCommand.Invoke(args);
    }
}
