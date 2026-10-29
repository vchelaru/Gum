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

        // Add Forms gave the beginner the controls the tutorials use.
        List<string> componentNames = Project().Components.Select(component => component.Name).ToList();
        foreach (string control in new[] { "Button", "Label", "TextBox", "CheckBox", "ListBox", "ComboBox", "Slider" })
        {
            componentNames.ShouldContain(name => name.EndsWith(control, StringComparison.Ordinal) || name.EndsWith(control + "Standard", StringComparison.Ordinal),
                $"Add Forms should give the beginner a {control}; it gave [{string.Join(", ", componentNames)}]");
        }

        // One control per family dragged from the long Components list onto the screen: the Controls folder
        // (including the last rows, which need a scroll), Elements and Styles-free standards.
        string[] dragged = { "Button", "Label", "TextBox", "CheckBox", "ListBox" };
        List<string> problems = new List<string>();
        foreach (ComponentSave component in dragged.Select(ControlComponent).Concat(new[] { Project().Components.Single(c => c.Name == "Controls/Window"), Project().Components.Single(c => c.Name == "Elements/Icon") }))
        {
            int before = screen.Instances.Count;
            DropOnScreen(tree, component, screen);
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
        problems.ShouldBeEmpty(string.Join("\n", problems));
        screen.Instances.Count.ShouldBe(1 + 7, $"instances: [{string.Join(", ", screen.Instances.Select(i => i.Name + ":" + i.BaseType))}]");

        // The same component again gets another name; a control dropped on the container goes inside it.
        DropOnScreen(tree, ControlComponent("Button"), screen);
        DropOnScreen(tree, ControlComponent("Button"), screen);
        screen.Instances.Select(instance => instance.Name).Distinct().Count().ShouldBe(screen.Instances.Count, "instance names are unique");
        tree.ClickCollapseToElementLevel();
        tree.BeginDrag(tree.NodeFor(ControlComponent("CheckBox")));
        tree.DragOver(tree.NodeFor(container), 0.5);
        tree.DropOn(tree.NodeFor(container));
        InstanceSave inside = screen.Instances.Last();
        screen.DefaultState.GetValue(inside.Name + ".Parent").ShouldBe(container.Name);
        tree.ChildTexts(tree.NodeFor(container)).ShouldBe(new[] { inside.Name });
        tree.Undo();
        tree.Redo();
        screen.DefaultState.GetValue(screen.Instances.Last().Name + ".Parent").ShouldBe(container.Name);

        // Every dropped instance is on disk, and renaming one then undoing restores its name and the files.
        tree.SaveAll();
        string screenFile = File.ReadAllText(Path.Combine(tree.Project.ProjectFolder, "Screens", "MainMenu.gusx"));
        foreach (InstanceSave instance in screen.Instances)
        {
            screenFile.ShouldContain(instance.Name, Case.Sensitive, $"{instance.Name} was saved");
        }
        ProjectFileSnapshot start = tree.SnapshotFiles();
        string renamedFrom = InstanceOf(screen, "CheckBox").Name;
        tree.Dialogs.AnswerNextUserString("Renamed");
        tree.Click(tree.NodeFor(InstanceOf(screen, "CheckBox")));
        tree.Press(Key.F2, PhysicalKey.F2);
        screen.Instances.Select(instance => instance.Name).ShouldContain("Renamed");
        tree.Undo();
        screen.Instances.Select(instance => instance.Name).ShouldContain(renamedFrom);
        screen.Instances.Select(instance => instance.Name).ShouldNotContain("Renamed");
        tree.SnapshotFiles().ShouldMatch(start, "undoing the rename restores the files");
        tree.ThrowIfCrashed();

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
        foreach (InstanceSave instance in screen.Instances)
        {
            screenCode.ShouldContain($" {instance.Name} {{ get; protected set; }}", Case.Sensitive, $"{instance.Name} has a property");
        }
        screenCode.ShouldNotContain("ERROR", Case.Insensitive);
        screenCode.ShouldNotContain("not supported", Case.Insensitive);

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

    [AvaloniaFact]
    public void AddScreenAndComponent_WithAnInvalidOrTakenName_DisableOk_AndAddNothing()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        tree.Project.AddScreen("MainMenu");
        tree.SaveAll();
        List<string> accepted = new List<string>();

        foreach (string name in new[] { "Main.Menu", "Main-Menu", "Main Menu!", "Main/Menu", "MainMenu" })
        {
            tree.Dialogs.AnswerNext<AddScreenDialogViewModel>(dialog =>
            {
                dialog.Value = name;
                if (dialog.AffirmativeCommand.CanExecute(null))
                {
                    accepted.Add("screen " + name);
                }
                return false;
            });
            tree.RightClick(tree.RootNode("Screens"));
            tree.PickMenu("Add Screen");
        }
        foreach (string name in new[] { "Panel.Big", "Panel-Big", "Panel!" })
        {
            tree.Dialogs.AnswerNext<AddComponentDialogViewModel>(dialog =>
            {
                dialog.Value = name;
                if (dialog.AffirmativeCommand.CanExecute(null))
                {
                    accepted.Add("component " + name);
                }
                return false;
            });
            tree.RightClick(tree.RootNode("Components"));
            tree.PickMenu("Add Component");
        }

        accepted.ShouldBeEmpty("these names should be refused: " + string.Join(", ", accepted));
        tree.Project.Project.Screens.Count.ShouldBe(1);
        tree.Project.Project.Components.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void ScreensNamedWithASpaceDigitOrKeyword_AreEitherRefusedOrGenerateALegalClassName()
    {
        using CodeTabHarness code = new CodeTabHarness(beforeLoad: folder => File.WriteAllText(Path.Combine(folder, "MyGame.csproj"), MonoGameCsproj));
        List<string> problems = new List<string>();

        foreach ((string name, string legalClassName) in new[] { ("Main Menu", "Main_Menu"), ("1MainMenu", "_1MainMenu"), ("class", "@class"), ("event", "@event"), ("string", "@string") })
        {
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
                continue;
            }

            ScreenSave screen = code.Project.Project.Screens.Single(candidate => candidate.Name == name);
            code.Tree.SaveAll();
            code.Select(screen);
            if (code.ShowsButton("Auto"))
            {
                code.ClickButton("Auto");
            }
            code.PickComboItem("Generation Behavior", "GenerateManually");
            code.ClickGenerate();

            string[] files = Directory.GetFiles(Path.Combine(code.Project.ProjectFolder, "Screens"), "*.Generated.cs");
            if (!files.Any(file => File.ReadAllText(file).Contains("partial class " + legalClassName + " ", StringComparison.Ordinal)))
            {
                problems.Add($"'{name}' generated no 'partial class {legalClassName}'; files: {string.Join(", ", files.Select(Path.GetFileName))}");
            }
        }

        problems.ShouldBeEmpty(string.Join("\n", problems));
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
        screen.Instances.FirstOrDefault(instance => instance.BaseType.Contains(control, StringComparison.Ordinal))
        ?? throw new InvalidOperationException($"No {control} instance; instances: [{string.Join(", ", screen.Instances.Select(i => i.Name + ":" + i.BaseType))}]");

    private static void DropOnScreen(ProjectTreeHarness tree, ComponentSave component, ScreenSave screen)
    {
        tree.ClickCollapseToElementLevel();
        tree.Dialogs.AnswerNextMessage(MessageDialogResult.Affirmative);
        tree.BeginDrag(tree.NodeFor(component));
        // A drop is preceded by drag-over events, as the pointer produces.
        tree.DragOver(tree.NodeFor(screen), 0.5);
        tree.DropOn(tree.NodeFor(screen));
    }

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
