using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
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

    [Fact]
    public void Create_ColorCategoryOwningComponents_ShouldOwnTheirOwnLocalCategory()
    {
        // CautionLines/VerticalLines/Icon/PercentBar/PercentBarIcon each used to forward a
        // friendly-named color property (LineColor/IconColor/BarColor) via ExposedAsName to a
        // child instance's ColorCategoryState. ExposedAsName forwarding requires the
        // underlying property to be instance-qualified (GraphicalUiElement.SetProperty splits
        // it on '.'); an early version of this migration promoted the property to live
        // unqualified on the owning component while keeping ExposedAsName, which crashes at
        // runtime (ArgumentOutOfRangeException). The fix: these components now own their
        // "ColorCategoryState" directly (unqualified, no ExposedAsName), matching the same
        // pattern already used by ButtonCategoryState/IconCategoryState elsewhere. Pin that
        // shape directly.
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");
        _sut.Create(filePath);

        ProjectLoadResult result = new ProjectLoader().Load(filePath);
        result.Success.ShouldBeTrue();

        string[] owningElements =
        [
            "Elements/CautionLines",
            "Elements/VerticalLines",
            "Elements/Icon",
            "Elements/PercentBar",
            "Elements/PercentBarIcon",
        ];

        foreach (string elementName in owningElements)
        {
            ComponentSave component = result.Project!.Components.First(c => c.Name == elementName);

            VariableSave? colorState = component.DefaultState.Variables
                .FirstOrDefault(v => v.Name == "ColorCategoryState");

            colorState.ShouldNotBeNull($"{elementName} should own an unqualified ColorCategoryState");
            colorState!.SourceObject.ShouldBeNull(
                $"{elementName}.ColorCategoryState should live on the component itself, not forward to a child");
            colorState.ExposedAsName.ShouldBeNullOrEmpty(
                $"{elementName}.ColorCategoryState is not instance-qualified, so it cannot use ExposedAsName " +
                "(that mechanism requires forwarding to a named child instance)");

            StateSaveCategory? category = component.Categories.FirstOrDefault(c => c.Name == "ColorCategory");
            category.ShouldNotBeNull($"{elementName} should own a local ColorCategory backing ColorCategoryState");
            category!.States.Select(s => s.Name).ShouldContain("Primary");
            category.States.Count.ShouldBe(12);
        }
    }

    [Fact]
    public void Create_PercentBarIconBarIconColor_ShouldForwardThroughIconsOwnColorCategoryState()
    {
        // BarIconColor is a genuine forward (PercentBarIcon -> IconInstance.ColorCategoryState),
        // legitimate because the underlying name is instance-qualified - Icon.ColorCategoryState
        // itself is covered (unqualified, un-exposed) by the test above.
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");
        _sut.Create(filePath);

        ProjectLoadResult result = new ProjectLoader().Load(filePath);
        result.Success.ShouldBeTrue();

        ComponentSave percentBarIcon = result.Project!.Components.First(c => c.Name == "Elements/PercentBarIcon");
        VariableSave? forward = percentBarIcon.DefaultState.Variables
            .FirstOrDefault(v => v.ExposedAsName == "BarIconColor");

        forward.ShouldNotBeNull();
        forward!.SourceObject.ShouldBe("IconInstance");
        forward.GetRootName().ShouldBe("ColorCategoryState");
    }

    [Fact]
    public void Create_StandardElements_ShouldNoLongerOwnColorCategory()
    {
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");
        _sut.Create(filePath);

        ProjectLoadResult result = new ProjectLoader().Load(filePath);
        result.Success.ShouldBeTrue();

        string[] formerOwners = ["NineSlice", "Sprite", "Text"];
        foreach (string name in formerOwners)
        {
            StandardElementSave standard = result.Project!.StandardElements.First(s => s.Name == name);
            standard.Categories.ShouldNotContain(c => c.Name == "ColorCategory",
                $"{name} should no longer define the now-dead ColorCategory");
            standard.DefaultState.Variables.ShouldNotContain(v => v.Name == "ColorCategoryState",
                $"{name} should no longer carry the ColorCategoryState stub variable");
        }
    }

    [Fact]
    public void Create_StandardElements_ShouldNoLongerOwnStyleCategory()
    {
        // Issue #4089 follow-up to #4079: StyleCategory was the same double-indirection shape
        // as ColorCategory (a category living on the shared standard, selected via
        // <instance>.StyleCategoryState from Standard-theme components), just for texture-region
        // and font-style picks instead of color. Migrated the same way: consumers now own their
        // resolved values locally instead of relying on the shared standard's category.
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");
        _sut.Create(filePath);

        ProjectLoadResult result = new ProjectLoader().Load(filePath);
        result.Success.ShouldBeTrue();

        string[] formerOwners = ["NineSlice", "Text"];
        foreach (string name in formerOwners)
        {
            StandardElementSave standard = result.Project!.StandardElements.First(s => s.Name == name);
            standard.Categories.ShouldNotContain(c => c.Name == "StyleCategory",
                $"{name} should no longer define the now-dead StyleCategory");
            standard.DefaultState.Variables.ShouldNotContain(v => v.Name == "StyleCategoryState",
                $"{name} should no longer carry the StyleCategoryState stub variable");
        }
    }

    [Fact]
    public void Create_ButtonStandard_ShouldOwnLocalStyleValuesInsteadOfStyleCategoryState()
    {
        // ButtonStandard.Background used NineSlice's "Bordered" StyleCategory preset (literal
        // texture-region ints, no indirection to Styles) and TextInstance used Text's "Strong"
        // preset (which itself referenced Components/Styles.Strong.* - copy that reference list
        // directly onto the consumer, same as ColorCategory's #4079 fix).
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");
        _sut.Create(filePath);

        ProjectLoadResult result = new ProjectLoader().Load(filePath);
        result.Success.ShouldBeTrue();

        ComponentSave buttonStandard = result.Project!.Components.First(c => c.Name == "Controls/ButtonStandard");

        buttonStandard.DefaultState.Variables.ShouldNotContain(v => v.Name == "StyleCategoryState",
            "ButtonStandard should no longer set any instance's StyleCategoryState");

        VariableSave? textureLeft = buttonStandard.DefaultState.Variables
            .FirstOrDefault(v => v.Name == "Background.TextureLeft");
        textureLeft.ShouldNotBeNull("Background should own its resolved 'Bordered' texture region directly");
        textureLeft!.Value.ShouldBe(24);

        VariableListSave? textReferences = buttonStandard.DefaultState.VariableLists
            .FirstOrDefault(v => v.Name == "TextInstance.VariableReferences");
        textReferences.ShouldNotBeNull();
        List<string> referenceStrings = textReferences!.ValueAsIList.Cast<string>().ToList();
        referenceStrings.ShouldContain("Font = Components/Styles.Strong.Font");
        referenceStrings.ShouldContain("FontSize = Components/Styles.Strong.FontSize");
    }

    [Fact]
    public void Create_ShouldNotDriftFromDefaultStandards()
    {
        // Issue #4092, follow-up to #4089: the last remaining drift was baked Standard-theme
        // visual defaults (NineSlice texture-atlas coordinates/anchoring, Text sizing, Sprite
        // texture scale) that differed from a blank project's Standards. Every consuming instance
        // that relied on inheriting one of these values now sets it explicitly (materialized from
        // the Standard's prior baked value, so appearance is unchanged), and the 3 Standards
        // (NineSlice/Sprite/Text) were reset to match a blank project. Add Forms (Standard theme)
        // no longer needs to change the project's Standards at all.
        string filePath = Path.Combine(_tempDirectory, "TestProject.gumx");
        _sut.Create(filePath);

        DiffStandardsResult result = new DiffStandardsService().Diff(filePath);

        result.Differences.ShouldBeEmpty();
        result.MissingFromProject.ShouldBeEmpty();
        result.ProjectOnlyStandards.ShouldBeEmpty();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
