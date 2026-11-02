using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// An import never drops an element, behavior or file it cannot bring over without saying so (#6854):
/// each one is reported with its reason, and everything else still imports.
/// </summary>
public class GumxImportSkippedItemsTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "GumSkippedItems6854_" + Guid.NewGuid().ToString("N"));
    private readonly List<string> _registeredComponents = new();
    private readonly List<string> _registeredScreens = new();

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    public enum Failure
    {
        None,
        BehaviorSourcePathTargetMissing,
        BehaviorFileMalformed,
        ComponentFileMissing,
        ComponentFileMalformed,
        ScreenFileMissing,
        StandardFileMissing,
        TextureMissing,
        NameCollision,
        AnimationFileMalformed,
        UnrecognizedSourceFileExtension,
    }

    [Theory]
    [InlineData(Failure.BehaviorSourcePathTargetMissing, "Behaviors/BadBehavior", "not found")]
    [InlineData(Failure.BehaviorFileMalformed, "Behaviors/BadBehavior", "could not be read")]
    [InlineData(Failure.ComponentFileMissing, "BadComponent", "not found")]
    [InlineData(Failure.ComponentFileMalformed, "BadComponent", "could not be read")]
    [InlineData(Failure.ScreenFileMissing, "BadScreen", "not found")]
    [InlineData(Failure.StandardFileMissing, "Standards/Circle", "not found")]
    [InlineData(Failure.TextureMissing, "BadComponent", "missing.png")]
    [InlineData(Failure.NameCollision, "BadComponent", "already")]
    [InlineData(Failure.AnimationFileMalformed, "Components/BadComponent animations", "could not be read")]
    [InlineData(Failure.UnrecognizedSourceFileExtension, "art/pic.zzzq", "BadComponent")]
    [InlineData(Failure.UnrecognizedSourceFileExtension, "art/pic.zzzq", "extension")]
    public async Task ImportAsync_ItemThatCannotBeImported_IsReportedWithReasonAndTheRestStillImports(
        Failure failure, string expectedSkippedName, string expectedReasonText)
    {
        string sourceDirectory = Path.Combine(_root, "Source");
        string projectDirectory = Path.Combine(_root, "Project");
        Directory.CreateDirectory(projectDirectory);
        string sourcePath = WriteSourceProject(sourceDirectory, failure);

        GumxSourceService sourceService = new GumxSourceService();
        GumProjectSave? source = await sourceService.LoadProjectAsync(sourcePath);
        source.ShouldNotBeNull();
        GumxImportService sut = CreateService(projectDirectory, source.Version, failure);

        ImportSelections everything = new ImportSelections
        {
            DirectComponents = source.Components.ToList(),
            DirectScreens = source.Screens.ToList(),
            Behaviors = source.Behaviors.ToList(),
            Standards = source.StandardElements.ToList(),
        };
        ImportResult result = await sut.ImportAsync(everything, source, sourceService.GetSourceBase(sourcePath), destinationSubfolder: string.Empty);

        string skipped = result.SkippedElements.SingleOrDefault(name => name == expectedSkippedName)
            ?? throw new ShouldAssertException($"'{expectedSkippedName}' was not reported. Reported: [{string.Join(", ", result.SkippedElements)}]");
        result.SkippedReasons[skipped].ShouldContain(expectedReasonText);
        result.SkippedElements.Count.ShouldBe(1);

        _registeredComponents.ShouldContain("GoodComponent");
        _registeredScreens.ShouldContain("GoodScreen");
        File.Exists(Path.Combine(projectDirectory, "Behaviors", "GoodBehavior.behx")).ShouldBeTrue();
    }

    [Fact]
    public async Task ImportAsync_ItemOutsideTheCallersScope_IsIgnored()
    {
        string sourcePath = WriteSourceProject(Path.Combine(_root, "Source"), Failure.StandardFileMissing);
        GumxSourceService sourceService = new GumxSourceService();
        GumProjectSave source = (await sourceService.LoadProjectAsync(sourcePath))!;
        GumxImportService sut = CreateService(Path.Combine(_root, "Project"), source.Version, Failure.StandardFileMissing);
        Directory.CreateDirectory(Path.Combine(_root, "Project"));

        ImportSelections withoutStandards = new ImportSelections
        {
            DirectComponents = source.Components.ToList(),
            ReportSourceProblem = problem => problem.Kind != ImportFromGumxPlugin.ViewModels.ElementItemType.Standard,
        };
        ImportResult result = await sut.ImportAsync(withoutStandards, source, sourceService.GetSourceBase(sourcePath), string.Empty);

        result.SkippedElements.ShouldBeEmpty();
    }

    [Fact]
    public async Task ImportAsync_FailureAfterTheFirstWrite_ThrowsNamingWhatWasAlreadyWritten()
    {
        string projectDirectory = Path.Combine(_root, "Project");
        Directory.CreateDirectory(projectDirectory);
        string sourcePath = WriteSourceProject(Path.Combine(_root, "Source"), Failure.None);
        GumxSourceService sourceService = new GumxSourceService();
        GumProjectSave source = (await sourceService.LoadProjectAsync(sourcePath))!;
        Mock<IFileCommands> fileCommands = new();
        fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Throws(new IOException("No space left on device"));
        GumxImportService sut = CreateService(projectDirectory, source.Version, Failure.None, fileCommands.Object);

        ImportSelections everything = new ImportSelections
        {
            DirectComponents = source.Components.ToList(),
            Behaviors = source.Behaviors.ToList(),
            Standards = source.StandardElements.ToList(),
        };
        ImportFailedException failure = await Should.ThrowAsync<ImportFailedException>(() =>
            sut.ImportAsync(everything, source, sourceService.GetSourceBase(sourcePath), string.Empty));

        failure.Message.ShouldContain("No space left on device");
        failure.WrittenFiles.ShouldContain(Path.Combine(projectDirectory, "Standards", "Circle.gutx"));
        failure.WrittenFiles.ShouldContain(Path.Combine(projectDirectory, "Behaviors", "GoodBehavior.behx"));
        File.Exists(Path.Combine(projectDirectory, "Standards", "Circle.gutx")).ShouldBeTrue();
    }

    // What a behavior's SourcePath may point at, as it works today. Nothing contains it to the theme folder.
    [Theory]
    [InlineData("../Shared/B.behx", "Theme/Shared/B.behx", false)]
    [InlineData("../../Outside/B.behx", "Outside/B.behx", false)]
    [InlineData("..\\Shared\\B.behx", "Theme/Shared/B.behx", false)]
    [InlineData("<absolute>", "Elsewhere/B.behx", true)]
    public async Task ImportAsync_BehaviorSourcePath_IsFollowedWhereverItPoints(string sourcePath, string fileUnderRoot, bool isAbsolute)
    {
        string sourceDirectory = Path.Combine(_root, "Theme", "Sub");
        string projectDirectory = Path.Combine(_root, "Project");
        Directory.CreateDirectory(sourceDirectory);
        Directory.CreateDirectory(projectDirectory);
        string behaviorFile = Path.Combine(_root, fileUnderRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(behaviorFile)!);
        new BehaviorSave { Name = "B" }.Save(behaviorFile, useCompactFormat: true);
        GumProjectSave source = new GumProjectSave
        {
            FullFileName = Path.Combine(sourceDirectory, "Source.gumx"),
            Version = (int)GumProjectSave.GumxVersions.AttributeVersion,
        };
        source.Behaviors.Add(new BehaviorSave { Name = "B" });
        source.BehaviorReferences.Add(new BehaviorReference { Name = "B", SourcePath = isAbsolute ? behaviorFile : sourcePath });
        GumxImportService sut = CreateService(projectDirectory, source.Version, Failure.None);

        ImportResult result = await sut.ImportAsync(
            new ImportSelections { Behaviors = source.Behaviors.ToList() }, source, sourceDirectory, string.Empty);

        result.SkippedElements.ShouldBeEmpty();
        File.Exists(Path.Combine(projectDirectory, "Behaviors", "B.behx")).ShouldBeTrue();
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("GoodScreen", false)]
    public async Task ImportAsync_ElementTheProjectFailedToSave_FailsTheImportAndIsNotListedAsWritten(
        string? elementThatFailsToSave, bool expectSuccess)
    {
        string projectDirectory = Path.Combine(_root, "Project");
        Directory.CreateDirectory(projectDirectory);
        string sourcePath = WriteSourceProject(Path.Combine(_root, "Source"), Failure.None);
        GumxSourceService sourceService = new GumxSourceService();
        GumProjectSave source = (await sourceService.LoadProjectAsync(sourcePath))!;
        Mock<IFileCommands> fileCommands = new();
        fileCommands.Setup(x => x.TryAutoSaveProject(It.IsAny<bool>())).Returns(true);
        GumxImportService sut = CreateService(projectDirectory, source.Version, Failure.None, fileCommands.Object, elementThatFailsToSave);
        ImportSelections selections = new ImportSelections
        {
            DirectComponents = source.Components.ToList(),
            DirectScreens = source.Screens.ToList(),
        };

        Task<ImportResult> import = sut.ImportAsync(selections, source, sourceService.GetSourceBase(sourcePath), string.Empty);

        if (expectSuccess)
        {
            ImportResult result = await import;
            result.WrittenFiles.ShouldContain(Path.Combine(projectDirectory, "Screens", "GoodScreen.gusx"));
            return;
        }
        ImportFailedException failure = await Should.ThrowAsync<ImportFailedException>(() => import);
        failure.Message.ShouldContain("GoodScreen.gusx");
        failure.WrittenFiles.ShouldContain(Path.Combine(projectDirectory, "Components", "GoodComponent.gucx"));
        failure.WrittenFiles.ShouldNotContain(Path.Combine(projectDirectory, "Screens", "GoodScreen.gusx"));
    }

    [Fact]
    public void ImportFailedException_ListsAtMostFiveWrittenFiles()
    {
        string[] written = Enumerable.Range(0, 12).Select(i => $"C:/project/Components/C{i}.gucx").ToArray();

        ImportFailedException failure = new ImportFailedException(new IOException("disk full"), written);

        failure.Message.ShouldContain("12 file(s) were already written");
        failure.Message.ShouldContain("(and 7 more)");
        failure.Message.ShouldNotContain("C5.gucx");
    }

    [Fact]
    public void DescribeSkipped_PutsEachItemOnItsOwnLineWithItsReason()
    {
        ImportResult result = new ImportResult();
        result.AddSkipped("Neon/Controls/Icon", "texture icon.png was not found");
        result.AddSkipped("Behaviors/ButtonBehavior", "file not found: ../FormsBehaviors/ButtonBehavior.behx");
        result.SkippedElements.Add("Reasonless");

        result.DescribeSkipped().ShouldBe(new[]
        {
            "Neon/Controls/Icon: texture icon.png was not found",
            "Behaviors/ButtonBehavior: file not found: ../FormsBehaviors/ButtonBehavior.behx",
            "Reasonless",
        });
    }

    private GumxImportService CreateService(
        string projectDirectory, int version, Failure failure, IFileCommands? fileCommands = null,
        string? elementThatFailsToSave = null)
    {
        GumProjectSave project = new GumProjectSave
        {
            FullFileName = Path.Combine(projectDirectory, "Test.gumx"),
            Version = version,
        };
        Mock<IProjectState> projectState = new();
        projectState.Setup(x => x.GumProjectSave).Returns(project);
        projectState.Setup(x => x.ProjectDirectory).Returns(projectDirectory + Path.DirectorySeparatorChar);

        Mock<IImportLogic> importLogic = new();
        importLogic.Setup(x => x.ImportComponent(It.IsAny<ComponentSave>(), It.IsAny<bool>()))
            .Returns((ComponentSave component, bool _) =>
            {
                if (failure == Failure.NameCollision && component.Name == "BadComponent")
                {
                    return null;
                }
                _registeredComponents.Add(component.Name);
                WriteLikeTheProjectDoes(projectDirectory, "Components", component, elementThatFailsToSave);
                return component;
            });
        importLogic.Setup(x => x.ImportScreen(It.IsAny<ScreenSave>(), It.IsAny<bool>()))
            .Returns((ScreenSave screen, bool _) =>
            {
                _registeredScreens.Add(screen.Name);
                WriteLikeTheProjectDoes(projectDirectory, "Screens", screen, elementThatFailsToSave);
                return screen;
            });
        importLogic.Setup(x => x.ImportBehavior(It.IsAny<FilePath>(), It.IsAny<string?>(), It.IsAny<bool>()))
            .Returns(new BehaviorSave());

        return new GumxImportService(
            importLogic.Object, projectState.Object, fileCommands ?? new Mock<IFileCommands>().Object,
            new GumxSourceService(), new ImportVersionGate(), new Mock<IFileWatchIgnoreList>().Object);
    }

    // The real import logic saves each element it registers; a failed save shows a dialog and returns
    // as if nothing happened, which is what an element named in failsToSave does here.
    private static void WriteLikeTheProjectDoes(string projectDirectory, string subfolder, ElementSave element, string? failsToSave)
    {
        if (element.Name == failsToSave)
        {
            return;
        }
        string extension = subfolder == "Screens" ? GumProjectSave.ScreenExtension : GumProjectSave.ComponentExtension;
        Directory.CreateDirectory(Path.Combine(projectDirectory, subfolder));
        element.Save(Path.Combine(projectDirectory, subfolder, element.Name + "." + extension), useCompactFormat: true);
    }

    // A source project with one healthy component, screen and behavior, plus one bad item of the given kind.
    private static string WriteSourceProject(string directory, Failure failure)
    {
        GumProjectSave project = new GumProjectSave
        {
            FullFileName = Path.Combine(directory, "Source.gumx"),
            Version = (int)GumProjectSave.GumxVersions.AttributeVersion,
        };
        Directory.CreateDirectory(directory);
        AddComponent(project, "GoodComponent");
        AddScreen(project, "GoodScreen");
        BehaviorSave good = new BehaviorSave { Name = "GoodBehavior" };
        project.Behaviors.Add(good);
        project.BehaviorReferences.Add(new BehaviorReference { Name = "GoodBehavior" });

        switch (failure)
        {
            case Failure.BehaviorSourcePathTargetMissing:
                project.Behaviors.Add(new BehaviorSave { Name = "BadBehavior" });
                project.BehaviorReferences.Add(new BehaviorReference
                {
                    Name = "BadBehavior",
                    SourcePath = "../FormsBehaviors/BadBehavior.behx",
                });
                break;
            case Failure.BehaviorFileMalformed:
                project.Behaviors.Add(new BehaviorSave { Name = "BadBehavior" });
                project.BehaviorReferences.Add(new BehaviorReference { Name = "BadBehavior" });
                break;
            case Failure.ComponentFileMissing:
            case Failure.ComponentFileMalformed:
            case Failure.NameCollision:
            case Failure.AnimationFileMalformed:
                AddComponent(project, "BadComponent");
                break;
            case Failure.ScreenFileMissing:
                AddScreen(project, "BadScreen");
                break;
            case Failure.None:
                project.StandardElements.Add(new StandardElementSave { Name = "Circle" });
                project.StandardElementReferences.Add(new ElementReference { Name = "Circle", ElementType = ElementType.Standard });
                break;
            case Failure.StandardFileMissing:
                project.StandardElements.Add(new StandardElementSave { Name = "Circle" });
                project.StandardElementReferences.Add(new ElementReference { Name = "Circle", ElementType = ElementType.Standard });
                break;
            case Failure.UnrecognizedSourceFileExtension:
                ComponentSave unknown = AddComponent(project, "BadComponent");
                unknown.DefaultState.Variables.Add(new VariableSave { Name = "SourceFile", Value = "art/pic.zzzq" });
                Directory.CreateDirectory(Path.Combine(directory, "art"));
                File.WriteAllText(Path.Combine(directory, "art", "pic.zzzq"), "x");
                break;
            case Failure.TextureMissing:
                ComponentSave textured = AddComponent(project, "BadComponent");
                textured.DefaultState.Variables.Add(new VariableSave { Name = "SourceFile", Value = "missing.png" });
                break;
        }

        project.Save(project.FullFileName, saveElements: true);
        foreach (BehaviorSave behavior in project.Behaviors.Where(b => b.Name != "BadBehavior" || failure == Failure.BehaviorFileMalformed))
        {
            behavior.Save(Path.Combine(directory, "Behaviors", behavior.Name + ".behx"), useCompactFormat: true);
        }

        switch (failure)
        {
            case Failure.BehaviorFileMalformed:
                File.WriteAllText(Path.Combine(directory, "Behaviors", "BadBehavior.behx"), "<BehaviorSave><Name>BadBehavior");
                break;
            case Failure.ComponentFileMissing:
                File.Delete(Path.Combine(directory, "Components", "BadComponent.gucx"));
                break;
            case Failure.ComponentFileMalformed:
                File.WriteAllText(Path.Combine(directory, "Components", "BadComponent.gucx"), "<ComponentSave><Name>BadComponent");
                break;
            case Failure.AnimationFileMalformed:
                File.WriteAllText(Path.Combine(directory, "Components", "BadComponentAnimations.ganx"), "<ElementAnimationsSave><Animations>");
                break;
            case Failure.ScreenFileMissing:
                File.Delete(Path.Combine(directory, "Screens", "BadScreen.gusx"));
                break;
            case Failure.StandardFileMissing:
                File.Delete(Path.Combine(directory, "Standards", "Circle.gutx"));
                break;
        }
        return project.FullFileName;
    }

    private static ComponentSave AddComponent(GumProjectSave project, string name)
    {
        ComponentSave component = new ComponentSave { Name = name };
        component.States.Add(new StateSave { Name = "Default" });
        project.Components.Add(component);
        project.ComponentReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Component });
        return component;
    }

    private static void AddScreen(GumProjectSave project, string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default" });
        project.Screens.Add(screen);
        project.ScreenReferences.Add(new ElementReference { Name = name, ElementType = ElementType.Screen });
    }
}
