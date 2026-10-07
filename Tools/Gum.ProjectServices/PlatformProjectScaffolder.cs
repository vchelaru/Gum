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
        "GumCommon", "MonoGameGum", "KniGum", "FnaGum", "RaylibGum", "SkiaGum", "SilkNetGum"
    };

    private static readonly Regex ValidProjectName = new Regex("^[A-Za-z_][A-Za-z0-9_.-]*$");

    /// <inheritdoc/>
    public PlatformProjectResult Create(string projectDirectory, HostPlatform platform, bool includeFormsTemplate)
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

        Directory.CreateDirectory(gumFolder);

        string rootNamespace = projectName.Replace(".", "_").Replace("-", "_");
        WriteHostFiles(fullDirectory, csprojPath, projectName, rootNamespace, platform);

        if (includeFormsTemplate)
        {
            new FormsTemplateCreator().Create(gumProjectPath);
        }
        else
        {
            new ProjectCreator().Create(gumProjectPath);
        }

        WriteCodeGenerationSettings(gumProjectPath, csprojPath, platform);

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

    private static void WriteHostFiles(
        string directory, string csprojPath, string projectName, string rootNamespace, HostPlatform platform)
    {
        string platformFolder = platform.ToString();
        string sharedFolder = platform is HostPlatform.MonoGame or HostPlatform.Kni ? "Xna" : platformFolder;

        File.WriteAllText(csprojPath, ReadTemplate(platformFolder, "Host.csproj.template", projectName, rootNamespace));
        File.WriteAllText(
            Path.Combine(directory, "Program.cs"),
            ReadTemplate(sharedFolder, "Program.cs.template", projectName, rootNamespace));

        if (sharedFolder == "Xna")
        {
            File.WriteAllText(
                Path.Combine(directory, "Game1.cs"),
                ReadTemplate(sharedFolder, "Game1.cs.template", projectName, rootNamespace));
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

    // Auto-detection can't tell the runtimes apart (it only recognizes MonoGame-family and raylib
    // package references), so the library is set from the platform the project was created for.
    private static void WriteCodeGenerationSettings(string gumProjectPath, string csprojPath, HostPlatform platform)
    {
        AutoSetupResult setup = new CodeGenerationAutoSetupService().Run(gumProjectPath, csprojPath);
        CodeOutputProjectSettings settings = setup.Settings
            ?? throw new InvalidOperationException(setup.ErrorMessage);

        settings.OutputLibrary = platform switch
        {
            HostPlatform.Raylib => OutputLibrary.Raylib,
            _ => OutputLibrary.MonoGameForms
        };

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
