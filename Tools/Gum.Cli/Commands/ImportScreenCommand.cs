using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli import-screen</c> command, which performs headlessly the merge step
/// that Content → Import → HTML… otherwise only exercises from inside the running tool: qualify
/// the screen name against a destination subfolder (resolving conflicts), copy staged
/// Images/Fonts/FontCache into the project, add the ScreenReference, and write the screen file.
/// </summary>
public static class ImportScreenCommand
{
    public static Command Create()
    {
        var projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx project file.");

        var screenArgument = new Argument<string>(
            "screen",
            "Path to the .gusx (or .gusj) screen file to import.");

        var subfolderOption = new Option<string?>(
            "--subfolder",
            getDefaultValue: () => null,
            description: "Destination subfolder under Screens/. Avoids name conflicts by importing under Screens/<subfolder>/.");

        var assetsOption = new Option<string?>(
            "--assets",
            getDefaultValue: () => null,
            description: "Staging directory containing Images/, Fonts/, and/or FontCache/ to copy into the project. Omit to skip asset copying.");

        var command = new Command(
            "import-screen",
            "Import a .gusx screen file into a project: qualify/uniquify its name, copy staged assets, add the ScreenReference, and write the screen file.")
        {
            projectArgument,
            screenArgument,
            subfolderOption,
            assetsOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            string screenPath = context.ParseResult.GetValueForArgument(screenArgument);
            string? subfolder = context.ParseResult.GetValueForOption(subfolderOption);
            string? assetsDirectory = context.ParseResult.GetValueForOption(assetsOption);
            context.ExitCode = Execute(projectPath, screenPath, subfolder, assetsDirectory);
        });

        return command;
    }

    private static int Execute(string projectPath, string screenPath, string? subfolderOption, string? assetsOption)
    {
        string fullProjectPath = Path.GetFullPath(projectPath);
        string fullScreenPath = Path.GetFullPath(screenPath);

        if (!File.Exists(fullScreenPath))
        {
            Console.Error.WriteLine($"Screen file not found: {fullScreenPath}");
            return 2;
        }

        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(fullProjectPath);

        if (!loadResult.Success)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }

        GumProjectSave project = loadResult.Project!;

        // Conflict checks (ScreenImportService, and the uniquify loop below) go through
        // ObjectFinder.Self, same as the tool's own import paths — see CheckReferencesCommand.
        ObjectFinder.Self.GumProjectSave = project;

        string? subfolder = string.IsNullOrWhiteSpace(subfolderOption) ? null : subfolderOption.Trim();

        ScreenSave screenSave;
        try
        {
            screenSave = ElementReference.DeserializeElement<ScreenSave>(fullScreenPath, GumProjectSave.NativeVersion);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to deserialize screen: {ex.Message}");
            return 2;
        }

        // Headless equivalent of the dialog the tool would show on conflict — auto-uniquify
        // instead, mirroring MainHtmlToGumPlugin.HandleImportHtml's screen-name resolution.
        string uniqueBaseName = HtmlImportNaming.ResolveUniqueScreenName(
            screenSave.Name, subfolder, name => ObjectFinder.Self.GetElementSave(name) != null);
        screenSave.Name = HtmlImportNaming.QualifyScreenName(uniqueBaseName, subfolder);

        IScreenImportService importService = new ScreenImportService();
        ScreenImportResult result = importService.ImportScreen(project, screenSave);

        if (!result.Success)
        {
            Console.Error.WriteLine($"A screen or component named \"{result.ConflictingScreenName}\" already exists.");
            return 1;
        }

        string projectDirectory = Path.GetDirectoryName(fullProjectPath) ?? "";

        if (!string.IsNullOrWhiteSpace(assetsOption))
        {
            AssetTreeCopier.CopyStagedAssets(Path.GetFullPath(assetsOption), projectDirectory);
        }

        project.Save(fullProjectPath, saveElements: false);

        bool isJsonFormat = GumProjectSave.IsJsonFormat(fullProjectPath);
        bool useCompact = project.Version >= (int)GumProjectSave.GumxVersions.AttributeVersion;
        string screenExtension = isJsonFormat ? GumProjectSave.ScreenJsonExtension : GumProjectSave.ScreenExtension;
        string screenFilePath = Path.Combine(
            projectDirectory, ElementReference.ScreenSubfolder, result.ImportedScreen!.Name + "." + screenExtension);
        result.ImportedScreen.Save(screenFilePath, useCompact);

        Console.WriteLine($"Imported screen \"{result.ImportedScreen.Name}\".");
        return 0;
    }
}
