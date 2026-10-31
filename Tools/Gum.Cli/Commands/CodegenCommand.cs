using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using Gum.Expressions;
using Gum.ProjectServices;
using Gum.ProjectServices.CodeGeneration;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli codegen</c> command which generates C# code for all elements in a Gum project.
/// </summary>
public static class CodegenCommand
{
    /// <summary>
    /// Creates the <c>codegen</c> command definition.
    /// </summary>
    public static Command Create()
    {
        var projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx project file.");

        var elementOption = new Option<string[]>(
            "--element",
            "Name of a specific element to generate code for. Can be specified multiple times.")
        {
            AllowMultipleArgumentsPerToken = true
        };

        var pruneOption = new Option<bool>(
            "--prune",
            "Delete generated (.Generated.cs) files that no element in the project accounts for. " +
            "Orphaned custom code and per-element .codsj settings files are reported but never deleted.");

        var command = new Command("codegen", "Generate C# code for a Gum project.")
        {
            projectArgument,
            elementOption,
            pruneOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            string[]? elements = context.ParseResult.GetValueForOption(elementOption);
            bool prune = context.ParseResult.GetValueForOption(pruneOption);
            context.ExitCode = Execute(projectPath, elements, prune);
        });

        return command;
    }

    private static int Execute(string projectPath, string[]? elementNames, bool prune) =>
        new HeadlessCodegenRunner(GumExpressionService.Initialize, Program.GetCliVersion())
            .Run(projectPath, elementNames, prune, Console.Out, Console.Error);
}
