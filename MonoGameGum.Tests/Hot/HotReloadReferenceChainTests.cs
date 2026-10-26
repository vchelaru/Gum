using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace MonoGameGum.Tests.Hot;

public enum ChainPlacement
{
    /// <summary>Every member is an instance of one component (Styles.Sw1 reads Styles.Sw2, ...).</summary>
    OneComponent,
    /// <summary>Every member lives in its own component (Components/Chain1 reads Components/Chain2, ...).</summary>
    AcrossComponents
}

public enum ChainEdit
{
    /// <summary>Only the last member's file changed; everything that reads it holds the old saved copy.</summary>
    TailOnly,
    /// <summary>The first member's reference row now points at another swatch; its saved copy and the reader's are old.</summary>
    MiddleRetargeted,
    /// <summary>The tool re-saved every member and the reader: nothing is stale.</summary>
    BothSaved
}

public enum ChainReader
{
    ButtonInScreen,
    ButtonNestedInPanel,
    /// <summary>A ColoredRectangle instance in the screen whose own row reads the chain.</summary>
    RectangleOnScreen
}

public enum ChainReaderState { Enabled, Highlighted, Disabled }

/// <summary>One cell of the chain matrix.</summary>
public sealed record ChainCell(
    int Length,
    ChainPlacement Placement,
    ChainEdit Edit,
    ChainReader Reader = ChainReader.ButtonInScreen,
    ChainReaderState State = ChainReaderState.Enabled,
    bool StylesLiveInScreen = false,
    bool Expressions = false)
{
    public override string ToString() =>
        $"len{Length} {Placement} {Edit} {Reader} {State}{(StylesLiveInScreen ? " live" : "")}{(Expressions ? " expr" : "")}";
}

/// <summary>
/// #6853: a value fed by a reference whose source is itself fed by a reference (reader -> Primary ->
/// Brand) must show the end of the chain after a reload, even when only the last file was edited and
/// the files in between still hold the value they were saved with. The reload has to evaluate the
/// chain in dependency order against the new project, like a tool save does.
/// </summary>
public class HotReloadReferenceChainTests : BaseTestClass
{
    private const int Old = 10;
    private const int New = 110;
    private const int Accent = 210;
    private const int Mid = 150;

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static void UseExpressions(bool useExpressions)
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
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

    private static void SetRed(StateSave state, string owner, int red)
    {
        string prefix = string.IsNullOrEmpty(owner) ? "" : owner + ".";
        SetVariable(state, prefix + "Red", "int", red);
        SetVariable(state, prefix + "Green", "int", 0);
        SetVariable(state, prefix + "Blue", "int", 0);
    }

    private static GumProjectSave NewProjectWithStandards()
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new GumProjectSave();
        foreach (string standard in new[] { "Container", "ColoredRectangle", "Text" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            // A copy: the manager hands out one shared state, which a test that adds rows would leak into the next.
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!.Clone());
            standardElement.DefaultState!.ParentContainer = standardElement;
            project.StandardElements.Add(standardElement);
        }
        project.Behaviors.Add(new BehaviorSave { Name = StandardFormsBehaviorNames.ButtonBehaviorName });
        project.Behaviors[0].FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        return project;
    }

    /// <summary>The reference path a reader row reads: the first swatch of the chain.</summary>
    private static string HeadPath(ChainCell cell) =>
        cell.Placement == ChainPlacement.OneComponent
            ? "Components/Styles.Sw1.Color"
            : "Components/Chain1.Swatch.Color";

    private static string AccentPath(ChainCell cell) =>
        cell.Placement == ChainPlacement.OneComponent
            ? "Components/Styles.Accent.Color"
            : "Components/ChainAccent.Swatch.Color";

    private static ComponentSave NewComponent(string name, string baseType = "Container")
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    private static void AddSwatch(ComponentSave component, string instance, int red, params string[] rows)
    {
        component.Instances.Add(new InstanceSave { Name = instance, BaseType = "ColoredRectangle", ParentContainer = component });
        SetRed(component.DefaultState!, instance, red);
        if (rows.Length > 0)
        {
            SetRows(component.DefaultState!, instance, rows);
        }
    }

    /// <summary>
    /// <paramref name="second"/> is the file as it is after the edit; false is what the running game loaded,
    /// where every saved value is <see cref="Old"/>.
    /// </summary>
    private static GumProjectSave Build(ChainCell cell, bool second)
    {
        GumProjectSave project = NewProjectWithStandards();

        bool tailEdited = second && cell.Edit != ChainEdit.MiddleRetargeted;
        int tail = tailEdited ? New : Old;
        int saved = second && cell.Edit == ChainEdit.BothSaved ? New : Old;
        bool retarget = second && cell.Edit == ChainEdit.MiddleRetargeted;
        int length = cell.Length;

        // Member i reads member i + 1 (the first reads the accent after a retarget); the last is plain.
        string RowFor(int i, Func<int, string> nextPath) =>
            "Color = " + (i == 1 && retarget ? (cell.Placement == ChainPlacement.OneComponent ? "Accent.Color" : "Components/ChainAccent.Swatch.Color")
                                              : nextPath(i + 1));

        if (cell.Placement == ChainPlacement.OneComponent)
        {
            ComponentSave styles = NewComponent("Styles");
            for (int i = 1; i <= length; i++)
            {
                if (i == length)
                {
                    AddSwatch(styles, "Sw" + i, tail);
                }
                else
                {
                    AddSwatch(styles, "Sw" + i, saved, RowFor(i, n => "Sw" + n + ".Color"));
                }
            }
            AddSwatch(styles, "Accent", Accent);
            project.Components.Add(styles);
        }
        else
        {
            for (int i = 1; i <= length; i++)
            {
                ComponentSave chain = NewComponent("Chain" + i);
                if (i == length)
                {
                    AddSwatch(chain, "Swatch", tail);
                }
                else
                {
                    AddSwatch(chain, "Swatch", saved, RowFor(i, n => "Components/Chain" + n + ".Swatch.Color"));
                }
                project.Components.Add(chain);
            }
            ComponentSave accent = NewComponent("ChainAccent");
            AddSwatch(accent, "Swatch", Accent);
            project.Components.Add(accent);
        }

        AddReaders(project, cell, readerSaved: saved);
        return project;
    }

    private static void AddReaders(GumProjectSave project, ChainCell cell, int readerSaved)
    {
        string path = HeadPath(cell);

        ComponentSave button = NewComponent("Controls/ButtonStandard");
        button.Instances.Add(new InstanceSave { Name = "Background", BaseType = "ColoredRectangle", ParentContainer = button });
        button.Instances.Add(new InstanceSave { Name = "TextInstance", BaseType = "Text", ParentContainer = button });
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = StandardFormsBehaviorNames.ButtonBehaviorName });
        SetRed(button.DefaultState!, "Background", readerSaved);
        SetRows(button.DefaultState!, "Background", "Color = " + path);
        StateSaveCategory category = new StateSaveCategory { Name = "ButtonCategory" };
        foreach (string stateName in new[] { "Enabled", "Highlighted", "Disabled" })
        {
            StateSave state = new StateSave { Name = stateName, ParentContainer = button };
            if (stateName != "Enabled")
            {
                SetRed(state, "Background", readerSaved);
                SetRows(state, "Background", "Color = " + path);
            }
            category.States.Add(state);
        }
        button.Categories.Add(category);
        project.Components.Add(button);

        ComponentSave panel = NewComponent("Controls/Panel");
        panel.Instances.Add(new InstanceSave { Name = "Button1", BaseType = button.Name, ParentContainer = panel });
        project.Components.Add(panel);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);
        screen.Instances.Add(new InstanceSave { Name = "Button1", BaseType = button.Name, ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Panel1", BaseType = panel.Name, ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Swatch", BaseType = "ColoredRectangle", ParentContainer = screen });
        SetRed(screenDefault, "Swatch", readerSaved);
        SetRows(screenDefault, "Swatch", "Color = " + path);
        if (cell.StylesLiveInScreen)
        {
            string stylesName = cell.Placement == ChainPlacement.OneComponent ? "Styles" : "Chain1";
            screen.Instances.Add(new InstanceSave { Name = "StylesLive", BaseType = stylesName, ParentContainer = screen });
        }
        project.Screens.Add(screen);

        project.Initialize();
    }

    // ---- Reading ----

    private static GraphicalUiElement ReaderVisual(GraphicalUiElement screen, ChainReader reader) => reader switch
    {
        ChainReader.ButtonInScreen => HotReloadFormsProjects.Child(HotReloadFormsProjects.Child(screen, "Button1"), "Background"),
        ChainReader.ButtonNestedInPanel => HotReloadFormsProjects.Child(
            HotReloadFormsProjects.Child(HotReloadFormsProjects.Child(screen, "Panel1"), "Button1"), "Background"),
        _ => HotReloadFormsProjects.Child(screen, "Swatch")
    };

    private static Button? ButtonOf(GraphicalUiElement screen, ChainReader reader) => reader switch
    {
        ChainReader.ButtonInScreen => HotReloadFormsProjects.Control<Button>(HotReloadFormsProjects.Child(screen, "Button1")),
        ChainReader.ButtonNestedInPanel => HotReloadFormsProjects.Control<Button>(
            HotReloadFormsProjects.Child(HotReloadFormsProjects.Child(screen, "Panel1"), "Button1")),
        _ => null
    };

    private static void EnterState(GraphicalUiElement screen, ChainCell cell)
    {
        Button? button = ButtonOf(screen, cell.Reader);
        if (button == null)
        {
            return;
        }

        if (cell.State == ChainReaderState.Highlighted)
        {
            button.Visual.SetProperty("ButtonCategoryState", "Highlighted");
        }
        else if (cell.State == ChainReaderState.Disabled)
        {
            button.IsEnabled = false;
        }
    }

    private static int ExpectedAfterEdit(ChainCell cell) => cell.Edit == ChainEdit.MiddleRetargeted ? Accent : New;

    private static GraphicalUiElement LoadFirst(ChainCell cell)
    {
        UseExpressions(cell.Expressions);
        return HotReloadFormsProjects.Load(Build(cell, second: false));
    }

    // ---- The matrix ----

    private static IEnumerable<ChainCell> StaleCells()
    {
        foreach (int length in new[] { 2, 3, 4 })
        {
            foreach (ChainPlacement placement in new[] { ChainPlacement.OneComponent, ChainPlacement.AcrossComponents })
            {
                foreach (ChainEdit edit in new[] { ChainEdit.TailOnly, ChainEdit.MiddleRetargeted })
                {
                    yield return new ChainCell(length, placement, edit);
                }
            }
        }

        // Readers and reader states.
        foreach (ChainPlacement placement in new[] { ChainPlacement.OneComponent, ChainPlacement.AcrossComponents })
        {
            yield return new ChainCell(3, placement, ChainEdit.TailOnly, ChainReader.ButtonNestedInPanel);
            yield return new ChainCell(3, placement, ChainEdit.TailOnly, ChainReader.RectangleOnScreen);
            yield return new ChainCell(3, placement, ChainEdit.MiddleRetargeted, ChainReader.RectangleOnScreen);
            yield return new ChainCell(3, placement, ChainEdit.TailOnly, ChainReader.ButtonInScreen, ChainReaderState.Highlighted);
            yield return new ChainCell(3, placement, ChainEdit.TailOnly, ChainReader.ButtonInScreen, ChainReaderState.Disabled);
            yield return new ChainCell(3, placement, ChainEdit.TailOnly, ChainReader.ButtonNestedInPanel, ChainReaderState.Disabled);
            yield return new ChainCell(3, placement, ChainEdit.MiddleRetargeted, ChainReader.ButtonInScreen, ChainReaderState.Highlighted);

            // The chain's component is also instantiated in the running screen.
            yield return new ChainCell(3, placement, ChainEdit.TailOnly, StylesLiveInScreen: true);
            yield return new ChainCell(3, placement, ChainEdit.MiddleRetargeted, StylesLiveInScreen: true);

            // With the optional Gum.Expressions package.
            yield return new ChainCell(3, placement, ChainEdit.TailOnly, Expressions: true);
            yield return new ChainCell(3, placement, ChainEdit.MiddleRetargeted, Expressions: true);
        }
    }

    public static IEnumerable<object[]> StaleMatrix() => StaleCells().Select(c => new object[] { c });

    public static IEnumerable<object[]> FreshMatrix()
    {
        foreach (int length in new[] { 1, 2, 3 })
        {
            foreach (ChainPlacement placement in new[] { ChainPlacement.OneComponent, ChainPlacement.AcrossComponents })
            {
                yield return new object[] { new ChainCell(length, placement, length == 1 ? ChainEdit.TailOnly : ChainEdit.BothSaved) };
            }
        }
    }

    [Theory]
    [MemberData(nameof(StaleMatrix))]
    public void ApplyDiff_OnlyAMemberOfTheChainEdited_ReaderShowsTheEndOfTheChain(ChainCell cell)
    {
        GraphicalUiElement screen = LoadFirst(cell);
        EnterState(screen, cell);
        GraphicalUiElement reader = ReaderVisual(screen, cell.Reader);
        HotReloadFormsProjects.Red(reader).ShouldBe(Old, "sanity: first load");

        HotReloadFormsProjects.Reload(screen, Build(cell, second: true));

        HotReloadFormsProjects.Red(reader).ShouldBe(ExpectedAfterEdit(cell));
    }

    [Theory]
    [MemberData(nameof(FreshMatrix))]
    public void ApplyDiff_NothingStaleOrDirectReference_ReaderShowsTheNewValue(ChainCell cell)
    {
        // Characterization: a direct reference (length 1) or files the tool saved already work.
        GraphicalUiElement screen = LoadFirst(cell);
        GraphicalUiElement reader = ReaderVisual(screen, cell.Reader);

        HotReloadFormsProjects.Reload(screen, Build(cell, second: true));

        HotReloadFormsProjects.Red(reader).ShouldBe(New);
    }

    [Theory]
    [InlineData(ChainReaderState.Enabled)]
    [InlineData(ChainReaderState.Highlighted)]
    [InlineData(ChainReaderState.Disabled)]
    public void ApplyDiff_SecondReloadAfterTheChainSettled_KeepsTheValueAndTheState(ChainReaderState state)
    {
        ChainCell cell = new ChainCell(3, ChainPlacement.AcrossComponents, ChainEdit.TailOnly, State: state);
        GraphicalUiElement screen = LoadFirst(cell);
        EnterState(screen, cell);
        GraphicalUiElement reader = ReaderVisual(screen, cell.Reader);

        HotReloadFormsProjects.Reload(screen, Build(cell, second: true));
        HotReloadFormsProjects.Reload(screen, Build(cell with { Edit = ChainEdit.BothSaved }, second: true));

        HotReloadFormsProjects.Red(reader).ShouldBe(New);
        Button button = ButtonOf(screen, ChainReader.ButtonInScreen)!;
        button.IsEnabled.ShouldBe(state != ChainReaderState.Disabled);
    }

    [Fact]
    public void ApplyDiff_ChainReloadedAndTextSetInCode_CodeValueSurvives()
    {
        ChainCell cell = new ChainCell(3, ChainPlacement.OneComponent, ChainEdit.TailOnly);
        GraphicalUiElement screen = LoadFirst(cell);
        Button button = ButtonOf(screen, ChainReader.ButtonInScreen)!;
        button.Text = "FromCode";

        HotReloadFormsProjects.Reload(screen, Build(cell, second: true));

        button.Text.ShouldBe("FromCode");
        HotReloadFormsProjects.Red(ReaderVisual(screen, cell.Reader)).ShouldBe(New);
    }

    [Fact]
    public void ApplyDiff_PlainValueEditedNextToAChain_ValueFlows()
    {
        // Characterization: a value with no reference behind it is not touched by chain evaluation.
        ChainCell cell = new ChainCell(3, ChainPlacement.OneComponent, ChainEdit.TailOnly);
        GraphicalUiElement screen = LoadFirst(cell);
        GumProjectSave second = Build(cell, second: true);
        ComponentSave button = second.Components.Single(c => c.Name == "Controls/ButtonStandard");
        SetVariable(button.DefaultState!, "Background.Alpha", "int", 77);

        HotReloadFormsProjects.Reload(screen, second);

        ((Gum.GueDeriving.ColoredRectangleRuntime)ReaderVisual(screen, ChainReader.ButtonInScreen)).Alpha.ShouldBe(77);
    }

    // ---- Removal and cycles ----

    [Fact]
    public void ApplyDiff_TailOfTheChainRemoved_FollowersKeepTheirSavedValueWithoutThrowing()
    {
        ChainCell cell = new ChainCell(3, ChainPlacement.OneComponent, ChainEdit.TailOnly);
        GraphicalUiElement screen = LoadFirst(cell);
        GraphicalUiElement reader = ReaderVisual(screen, cell.Reader);

        GumProjectSave second = Build(cell, second: true);
        ComponentSave styles = second.Components.Single(c => c.Name == "Styles");
        styles.Instances.RemoveAll(i => i.Name == "Sw3");
        styles.DefaultState!.Variables.RemoveAll(v => v.Name.StartsWith("Sw3."));

        Should.NotThrow(() => HotReloadFormsProjects.Reload(screen, second));

        HotReloadFormsProjects.Red(reader).ShouldBe(Old);
    }

    [Fact]
    public void ApplyDiff_ReferenceRowRemovedFromAMember_FollowersShowItsSavedValue()
    {
        ChainCell cell = new ChainCell(3, ChainPlacement.AcrossComponents, ChainEdit.TailOnly);
        GraphicalUiElement screen = LoadFirst(cell);
        GraphicalUiElement reader = ReaderVisual(screen, cell.Reader);

        GumProjectSave second = Build(cell, second: true);
        ComponentSave chain2 = second.Components.Single(c => c.Name == "Chain2");
        chain2.DefaultState!.VariableLists.Clear();
        SetRed(chain2.DefaultState!, "Swatch", Mid);

        HotReloadFormsProjects.Reload(screen, second);

        HotReloadFormsProjects.Red(reader).ShouldBe(Mid);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyDiff_ChainIsACycle_Terminates(bool expressions)
    {
        ChainCell cell = new ChainCell(2, ChainPlacement.OneComponent, ChainEdit.TailOnly, Expressions: expressions);
        GraphicalUiElement screen = LoadFirst(cell);
        GraphicalUiElement reader = ReaderVisual(screen, cell.Reader);

        GumProjectSave second = Build(cell, second: true);
        ComponentSave styles = second.Components.Single(c => c.Name == "Styles");
        SetRows(styles.DefaultState!, "Sw2", "Color = Sw1.Color");

        Should.NotThrow(() => HotReloadFormsProjects.Reload(screen, second));

        HotReloadFormsProjects.Red(reader).ShouldBeInRange(0, 255);
    }

    // ---- A Standards element in the chain ----

    private static GumProjectSave BuildStandardChain(bool standardInMiddle, bool second)
    {
        GumProjectSave project = NewProjectWithStandards();
        StandardElementSave container = project.StandardElements.Single(s => s.Name == "Container");
        float newValue = 160f;
        float oldValue = 60f;
        float edited = second ? newValue : oldValue;

        ComponentSave styles = NewComponent("Styles");
        if (!standardInMiddle)
        {
            // Button.Height -> Styles.ControlHeight -> Standards/Container.Width (only the Standard is edited).
            SetVariable(container.DefaultState!, "Width", "float", edited);
            SetVariable(styles.DefaultState!, "ControlHeight", "float", oldValue);
            SetRows(styles.DefaultState!, "", "ControlHeight = Standards/Container.Width");
        }
        else
        {
            // Button.Height -> Standards/Container.Width -> Styles.ControlHeight (only Styles is edited).
            SetVariable(styles.DefaultState!, "ControlHeight", "float", edited);
            SetVariable(container.DefaultState!, "Width", "float", oldValue);
            SetRows(container.DefaultState!, "", "Width = Components/Styles.ControlHeight");
        }
        project.Components.Add(styles);

        ComponentSave button = NewComponent("Controls/ButtonStandard");
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = StandardFormsBehaviorNames.ButtonBehaviorName });
        SetVariable(button.DefaultState!, "Height", "float", oldValue);
        SetRows(button.DefaultState!, "", standardInMiddle
            ? "Height = Standards/Container.Width"
            : "Height = Components/Styles.ControlHeight");
        project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Button1", BaseType = button.Name, ParentContainer = screen });
        project.Screens.Add(screen);
        project.Initialize();
        return project;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyDiff_ChainThroughAStandardElement_ReaderShowsTheNewValue(bool standardInMiddle)
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(BuildStandardChain(standardInMiddle, second: false));
        GraphicalUiElement button = HotReloadFormsProjects.Child(screen, "Button1");
        button.Height.ShouldBe(60f, "sanity: first load");

        HotReloadFormsProjects.Reload(screen, BuildStandardChain(standardInMiddle, second: true));

        button.Height.ShouldBe(160f);
    }

    // ---- Through the real reload ----

    [Fact]
    public void PerformReload_FilesWrittenOneAfterTheOther_ChainShowsTheEndAfterEachWrite()
    {
        string sourceDirectory = Path.Combine(Path.GetTempPath(), "HotReloadChain6853_" + Path.GetRandomFileName());
        Directory.CreateDirectory(sourceDirectory);
        try
        {
            ChainCell cell = new ChainCell(3, ChainPlacement.AcrossComponents, ChainEdit.TailOnly);
            string gumxPath = Path.Combine(sourceDirectory, "Proj.gumx");
            HotReloadFormsProjects.SaveToDisk(Build(cell, second: false), gumxPath);
            GumProjectSave loaded = GumProjectSave.Load(gumxPath);
            loaded.Initialize();
            GraphicalUiElement screen = HotReloadFormsProjects.Load(loaded);
            GraphicalUiElement reader = ReaderVisual(screen, cell.Reader);
            HotReloadFormsProjects.Red(reader).ShouldBe(Old, "sanity");

            GumHotReloadManager manager = new GumHotReloadManager(
                Gum.GumService.ApplyProjectTextureFilter, GumAnimationLoader.LoadAnimationsFromProvider,
                path => RenderingLibrary.Content.LoaderManager.Self.Dispose(path));
            manager.Start(gumxPath);
            manager.Stop();

            // A text editor saves the tail file only.
            HotReloadFormsProjects.SaveToDisk(Build(cell, second: true), gumxPath);
            manager.PerformReload(new[] { screen });
            HotReloadFormsProjects.Red(reader).ShouldBe(New);

            // The tool then saves the rest, after the debounce.
            Thread.Sleep(250);
            HotReloadFormsProjects.SaveToDisk(Build(cell with { Edit = ChainEdit.BothSaved }, second: true), gumxPath);
            manager.PerformReload(new[] { screen });
            HotReloadFormsProjects.Red(reader).ShouldBe(New);
        }
        finally
        {
            try { Directory.Delete(sourceDirectory, recursive: true); } catch { /* best-effort */ }
        }
    }
}
