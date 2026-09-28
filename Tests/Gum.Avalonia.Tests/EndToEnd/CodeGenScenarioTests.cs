using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Gum.Services.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// End-to-end scenarios on the Code tab (inventory area CODE): the preview, the settings grid and
/// Generate, driven through the head's own tab on a temp project, with the generated files read
/// back from disk. Assertions look for the declarations that decide whether the code compiles
/// (class, base type, namespace, members), not whole-file text.
/// </summary>
[Trait("Category", "EndToEnd")]
public class CodeGenScenarioTests
{
    [AvaloniaFact]
    [Trait("Feature", "CODE-001")]
    [Trait("Feature", "CODE-002")]
    [Trait("Feature", "CODE-004")]
    [Trait("Feature", "CODE-005")]
    [Trait("Feature", "CODE-006")]
    public void Generate_AfterSettingUpTheTab_WritesTheComponentsGeneratedAndCustomFiles()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ComponentSave card = code.Project.AddComponent("Card");
        code.Project.AddInstance(card, "Title", "Text");
        code.Tree.SaveAll();
        code.Select(card);

        code.Preview.ShouldContain("partial class Card");
        File.Exists(code.CodeFile("Components/Card.Generated.cs")).ShouldBeFalse("nothing is written before a Code Project Root is set");

        code.SetUpManualGeneration();
        code.View.GenerateButton.IsEffectivelyVisible.ShouldBeTrue();
        code.ClickGenerate();

        string generated = File.ReadAllText(code.CodeFile("Components/Card.Generated.cs"));
        generated.ShouldContain("partial class Card : global::Gum.Forms.Controls.FrameworkElement");
        generated.ShouldContain("public TextRuntime Title");
        generated.ShouldContain("GetGraphicalUiElementByName(\"Title\")");
        string custom = File.ReadAllText(code.CodeFile("Components/Card.cs"));
        custom.ShouldContain("partial class Card");
        custom.ShouldContain("partial void CustomInitialize()");
        File.ReadAllText(Path.Combine(code.Project.ProjectFolder, "ProjectCodeSettings.codsj")).ShouldContain("\"CodeProjectRoot\": \"Code");
        code.Project.Dialogs.Messages.Last().ShouldContain("Card.Generated.cs");

        code.PickComboItem("Object Instantiation Type", "Fully in Code (no loaded Gum Project)");
        code.PickComboItem("Output Library", "SkiaSharp");
        code.Preview.ShouldContain("partial class CardRuntime : SkiaGum.GueDeriving.ContainerRuntime");
        code.ClickGenerate();

        generated = File.ReadAllText(code.CodeFile("Components/CardRuntime.Generated.cs"));
        generated.ShouldContain("partial class CardRuntime : SkiaGum.GueDeriving.ContainerRuntime");
        generated.ShouldContain("Title = new global::SkiaGum.GueDeriving.TextRuntime()");
        generated.ShouldNotContain("GetGraphicalUiElementByName");

        code.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "CODE-007")]
    [Trait("Feature", "CODE-008")]
    [Trait("Feature", "CODE-009")]
    public void ProjectWideSettings_ShapeEveryGeneratedFile_WhenGeneratingAllElements()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ScreenSave title = code.Project.AddScreen("TitleScreen");
        ComponentSave toggle = code.Project.AddComponent("Controls/Toggle");
        code.Tree.SaveAll();
        code.Select(toggle);
        code.SetUpManualGeneration();

        code.TypeAndLeave("Project-wide Using Statements", "using System.Text;");
        code.TypeAndEnter("Root Namespace", "MyGame");
        // New projects append the folder; the scenario reads the row rather than assuming it.
        if (!(bool)code.Member("Append Folder to Namespace").Value!)
        {
            code.ClickCheckBox("Append Folder to Namespace");
        }
        code.TypeAndEnter("Default Screen Base", "MyScreenBase");
        code.Select(title);
        code.PickComboItem("Generation Behavior", "GenerateManually");
        code.ClickButton("All");
        code.ClickGenerate();

        code.Project.Dialogs.Messages.Last().ShouldBe("Generated code for 2 element(s)");
        string toggleCode = File.ReadAllText(code.CodeFile("Components/Controls/Toggle.Generated.cs"));
        toggleCode.ShouldContain("using System.Text;");
        toggleCode.ShouldContain("namespace MyGame.Components.Controls");
        string screenCode = File.ReadAllText(code.CodeFile("Screens/TitleScreen.Generated.cs"));
        screenCode.ShouldContain("using System.Text;");
        screenCode.ShouldContain("namespace MyGame.Screens");
        screenCode.ShouldContain("partial class TitleScreen : MyScreenBase");

        code.Select(toggle);
        code.ClickCheckBox("Append Folder to Namespace");
        File.ReadAllText(Path.Combine(code.Project.ProjectFolder, "ProjectCodeSettings.codsj")).ShouldContain("\"AppendFolderToNamespace\": false");
        code.ClickButton("This");
        File.WriteAllText(code.CodeFile("Screens/TitleScreen.Generated.cs"), "// not regenerated");
        code.ClickGenerate();

        toggleCode = File.ReadAllText(code.CodeFile("Components/Controls/Toggle.Generated.cs"));
        toggleCode.ShouldContain("namespace MyGame.Components");
        toggleCode.ShouldNotContain("namespace MyGame.Components.Controls");
        File.ReadAllText(code.CodeFile("Screens/TitleScreen.Generated.cs")).ShouldBe("// not regenerated", "\"This\" generates the selected element only");

        code.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "CODE-013")]
    [Trait("Feature", "CODE-014")]
    [Trait("Feature", "CODE-015")]
    public void ElementSettings_AreSavedPerElement_AndShapeOnlyThatElementsFile()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ComponentSave card = code.Project.AddComponent("Card");
        ComponentSave badge = code.Project.AddComponent("Badge");
        code.Tree.SaveAll();
        code.Select(card);
        code.SetUpManualGeneration();

        code.TypeAndLeave("Using Statements", "using System.Numerics;");
        code.TypeAndEnter("Namespace", "Cards.Ui");
        code.TypeAndEnter("Generated File Name", "Special/CardView.Generated.cs");
        code.ClickGenerate();

        string cardCode = File.ReadAllText(Path.Combine(code.Project.ProjectFolder, "Special", "CardView.Generated.cs"));
        cardCode.ShouldContain("using System.Numerics;");
        cardCode.ShouldContain("namespace Cards.Ui");
        cardCode.ShouldContain("partial class Card");
        File.Exists(code.CodeFile("Components/Card.Generated.cs")).ShouldBeFalse();
        File.ReadAllText(Path.Combine(code.Project.ProjectFolder, "Components", "Card.codsj")).ShouldContain("Cards.Ui");

        code.Select(badge);
        code.PickComboItem("Generation Behavior", "NeverGenerate");
        code.Preview.ShouldContain("code generation disabled");
        code.View.GenerateButton.IsEffectivelyVisible.ShouldBeFalse();
        code.Member("Namespace").Value.ShouldBe("", "the Card's namespace belongs to the Card");

        code.Select(card);
        code.Member("Namespace").Value.ShouldBe("Cards.Ui");
        code.View.GenerateButton.IsEffectivelyVisible.ShouldBeTrue();

        code.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "CODE-010")]
    [Trait("Feature", "CODE-011")]
    [Trait("Feature", "CODE-012")]
    public void MauiOnlySettings_ShowForAMauiProject_AndChangeTheGeneratedCode()
    {
        // The Output Library list offers no Maui; a project set up for it before shows these rows.
        using CodeTabHarness code = new CodeTabHarness(beforeLoad: folder => File.WriteAllText(Path.Combine(folder, "ProjectCodeSettings.codsj"),
            """{ "CodeProjectRoot": "Code/", "OutputLibrary": 3, "ObjectInstantiationType": 0 }"""));
        ComponentSave baseCard = code.Project.AddComponent("BaseCard");
        ComponentSave card = code.Project.AddComponent("Card", baseType: "BaseCard");
        InstanceSave label = code.Project.AddInstance(card, "Label", "Text");
        code.Tree.SaveAll();
        code.Tree.Click(code.Tree.NodeFor(label));
        code.Tree.Grid.TypeAndEnter("X", "40");
        code.Select(card);
        code.PickComboItem("Generation Behavior", "GenerateManually");
        code.ClickGenerate();
        string before = GeneratedFileFor(code, "Card");
        before.ShouldContain("protected override void InitializeInstances()", customMessage: "BaseCard's class is generated too, so Card builds on it");
        before.ShouldNotContain("AssignGumReferences();");

        code.ClickCheckBox("Adjust Pixel Values for Density");
        code.TypeAndLeave("Base types ignored in code generation", "BaseCard");
        code.ClickCheckBox("Generate Gum DataTypes Code");
        code.ClickGenerate();

        // Density only changes Xamarin.Forms-visual instances, which a Gum project cannot add; the
        // row's effect here is the saved setting.
        string after = GeneratedFileFor(code, "Card");
        after.ShouldNotContain("protected override void InitializeInstances()");
        after.ShouldContain("AssignGumReferences();");
        string settings = File.ReadAllText(Path.Combine(code.Project.ProjectFolder, "ProjectCodeSettings.codsj"));
        settings.ShouldContain("\"AdjustPixelValuesForDensity\": true");
        settings.ShouldContain("\"GenerateGumDataTypes\": true");

        code.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "CODE-016")]
    public void LocalizeElement_OnAComponent_MakesElementsThatUseItLocalizeTheirInstances()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ComponentSave label = code.Project.AddComponent("Label");
        ScreenSave menu = code.Project.AddScreen("Menu");
        code.Project.AddInstance(menu, "Greeting", "Label");
        code.Tree.SaveAll();
        code.Select(menu);
        code.SetUpManualGeneration(instantiation: "Fully in Code (no loaded Gum Project)");
        code.ClickGenerate();
        File.ReadAllText(code.CodeFile("Screens/Menu.Generated.cs")).ShouldNotContain("Greeting.ApplyLocalization();");

        code.Select(label);
        code.ClickCheckBox("Localize Element");
        code.Select(menu);
        code.ClickGenerate();

        File.ReadAllText(code.CodeFile("Screens/Menu.Generated.cs")).ShouldContain("Greeting.ApplyLocalization();");
        File.ReadAllText(Path.Combine(code.Project.ProjectFolder, "Components", "Label.codsj")).ShouldContain("\"LocalizeElement\":true");

        code.AssertOracles();
    }

    private static string GeneratedFileFor(CodeTabHarness code, string className) =>
        File.ReadAllText(Directory.GetFiles(code.CodeFolder, className + "*.Generated.cs", SearchOption.AllDirectories).Single());

    [AvaloniaFact]
    [Trait("Feature", "CODE-003")]
    public void AutomaticGeneration_RewritesTheFileOnEachEdit_AndUndoRewritesItBack_WhileManualWaitsForGenerate()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        code.Select(card);
        code.SetUpManualGeneration(instantiation: "Fully in Code (no loaded Gum Project)");
        code.PickComboItem("Generation Behavior", "GenerateAutomaticallyOnPropertyChange");
        string file = code.CodeFile("Components/Card.Generated.cs");

        code.Tree.Grid.TypeAndEnter("Width", "175");

        File.ReadAllText(file).ShouldContain("175");

        code.Undo();

        File.ReadAllText(file).ShouldNotContain("175", customMessage: "undo regenerates the file from the restored element");

        code.PickComboItem("Generation Behavior", "GenerateManually");
        code.Tree.Grid.TypeAndEnter("Width", "250");

        File.ReadAllText(file).ShouldNotContain("250", customMessage: "manual generation waits for Generate");
        code.ClickGenerate();
        File.ReadAllText(file).ShouldContain("250");

        code.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "CODE-017")]
    public void AutoSetup_WithACsprojAboveTheProject_FillsTheSettingsFromIt()
    {
        using CodeTabHarness code = new CodeTabHarness(beforeLoad: folder => File.WriteAllText(Path.Combine(folder, "MyGame.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <RootNamespace>MyGame.Client</RootNamespace>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.*" />
              </ItemGroup>
            </Project>
            """));
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        code.Select(card);

        code.ShowsButton("Auto").ShouldBeTrue("a project next to a .csproj is offered setup");
        code.Grid.IsEffectivelyVisible.ShouldBeFalse();
        code.ClickButton("Auto");

        code.Grid.IsEffectivelyVisible.ShouldBeTrue();
        code.Member("Root Namespace").Value.ShouldBe("MyGame.Client");
        code.Member("Output Library").Value.ShouldBe("MonoGame + Forms");
        code.PickComboItem("Generation Behavior", "GenerateManually");
        code.ClickGenerate();

        string generated = File.ReadAllText(Path.Combine(code.Project.ProjectFolder, "Components", "Card.Generated.cs"));
        generated.ShouldContain("namespace MyGame.Client.Components");
        generated.ShouldContain("partial class Card : global::Gum.Forms.Controls.FrameworkElement");

        code.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "CODE-017")]
    public void ManualSetup_IsForTheOpenProject_AndAProjectOpenedAfterItIsOfferedSetupAgain()
    {
        using CodeTabHarness code = new CodeTabHarness(beforeLoad: WriteMinimalCsproj);
        code.Select(code.Project.AddComponent("Card"));
        code.ClickButton("Manual");
        code.Grid.IsEffectivelyVisible.ShouldBeTrue();

        // Manual is not saved anywhere; the project still has no Code Project Root.
        code.Reopen();
        code.Select(code.Project.Project.Components.Single());

        code.ShowsButton("Auto").ShouldBeTrue("a project opened with no code settings is offered setup");
        code.Grid.IsEffectivelyVisible.ShouldBeFalse();
        code.AssertOracles();
    }

    [AvaloniaFact]
    public void CodeSettingsOfAFinishedScenario_DoNotReachTheNextTestsProject()
    {
        using (CodeTabHarness first = new CodeTabHarness())
        {
            first.Select(first.Project.AddComponent("Card"));
            first.SetUpManualGeneration();
        }

        using ToolProjectFixture next = new ToolProjectFixture("GumCodeTab");
        // A new component generates on add when the project has a Code Project Root.
        next.AddComponent("Panel");

        Directory.Exists(Path.Combine(next.ProjectFolder, CodeTabHarness.CodeFolderName)).ShouldBeFalse(
            "the next project has no code settings, so nothing is generated into it");
    }

    private static void WriteMinimalCsproj(string folder) =>
        File.WriteAllText(Path.Combine(folder, "Game.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");

    [AvaloniaFact]
    [Trait("Feature", "CODE-018")]
    public void RenamingAComponent_RenamesItsCodeFiles_AndTheClassInside()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        code.Select(card);
        code.SetUpManualGeneration();
        code.ClickGenerate();

        code.Project.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "Panel"; return true; });
        // "This will change the file name for Card..."
        code.Project.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        code.Tree.Click(code.Tree.NodeFor(card));
        code.Tree.Press(Key.F2, PhysicalKey.F2);

        card.Name.ShouldBe("Panel");
        File.Exists(code.CodeFile("Components/Card.Generated.cs")).ShouldBeFalse();
        File.Exists(code.CodeFile("Components/Card.cs")).ShouldBeFalse();
        File.ReadAllText(code.CodeFile("Components/Panel.Generated.cs")).ShouldContain("partial class Panel");
        File.ReadAllText(code.CodeFile("Components/Panel.cs")).ShouldContain("partial class Panel");

        code.AssertOracles();
    }

    [AvaloniaFact]
    [Trait("Feature", "CODE-019")]
    public void DeletingAComponent_OffersToDeleteItsEditedCustomCode_AndRemovesTheGeneratedFile()
    {
        using CodeTabHarness code = new CodeTabHarness();
        ComponentSave card = code.Project.AddComponent("Card");
        code.Tree.SaveAll();
        code.Select(card);
        code.SetUpManualGeneration();
        code.ClickGenerate();
        string customFile = code.CodeFile("Components/Card.cs");
        File.WriteAllText(customFile, File.ReadAllText(customFile).Replace("partial void CustomInitialize()", "partial void CustomInitialize() { System.Console.WriteLine(); }"));
        List<string> offered = new List<string>();
        code.Project.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(dialog =>
        {
            offered.AddRange(dialog.CheckBoxes.Select(box => box.Label));
            dialog.CheckBoxes.Single(box => box.Label.Contains("custom code", StringComparison.Ordinal)).IsChecked = true;
            return true;
        });

        code.Tree.RightClick(code.Tree.NodeFor(card));
        code.Tree.PickMenu("Delete");

        offered.ShouldContain("Delete custom code file (contains your code)");
        code.Project.Project.Components.ShouldBeEmpty();
        File.Exists(code.CodeFile("Components/Card.Generated.cs")).ShouldBeFalse();
        File.Exists(customFile).ShouldBeFalse();
        File.Exists(Path.Combine(code.Project.ProjectFolder, "Components", "Card.codsj")).ShouldBeFalse();

        code.AssertOracles();
    }
}
