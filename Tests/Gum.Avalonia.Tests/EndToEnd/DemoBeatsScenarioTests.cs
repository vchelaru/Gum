using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Services.Dialogs;
using GumFormsPlugin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The beginner demo, done in the tool's own UI: Add Forms with the default theme, a screen and a
/// component, controls dragged in from the tree, variables typed into the grid, rename and delete
/// with references, the Code tab's Auto setup and Generate, then save and reopen.
/// </summary>
[Trait("Category", "EndToEnd")]
public class DemoBeatsScenarioTests
{
    private static readonly TimeSpan AsyncWork = TimeSpan.FromSeconds(60);

    private static IServiceProvider Services => TestAppBuilder.Services;

    private const string MonoGameCsproj =
        """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <RootNamespace>MyGame</RootNamespace>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="MonoGame.Framework.DesktopGL" Version="3.8.*" />
          </ItemGroup>
        </Project>
        """;

    [AvaloniaFact]
    public void AddForms_WithTheDefaultTheme_ListsButtonLabelTextBoxAndFriendsAsComponents()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddFormsWithDefaultTheme(tree);

        List<string> names = Project().Components.Select(component => component.Name).ToList();
        foreach (string control in new[] { "Button", "Label", "TextBox", "CheckBox", "ListBox", "ComboBox", "Slider" })
        {
            names.ShouldContain(name => name.EndsWith(control, StringComparison.Ordinal) || name.Contains(control + "/", StringComparison.Ordinal),
                $"Add Forms should give the beginner a {control}; it gave [{string.Join(", ", names)}]");
        }

        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void TheWholeDemo_FormsScreenInstancesVariablesRenameDeleteGenerateReopen()
    {
        using CodeTabHarness code = new CodeTabHarness(beforeLoad: folder => File.WriteAllText(Path.Combine(folder, "MyGame.csproj"), MonoGameCsproj));
        ProjectTreeHarness tree = code.Tree;
        AddFormsWithDefaultTheme(tree);

        // A screen from the Screens root menu.
        tree.Dialogs.AnswerNext<AddScreenDialogViewModel>(dialog => { dialog.Value = "MainMenu"; return true; });
        tree.RightClick(tree.RootNode("Screens"));
        tree.PickMenu("Add Screen");
        ScreenSave screen = Project().Screens.Single(candidate => candidate.Name == "MainMenu");
        tree.SelectedState.SelectedScreen.ShouldBeSameAs(screen);

        // A Container from the screen's menu, then a Button, Label and TextBox dragged from the Components list.
        tree.RightClick(tree.NodeFor(screen));
        tree.PickMenu("Add object to MainMenu", "Container");
        InstanceSave container = screen.Instances.Single();
        container.BaseType.ShouldBe("Container");

        foreach (string control in new[] { "Button", "Label", "TextBox" })
        {
            ComponentSave component = ControlComponent(control);
            tree.ClickCollapseToElementLevel();
            tree.BeginDrag(tree.NodeFor(component));
            tree.DropOn(tree.NodeFor(screen)).ShouldNotBe(DragDropEffects.None, $"dropping {control} on the screen");
        }
        screen.Instances.Count.ShouldBe(4, $"instances: [{string.Join(", ", screen.Instances.Select(i => i.Name + ":" + i.BaseType))}]");
        InstanceSave button = InstanceOf(screen, "Button");
        InstanceSave label = InstanceOf(screen, "Label");

        // Variables typed into the grid.
        tree.Click(tree.NodeFor(label));
        string rows = string.Join(",", tree.Grid.ShownMemberNames());
        tree.Grid.TypeAndLeave(label.Name + ".Text", "Score: 0");
        screen.DefaultState.GetValue($"{label.Name}.Text").ShouldBe("Score: 0", "where: " + string.Join(";", Project().Screens.Cast<ElementSave>().Concat(Project().Components).SelectMany(e => e.AllStates.SelectMany(st => st.Variables.Where(v => v.Value as string == "Score: 0").Select(v => e.Name + "/" + st.Name + "/" + v.Name))))
            + " selected=" + tree.SelectedState.SelectedElement?.Name + "/" + tree.SelectedState.SelectedInstance?.Name + " sameproj=" + (Project() == tree.Project.Project) + " screenInProj=" + Project().Screens.Contains(screen));
        tree.Click(tree.NodeFor(button));
        tree.Grid.TypeAndEnter(button.Name + ".Width", "200");
        screen.DefaultState.GetValue($"{button.Name}.Width").ShouldBe(200f);
        tree.Undo();
        screen.DefaultState.GetValue($"{button.Name}.Width").ShouldNotBe(200f);
        tree.Redo();
        screen.DefaultState.GetValue($"{button.Name}.Width").ShouldBe(200f);

        // Code tab: Auto setup, then generate all.
        code.Select(screen);
        code.ShowsButton("Auto").ShouldBeTrue();
        code.ClickButton("Auto");
        code.Member("Root Namespace").Value.ShouldBe("MyGame");
        code.PickComboItem("Generation Behavior", "GenerateManually");
        code.ClickGenerate();
        string screenCode = File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "Screens", "MainMenu.Generated.cs"));
        screenCode.ShouldContain("partial class MainMenu");
        screenCode.ShouldContain(button.Name);
        screenCode.ShouldContain(label.Name);

        // Rename the button; regenerate; the generated file follows and the stub is not rewritten.
        string stubFile = Path.Combine(tree.Project.ProjectFolder, "Screens", "MainMenu.cs");
        File.Exists(stubFile).ShouldBeTrue();
        string stubBefore = File.ReadAllText(stubFile);
        string oldName = button.Name;
        tree.Dialogs.AnswerNextUserString("PlayButton");
        tree.Click(tree.NodeFor(button));
        tree.Press(Key.F2, PhysicalKey.F2);
        button.Name.ShouldBe("PlayButton");
        code.Select(screen);
        code.ClickGenerate();
        screenCode = File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "Screens", "MainMenu.Generated.cs"));
        screenCode.ShouldContain("PlayButton");
        screenCode.Contains(oldName + " ", StringComparison.Ordinal).ShouldBeFalse("the old instance name is gone from the generated class");
        File.ReadAllText(stubFile).ShouldBe(stubBefore);

        // Save, close and reopen: the instances, their variables and the code settings come back.
        code.Reopen();
        ScreenSave reopened = Project().Screens.Single(candidate => candidate.Name == "MainMenu");
        reopened.Instances.Select(instance => instance.Name).ShouldContain("PlayButton");
        reopened.DefaultState.GetValue("PlayButton.Width").ShouldBe(200f);
        File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "ProjectCodeSettings.codsj")).ShouldContain("MyGame");

        code.AssertOracles();
    }

    [AvaloniaFact]
    public void DeletingAComponentThatAScreenUses_NamesTheInstancesThatBecomeInvalid()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave scorePanel = tree.Project.AddComponent("ScorePanel");
        tree.Project.AddInstance(scorePanel, "ScoreText", "Text");
        ScreenSave screen = tree.Project.AddScreen("MainMenu");
        tree.Project.AddInstance(screen, "ScorePanelInstance", "ScorePanel");
        tree.SaveAll();

        string? shown = null;
        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(dialog => { shown = dialog.Message + "|" + string.Join(";", dialog.CheckBoxes.Select(b => b.Label + "=" + b.IsChecked)); return true; });
        tree.RightClick(tree.NodeFor(scorePanel));
        tree.PickMenu("Delete");

        tree.Project.Project.Components.ShouldNotContain(scorePanel);
        shown.ShouldNotBeNull().ShouldContain("ScorePanelInstance in MainMenu");
        shown.ShouldContain("lose their type");
        screen.Instances.Count.ShouldBe(1, "the instance is kept, flagged invalid, not deleted behind the beginner's back");
    }

    [AvaloniaFact]
    public void AfterAddForms_TypingIntoAStandardTextInstancesTextRow_SetsItOnTheScreen()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddFormsWithDefaultTheme(tree);
        tree.Dialogs.AnswerNext<AddScreenDialogViewModel>(dialog => { dialog.Value = "MainMenu"; return true; });
        tree.RightClick(tree.RootNode("Screens"));
        tree.PickMenu("Add Screen");
        ScreenSave screen = Project().Screens.Single();
        tree.RightClick(tree.NodeFor(screen));
        tree.PickMenu("Add object to MainMenu", "Text");
        InstanceSave text = screen.Instances.Single();
        tree.Click(tree.NodeFor(text));

        tree.Grid.TypeAndLeave(text.Name + ".Text", "Score: 0");

        screen.DefaultState.GetValue(text.Name + ".Text").ShouldBe("Score: 0");
    }

    [AvaloniaFact]
    public void BeforeAddForms_TypingIntoAStandardTextInstancesTextRow_SetsItOnTheScreen()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("MainMenu");
        InstanceSave text = tree.Project.AddInstance(screen, "Score", "Text");
        tree.Click(tree.NodeFor(text));

        tree.Grid.TypeAndLeave("Score.Text", "Score: 0");

        screen.DefaultState.GetValue("Score.Text").ShouldBe("Score: 0");
    }

    #region Splash damage: the neighboring beginner behavior

    [AvaloniaFact]
    public void EveryFormsComponent_DraggedIntoAScreen_BecomesAUniqueSelectedInstance_WithAGridAndSavedOnDisk()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddFormsWithDefaultTheme(tree);
        ScreenSave screen = AddScreen(tree, "MainMenu");
        List<string> problems = new List<string>();

        foreach (ComponentSave component in Project().Components.Where(IsControl).ToList())
        {
            int before = screen.Instances.Count;
            tree.ClickCollapseToElementLevel();
            tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
            tree.BeginDrag(tree.NodeFor(component));
            tree.DragOver(tree.NodeFor(screen), 0.5);
            tree.DropOn(tree.NodeFor(screen));
            if (screen.Instances.Count != before + 1)
            {
                problems.Add($"{component.Name}: dropping it added {screen.Instances.Count - before} instances; messages: {string.Join("|", tree.Dialogs.Messages)}");
                continue;
            }
            InstanceSave added = screen.Instances.Last();
            if (added.BaseType != component.Name)
            {
                problems.Add($"{component.Name}: instance base type is {added.BaseType}");
            }
            if (tree.SelectedState.SelectedInstance != added)
            {
                problems.Add($"{component.Name}: the new instance {added.Name} is not selected");
            }
            if (tree.Grid.ShownMemberNames().Count == 0)
            {
                problems.Add($"{component.Name}: the Variables tab shows no rows for {added.Name}");
            }
        }

        screen.Instances.Select(instance => instance.Name).Distinct().Count().ShouldBe(screen.Instances.Count, "instance names are unique");
        screen.Instances.Count.ShouldBeGreaterThan(5);
        problems.ShouldBeEmpty(string.Join("\n", problems));
        tree.SaveAll();
        string screenFile = File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "Screens", "MainMenu.gusx"));
        foreach (InstanceSave instance in screen.Instances)
        {
            screenFile.ShouldContain(instance.Name, Case.Sensitive, $"{instance.Name} was saved");
        }
        tree.ThrowIfCrashed();
        tree.AssertOracles();
    }

    [AvaloniaTheory]
    [InlineData("Controls/Window")]
    [InlineData("Elements/Icon")]
    [InlineData("Controls/ButtonStandard")]
    public void ADroppedComponent_FromAnywhereInTheLongComponentsList_AddsAnInstanceToTheScreen(string componentName)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddFormsWithDefaultTheme(tree);
        ScreenSave screen = AddScreen(tree, "MainMenu");
        ComponentSave component = Project().Components.Single(candidate => candidate.Name == componentName);
        tree.ClickCollapseToElementLevel();
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);

        tree.BeginDrag(tree.NodeFor(component));
        DragDropEffects over = tree.DragOver(tree.NodeFor(screen), 0.5);
        bool visible = tree.IsScrolledIntoView(tree.NodeFor(screen));
        string rows = string.Join(",", tree.VisibleNodes().Take(6).Select(n => n.Text));
        tree.DropOn(tree.NodeFor(screen));

        screen.Instances.Count.ShouldBe(1, $"over={over} screenVisible={visible} firstRows={rows} " + string.Join("|", tree.Dialogs.Messages));
        screen.Instances.Single().BaseType.ShouldBe(componentName);
    }

    [AvaloniaFact]
    public void DraggingTheSameComponentTwice_GivesTheSecondInstanceAnotherName()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddFormsWithDefaultTheme(tree);
        ScreenSave screen = AddScreen(tree, "MainMenu");
        ComponentSave button = ControlComponent("Button");

        for (int i = 0; i < 3; i++)
        {
            tree.ClickCollapseToElementLevel();
            tree.BeginDrag(tree.NodeFor(button));
            tree.DropOn(tree.NodeFor(screen));
        }

        screen.Instances.Count.ShouldBe(3);
        screen.Instances.Select(instance => instance.Name).Distinct().Count().ShouldBe(3);
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void ADroppedControlIsInsideAContainerWhenTheContainerIsTheDropTarget()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddFormsWithDefaultTheme(tree);
        ScreenSave screen = AddScreen(tree, "MainMenu");
        tree.RightClick(tree.NodeFor(screen));
        tree.PickMenu("Add object to MainMenu", "Container");
        InstanceSave container = screen.Instances.Single();

        tree.ClickCollapseToElementLevel();
        tree.BeginDrag(tree.NodeFor(ControlComponent("Button")));
        tree.DropOn(tree.NodeFor(container));

        InstanceSave button = screen.Instances.Single(instance => instance != container);
        screen.DefaultState.GetValue(button.Name + ".Parent").ShouldBe(container.Name);
        tree.ChildTexts(tree.NodeFor(container)).ShouldBe(new[] { button.Name });
        tree.Undo();
        tree.Redo();
        screen.DefaultState.GetValue(button.Name + ".Parent").ShouldBe(container.Name);
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void StackingAndSizeUnits_TypedIntoTheGrid_AreSavedAndUndone()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("MainMenu");
        InstanceSave container = tree.Project.AddInstance(screen, "Menu", "Container");
        tree.Click(tree.NodeFor(container));

        tree.Grid.PressToggle("Menu.ChildrenLayout", Gum.Managers.ChildrenLayout.TopToBottomStack);
        tree.Grid.PressToggle("Menu.HeightUnits", Gum.DataTypes.DimensionUnitType.RelativeToChildren);
        tree.Grid.TypeAndEnter("Menu.Width", "300");

        screen.DefaultState.GetValue("Menu.ChildrenLayout").ShouldNotBeNull().ToString().ShouldBe("TopToBottomStack");
        screen.DefaultState.GetValue("Menu.HeightUnits").ShouldNotBeNull().ToString().ShouldBe("RelativeToChildren");
        screen.DefaultState.GetValue("Menu.Width").ShouldBe(300f);
        tree.Undo();
        tree.Undo();
        tree.Undo();
        screen.DefaultState.GetValue("Menu.Width").ShouldNotBe(300f);
        screen.DefaultState.GetValue("Menu.ChildrenLayout").ShouldNotBe(Gum.Managers.ChildrenLayout.TopToBottomStack);
        tree.Redo();
        tree.Redo();
        tree.Redo();
        screen.DefaultState.GetValue("Menu.Width").ShouldBe(300f);
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void ASmallStateCategory_CreatedAndEdited_SurvivesSaveAndReopen()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave panel = tree.Project.AddComponent("ScorePanel");
        tree.Project.AddInstance(panel, "Back", "NineSlice");
        StateSaveCategory size = tree.Project.AddCategory(panel, "Size");
        StateSave small = tree.Project.AddState(panel, size, "Small");
        StateSave big = tree.Project.AddState(panel, size, "Big");
        tree.SaveAll();
        tree.Project.SaveAndReload();

        ComponentSave reopened = Project().Components.Single(component => component.Name == "ScorePanel");
        reopened.Categories.Single().States.Select(state => state.Name).ShouldBe(new[] { "Small", "Big" });
    }

    [AvaloniaFact]
    public void RenamingAnInstanceUsedByAParentReference_KeepsTheChildInTheContainer()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("MainMenu");
        InstanceSave panel = tree.Project.AddInstance(screen, "Panel", "Container");
        InstanceSave text = tree.Project.AddInstance(screen, "Title", "Text");
        screen.DefaultState.SetValue("Title.Parent", "Panel", "string");
        tree.SaveAll();
        tree.Click(tree.NodeFor(panel));

        tree.Dialogs.AnswerNextUserString("Menu");
        tree.Press(Key.F2, PhysicalKey.F2);

        panel.Name.ShouldBe("Menu");
        screen.DefaultState.GetValue("Title.Parent").ShouldBe("Menu");
        tree.Undo();
        screen.Instances.Select(instance => instance.Name).ShouldBe(new[] { "Panel", "Title" });
        screen.DefaultState.GetValue("Title.Parent").ShouldBe("Panel");
        tree.AssertOracles();
    }

    [AvaloniaFact]
    public void RenamingAComponentUsedByAScreen_UpdatesTheScreensInstanceBaseType_AndItsFile()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave scorePanel = tree.Project.AddComponent("ScorePanel");
        ScreenSave screen = tree.Project.AddScreen("MainMenu");
        InstanceSave use = tree.Project.AddInstance(screen, "Score", "ScorePanel");
        tree.SaveAll();
        tree.Click(tree.NodeFor(scorePanel));

        tree.Dialogs.AnswerNext<RenameElementDialogViewModel>(dialog => { dialog.Value = "ScoreBox"; return true; });
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.Press(Key.F2, PhysicalKey.F2);

        use.BaseType.ShouldBe("ScoreBox");
        File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "Screens", "MainMenu.gusx")).ShouldContain("ScoreBox");
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "ScoreBox.gucx")).ShouldBeTrue();
        File.Exists(Path.Combine(tree.Project.ProjectFolder, "Components", "ScorePanel.gucx")).ShouldBeFalse();
        tree.AssertOracles();
    }

    [AvaloniaTheory]
    [InlineData("Main.Menu")]
    [InlineData("Main-Menu")]
    [InlineData("Main Menu!")]
    [InlineData("Main/Menu")]
    public void AddScreen_WithAnInvalidName_DisablesOk_AndAddsNothing(string name)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        bool canAdd = true;
        tree.Dialogs.AnswerNext<AddScreenDialogViewModel>(dialog =>
        {
            dialog.Value = name;
            canAdd = dialog.AffirmativeCommand.CanExecute(null);
            return false;
        });

        tree.RightClick(tree.RootNode("Screens"));
        tree.PickMenu("Add Screen");

        canAdd.ShouldBeFalse($"'{name}' should be refused");
        tree.Project.Project.Screens.ShouldBeEmpty();
    }

    [AvaloniaTheory]
    [InlineData("Panel.Big")]
    [InlineData("Panel-Big")]
    [InlineData("Panel!")]
    public void AddComponent_WithAnInvalidName_DisablesOk_AndAddsNothing(string name)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        bool canAdd = true;
        tree.Dialogs.AnswerNext<AddComponentDialogViewModel>(dialog =>
        {
            dialog.Value = name;
            canAdd = dialog.AffirmativeCommand.CanExecute(null);
            return false;
        });

        tree.RightClick(tree.RootNode("Components"));
        tree.PickMenu("Add Component");

        canAdd.ShouldBeFalse($"'{name}' should be refused");
        tree.Project.Project.Components.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void AddScreen_WithANameThatIsTaken_DisablesOk_AndAddsNothing()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddScreen("MainMenu");
        tree.SaveAll();
        bool canAdd = true;
        tree.Dialogs.AnswerNext<AddScreenDialogViewModel>(dialog => { dialog.Value = "MainMenu"; canAdd = dialog.AffirmativeCommand.CanExecute(null); return false; });

        tree.RightClick(tree.RootNode("Screens"));
        tree.PickMenu("Add Screen");

        canAdd.ShouldBeFalse();
        tree.Project.Project.Screens.Count.ShouldBe(1);
    }

    [AvaloniaTheory]
    [InlineData("Main Menu", "Main_Menu")]
    [InlineData("1MainMenu", "_1MainMenu")]
    [InlineData("class", "@class")]
    [InlineData("event", "@event")]
    [InlineData("string", "@string")]
    public void AScreenNamedWithASpaceDigitOrKeyword_IsEitherRefusedOrGeneratesALegalClassName(string name, string legalClassName)
    {
        using CodeTabHarness code = new CodeTabHarness(beforeLoad: folder => File.WriteAllText(Path.Combine(folder, "MyGame.csproj"), MonoGameCsproj));
        bool canAdd = true;
        code.Tree.Dialogs.AnswerNext<AddScreenDialogViewModel>(dialog =>
        {
            dialog.Value = name;
            canAdd = dialog.AffirmativeCommand.CanExecute(null);
            return canAdd;
        });
        code.Tree.RightClick(code.Tree.RootNode("Screens"));
        code.Tree.PickMenu("Add Screen");
        if (!canAdd)
        {
            code.Project.Project.Screens.ShouldBeEmpty();
            return;
        }

        ScreenSave screen = code.Project.Project.Screens.Single();
        code.Tree.SaveAll();
        code.Select(screen);
        code.ClickButton("Auto");
        code.PickComboItem("Generation Behavior", "GenerateManually");
        code.ClickGenerate();

        string screens = Path.Combine(code.Project.ProjectFolder, "Screens");
        string[] generatedFiles = Directory.GetFiles(screens, "*.Generated.cs");
        generatedFiles.Length.ShouldBe(1, "files: " + string.Join(", ", Directory.GetFiles(code.Project.ProjectFolder, "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(code.Project.ProjectFolder, f))) + " messages: " + string.Join(" | ", code.Project.Dialogs.Messages));
        File.ReadAllText(generatedFiles[0]).ShouldContain("partial class " + legalClassName, Case.Sensitive);
    }

    [AvaloniaFact]
    public void EveryFormsControlInAScreen_GeneratesAFieldOfItsOwnType_WithNoErrorTextInTheCode()
    {
        using CodeTabHarness code = new CodeTabHarness(beforeLoad: folder => File.WriteAllText(Path.Combine(folder, "MyGame.csproj"), MonoGameCsproj));
        ProjectTreeHarness tree = code.Tree;
        AddFormsWithDefaultTheme(tree);
        ScreenSave screen = AddScreen(tree, "MainMenu");
        foreach (string control in new[] { "Button", "Label", "TextBox", "CheckBox", "ListBox", "ComboBox", "Slider" })
        {
            ComponentSave component = Project().Components.FirstOrDefault(c => c.Name == control || c.Name.EndsWith("/" + control, StringComparison.Ordinal) || c.Name.EndsWith(control + "Standard", StringComparison.Ordinal)
                || c.Name.EndsWith("/" + control + "Standard", StringComparison.Ordinal))!;
            if (component == null)
            {
                continue;
            }
            tree.ClickCollapseToElementLevel();
            tree.BeginDrag(tree.NodeFor(component));
            tree.DragOver(tree.NodeFor(screen), 0.5);
            tree.DropOn(tree.NodeFor(screen));
        }
        tree.SaveAll();
        code.Select(screen);
        code.ClickButton("Auto");
        code.PickComboItem("Generation Behavior", "GenerateManually");
        code.ClickGenerate();

        string generated = File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "Screens", "MainMenu.Generated.cs"));
        foreach (InstanceSave instance in screen.Instances)
        {
            generated.ShouldContain($" {instance.Name} {{ get; protected set; }}", Case.Sensitive, $"{instance.Name} has a property");
        }
        generated.ShouldNotContain("ERROR", Case.Insensitive);
        generated.ShouldNotContain("not supported", Case.Insensitive);
    }

    [AvaloniaFact]
    public void GeneratingTwiceWithNoChange_LeavesBothFilesByteForByte()
    {
        using CodeTabHarness code = new CodeTabHarness(beforeLoad: folder => File.WriteAllText(Path.Combine(folder, "MyGame.csproj"), MonoGameCsproj));
        ScreenSave screen = code.Project.AddScreen("MainMenu");
        code.Project.AddInstance(screen, "Title", "Text");
        code.Tree.SaveAll();
        code.Select(screen);
        code.ClickButton("Auto");
        code.PickComboItem("Generation Behavior", "GenerateManually");
        code.ClickGenerate();
        string generatedFile = Path.Combine(code.Project.ProjectFolder, "Screens", "MainMenu.Generated.cs");
        string stubFile = Path.Combine(code.Project.ProjectFolder, "Screens", "MainMenu.cs");
        File.WriteAllText(stubFile, File.ReadAllText(stubFile) + "\n// mine\n");
        string generated = File.ReadAllText(generatedFile);
        string stub = File.ReadAllText(stubFile);

        code.ClickGenerate();

        File.ReadAllText(generatedFile).ShouldBe(generated);
        File.ReadAllText(stubFile).ShouldBe(stub, "the hand-written file is never overwritten");
    }

    [AvaloniaFact]
    public void ReopeningAProject_KeepsTheCodeSettings_AndTheTreeExpandedStateRoundTrips()
    {
        using CodeTabHarness code = new CodeTabHarness(beforeLoad: folder => File.WriteAllText(Path.Combine(folder, "MyGame.csproj"), MonoGameCsproj));
        ScreenSave screen = code.Project.AddScreen("MainMenu");
        code.Tree.SaveAll();
        code.Select(screen);
        code.ClickButton("Auto");
        code.Member("Root Namespace").Value.ShouldBe("MyGame");

        code.Reopen();
        code.Select(code.Project.Project.Screens.Single());

        code.ShowsButton("Auto").ShouldBeFalse("a project with code settings is not offered setup again");
        code.Member("Root Namespace").Value.ShouldBe("MyGame");
    }

    [AvaloniaTheory]
    [InlineData("Label")]
    [InlineData("Button")]
    [InlineData("TextBox")]
    public void RenamingAFormsInstance_ThenUndoing_RestoresNameAndFile(string control)
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        AddFormsWithDefaultTheme(tree);
        ScreenSave screen = AddScreen(tree, "MainMenu");
        tree.ClickCollapseToElementLevel();
        tree.BeginDrag(tree.NodeFor(ControlComponent(control)));
        tree.DropOn(tree.NodeFor(screen));
        InstanceSave instance = screen.Instances.Single();
        string original = instance.Name;
        tree.SaveAll();
        ProjectFileSnapshot start = tree.SnapshotFiles();

        tree.Dialogs.AnswerNextUserString("Renamed");
        tree.Click(tree.NodeFor(instance));
        tree.Press(Key.F2, PhysicalKey.F2);
        instance.Name.ShouldBe("Renamed");
        tree.Undo();

        screen.Instances.Single().Name.ShouldBe(original);
        tree.SnapshotFiles().ShouldMatch(start, "undoing the rename restores the files");
        tree.AssertOracles();
    }

    #endregion

    private static ScreenSave AddScreen(ProjectTreeHarness tree, string name)
    {
        tree.Dialogs.AnswerNext<AddScreenDialogViewModel>(dialog => { dialog.Value = name; return true; });
        tree.RightClick(tree.RootNode("Screens"));
        tree.PickMenu("Add Screen");
        return Project().Screens.Single(candidate => candidate.Name == name);
    }

    private static bool IsControl(ComponentSave component) =>
        component.Behaviors.Count > 0 || component.BaseType is "Container" or "Text" or "Sprite" or "NineSlice";

    private static InstanceSave InstanceOf(ScreenSave screen, string control) =>
        screen.Instances.SingleOrDefault(instance => instance.BaseType.Contains(control, StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"No {control} instance; instances: [{string.Join(", ", screen.Instances.Select(i => i.Name + ":" + i.BaseType))}]");

    private static GumProjectSave Project() => Services.GetRequiredService<IProjectManager>().GumProjectSave.ShouldNotBeNull();

    private static ComponentSave ControlComponent(string control) =>
        Project().Components.SingleOrDefault(component => component.Name == control || component.Name.EndsWith("/" + control, StringComparison.Ordinal)
            || component.Name.EndsWith(control + "Standard", StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"No {control} component; components: [{string.Join(", ", Project().Components.Select(c => c.Name))}]");

    private static void AddFormsWithDefaultTheme(ProjectTreeHarness tree)
    {
        tree.Dialogs.AnswerNext<AddFormsViewModel>(dialog =>
        {
            dialog.IsIncludeDemoScreenGum = false;
            return true;
        });
        tree.PickMainMenu("Content", "Add Forms Components");
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        tree.WaitUntil(() => projectManager.GumProjectSave != tree.Project.Project, AsyncWork, "the project to reload after Add Forms");
    }
}
