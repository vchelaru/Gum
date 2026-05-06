using System.CommandLine;
using System.Reflection;
using Gum.Cli.Commands;

namespace Gum.Cli;

/// <summary>
/// Entry point for the Gum CLI.
/// </summary>
public class Program
{
    public static int Main(string[] args)
    {
        string version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "unknown";

        // Banner goes to stderr so stdout stays clean for machine-readable output
        // (e.g. `gumcli check foo.gumx --json | jq .`). This matches the convention
        // used by gh/aws/kubectl: informational chrome on stderr, command output on stdout.
        System.Console.Error.WriteLine($"gumcli v{version}");

        var rootCommand = new RootCommand($"gumcli v{version} - create projects, check for errors, and generate code.");

        rootCommand.AddCommand(NewCommand.Create());
        rootCommand.AddCommand(CheckCommand.Create());
        rootCommand.AddCommand(PackCommand.Create());
        rootCommand.AddCommand(CodegenCommand.Create());
        rootCommand.AddCommand(CodegenInitCommand.Create());
        rootCommand.AddCommand(FontsCommand.Create());
        rootCommand.AddCommand(ScreenshotCommand.Create());
        rootCommand.AddCommand(SvgCommand.Create());

        return rootCommand.Invoke(args);
    }
}
