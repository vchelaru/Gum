using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

public class ReferencedFileChoicesTests : BaseTestClass
{
    private readonly string _projectDirectory;
    private readonly GumProjectSave _project = new GumProjectSave();
    private readonly ComponentSave _component = new ComponentSave { Name = "Button" };
    private readonly ReferencedFileChoices _choices;

    public ReferencedFileChoicesTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumReferencedFileChoices_" + Guid.NewGuid().ToString("N")) + "/";
        Directory.CreateDirectory(_projectDirectory);
        _project.FullFileName = _projectDirectory + "Project.gumx";
        _component.States.Add(new StateSave { Name = "Default", ParentContainer = _component });
        _project.Components.Add(_component);
        ObjectFinder.Self.GumProjectSave = _project;

        Mock<IProjectState> projectState = new Mock<IProjectState>();
        projectState.SetupGet(state => state.GumProjectSave).Returns(_project);
        projectState.SetupGet(state => state.ProjectDirectory).Returns(_projectDirectory);
        _choices = new ReferencedFileChoices(projectState.Object);
    }

    public override void Dispose()
    {
        base.Dispose();
        Directory.Delete(_projectDirectory, recursive: true);
    }

    [Fact]
    public void GetChoices_ListsEveryReferencedFileTheFilterAccepts_AlphabeticallyAndOnce()
    {
        AddSprite("Zebra", "zebra.png");
        AddSprite("Apple", "Art/apple.png");
        AddSprite("AppleAgain", "Art/apple.png");
        AddSprite("Sheet", "sheet.achx");
        AddText("Label", "fonts/label.fnt");

        IReadOnlyList<string> choices = _choices.GetChoices(ReferencedFileChoices.AcceptingExtensions("Images|*.png;*.achx"), currentValue: null);

        choices.ShouldBe(new[] { "Art/apple.png", "sheet.achx", "zebra.png" });
    }

    [Fact]
    public void GetChoices_LeavesOutAFileThatIsNotOnDisk()
    {
        AddSprite("Present", "present.png");
        AddSprite("Gone", "gone.png", createFile: false);

        IReadOnlyList<string> choices = _choices.GetChoices(_ => true, currentValue: null);

        choices.ShouldBe(new[] { "present.png" });
    }

    [Fact]
    public void GetChoices_AddsTheCurrentValueWhenTheProjectDoesNotReferenceIt_AndDoesNotRepeatOneItDoes()
    {
        AddSprite("Present", "present.png");

        _choices.GetChoices(_ => true, currentValue: "missing/other.png")
            .ShouldBe(new[] { "missing/other.png", "present.png" });
        _choices.GetChoices(_ => true, currentValue: "present.png")
            .ShouldBe(new[] { "present.png" });
    }

    [Fact]
    public void GetChoices_WithNoProject_ListsOnlyTheCurrentValue()
    {
        Mock<IProjectState> noProject = new Mock<IProjectState>();
        ReferencedFileChoices choices = new ReferencedFileChoices(noProject.Object);

        choices.GetChoices(_ => true, currentValue: null).ShouldBeEmpty();
        choices.GetChoices(_ => true, currentValue: "a.png").ShouldBe(new[] { "a.png" });
    }

    [Theory]
    [InlineData("Effect File (*.fx;*.slang)|*.fx;*.slang", "shaders/blur.fx", true)]
    [InlineData("Effect File (*.fx;*.slang)|*.fx;*.slang", "shaders/blur.SLANG", true)]
    [InlineData("Effect File (*.fx;*.slang)|*.fx;*.slang", "shaders/blur.png", false)]
    [InlineData("Bitmap Font Generator Font|*.fnt", "a.fnt", true)]
    [InlineData("All Files|*.*", "a.anything", true)]
    public void AcceptingExtensions_ReadsThePatternsOfAPickerFilter(string filter, string path, bool expected)
    {
        ReferencedFileChoices.AcceptingExtensions(filter)(path).ShouldBe(expected);
    }

    private void AddSprite(string name, string sourceFile, bool createFile = true)
    {
        AddInstance(name, "Sprite", "SourceFile", sourceFile, createFile);
    }

    private void AddText(string name, string customFontFile)
    {
        AddInstance(name, "Text", "CustomFontFile", customFontFile, createFile: true);
        _component.DefaultState.Variables.Add(new VariableSave { Name = name + ".UseCustomFont", Type = "bool", Value = true, SetsValue = true });
    }

    private void AddInstance(string name, string baseType, string variable, string file, bool createFile)
    {
        _component.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = _component });
        _component.DefaultState.Variables.Add(new VariableSave
        {
            Name = name + "." + variable,
            Type = "string",
            Value = file,
            IsFile = true,
            SetsValue = true,
        });

        if (createFile)
        {
            string path = _projectDirectory + file;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "");
        }
    }
}
