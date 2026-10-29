using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
        string? themeDirectory = null)
    {
        string fullDirectory = Path.GetFullPath(projectDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string projectName = Path.GetFileName(fullDirectory);

        string? nameError = GetProjectNameError(projectName);
        if (nameError != null)
        {
            return Fail(nameError);
        }

        string csprojPath = Path.Combine(fullDirectory, projectName + ".csproj");
        string gumFolder = Path.Combine(fullDirectory, "Content", GumProjectFolderName);
        string gumProjectPath = Path.Combine(gumFolder, GumProjectFolderName + ".gumj");

        if (File.Exists(csprojPath))
        {
            return Fail($"Project already exists: {csprojPath}");
        }

        if (File.Exists(gumProjectPath))
        {
            return Fail($"Gum project already exists: {gumProjectPath}");
        }

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
            gumReference = $"<ProjectReference Include=\"{include}\" />";
        }

        Directory.CreateDirectory(gumFolder);

        string rootNamespace = NameRules.ToCSharpIdentifier(projectName);
        WriteHostFiles(fullDirectory, csprojPath, projectName, rootNamespace, platform, gumReference);

        if (themeDirectory != null)
        {
            new FormsTemplateCreator().CreateFromTheme(gumProjectPath, themeDirectory);
        }
        else if (includeFormsTemplate)
        {
            new FormsTemplateCreator().Create(gumProjectPath);
        }
        else
        {
            new ProjectCreator().Create(gumProjectPath);
        }

        WriteCodeGenerationSettings(gumProjectPath, csprojPath);

        return new PlatformProjectResult
        {
            Success = true,
            CsprojPath = csprojPath,
            GumProjectPath = gumProjectPath
        };
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
        string gumReference)
    {
        string platformFolder = platform.ToString();
        // The XNA-style hosts share Program.cs and run a Game1. KNI Skia draws through a Skia canvas,
        // so it has its own Game1.
        bool isXnaGame = platform is HostPlatform.MonoGame or HostPlatform.Kni or HostPlatform.KniSkia;
        string programFolder = isXnaGame ? "Xna" : platformFolder;

        File.WriteAllText(
            csprojPath,
            ReadTemplate(platformFolder, "Host.csproj.template", projectName, rootNamespace)
                .Replace("{{GumReference}}", gumReference));
        File.WriteAllText(
            Path.Combine(directory, "Program.cs"),
            ReadTemplate(programFolder, "Program.cs.template", projectName, rootNamespace));

        if (isXnaGame)
        {
            string gameFolder = platform == HostPlatform.KniSkia ? platformFolder : "Xna";
            File.WriteAllText(
                Path.Combine(directory, "Game1.cs"),
                ReadTemplate(gameFolder, "Game1.cs.template", projectName, rootNamespace));
        }
    }

    private static string ReadTemplate(string folder, string fileName, string projectName, string rootNamespace)
    {
        string resourceName = ResourcePrefix + folder + "." + fileName;
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded template {resourceName}");
        using StreamReader reader = new StreamReader(stream);

        return reader.ReadToEnd()
            .Replace("{{ProjectName}}", projectName)
            .Replace("{{RootNamespace}}", rootNamespace);
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
