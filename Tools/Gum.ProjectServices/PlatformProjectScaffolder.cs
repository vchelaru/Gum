using System;
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
    private const string GumProjectFolderName = "GumProject";

    // Each is the assembly name of a Gum runtime package. A host project with the same name writes a
    // same-named DLL over the runtime's copy in bin/ and fails at runtime with no build warning.
    private static readonly string[] ReservedProjectNames =
    {
        "GumCommon", "MonoGameGum", "KniGum", "FnaGum", "RaylibGum", "SkiaGum", "SilkNetGum", "StrideGum", "KniSkiaGum"
    };

    private static readonly Regex ValidProjectName = new Regex("^[A-Za-z_][A-Za-z0-9_.-]*$");

    /// <inheritdoc/>
    public PlatformProjectResult Create(
        string projectDirectory, HostPlatform platform, bool includeFormsTemplate, string? gumSourceDirectory = null,
        string? themeDirectory = null, bool useXmlFormat = false)
    {
        string fullDirectory = GetFullDirectory(projectDirectory);
        string projectName = Path.GetFileName(fullDirectory);

        NewProjectError? preflightError = GetPreflightError(fullDirectory);
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

        string rootNamespace = GetRootNamespace(projectName);
        WriteHostFiles(
            fullDirectory, csprojPath, projectName, rootNamespace, platform, gumReference, Path.GetFileName(gumProjectPath));

        GumProjectTemplateWriter.Write(gumProjectPath, includeFormsTemplate, themeDirectory);

        WriteCodeGenerationSettings(gumProjectPath, csprojPath);

        return new PlatformProjectResult
        {
            Success = true,
            CsprojPath = csprojPath,
            GumProjectPath = gumProjectPath
        };
    }

    // A namespace named Program clashes with the Program class that top-level statements create in the
    // global namespace (CS0101), so the root namespace gets a suffix.
    internal static string GetRootNamespace(string projectName)
    {
        string rootNamespace = NameRules.ToCSharpIdentifier(projectName);
        return rootNamespace == "Program" ? rootNamespace + "_" : rootNamespace;
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
            Path.Combine(fullDirectory, "Content", GumProjectFolderName, GumProjectFolderName + "." + extension));
    }

    /// <summary>
    /// The reason a project cannot be created in <paramref name="fullDirectory"/> (bad name, or a project already there),
    /// checked before anything is written; <see langword="null"/> when it can.
    /// </summary>
    internal static NewProjectError? GetPreflightError(string fullDirectory)
    {
        string projectName = Path.GetFileName(fullDirectory);

        string? nameError = GetProjectNameError(projectName);
        if (nameError != null)
        {
            return new NewProjectError(NewProjectErrorKind.InvalidProjectName, nameError);
        }

        (string csprojPath, string gumProjectPath) = GetProjectPaths(fullDirectory, useXmlFormat: false);
        if (File.Exists(csprojPath))
        {
            return new NewProjectError(NewProjectErrorKind.ProjectAlreadyExists, $"Project already exists: {csprojPath}");
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

    private static string? GetProjectNameError(string projectName)
    {
        if (!ValidProjectName.IsMatch(projectName))
        {
            return $"'{projectName}' is not a valid project name. Use letters, digits, '_', '-' and '.', starting with a letter or '_'.";
        }

        if (ReservedProjectNames.Contains(projectName, StringComparer.OrdinalIgnoreCase))
        {
            return $"'{projectName}' is the assembly name of a Gum runtime package. Pick another project name.";
        }

        return null;
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
        string directory, string csprojPath, string projectName, string rootNamespace, HostPlatform platform,
        string gumReference, string gumProjectFileName)
    {
        string platformFolder = platform.ToString();
        // The XNA-style hosts share Program.cs and run a Game1. KNI Skia draws through a Skia canvas,
        // so it has its own Game1.
        bool isXnaGame = platform is HostPlatform.MonoGame or HostPlatform.Kni or HostPlatform.KniSkia;
        string programFolder = isXnaGame ? "Xna" : platformFolder;

        File.WriteAllText(
            csprojPath,
            ReadTemplate(platformFolder, "Host.csproj.template", projectName, rootNamespace, gumProjectFileName)
                .Replace("{{GumReference}}", gumReference));
        File.WriteAllText(
            Path.Combine(directory, "Program.cs"),
            ReadTemplate(programFolder, "Program.cs.template", projectName, rootNamespace, gumProjectFileName));

        if (isXnaGame)
        {
            string gameFolder = platform == HostPlatform.KniSkia ? platformFolder : "Xna";
            File.WriteAllText(
                Path.Combine(directory, "Game1.cs"),
                ReadTemplate(gameFolder, "Game1.cs.template", projectName, rootNamespace, gumProjectFileName));
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
    private static void WriteCodeGenerationSettings(string gumProjectPath, string csprojPath)
    {
        AutoSetupResult setup = new CodeGenerationAutoSetupService().Run(gumProjectPath, csprojPath);
        CodeOutputProjectSettings settings = setup.Settings
            ?? throw new InvalidOperationException(setup.ErrorMessage);

        settings.OutputLibrary = OutputLibrary.GumForms;

        string gumFolder = Path.GetDirectoryName(gumProjectPath)! + Path.DirectorySeparatorChar;
        new CodeOutputProjectSettingsManager(new NullCodeGenLogger(), new FixedProjectDirectoryProvider(gumFolder))
            .WriteSettingsForProject(settings);
    }

    private static PlatformProjectResult Fail(string message) =>
        new PlatformProjectResult { Success = false, ErrorMessage = message };

    private class NullCodeGenLogger : ICodeGenLogger
    {
        public void PrintOutput(string message) { }

        public void PrintError(string message) { }
    }
}
