using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using Gum.DataTypes;
using Gum.ProjectServices.CodeGeneration;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class PlatformProjectScaffolder : IPlatformProjectScaffolder
{
    private const string ResourcePrefix = "Gum.ProjectServices.Templates.Platforms.";

    // Each is the assembly name of a Gum runtime package. A host project with the same name writes a
    // same-named DLL over the runtime's copy in bin/ and fails at runtime with no build warning.
    private static readonly string[] ReservedProjectNames =
    {
        "GumCommon", "MonoGameGum", "KniGum", "FnaGum", "RaylibGum", "SkiaGum", "SilkNetGum", "StrideGum", "KniSkiaGum"
    };

    private static readonly Regex ValidProjectName = new Regex("^[A-Za-z_][A-Za-z0-9_.-]*$");
    private static readonly Regex PackageReferenceInclude = new Regex("<PackageReference\\s+Include=\"([^\"]+)\"");

    /// <inheritdoc/>
    public PlatformProjectResult Create(
        string projectDirectory, HostPlatform platform, bool includeFormsTemplate, string? gumSourceDirectory = null,
        string? themeDirectory = null, bool useXmlFormat = false, bool includeDemoScreen = true)
    {
        string fullDirectory = GetFullDirectory(projectDirectory);
        string projectName = Path.GetFileName(fullDirectory);

        NewProjectError? preflightError = GetPreflightError(fullDirectory, platform);
        if (preflightError != null)
        {
            return Fail(preflightError.Message);
        }

        (string csprojPath, string gumProjectPath) = GetProjectPaths(fullDirectory, useXmlFormat);
        string gumFolder = Path.GetDirectoryName(gumProjectPath)!;

        (string packageName, string runtimeProjectPath) = GetGumRuntime(platform);
        string gumReference = $"<PackageReference Include=\"{packageName}\" Version=\"*\" />";
        if (gumSourceDirectory != null)
        {
            string runtimeCsproj = Path.GetFullPath(Path.Combine(gumSourceDirectory, runtimeProjectPath));
            if (!File.Exists(runtimeCsproj))
            {
                return Fail($"Gum source not found: {runtimeCsproj} does not exist. Pass the root of a Gum checkout.");
            }

            // MSBuild paths use backslashes by convention; the syntax version detection also expects them.
            string include = Path.GetRelativePath(fullDirectory, runtimeCsproj).Replace('/', '\\');
            gumReference = $"<ProjectReference Include=\"{EscapeForIncludeAttribute(include)}\" />";
        }

        Directory.CreateDirectory(gumFolder);

        string rootNamespace = NameRules.ToCSharpIdentifier(projectName);
        WriteHostFiles(fullDirectory, csprojPath, projectName, platform, gumReference, Path.GetFileName(gumProjectPath));

        GumProjectTemplateWriter.Write(gumProjectPath, includeFormsTemplate, themeDirectory);

        CodeOutputProjectSettings settings = WriteCodeGenerationSettings(gumProjectPath, csprojPath);

        // The screen is found from what was just written; the host is written last so it shows it.
        DemoScreenInfo? demoScreen = includeDemoScreen ? FindDemoScreen(gumProjectPath, settings) : null;
        WriteHostSources(
            fullDirectory, projectName, rootNamespace, platform, Path.GetFileName(gumProjectPath),
            demoScreen, screenCodeGenerated: false, addedPackages: Array.Empty<string>());

        return new PlatformProjectResult
        {
            Success = true,
            CsprojPath = csprojPath,
            GumProjectPath = gumProjectPath,
            DemoScreen = demoScreen
        };
    }

    // The class code generation makes for the demo screen: its name and the namespace the settings give it.
    private static DemoScreenInfo? FindDemoScreen(string gumProjectPath, CodeOutputProjectSettings settings)
    {
        string? name = DemoScreenRule.FindDemoScreenName(gumProjectPath);
        if (name == null)
        {
            return null;
        }

        string[] segments = name.Split('/');
        string ns = settings.RootNamespace + ".Screens";
        if (settings.AppendFolderToNamespace)
        {
            foreach (string folder in segments.Take(segments.Length - 1))
            {
                ns += "." + NameRules.ToCSharpIdentifier(folder);
            }
        }

        return new DemoScreenInfo { Name = name, ClassName = NameRules.ToCSharpIdentifier(segments[^1]), Namespace = ns };
    }

    /// <summary>
    /// Writes the host game's startup code again for what exists now: the packages that were added and whether the demo
    /// screen's code was generated. The host only ever names what is in the project, so a step that did not finish leaves a
    /// host that still builds.
    /// </summary>
    internal static void RewriteHostSources(
        string directory, HostPlatform platform, string gumProjectFileName, DemoScreenInfo? demoScreen, bool screenCodeGenerated,
        IReadOnlyList<string> addedPackages)
    {
        string fullDirectory = GetFullDirectory(directory);
        string projectName = Path.GetFileName(fullDirectory);
        WriteHostSources(
            fullDirectory, projectName, NameRules.ToCSharpIdentifier(projectName), platform, gumProjectFileName, demoScreen,
            screenCodeGenerated, addedPackages);
    }

    // MSBuild reads %XX in an item include as a character, splits it at ';', expands '*' and '?' and
    // evaluates $(...) and @(...); the XML escape comes last.
    private static string EscapeForIncludeAttribute(string include)
    {
        StringBuilder builder = new StringBuilder(include.Length);
        foreach (char c in include)
        {
            if (c is '%' or ';' or '*' or '?' or '$' or '@')
            {
                builder.Append('%').Append(((int)c).ToString("X2"));
            }
            else
            {
                builder.Append(c);
            }
        }

        return SecurityElement.Escape(builder.ToString());
    }

    internal static string GetFullDirectory(string projectDirectory) =>
        Path.GetFullPath(projectDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    internal static (string CsprojPath, string GumProjectPath) GetProjectPaths(string fullDirectory, bool useXmlFormat)
    {
        string projectName = Path.GetFileName(fullDirectory);
        string extension = useXmlFormat ? GumProjectSave.ProjectExtension : GumProjectSave.ProjectJsonExtension;
        return (
            Path.Combine(fullDirectory, projectName + ".csproj"),
            Path.Combine(
                fullDirectory, NewProjectLayout.ContentFolderName, NewProjectLayout.GumProjectFolderName,
                NewProjectLayout.GumProjectFolderName + "." + extension));
    }

    internal static string GetSolutionPath(string fullDirectory) =>
        Path.Combine(fullDirectory, NewProjectLayout.GetSolutionFileName(Path.GetFileName(fullDirectory)));

    /// <summary>
    /// The reason a project cannot be created in <paramref name="fullDirectory"/> (bad name, or a project already there),
    /// checked before anything is written; <see langword="null"/> when it can.
    /// </summary>
    internal static NewProjectError? GetPreflightError(string fullDirectory, HostPlatform platform)
    {
        string projectName = Path.GetFileName(fullDirectory);

        string? nameError = GetProjectNameError(projectName, platform);
        if (nameError != null)
        {
            return new NewProjectError(NewProjectErrorKind.InvalidProjectName, nameError);
        }

        (string csprojPath, string gumProjectPath) = GetProjectPaths(fullDirectory, useXmlFormat: false);
        if (File.Exists(csprojPath))
        {
            return new NewProjectError(NewProjectErrorKind.ProjectAlreadyExists, $"Project already exists: {csprojPath}");
        }

        // The solution is refused like the .csproj: writing over it would destroy a file the user made.
        string slnxPath = GetSolutionPath(fullDirectory);
        if (File.Exists(slnxPath))
        {
            return new NewProjectError(NewProjectErrorKind.ProjectAlreadyExists, $"Solution already exists: {slnxPath}");
        }

        // Either format counts: a .gumx beside a new .gumj would leave two Gum projects in one folder.
        foreach (bool useXmlFormat in new[] { false, true })
        {
            string existing = GetProjectPaths(fullDirectory, useXmlFormat).GumProjectPath;
            if (File.Exists(existing))
            {
                return new NewProjectError(NewProjectErrorKind.ProjectAlreadyExists, $"Gum project already exists: {existing}");
            }
        }

        return null;
    }

    private static string? GetProjectNameError(string projectName, HostPlatform platform)
    {
        if (!ValidProjectName.IsMatch(projectName))
        {
            return $"'{projectName}' is not a valid project name. Use letters, digits, '_', '-' and '.', starting with a letter or '_'.";
        }

        if (ReservedProjectNames.Contains(projectName, StringComparer.OrdinalIgnoreCase))
        {
            return $"'{projectName}' is the assembly name of a Gum runtime package. Pick another project name.";
        }

        // The Program class that top-level statements create in the global namespace clashes (CS0101) with a
        // root namespace of the same name.
        if (NameRules.ToCSharpIdentifier(projectName) == "Program")
        {
            return $"'{projectName}' cannot be a project name: the generated code has a class named Program, which clashes with a namespace of that name. Pick another project name.";
        }

        // A project named like a package it references fails to restore with NU1108 (a cycle).
        if (GetReferencedPackageIds(platform).Contains(projectName, StringComparer.OrdinalIgnoreCase))
        {
            return $"'{projectName}' is the name of a package the {platform} project references, which makes restore fail with a cycle. Pick another project name.";
        }

        return null;
    }

    // Every package id the generated csproj references: the Gum package from the runtime table plus the
    // PackageReference lines of the platform's own template, the two places that write them.
    private static string[] GetReferencedPackageIds(HostPlatform platform)
    {
        string template = ReadTemplate(platform.ToString(), "Host.csproj.template", "", "", "");
        return PackageReferenceInclude.Matches(template)
            .Select(match => match.Groups[1].Value)
            .Append(GetGumRuntime(platform).PackageName)
            .ToArray();
    }

    // Each platform's Gum package, and the location of that package's project inside a Gum checkout.
    private static (string PackageName, string RuntimeProjectPath) GetGumRuntime(HostPlatform platform) => platform switch
    {
        HostPlatform.MonoGame => ("Gum.MonoGame", Path.Combine("MonoGameGum", "MonoGameGum.csproj")),
        HostPlatform.Kni => ("Gum.KNI", Path.Combine("MonoGameGum", "KniGum", "KniGum.csproj")),
        HostPlatform.Raylib => ("Gum.raylib", Path.Combine("Runtimes", "RaylibGum", "RaylibGum.csproj")),
        HostPlatform.Stride => ("Gum.Stride", Path.Combine("Runtimes", "StrideGum", "StrideGum.csproj")),
        HostPlatform.SilkNet => ("Gum.SilkNet", Path.Combine("Runtimes", "SilkNetGum", "SilkNetGum.csproj")),
        HostPlatform.KniSkia => ("Gum.KniSkia", Path.Combine("Runtimes", "KniSkiaGum", "KniSkiaGum.csproj")),
        _ => throw new ArgumentOutOfRangeException(nameof(platform), platform, null)
    };

    private static void WriteHostFiles(
        string directory, string csprojPath, string projectName, HostPlatform platform, string gumReference, string gumProjectFileName)
    {
        string rootNamespace = NameRules.ToCSharpIdentifier(projectName);
        File.WriteAllText(
            csprojPath,
            ReadTemplate(platform.ToString(), "Host.csproj.template", projectName, rootNamespace, gumProjectFileName)
                .Replace("{{GumReference}}", gumReference));

        // Only the game project: a source-linked Gum runtime stays a ProjectReference and needs no entry.
        File.WriteAllText(
            Path.Combine(directory, NewProjectLayout.GetSolutionFileName(projectName)),
            "<Solution>" + Environment.NewLine +
            $"  <Project Path=\"{SecurityElement.Escape(projectName + ".csproj")}\" />" + Environment.NewLine +
            "</Solution>" + Environment.NewLine);
    }

    // Program.cs, and Game1.cs where the platform has one: the part of the host that initializes Gum and shows a screen.
    private static void WriteHostSources(
        string directory, string projectName, string rootNamespace, HostPlatform platform, string gumProjectFileName,
        DemoScreenInfo? demoScreen, bool screenCodeGenerated, IReadOnlyList<string> addedPackages)
    {
        string platformFolder = platform.ToString();
        // The XNA-style hosts share Program.cs and run a Game1. KNI Skia draws through a Skia canvas,
        // so it has its own Game1.
        bool isXnaGame = NewProjectLayout.WritesGame1(platform);
        string programFolder = isXnaGame ? "Xna" : platformFolder;

        string Fill(string folder, string fileName)
        {
            string text = ReadTemplate(folder, fileName, projectName, rootNamespace, gumProjectFileName);
            text = PlatformHostContent.Expand(text, "{{HostUsings}}", PlatformHostContent.GetUsings(demoScreen, screenCodeGenerated));
            text = PlatformHostContent.Expand(text, "{{InitLines}}", PlatformHostContent.GetInitLines(addedPackages));
            return PlatformHostContent.Expand(text, "{{ScreenLines}}", PlatformHostContent.GetScreenLines(demoScreen, screenCodeGenerated));
        }

        File.WriteAllText(Path.Combine(directory, NewProjectLayout.ProgramFileName), Fill(programFolder, "Program.cs.template"));

        if (isXnaGame)
        {
            string gameFolder = platform == HostPlatform.KniSkia ? platformFolder : "Xna";
            File.WriteAllText(Path.Combine(directory, NewProjectLayout.Game1FileName), Fill(gameFolder, "Game1.cs.template"));
        }
    }

    private static string ReadTemplate(string folder, string fileName, string projectName, string rootNamespace, string gumProjectFileName)
    {
        string resourceName = ResourcePrefix + folder + "." + fileName;
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded template {resourceName}");
        using StreamReader reader = new StreamReader(stream);

        return reader.ReadToEnd()
            .Replace("{{ProjectName}}", projectName)
            .Replace("{{RootNamespace}}", rootNamespace)
            .Replace("{{GumProjectFile}}", gumProjectFileName);
    }

    // Every runtime uses the same Gum Forms output (GumForms), so the library does not depend on the platform.
    private static CodeOutputProjectSettings WriteCodeGenerationSettings(string gumProjectPath, string csprojPath)
    {
        AutoSetupResult setup = new CodeGenerationAutoSetupService().Run(gumProjectPath, csprojPath);
        CodeOutputProjectSettings settings = setup.Settings
            ?? throw new InvalidOperationException(setup.ErrorMessage);

        settings.OutputLibrary = OutputLibrary.GumForms;

        string gumFolder = Path.GetDirectoryName(gumProjectPath)! + Path.DirectorySeparatorChar;
        new CodeOutputProjectSettingsManager(new NullCodeGenLogger(), new FixedProjectDirectoryProvider(gumFolder))
            .WriteSettingsForProject(settings);
        return settings;
    }

    private static PlatformProjectResult Fail(string message) =>
        new PlatformProjectResult { Success = false, ErrorMessage = message };

    private class NullCodeGenLogger : ICodeGenLogger
    {
        public void PrintOutput(string message) { }

        public void PrintError(string message) { }
    }
}
