using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class FormsTemplateCreatorTests : IDisposable
{
    private readonly FormsTemplateCreator _sut;
    private readonly string _tempDirectory;

    public FormsTemplateCreatorTests()
    {
        _sut = new FormsTemplateCreator();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumFormsTemplateCreatorTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Create_ShouldCreateBehaviorFiles()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        string behaviorsDir = Path.Combine(_tempDirectory, "Behaviors");
        File.Exists(Path.Combine(behaviorsDir, "ButtonBehavior.behx")).ShouldBeTrue();
        File.Exists(Path.Combine(behaviorsDir, "ListBoxBehavior.behx")).ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldCreateControlComponents()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        string controlsDir = Path.Combine(_tempDirectory, "Components", "Controls");
        File.Exists(Path.Combine(controlsDir, "ButtonStandard.gucx")).ShouldBeTrue();
        File.Exists(Path.Combine(controlsDir, "CheckBox.gucx")).ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldCreateGumxFile()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        File.Exists(filePath).ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldCreateStandardFiles()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        string standardsDir = Path.Combine(_tempDirectory, "Standards");
        string[] expectedElements = { "Circle", "Component", "Container",
            "NineSlice", "Polygon", "Rectangle", "Sprite", "Text" };

        foreach (string name in expectedElements)
        {
            File.Exists(Path.Combine(standardsDir, $"{name}.gutx")).ShouldBeTrue($"{name}.gutx should exist");
        }

        // ColoredRectangle was dropped from the v3 FormsTemplate in favor of Rectangle's full fill surface.
        File.Exists(Path.Combine(standardsDir, "ColoredRectangle.gutx")).ShouldBeFalse();
    }

    [Fact]
    public void Create_ShouldCreateUISpriteSheet()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        File.Exists(Path.Combine(_tempDirectory, "UISpriteSheet.png")).ShouldBeTrue();
    }

    [Fact]
    public void Create_ShouldProduceLoadableProject()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        ProjectLoader loader = new ProjectLoader();
        ProjectLoadResult result = loader.Load(filePath);

        result.Success.ShouldBeTrue();
        result.Project.ShouldNotBeNull();
        result.LoadErrors.ShouldBeEmpty();
        result.Project!.Version.ShouldBe(GumProjectSave.NativeVersion);
    }

    [Fact]
    public void Create_ShouldRenameGumxToMatchProjectName()
    {
        string filePath = Path.Combine(_tempDirectory, "MyCustomProject.gumx");

        _sut.Create(filePath);

        File.Exists(filePath).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDirectory, "GumProject.gumx")).ShouldBeFalse();
    }

    [Fact]
    public void Create_StylesColorReferences_ShouldTargetFillChannels()
    {
        // The Styles swatches moved from ColoredRectangle (flat Red/Green/Blue) to the v3
        // Rectangle Fill surface (FillRed/FillGreen/FillBlue). The ColorCategory states in the
        // themed standards (NineSlice/Sprite/Text) reference those swatch colors by name, so any
        // reference still pointing at a legacy .Red/.Green/.Blue channel is now dangling.
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");

        _sut.Create(filePath);

        ProjectLoadResult result = new ProjectLoader().Load(filePath);
        result.Success.ShouldBeTrue();

        string[] legacyChannels = { "Red", "Green", "Blue", "Alpha" };
        List<string> dangling = new();

        IEnumerable<ElementSave> allElements = result.Project!.StandardElements
            .Cast<ElementSave>()
            .Concat(result.Project.Components);

        foreach (ElementSave element in allElements)
        {
            foreach (var state in element.AllStates)
            {
                foreach (var variableList in state.VariableLists)
                {
                    if (variableList.GetRootName() != "VariableReferences")
                    {
                        continue;
                    }

                    foreach (string referenceString in variableList.ValueAsIList.Cast<string>())
                    {
                        int equalsIndex = referenceString.IndexOf('=');
                        if (equalsIndex < 0)
                        {
                            continue;
                        }

                        string right = referenceString.Substring(equalsIndex + 1).Trim();
                        if (!right.Contains("Styles."))
                        {
                            continue;
                        }

                        string channel = right.Substring(right.LastIndexOf('.') + 1);
                        if (legacyChannels.Contains(channel))
                        {
                            dangling.Add($"{element.Name} / {state.Name}: {referenceString}");
                        }
                    }
                }
            }
        }

        dangling.ShouldBeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
