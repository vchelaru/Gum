using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #6853: a value a control gets through a variable reference (<c>Color = Components/Styles.Primary.Color</c>)
/// is saved into the control's own file as a plain value by the Gum tool. Editing only the Styles
/// file (a text editor, or any save that leaves the referencing file alone) leaves that saved copy
/// stale, so a reload that only re-applies each element's own default state shows the old look.
/// Each test builds the project twice, like <see cref="HotReloadRemovedVariableTests"/>: the first
/// copy is what the running game loaded, the second is what the reload reads. <c>file</c> is what
/// the edited source element holds; <c>stale</c> is the copy saved in the referencing control.
/// </summary>
public class HotReloadStylesReferenceTests : BaseTestClass
{
    public enum Scenario
    {
        /// <summary>Background.Color = Components/Styles.Primary.Color, button Enabled.</summary>
        StylesColor,
        /// <summary>Width = Standards/Container.Width.</summary>
        StandardVariable,
        /// <summary>Height = Components/Styles.ControlHeight (a variable on the component itself).</summary>
        ComponentVariable,
        /// <summary>Width = Screens/ThemeScreen.SharedWidth (a variable on a screen).</summary>
        ScreenVariable,
        /// <summary>Control -> Styles.Primary -> Styles.Brand; only Brand is edited.</summary>
        Chain,
        /// <summary>The Highlighted state of ButtonCategory references Styles.PrimaryLight.</summary>
        HighlightedState,
        /// <summary>The Disabled state of ButtonCategory references Styles.PrimaryDisabled.</summary>
        DisabledState,
        /// <summary>A plain value edited in the control's own file; no reference involved.</summary>
        NonReference
    }

    private sealed record Values(
        int Primary, int PrimaryLight, int PrimaryDisabled, int Brand,
        float ControlHeight, float StandardWidth, float ScreenWidth, int OwnGreen);

    private static readonly Values First = new(10, 20, 30, 40, 50f, 60f, 70f, 80);
    private static readonly Values Second = new(110, 120, 130, 140, 150f, 160f, 170f, 180);

    public static IEnumerable<object[]> ReferenceScenarios() =>
        new[]
        {
            Scenario.StylesColor, Scenario.StandardVariable, Scenario.ComponentVariable,
            Scenario.ScreenVariable, Scenario.Chain, Scenario.HighlightedState, Scenario.DisabledState
        }.Select(s => new object[] { s });

    public static IEnumerable<object[]> AllScenarios() =>
        ReferenceScenarios().Concat(new[] { new object[] { Scenario.NonReference } });

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    // ---- Project construction ----

    private static void SetVariable(StateSave state, string name, string type, object value)
    {
        state.Variables.RemoveAll(v => v.Name == name);
        state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
    }

    private static void SetRows(StateSave state, string owner, params string[] rows)
    {
        VariableListSave<string> list = new VariableListSave<string>
        {
            Type = "string",
            Name = string.IsNullOrEmpty(owner) ? "VariableReferences" : owner + ".VariableReferences"
        };
        list.Value.AddRange(rows);
        state.VariableLists.Add(list);
    }

    private static void SetColor(StateSave state, string owner, int red)
    {
        SetVariable(state, owner + ".Red", "int", red);
        SetVariable(state, owner + ".Green", "int", 0);
        SetVariable(state, owner + ".Blue", "int", 0);
    }

    private static GumProjectSave BuildProject(Scenario scenario, Values file, Values stale)
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
        SetVariable(project.StandardElements.First(s => s.Name == "Container").DefaultState!, "Width", "float", file.StandardWidth);

        project.Behaviors.Add(new BehaviorSave { Name = StandardFormsBehaviorNames.ButtonBehaviorName });

        // ---- Styles ----
        ComponentSave styles = new ComponentSave { Name = "Styles", BaseType = "Container" };
        StateSave stylesDefault = new StateSave { Name = "Default", ParentContainer = styles };
        styles.States.Add(stylesDefault);
        foreach (string instance in new[] { "Primary", "PrimaryLight", "PrimaryDisabled", "Brand" })
        {
            styles.Instances.Add(new InstanceSave { Name = instance, BaseType = "ColoredRectangle", ParentContainer = styles });
        }
        SetColor(stylesDefault, "Primary", scenario == Scenario.Chain ? stale.Brand : file.Primary);
        SetColor(stylesDefault, "PrimaryLight", file.PrimaryLight);
        SetColor(stylesDefault, "PrimaryDisabled", file.PrimaryDisabled);
        SetColor(stylesDefault, "Brand", file.Brand);
        SetVariable(stylesDefault, "ControlHeight", "float", file.ControlHeight);
        if (scenario == Scenario.Chain)
        {
            SetRows(stylesDefault, "Primary", "Color = Brand.Color");
        }
        project.Components.Add(styles);

        // ---- A screen that only holds a value ----
        ScreenSave theme = new ScreenSave { Name = "ThemeScreen" };
        StateSave themeDefault = new StateSave { Name = "Default", ParentContainer = theme };
        SetVariable(themeDefault, "SharedWidth", "float", file.ScreenWidth);
        theme.States.Add(themeDefault);
        project.Screens.Add(theme);

        // ---- The control ----
        ComponentSave button = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        StateSave buttonDefault = new StateSave { Name = "Default", ParentContainer = button };
        button.States.Add(buttonDefault);
        button.Instances.Add(new InstanceSave { Name = "Background", BaseType = "ColoredRectangle", ParentContainer = button });
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = StandardFormsBehaviorNames.ButtonBehaviorName });
        SetVariable(buttonDefault, "ButtonCategoryState", "ButtonCategory", null!);
        buttonDefault.Variables.Last().SetsValue = false;

        // What the tool saves next to a reference row: the evaluated value, as a plain variable.
        SetColor(buttonDefault, "Background", stale.Primary);
        SetRows(buttonDefault, "Background", "Color = Components/Styles.Primary.Color");

        switch (scenario)
        {
            case Scenario.Chain:
                SetColor(buttonDefault, "Background", stale.Brand);
                break;
            case Scenario.StandardVariable:
                SetVariable(buttonDefault, "Width", "float", stale.StandardWidth);
                SetRows(buttonDefault, "", "Width = Standards/Container.Width");
                break;
            case Scenario.ComponentVariable:
                SetVariable(buttonDefault, "Height", "float", stale.ControlHeight);
                SetRows(buttonDefault, "", "Height = Components/Styles.ControlHeight");
                break;
            case Scenario.ScreenVariable:
                SetVariable(buttonDefault, "Width", "float", stale.ScreenWidth);
                SetRows(buttonDefault, "", "Width = Screens/ThemeScreen.SharedWidth");
                break;
            case Scenario.NonReference:
                buttonDefault.VariableLists.Clear();
                SetVariable(buttonDefault, "Background.Green", "int", file.OwnGreen);
                break;
        }

        StateSaveCategory category = new StateSaveCategory { Name = "ButtonCategory" };
        StateSave enabled = new StateSave { Name = "Enabled", ParentContainer = button };
        StateSave highlighted = new StateSave { Name = "Highlighted", ParentContainer = button };
        StateSave disabled = new StateSave { Name = "Disabled", ParentContainer = button };
        SetColor(highlighted, "Background", stale.PrimaryLight);
        SetRows(highlighted, "Background", "Color = Components/Styles.PrimaryLight.Color");
        SetColor(disabled, "Background", stale.PrimaryDisabled);
        SetRows(disabled, "Background", "Color = Components/Styles.PrimaryDisabled.Color");
        category.States.AddRange(new[] { enabled, highlighted, disabled });
        button.Categories.Add(category);
        project.Components.Add(button);

        // ---- The running screen ----
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Button1", BaseType = button.Name, ParentContainer = screen });
        project.Screens.Add(screen);

        project.Initialize();
        return project;
    }

    private static GraphicalUiElement Load(GumProjectSave project)
    {
        ObjectFinder.Self.GumProjectSave = project;
        FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        ElementSaveExtensions.RegisterGueInstantiation("ColoredRectangle", () => new Gum.GueDeriving.ColoredRectangleRuntime());
        return project.Screens.Single(s => s.Name == "TestScreen")
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
    }

    private static int Red(GraphicalUiElement element) => ((Gum.GueDeriving.ColoredRectangleRuntime)element).Red;

    private static int Green(GraphicalUiElement element) => ((Gum.GueDeriving.ColoredRectangleRuntime)element).Green;

    private static GraphicalUiElement ButtonOf(GraphicalUiElement screen) =>
        screen.GetGraphicalUiElementByName("Button1")!;

    private static void Reload(GraphicalUiElement root, GumProjectSave newProject)
    {
        ObjectFinder.Self.GumProjectSave = newProject;
        GumHotReloadManager.ApplyDiff(new[] { root }, newProject, SystemManagers.Default);
    }

    /// <summary>Puts the button into the state the scenario is about; returns what to read afterward.</summary>
    private static Func<float> PrepareAndReader(Scenario scenario, GraphicalUiElement screen, Values expectedFrom, out float expected, bool enterState = true)
    {
        GraphicalUiElement button = ButtonOf(screen);
        GraphicalUiElement background = button.GetGraphicalUiElementByName("Background")!;
        FrameworkElement control = (FrameworkElement)((InteractiveGue)button).FormsControlAsObject!;
        switch (scenario)
        {
            case Scenario.StandardVariable:
                expected = expectedFrom.StandardWidth;
                return () => button.Width;
            case Scenario.ScreenVariable:
                expected = expectedFrom.ScreenWidth;
                return () => button.Width;
            case Scenario.ComponentVariable:
                expected = expectedFrom.ControlHeight;
                return () => button.Height;
            case Scenario.Chain:
                expected = expectedFrom.Brand;
                return () => Red(background);
            case Scenario.HighlightedState:
                if (enterState) { button.SetProperty("ButtonCategoryState", "Highlighted"); }
                expected = expectedFrom.PrimaryLight;
                return () => Red(background);
            case Scenario.DisabledState:
                if (enterState) { control.IsEnabled = false; }
                expected = expectedFrom.PrimaryDisabled;
                return () => Red(background);
            case Scenario.NonReference:
                expected = expectedFrom.OwnGreen;
                return () => Green(background);
            default:
                expected = expectedFrom.Primary;
                return () => Red(background);
        }
    }

    // ---- Tests ----

    [Theory]
    [MemberData(nameof(AllScenarios))]
    public void Sanity_FirstLoad_ShowsTheFirstValues(Scenario scenario)
    {
        GraphicalUiElement screen = Load(BuildProject(scenario, First, First));

        Func<float> read = PrepareAndReader(scenario, screen, First, out float expected);

        read().ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(AllScenarios))]
    public void ApplyDiff_BothFilesSavedByTheTool_ControlShowsTheNewValue(Scenario scenario)
    {
        // The tool re-evaluates dependents and saves them, so the referencing file is fresh too.
        GraphicalUiElement screen = Load(BuildProject(scenario, First, First));
        Func<float> read = PrepareAndReader(scenario, screen, First, out float _);

        Reload(screen, BuildProject(scenario, Second, Second));

        PrepareAndReader(scenario, screen, Second, out float expected, enterState: false);
        read().ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(ReferenceScenarios))]
    public void ApplyDiff_OnlyTheSourceFileEdited_ControlShowsTheNewValue(Scenario scenario)
    {
        // A text-editor edit of Styles: the control's saved copy of the referenced value is stale.
        GraphicalUiElement screen = Load(BuildProject(scenario, First, First));
        Func<float> read = PrepareAndReader(scenario, screen, First, out float _);

        Reload(screen, BuildProject(scenario, Second, First));

        PrepareAndReader(scenario, screen, Second, out float expected, enterState: false);
        read().ShouldBe(expected);
    }

    [Theory]
    [MemberData(nameof(ReferenceScenarios))]
    public void ApplyDiff_OnlyTheSourceFileEdited_AfterApplyAllVariableReferences_ControlShowsTheNewValue(
        Scenario scenario)
    {
        // The smallest fix shape: propagate references in the new project before applying it.
        GraphicalUiElement screen = Load(BuildProject(scenario, First, First));
        Func<float> read = PrepareAndReader(scenario, screen, First, out float _);

        GumProjectSave second = BuildProject(scenario, Second, First);
        second.ApplyAllVariableReferences();
        Reload(screen, second);

        PrepareAndReader(scenario, screen, Second, out float expected, enterState: false);
        read().ShouldBe(expected);
    }

    [Fact]
    public void PerformReload_OnlyStylesFileEditedOnDisk_ControlShowsTheNewColor()
    {
        string sourceDirectory = Path.Combine(
            Path.GetTempPath(), "HotReloadStylesReferenceTests_" + Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDirectory);

        try
        {
            string gumxPath = Path.Combine(sourceDirectory, "Proj.gumx");
            SaveToDisk(BuildProject(Scenario.StylesColor, First, First), gumxPath);

            GumProjectSave loaded = GumProjectSave.Load(gumxPath);
            loaded.Initialize();
            GraphicalUiElement screen = Load(loaded);
            GraphicalUiElement background = ButtonOf(screen).GetGraphicalUiElementByName("Background")!;
            Red(background).ShouldBe(First.Primary, "sanity");

            // Only Styles changes on disk; the control file keeps the old saved copy.
            SaveToDisk(BuildProject(Scenario.StylesColor, Second, First), gumxPath);
            GumHotReloadManager manager = new GumHotReloadManager(
                Gum.GumService.ApplyProjectTextureFilter, GumAnimationLoader.LoadAnimationsFromProvider,
                path => RenderingLibrary.Content.LoaderManager.Self.Dispose(path));
            manager.Start(gumxPath);
            manager.Stop();

            manager.PerformReload(new[] { screen });

            Red(background).ShouldBe(Second.Primary);
        }
        finally
        {
            try { Directory.Delete(sourceDirectory, recursive: true); } catch { /* best-effort */ }
        }
    }

    private static void SaveToDisk(GumProjectSave project, string gumxPath)
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
