using Gum.DataTypes;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Moq;
using Shouldly;
using StateAnimationPlugin.Managers;
using System;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace GumToolUnitTests.Plugins.StateAnimationPlugin;

/// <summary>
/// Characterization tests pinning <see cref="AnimationCollectionViewModelManager"/>'s animation-file
/// load behavior after it was drained from a Singleton to constructor injection. The file-path
/// resolution is mocked via <see cref="IAnimationFilePathService"/> so these run without a loaded
/// project or the service locator.
/// </summary>
public class AnimationCollectionViewModelManagerTests : BaseTestClass
{
    private readonly string _tempDirectory;
    private readonly Mock<IAnimationFilePathService> _animationFilePathService;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IOutputManager> _outputManager;
    private readonly Mock<IFileWatchManager> _fileWatchManager;
    private readonly AnimationCollectionViewModelManager _manager;

    public AnimationCollectionViewModelManagerTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(), "GumAcvmmTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        _animationFilePathService = new Mock<IAnimationFilePathService>();
        _selectedState = new Mock<ISelectedState>();
        _outputManager = new Mock<IOutputManager>();
        _fileWatchManager = new Mock<IFileWatchManager>();

        _manager = new AnimationCollectionViewModelManager(
            _selectedState.Object,
            _outputManager.Object,
            _fileWatchManager.Object,
            _animationFilePathService.Object,
            () => throw new InvalidOperationException(
                "The animation view model factory should not be invoked by these tests."));
    }

    [Fact]
    public void GetAnimationCollectionViewModel_returns_null_for_null_element()
    {
        _manager.GetAnimationCollectionViewModel(null).ShouldBeNull();
    }

    [Fact]
    public void GetElementAnimationsSave_deserializes_existing_file()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        FilePath ganxPath = new FilePath(Path.Combine(_tempDirectory, "FooAnimations.ganx"));

        ElementAnimationsSave toWrite = new ElementAnimationsSave { ElementName = "Foo" };
        toWrite.Animations.Add(new AnimationSave { Name = "Walk", Loops = true });
        FileManager.XmlSerialize(toWrite, ganxPath.FullPath);

        _animationFilePathService
            .Setup(x => x.GetAbsoluteAnimationFileNameFor(It.IsAny<ElementSave>()))
            .Returns(ganxPath);

        ElementAnimationsSave? loaded = _manager.GetElementAnimationsSave(element);

        loaded.ShouldNotBeNull();
        loaded.Animations.Count.ShouldBe(1);
        loaded.Animations[0].Name.ShouldBe("Walk");
        loaded.Animations[0].Loops.ShouldBeTrue();
    }

    [Fact]
    public void GetElementAnimationsSave_deserializes_ganj_as_json()
    {
        // Issue #4595: the path was resolved as .ganj for a JSON project but the file was still
        // read with the XML deserializer, so a converted project's animations came back empty and
        // the next save overwrote them.
        ComponentSave element = new ComponentSave { Name = "Foo" };
        FilePath ganjPath = new FilePath(Path.Combine(_tempDirectory, "FooAnimations.ganj"));

        ElementAnimationsSave toWrite = new ElementAnimationsSave { ElementName = "Foo" };
        toWrite.Animations.Add(new AnimationSave { Name = "Walk", Loops = true });
        toWrite.Save(ganjPath.FullPath);

        _animationFilePathService
            .Setup(x => x.GetAbsoluteAnimationFileNameFor(It.IsAny<ElementSave>()))
            .Returns(ganjPath);

        ElementAnimationsSave? loaded = _manager.GetElementAnimationsSave(element);

        loaded.ShouldNotBeNull();
        loaded.Animations.Count.ShouldBe(1);
        loaded.Animations[0].Name.ShouldBe("Walk");
        loaded.Animations[0].Loops.ShouldBeTrue();
    }

    [Fact]
    public void SaveElementAnimations_writes_json_when_path_is_ganj()
    {
        // The write side of the same bug: XML written to a .ganj path parses in the tool but breaks
        // the runtime's animation loader, which JSON-deserializes every *Animations.ganj it finds.
        ComponentSave element = new ComponentSave { Name = "Foo" };
        FilePath ganjPath = new FilePath(Path.Combine(_tempDirectory, "FooAnimations.ganj"));
        _animationFilePathService
            .Setup(x => x.GetAbsoluteAnimationFileNameFor(It.IsAny<ElementSave>()))
            .Returns(ganjPath);

        ElementAnimationsSave toWrite = new ElementAnimationsSave { ElementName = "Foo" };
        toWrite.Animations.Add(new AnimationSave { Name = "Walk" });

        _manager.SaveElementAnimations(element, toWrite);

        File.ReadAllText(ganjPath.FullPath).TrimStart('\uFEFF', ' ', '\r', '\n').ShouldStartWith("{");
        ElementAnimationsSave.Load(ganjPath.FullPath).Animations[0].Name.ShouldBe("Walk");
    }

    [Fact]
    public void SaveElementAnimations_writes_xml_when_path_is_ganx()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        FilePath ganxPath = new FilePath(Path.Combine(_tempDirectory, "FooAnimations.ganx"));
        _animationFilePathService
            .Setup(x => x.GetAbsoluteAnimationFileNameFor(It.IsAny<ElementSave>()))
            .Returns(ganxPath);

        ElementAnimationsSave toWrite = new ElementAnimationsSave { ElementName = "Foo" };
        toWrite.Animations.Add(new AnimationSave { Name = "Walk" });

        _manager.SaveElementAnimations(element, toWrite);

        File.ReadAllText(ganxPath.FullPath).TrimStart('\uFEFF', ' ', '\r', '\n').ShouldStartWith("<");
        ElementAnimationsSave.Load(ganxPath.FullPath).Animations[0].Name.ShouldBe("Walk");
    }

    [Fact]
    public void GetElementAnimationsSave_returns_null_when_file_missing()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        FilePath missing = new FilePath(Path.Combine(_tempDirectory, "FooAnimations.ganx"));
        _animationFilePathService
            .Setup(x => x.GetAbsoluteAnimationFileNameFor(It.IsAny<ElementSave>()))
            .Returns(missing);

        _manager.GetElementAnimationsSave(element).ShouldBeNull();
    }

    [Fact]
    public void GetElementAnimationsSave_returns_null_when_path_is_null()
    {
        ComponentSave element = new ComponentSave { Name = "Foo" };
        _animationFilePathService
            .Setup(x => x.GetAbsoluteAnimationFileNameFor(It.IsAny<ElementSave>()))
            .Returns((FilePath?)null);

        _manager.GetElementAnimationsSave(element).ShouldBeNull();
    }

    public override void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }

        base.Dispose();
    }
}
