using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Serialization.Json;
using Gum.Logic.FileWatch;
using Gum.StateAnimation.SaveClasses;
using Moq;
using Shouldly;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Covers the "Convert to JSON" service (issue #4175): writing <c>.gumj</c>/<c>.gusj</c>/<c>.gucj</c>/
/// <c>.gutj</c>/<c>.behj</c>/<c>.ganj</c> siblings for an XML project without touching the XML.
/// </summary>
public class ConvertProjectToJsonServiceTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly Mock<IFileWatchIgnoreList> _fileWatchIgnoreList;
    private readonly IConvertProjectToJsonService _service;

    public ConvertProjectToJsonServiceTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumConvertToJsonTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _fileWatchIgnoreList = new Mock<IFileWatchIgnoreList>();
        _service = new ConvertProjectToJsonService(_fileWatchIgnoreList.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ConvertToJson_FullProject_WritesJsonSiblingsForEveryReferencedFileAndLeavesXmlUntouched()
    {
        string gumxPath = Path.Combine(_tempDirectory, "Project.gumx");
        GumProjectSave project = new GumProjectSave { Version = GumProjectSave.NativeVersion, FullFileName = gumxPath.Replace('\\', '/') };
        ScreenSave screen = new ScreenSave { Name = "MainMenu" };
        ComponentSave component = new ComponentSave { Name = "Button" };
        StandardElementSave standard = new StandardElementSave { Name = "Container" };
        project.Screens.Add(screen);
        project.Components.Add(component);
        project.StandardElements.Add(standard);
        project.Behaviors.Add(new BehaviorSave { Name = "ButtonBehavior", DefaultImplementation = "Controls/Button" });
        project.BehaviorReferences.Add(new BehaviorReference { Name = "ButtonBehavior" });

        // Writes the real XML files this project would have on disk (.gumx/.gusx/.gucx/.gutx),
        // so the "leaves XML untouched" assertion below is meaningful.
        project.Save(gumxPath, saveElements: true);
        string behaviorXmlPath = Path.Combine(_tempDirectory, "Behaviors", "ButtonBehavior.behx");
        Directory.CreateDirectory(Path.GetDirectoryName(behaviorXmlPath)!);
        project.Behaviors[0].Save(behaviorXmlPath);
        string animationXmlPath = Path.Combine(_tempDirectory, "Components", "ButtonAnimations.ganx");
        WriteAnimationXml(animationXmlPath, animationName: "FadeIn");

        byte[] gumxBytesBefore = File.ReadAllBytes(gumxPath);
        byte[] componentXmlBytesBefore = File.ReadAllBytes(Path.Combine(_tempDirectory, "Components", "Button.gucx"));
        byte[] behaviorXmlBytesBefore = File.ReadAllBytes(behaviorXmlPath);
        byte[] animationXmlBytesBefore = File.ReadAllBytes(animationXmlPath);

        ConvertProjectToJsonResult result = _service.ConvertToJson(project);

        string gumjPath = Path.Combine(_tempDirectory, "Project.gumj");
        result.ProjectFilePath.ShouldBe(gumjPath.Replace('\\', '/'));
        result.ScreenCount.ShouldBe(1);
        result.ComponentCount.ShouldBe(1);
        result.StandardElementCount.ShouldBe(1);
        result.BehaviorCount.ShouldBe(1);
        result.AnimationCount.ShouldBe(1);
        result.TotalFileCount.ShouldBe(6);

        File.Exists(gumjPath).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDirectory, "Screens", "MainMenu.gusj")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDirectory, "Components", "Button.gucj")).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDirectory, "Standards", "Container.gutj")).ShouldBeTrue();
        string behaviorJsonPath = Path.Combine(_tempDirectory, "Behaviors", "ButtonBehavior.behj");
        File.Exists(behaviorJsonPath).ShouldBeTrue();
        string animationJsonPath = Path.Combine(_tempDirectory, "Components", "ButtonAnimations.ganj");
        File.Exists(animationJsonPath).ShouldBeTrue();

        BehaviorSave loadedBehavior = GumJsonFileSerializer.DeserializeBehavior(File.ReadAllText(behaviorJsonPath));
        loadedBehavior.DefaultImplementation.ShouldBe("Controls/Button");
        ElementAnimationsSave loadedAnimations = GumAnimationJsonFileSerializer.DeserializeElementAnimations(File.ReadAllText(animationJsonPath));
        loadedAnimations.Animations.Single().Name.ShouldBe("FadeIn");

        // Non-destructive: none of the original XML files were modified.
        File.ReadAllBytes(gumxPath).ShouldBe(gumxBytesBefore);
        File.ReadAllBytes(Path.Combine(_tempDirectory, "Components", "Button.gucx")).ShouldBe(componentXmlBytesBefore);
        File.ReadAllBytes(behaviorXmlPath).ShouldBe(behaviorXmlBytesBefore);
        File.ReadAllBytes(animationXmlPath).ShouldBe(animationXmlBytesBefore);
    }

    [Fact]
    public void ConvertToJson_FullProject_RegistersFileWatchIgnoreForEveryJsonFileBeforeWritingIt()
    {
        string gumxPath = Path.Combine(_tempDirectory, "Project.gumx");
        GumProjectSave project = new GumProjectSave { Version = GumProjectSave.NativeVersion, FullFileName = gumxPath.Replace('\\', '/') };
        ComponentSave component = new ComponentSave { Name = "Button" };
        project.Components.Add(component);
        project.Behaviors.Add(new BehaviorSave { Name = "ButtonBehavior", DefaultImplementation = "Controls/Button" });
        project.BehaviorReferences.Add(new BehaviorReference { Name = "ButtonBehavior" });
        project.Save(gumxPath, saveElements: true);
        string behaviorXmlPath = Path.Combine(_tempDirectory, "Behaviors", "ButtonBehavior.behx");
        Directory.CreateDirectory(Path.GetDirectoryName(behaviorXmlPath)!);
        project.Behaviors[0].Save(behaviorXmlPath);
        string animationXmlPath = Path.Combine(_tempDirectory, "Components", "ButtonAnimations.ganx");
        WriteAnimationXml(animationXmlPath, animationName: "FadeIn");

        List<FilePath> ignoredBeforeGumjWritten = new List<FilePath>();
        _fileWatchIgnoreList
            .Setup(x => x.IgnoreNextChangeUntil(It.IsAny<FilePath>(), null))
            .Callback<FilePath, DateTime?>((path, _) =>
            {
                if (!File.Exists(path.FullPath))
                {
                    ignoredBeforeGumjWritten.Add(path);
                }
            });

        _service.ConvertToJson(project);

        string gumjPath = Path.Combine(_tempDirectory, "Project.gumj");
        string componentJsonPath = Path.Combine(_tempDirectory, "Components", "Button.gucj");
        string behaviorJsonPath = Path.Combine(_tempDirectory, "Behaviors", "ButtonBehavior.behj");
        string animationJsonPath = Path.Combine(_tempDirectory, "Components", "ButtonAnimations.ganj");

        // Every path was ignored before its file existed on disk - i.e. before the write happened,
        // not after (issue #4219: registering after the write is too late to mute the watcher).
        ignoredBeforeGumjWritten.ShouldContain((FilePath)gumjPath);
        ignoredBeforeGumjWritten.ShouldContain((FilePath)componentJsonPath);
        ignoredBeforeGumjWritten.ShouldContain((FilePath)behaviorJsonPath);
        ignoredBeforeGumjWritten.ShouldContain((FilePath)animationJsonPath);
    }

    [Fact]
    public void ConvertToJson_ElementWithMissingSourceFile_IsSkippedAndNotCounted()
    {
        string gumxPath = Path.Combine(_tempDirectory, "Project.gumx");
        GumProjectSave project = new GumProjectSave { Version = GumProjectSave.NativeVersion, FullFileName = gumxPath.Replace('\\', '/') };
        project.Screens.Add(new ScreenSave { Name = "GhostScreen", IsSourceFileMissing = true });
        project.Screens.Add(new ScreenSave { Name = "RealScreen" });

        ConvertProjectToJsonResult result = _service.ConvertToJson(project);

        result.ScreenCount.ShouldBe(1);
        File.Exists(Path.Combine(_tempDirectory, "Screens", "GhostScreen.gusj")).ShouldBeFalse();
        File.Exists(Path.Combine(_tempDirectory, "Screens", "RealScreen.gusj")).ShouldBeTrue();
    }

    [Fact]
    public void ConvertToJson_NoAnimationFilePresent_AnimationCountIsZeroAndNoGanjWritten()
    {
        string gumxPath = Path.Combine(_tempDirectory, "Project.gumx");
        GumProjectSave project = new GumProjectSave { Version = GumProjectSave.NativeVersion, FullFileName = gumxPath.Replace('\\', '/') };
        project.Components.Add(new ComponentSave { Name = "Button" });

        ConvertProjectToJsonResult result = _service.ConvertToJson(project);

        result.AnimationCount.ShouldBe(0);
        File.Exists(Path.Combine(_tempDirectory, "Components", "ButtonAnimations.ganj")).ShouldBeFalse();
    }

    [Fact]
    public void ConvertToJson_ProjectAlreadyJson_ThrowsInvalidOperationException()
    {
        GumProjectSave project = new GumProjectSave
        {
            FullFileName = Path.Combine(_tempDirectory, "Project.gumj").Replace('\\', '/')
        };

        Should.Throw<InvalidOperationException>(() => _service.ConvertToJson(project));
    }

    [Fact]
    public void ConvertToJson_ProjectWithoutFullFileName_ThrowsInvalidOperationException()
    {
        GumProjectSave project = new GumProjectSave();

        Should.Throw<InvalidOperationException>(() => _service.ConvertToJson(project));
    }

    private static void WriteAnimationXml(string path, string animationName)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        AnimationSave animation = new AnimationSave { Name = animationName };
        ElementAnimationsSave animations = new ElementAnimationsSave();
        animations.Animations.Add(animation);
        FileManager.XmlSerialize(animations, path);
    }
}
