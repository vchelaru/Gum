using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Logic.FileWatch;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.ImportPlugin.Services;
using Gum.ProjectServices;
using Gum.ToolStates;
using ImportFromGumxPlugin.Services;
using ImportFromGumxPlugin.ViewModels;
using Moq;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Add Forms must bring in every behavior the standard theme lists (#6854). The checked-in template
/// links most of them with a SourcePath into the repo's shared FormsBehaviors folder; a staged
/// theme has them flattened into its own Behaviors folder.
/// </summary>
public class FormsThemeBehaviorImportTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GumFormsBehaviorImport_" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Fact]
    public async Task ImportAsync_StaleStagedTheme_DoesNotSilentlyDropBehaviors()
    {
        // Behaviors/ is filled but the .gumx still carries the SourcePath links: the shape of a
        // theme staged before SourcePaths were dropped from the staged project (the maintainer's
        // Tool/Gum.Avalonia/bin/Debug/net10.0/Content/FormsThemes/Standard on 2026-10-26).
        string themeDirectory = Path.Combine(_root, "StaleTheme");
        CopyDirectory(Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsTemplate"), themeDirectory);
        Directory.CreateDirectory(Path.Combine(themeDirectory, "Behaviors"));
        foreach (string behaviorFile in Directory.GetFiles(
            Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsBehaviors"), "*.behx"))
        {
            File.Copy(behaviorFile, Path.Combine(themeDirectory, "Behaviors", Path.GetFileName(behaviorFile)), overwrite: true);
        }

        (string[] expected, string[] onDisk, ImportResult result) = await ImportBehaviorsAsync(themeDirectory);

        expected.Length.ShouldBeGreaterThan(20);
        string[] missingAndUnreported = expected
            .Where(name => !onDisk.Contains(name) && !result.SkippedElements.Any(skipped => skipped.Contains(name)))
            .ToArray();
        missingAndUnreported.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportAsync_FreshlyStagedTheme_ImportsEveryBehavior()
    {
        string themeDirectory = Path.Combine(_root, "FreshTheme");
        string templateDirectory = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsTemplate");
        CopyDirectory(templateDirectory, themeDirectory);
        new FormsThemeBehaviorStagingService().Stage(
            Path.Combine(templateDirectory, "GumProject.gumx"),
            Path.Combine(themeDirectory, "Behaviors"));

        (string[] expected, string[] onDisk, ImportResult _) = await ImportBehaviorsAsync(themeDirectory);

        expected.Length.ShouldBeGreaterThan(20);
        onDisk.OrderBy(name => name).ShouldBe(expected.OrderBy(name => name));
    }

    private async Task<(string[] Expected, string[] OnDisk, ImportResult Result)> ImportBehaviorsAsync(string themeDirectory)
    {
        string projectDirectory = Path.Combine(_root, "Project");
        Directory.CreateDirectory(projectDirectory);
        GumProjectSave project = new GumProjectSave { FullFileName = Path.Combine(projectDirectory, "Test.gumx") };

        Mock<IProjectState> projectState = new();
        projectState.Setup(x => x.GumProjectSave).Returns(project);
        projectState.Setup(x => x.ProjectDirectory).Returns(projectDirectory + Path.DirectorySeparatorChar);
        Mock<IImportLogic> importLogic = new();
        importLogic.Setup(x => x.ImportBehavior(It.IsAny<FilePath>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .Returns(new BehaviorSave());

        GumxSourceService sourceService = new GumxSourceService();
        GumxImportService importService = new GumxImportService(
            importLogic.Object, projectState.Object, new Mock<Gum.Commands.IFileCommands>().Object,
            sourceService, new ImportVersionGate(), new Mock<IFileWatchIgnoreList>().Object);

        string themeProjectPath = Path.Combine(themeDirectory, "GumProject.gumx");
        GumProjectSave? theme = await sourceService.LoadProjectAsync(themeProjectPath);
        theme.ShouldNotBeNull();
        project.Version = theme.Version;

        ImportSelections selections = new ImportSelections { Behaviors = theme.Behaviors.ToList() };
        ImportResult result = await importService.ImportAsync(
            selections, theme, sourceService.GetSourceBase(themeProjectPath), destinationSubfolder: string.Empty);

        string behaviorsDirectory = Path.Combine(projectDirectory, "Behaviors");
        string[] onDisk = Directory.Exists(behaviorsDirectory)
            ? Directory.GetFiles(behaviorsDirectory, "*.behx").Select(Path.GetFileNameWithoutExtension).ToArray()!
            : Array.Empty<string>();
        return (theme.Behaviors.Select(behavior => behavior.Name).ToArray(), onDisk, result);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }
        foreach (string directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 12; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates")))
            {
                return current;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate repo root from " + AppContext.BaseDirectory);
    }
}
