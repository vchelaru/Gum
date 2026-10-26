using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// Projects for the hot reload Forms state tests. Each call builds a whole project, like a reload
/// reads one: the first copy is what the running game loaded, the second is what the reload reads.
/// </summary>
internal static class HotReloadFormsProjects
{
    /// <summary>The values one version of the saved files holds.</summary>
    internal sealed record Look(
        int Default, int Highlighted, int Disabled, int Pushed, int Focused, int Enabled, int CheckedOn,
        string AuthoredText, bool? AuthoredEnabled);

    internal static readonly Look First = new(10, 20, 30, 40, 50, 60, 70, "First", null);
    internal static readonly Look Second = new(110, 120, 130, 140, 150, 160, 170, "Second", null);

    private static void SetVariable(StateSave state, string name, string type, object value)
    {
        state.Variables.RemoveAll(v => v.Name == name);
        state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
    }

    private static void SetRed(StateSave state, int red)
    {
        SetVariable(state, "Background.Red", "int", red);
        SetVariable(state, "Background.Green", "int", 0);
        SetVariable(state, "Background.Blue", "int", 0);
    }

    private static ComponentSave BuildFormsComponent(
        string name, string behaviorName, string categoryName, Look look,
        IEnumerable<(string state, int red)> categoryStates, string? defaultStateName, int defaultRed)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        StateSave componentDefault = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(componentDefault);
        component.Instances.Add(new InstanceSave { Name = "Background", BaseType = "ColoredRectangle", ParentContainer = component });
        component.Instances.Add(new InstanceSave { Name = "TextInstance", BaseType = "Text", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });
        SetRed(componentDefault, defaultRed);
        SetVariable(componentDefault, "TextInstance.Text", "string", look.AuthoredText);

        StateSaveCategory category = new StateSaveCategory { Name = categoryName };
        foreach ((string state, int red) in categoryStates)
        {
            StateSave save = new StateSave { Name = state, ParentContainer = component };
            if (red >= 0)
            {
                SetRed(save, red);
            }
            category.States.Add(save);
        }
        component.Categories.Add(category);
        return component;
    }

    /// <summary>
    /// Screen "TestScreen": Button1 (a ButtonStandard), Check1 (a CheckBoxStandard), Panel1 (a
    /// component that holds its own Button1), and Plain1 (a Container with a MyCategory that only
    /// code drives). <paramref name="withoutDisabledState"/> models the edit that deleted the state.
    /// </summary>
    internal static GumProjectSave Build(Look look, bool withoutDisabledState = false, bool withoutCategory = false)
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new GumProjectSave();

        foreach (string standard in new[] { "Container", "ColoredRectangle", "Text" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!);
            standardElement.DefaultState!.ParentContainer = standardElement;
            project.StandardElements.Add(standardElement);
        }

        project.Behaviors.Add(new BehaviorSave { Name = StandardFormsBehaviorNames.ButtonBehaviorName });
        BehaviorSave checkBoxBehavior = new BehaviorSave { Name = StandardFormsBehaviorNames.CheckBoxBehaviorName };
        checkBoxBehavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        project.Behaviors.Add(checkBoxBehavior);
        project.Behaviors[0].FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });

        List<(string, int)> buttonStates = new()
        {
            ("Enabled", look.Enabled), ("Highlighted", look.Highlighted),
            ("Pushed", look.Pushed), ("Focused", look.Focused)
        };
        if (!withoutDisabledState)
        {
            buttonStates.Add(("Disabled", look.Disabled));
        }
        ComponentSave button = BuildFormsComponent(
            "Controls/ButtonStandard", StandardFormsBehaviorNames.ButtonBehaviorName, "ButtonCategory",
            look, withoutCategory ? new List<(string, int)>() : buttonStates, null, look.Default);
        if (withoutCategory)
        {
            button.Categories.Clear();
        }
        project.Components.Add(button);

        project.Components.Add(BuildFormsComponent(
            "Controls/CheckBoxStandard", StandardFormsBehaviorNames.CheckBoxBehaviorName, "CheckBoxCategory",
            look, new[] { ("EnabledOn", look.CheckedOn), ("EnabledOff", look.Enabled), ("DisabledOn", look.Disabled) },
            null, look.Default));

        ComponentSave panel = new ComponentSave { Name = "Controls/Panel", BaseType = "Container" };
        panel.States.Add(new StateSave { Name = "Default", ParentContainer = panel });
        panel.Instances.Add(new InstanceSave { Name = "Button1", BaseType = button.Name, ParentContainer = panel });
        project.Components.Add(panel);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        screen.Instances.Add(new InstanceSave { Name = "Button1", BaseType = button.Name, ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Check1", BaseType = "Controls/CheckBoxStandard", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Panel1", BaseType = panel.Name, ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Plain1", BaseType = "ColoredRectangle", ParentContainer = screen });
        if (look.AuthoredEnabled != null)
        {
            SetVariable(screenDefault, "Button1.IsEnabled", "bool", look.AuthoredEnabled.Value);
        }
        SetVariable(screenDefault, "Plain1.Red", "int", look.Default);

        StateSaveCategory plainCategory = new StateSaveCategory { Name = "MyCategory" };
        plainCategory.States.Add(new StateSave { Name = "A", ParentContainer = screen });
        StateSave plainB = new StateSave { Name = "B", ParentContainer = screen };
        SetVariable(plainB, "Plain1.Red", "int", look.Highlighted);
        plainCategory.States.Add(plainB);
        screen.Categories.Add(plainCategory);
        project.Screens.Add(screen);

        project.Initialize();
        return project;
    }

    internal static GraphicalUiElement Load(GumProjectSave project)
    {
        ObjectFinder.Self.GumProjectSave = project;
        FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        ElementSaveExtensions.RegisterGueInstantiation("ColoredRectangle", () => new Gum.GueDeriving.ColoredRectangleRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Text", () => new Gum.GueDeriving.TextRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Container", () => new Gum.GueDeriving.ContainerRuntime());
        return project.Screens.Single(s => s.Name == "TestScreen")
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
    }

    internal static void Reload(GraphicalUiElement root, GumProjectSave newProject)
    {
        ObjectFinder.Self.GumProjectSave = newProject;
        GumHotReloadManager.ApplyDiff(new[] { root }, newProject, SystemManagers.Default);
    }

    internal static int Red(GraphicalUiElement element) => ((Gum.GueDeriving.ColoredRectangleRuntime)element).Red;

    internal static GraphicalUiElement Child(GraphicalUiElement parent, string name) =>
        parent.GetGraphicalUiElementByName(name)!;

    internal static T Control<T>(GraphicalUiElement visual) where T : FrameworkElement =>
        (T)((InteractiveGue)visual).FormsControlAsObject!;

    internal static void SaveToDisk(GumProjectSave project, string gumxPath)
    {
        project.ScreenReferences.Clear();
        project.ComponentReferences.Clear();
        project.StandardElementReferences.Clear();
        project.BehaviorReferences.Clear();
        foreach (ScreenSave screen in project.Screens)
        {
            project.ScreenReferences.Add(new ElementReference { Name = screen.Name, ElementType = ElementType.Screen });
        }
        foreach (ComponentSave component in project.Components)
        {
            project.ComponentReferences.Add(new ElementReference { Name = component.Name, ElementType = ElementType.Component });
        }
        foreach (StandardElementSave standard in project.StandardElements)
        {
            project.StandardElementReferences.Add(new ElementReference { Name = standard.Name, ElementType = ElementType.Standard });
        }
        foreach (BehaviorSave behavior in project.Behaviors)
        {
            project.BehaviorReferences.Add(new BehaviorReference { Name = behavior.Name });
        }
        project.Save(gumxPath, saveElements: true);
    }
}

/// <summary>
/// Characterization tests for the process rule: a value authored on a Visual in the Gum tool still
/// flows to the control, and a reload applies the value the edited file CHANGED. Green before the
/// #6855 fix and must stay green.
/// </summary>
public class HotReloadAuthoredValuesTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Fact]
    public void FirstLoad_AuthoredEnabledFalseAndText_FlowToTheControl()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(
            HotReloadFormsProjects.First with { AuthoredEnabled = false }));

        Button button = HotReloadFormsProjects.Control<Button>(HotReloadFormsProjects.Child(screen, "Button1"));

        button.IsEnabled.ShouldBeFalse();
        button.Text.ShouldBe("First");
    }

    [Fact]
    public void ApplyDiff_FileChangesAuthoredEnabledAndText_TheFileWins()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(
            HotReloadFormsProjects.First with { AuthoredEnabled = false }));
        Button button = HotReloadFormsProjects.Control<Button>(HotReloadFormsProjects.Child(screen, "Button1"));

        HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(
            HotReloadFormsProjects.Second with { AuthoredEnabled = true }));

        button.IsEnabled.ShouldBeTrue();
        button.Text.ShouldBe("Second");
    }

    [Fact]
    public void ApplyDiff_AuthoredEnabledFalseUnchangedInFile_StaysDisabled()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(
            HotReloadFormsProjects.First with { AuthoredEnabled = false }));
        Button button = HotReloadFormsProjects.Control<Button>(HotReloadFormsProjects.Child(screen, "Button1"));

        HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(
            HotReloadFormsProjects.Second with { AuthoredEnabled = false }));

        button.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void ApplyDiff_IsEnabledSetInCodeAndNotAuthored_StaysWhatCodeSet()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(HotReloadFormsProjects.First));
        Button button = HotReloadFormsProjects.Control<Button>(HotReloadFormsProjects.Child(screen, "Button1"));
        button.IsEnabled = false;

        HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(HotReloadFormsProjects.Second));

        button.IsEnabled.ShouldBeFalse();
    }
}

/// <summary>
/// #6855 / #6876: a reload re-applies the new default state and has to put every visual back into
/// its current categorical state, taken from the NEW file. General condition: a visual whose
/// current categorical state is not the default state when a reload re-applies the defaults.
/// </summary>
public class HotReloadFormsStateTests : BaseTestClass
{
    private static readonly HotReloadFormsProjects.Look First = HotReloadFormsProjects.First;
    private static readonly HotReloadFormsProjects.Look Second = HotReloadFormsProjects.Second;

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static ICursor Cursor => FrameworkElement.MainCursor;

    private static void EnterState(string state, GraphicalUiElement buttonVisual)
    {
        Button button = HotReloadFormsProjects.Control<Button>(buttonVisual);
        InteractiveGue visual = (InteractiveGue)buttonVisual;
        switch (state)
        {
            case "Disabled":
                button.IsEnabled = false;
                break;
            case "Highlighted":
                Cursor.VisualOver = visual;
                button.UpdateState();
                break;
            case "Pushed":
                Cursor.VisualOver = visual;
                Cursor.WindowPushed = visual;
                Mock.Get(Cursor).Setup(c => c.PrimaryDown).Returns(true);
                button.UpdateState();
                break;
            case "Focused":
                button.IsFocused = true;
                break;
            case "Enabled":
                break;
        }
    }

    private static int ExpectedRed(string state, HotReloadFormsProjects.Look look) => state switch
    {
        "Disabled" => look.Disabled,
        "Highlighted" => look.Highlighted,
        "Pushed" => look.Pushed,
        "Focused" => look.Focused,
        _ => look.Enabled
    };

    public static IEnumerable<object[]> States() =>
        new[] { "Enabled", "Disabled", "Highlighted", "Pushed", "Focused" }.Select(s => new object[] { s });

    [Theory]
    [MemberData(nameof(States))]
    public void Sanity_FirstLoad_ButtonInStateShowsTheStateValue(string state)
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(First));
        GraphicalUiElement button = HotReloadFormsProjects.Child(screen, "Button1");

        EnterState(state, button);

        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(button, "Background")).ShouldBe(ExpectedRed(state, First));
    }

    [Theory]
    [MemberData(nameof(States))]
    public void ApplyDiff_ButtonInState_KeepsTheStateWithTheNewFilesValues(string state)
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(First));
        GraphicalUiElement button = HotReloadFormsProjects.Child(screen, "Button1");
        EnterState(state, button);

        HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(Second));

        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(button, "Background")).ShouldBe(ExpectedRed(state, Second));
    }

    [Fact]
    public void ApplyDiff_StateChangesAfterReload_AppliesTheNewFilesValuesNotTheOldOnes()
    {
        // #6876: mouse over then off after a reload used to bring the old Enabled color back.
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(First));
        GraphicalUiElement button = HotReloadFormsProjects.Child(screen, "Button1");
        Button control = HotReloadFormsProjects.Control<Button>(button);
        GraphicalUiElement background = HotReloadFormsProjects.Child(button, "Background");

        HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(Second));
        Cursor.VisualOver = (InteractiveGue)button;
        control.UpdateState();
        HotReloadFormsProjects.Red(background).ShouldBe(Second.Highlighted);
        Cursor.VisualOver = null;
        control.UpdateState();

        HotReloadFormsProjects.Red(background).ShouldBe(Second.Enabled);
    }

    [Fact]
    public void ApplyDiff_ButtonInsideAComponentInsideAScreen_KeepsDisabledWithTheNewValues()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(First));
        GraphicalUiElement nested = HotReloadFormsProjects.Child(HotReloadFormsProjects.Child(screen, "Panel1"), "Button1");
        HotReloadFormsProjects.Control<Button>(nested).IsEnabled = false;

        HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(Second));

        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(nested, "Background")).ShouldBe(Second.Disabled);
    }

    [Fact]
    public void ApplyDiff_CheckBoxChecked_KeepsCheckedWithTheNewFilesValues()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(First));
        GraphicalUiElement visual = HotReloadFormsProjects.Child(screen, "Check1");
        CheckBox check = HotReloadFormsProjects.Control<CheckBox>(visual);
        check.IsChecked = true;
        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(visual, "Background")).ShouldBe(First.CheckedOn, "sanity");

        HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(Second));

        check.IsChecked.ShouldBe(true);
        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(visual, "Background")).ShouldBe(Second.CheckedOn);
    }

    [Fact]
    public void ApplyDiff_TextSetInCode_SurvivesTheReload()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(First));
        Button button = HotReloadFormsProjects.Control<Button>(HotReloadFormsProjects.Child(screen, "Button1"));
        button.Text = "FromCode";

        HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(Second));

        button.Text.ShouldBe("FromCode");
    }

    [Fact]
    public void ApplyDiff_ShownByCodeDrivenCategoryOnAPlainVisual_KeepsStateWithNewContents()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(First));
        GraphicalUiElement plain = HotReloadFormsProjects.Child(screen, "Plain1");
        screen.SetProperty("MyCategoryState", "B");
        HotReloadFormsProjects.Red(plain).ShouldBe(First.Highlighted, "sanity");

        HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(Second));

        HotReloadFormsProjects.Red(plain).ShouldBe(Second.Highlighted);
    }

    [Fact]
    public void ApplyDiff_DisabledStateRemovedByTheEdit_FallsBackToTheNewDefaultWithoutThrowing()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(First));
        GraphicalUiElement button = HotReloadFormsProjects.Child(screen, "Button1");
        HotReloadFormsProjects.Control<Button>(button).IsEnabled = false;

        HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(Second, withoutDisabledState: true));

        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(button, "Background")).ShouldBe(Second.Default);
    }

    [Fact]
    public void ApplyDiff_WholeCategoryRemovedByTheEdit_DoesNotThrow()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(First));
        GraphicalUiElement button = HotReloadFormsProjects.Child(screen, "Button1");
        HotReloadFormsProjects.Control<Button>(button).IsEnabled = false;

        Should.NotThrow(() =>
            HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(Second, withoutCategory: true)));

        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(button, "Background")).ShouldBe(Second.Default);
    }

    [Fact]
    public void ApplyDiff_RuntimeAddedDisabledButton_IsLeftAlone()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(First));
        Button added = new Button();
        screen.Children.Add(added.Visual);
        added.Visual.Parent = screen;
        added.IsEnabled = false;

        HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(Second));

        added.IsEnabled.ShouldBeFalse();
        added.Visual.Parent.ShouldBe(screen);
    }

    [Fact]
    public void PerformReload_ButtonDisabledAndFilesWrittenOneAfterTheOther_ShowsDisabledWithTheNewValue()
    {
        // #6855 through the real reload, and #6876's timing hypothesis: the Styles-like file and the
        // control's file land in separate reloads, each after the debounce.
        string sourceDirectory = Path.Combine(Path.GetTempPath(), "HotReloadFormsState6855_" + Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDirectory);
        try
        {
            string gumxPath = Path.Combine(sourceDirectory, "Proj.gumx");
            HotReloadFormsProjects.SaveToDisk(HotReloadFormsProjects.Build(First), gumxPath);
            GumProjectSave loaded = GumProjectSave.Load(gumxPath);
            loaded.Initialize();
            GraphicalUiElement screen = HotReloadFormsProjects.Load(loaded);
            GraphicalUiElement button = HotReloadFormsProjects.Child(screen, "Button1");
            GraphicalUiElement background = HotReloadFormsProjects.Child(button, "Background");
            HotReloadFormsProjects.Control<Button>(button).IsEnabled = false;
            GumHotReloadManager manager = new GumHotReloadManager(
                Gum.GumService.ApplyProjectTextureFilter, GumAnimationLoader.LoadAnimationsFromProvider,
                path => RenderingLibrary.Content.LoaderManager.Self.Dispose(path));
            manager.Start(gumxPath);
            manager.Stop();

            HotReloadFormsProjects.SaveToDisk(HotReloadFormsProjects.Build(Second), gumxPath);
            manager.PerformReload(new[] { screen });
            HotReloadFormsProjects.Red(background).ShouldBe(Second.Disabled);

            HotReloadFormsProjects.Control<Button>(button).IsEnabled = true;
            HotReloadFormsProjects.Red(background).ShouldBe(Second.Enabled);

            // A second reload (the later file of the burst) keeps the new values too.
            HotReloadFormsProjects.SaveToDisk(HotReloadFormsProjects.Build(Second with { Enabled = 161 }), gumxPath);
            manager.PerformReload(new[] { screen });
            Cursor.VisualOver = (InteractiveGue)button;
            HotReloadFormsProjects.Control<Button>(button).UpdateState();
            Cursor.VisualOver = null;
            HotReloadFormsProjects.Control<Button>(button).UpdateState();
            HotReloadFormsProjects.Red(background).ShouldBe(161);
        }
        finally
        {
            try { Directory.Delete(sourceDirectory, recursive: true); } catch { /* best-effort */ }
        }
    }
}
