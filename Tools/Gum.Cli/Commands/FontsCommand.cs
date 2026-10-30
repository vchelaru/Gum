using System;
using System.Collections.Generic;
using System.Linq;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Gum.DataTypes;
using Gum.Expressions;
using Gum.Managers;
using Gum.ProjectServices;
using Gum.ProjectServices.FontGeneration;
using ToolsUtilities;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli fonts</c> command which generates missing bitmap font files for a project.
/// Supports both bmfont.exe (Windows-only) and KernSmith (cross-platform) backends.
/// </summary>
public static class FontsCommand
{
    /// <summary>
    /// Creates the <c>fonts</c> command definition.
    /// </summary>
    public static Command Create()
    {
        Argument<string> projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx project file.");

        Option<string?> fontOption = new Option<string?>(
            "--font",
            "Also generate this font, which no element needs to use: a system font family or a .ttf/.otf path relative to the project. Requires --size.");

        Option<int[]> sizeOption = new Option<int[]>(
            "--size",
            "Font size(s) to generate for --font, for example --size 18 24 30. Repeat the option or list several sizes.")
        {
            AllowMultipleArgumentsPerToken = true
        };

        Option<bool> boldOption = new Option<bool>("--bold", "Generate the --font sizes bold.");
        Option<bool> italicOption = new Option<bool>("--italic", "Generate the --font sizes italic.");
        Option<int> outlineOption = new Option<int>("--outline", () => 0, "Outline thickness in pixels for the --font sizes.");

        Command command = new Command("fonts", "Generate missing bitmap font files for a Gum project.")
        {
            projectArgument,
            fontOption,
            sizeOption,
            boldOption,
            italicOption,
            outlineOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            string? font = context.ParseResult.GetValueForOption(fontOption);
            int[] sizes = context.ParseResult.GetValueForOption(sizeOption) ?? Array.Empty<int>();
            bool isBold = context.ParseResult.GetValueForOption(boldOption);
            bool isItalic = context.ParseResult.GetValueForOption(italicOption);
            int outline = context.ParseResult.GetValueForOption(outlineOption);

            bool anyStyle = isBold || isItalic || outline != 0;
            string? error = ValidateExtraFontOptions(font, sizes, anyStyle);
            if (error != null)
            {
                Console.Error.WriteLine($"error: {error}");
                context.ExitCode = 2;
                return;
            }

            List<ExtraFontRequest> extraFonts = new List<ExtraFontRequest>();
            foreach (int size in sizes.Distinct())
            {
                extraFonts.Add(new ExtraFontRequest(font!, size, isBold, isItalic, outline));
            }

            context.ExitCode = Execute(projectPath, extraFonts).GetAwaiter().GetResult();
        });

        return command;
    }

    private static string? ValidateExtraFontOptions(string? font, int[] sizes, bool anyStyle)
    {
        if (string.IsNullOrWhiteSpace(font))
        {
            if (sizes.Length > 0 || anyStyle)
            {
                return "--size, --bold, --italic and --outline need --font to say which font to generate.";
            }

            return null;
        }

        if (sizes.Length == 0)
        {
            return "--font needs at least one --size.";
        }

        if (sizes.Any(size => size <= 0))
        {
            return "--size must be greater than zero.";
        }

        return null;
    }

    private static async System.Threading.Tasks.Task<int> Execute(string projectPath, IReadOnlyCollection<ExtraFontRequest> extraFonts)
    {
        string fullPath = Path.GetFullPath(projectPath);

        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(fullPath);

        if (!loadResult.Success)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }

        foreach (ErrorResult loadError in loadResult.LoadErrors)
        {
            Console.Error.WriteLine($"warning: {loadError.ElementName}: {loadError.Message}");
        }

        GumProjectSave project = loadResult.Project!;
        string projectDirectory = Path.GetDirectoryName(fullPath)! + Path.DirectorySeparatorChar;

        StandardElementsManager.Self.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        FileManager.RelativeDirectory = projectDirectory;

        // Wires Roslyn-based evaluation of VariableReferences so conditional (ternary) Font
        // expressions resolve (and every branch gets pregenerated - see #4042) instead of
        // silently failing to resolve at all (mirrors CheckReferencesCommand).
        GumExpressionService.Initialize();

        IFontGenerationCallbacks callbacks = new ConsoleFontGenerationCallbacks();
        IFontFileGenerator fontFileGenerator = CreateFontFileGenerator(project, callbacks);
        IHeadlessFontGenerationService fontService = new HeadlessFontGenerationService(fontFileGenerator, callbacks);

        try
        {
            await fontService.CreateAllMissingFontFiles(project, projectDirectory, extraFonts);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }

        return 0;
    }

    /// <summary>
    /// Selects the appropriate font file generator based on the project's <see cref="GumProjectSave.FontGenerator"/> setting.
    /// </summary>
    private static IFontFileGenerator CreateFontFileGenerator(GumProjectSave project, IFontGenerationCallbacks callbacks)
    {
        // bmfont.exe is Windows-only; off Windows a BmFont project is served by KernSmith.
        return FontGeneratorResolver.Resolve(project.FontGenerator) switch
        {
            FontGeneratorType.KernSmith => new KernSmithFileGenerator(callbacks),
            _ => new BmFontExeFileGenerator(callbacks)
        };
    }

    /// <summary>
    /// Routes font generation output to the console.
    /// </summary>
    private sealed class ConsoleFontGenerationCallbacks : IFontGenerationCallbacks
    {
        public void OnOutput(string message) => Console.WriteLine(message);
    }
}
