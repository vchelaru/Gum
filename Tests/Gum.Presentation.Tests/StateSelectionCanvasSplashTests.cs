using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Expressions;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

/// <summary>
/// Splash damage for #6851: what the canvas shows after the user selects a state, across state kinds,
/// reference rows on the default and the selected state, element kinds, authored behavior values and
/// refreshes. Every test drives the real <c>WireframeObjectManager.RefreshAll</c>.
/// </summary>
public partial class StateSelectionCanvasSplashTests : BaseTestClass
{
    private readonly Func<string, ISystemManagers, IRenderable?>? _previousRenderableFunc;

    public StateSelectionCanvasSplashTests()
    {
        GumExpressionService.Initialize();
        _previousRenderableFunc = ElementSaveExtensions.CustomCreateGraphicalComponentFunc;
        // Only Standard elements get a renderable, so a derived component keeps walking to its base.
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = (name, _) =>
            ObjectFinder.Self.GumProjectSave?.StandardElements.Any(standard => standard.Name == name) == true
                ? new RecordingRenderable()
                : null;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = _previousRenderableFunc;
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.VariableChangedThroughReference = null;
        base.Dispose();
    }

    #region Button states: every Forms state, with and without default reference, materialized or not, authored IsEnabled

    private static readonly Dictionary<string, (int Red, int Green, int Blue)> ButtonColors = new()
    {
        ["Enabled"] = (6, 159, 177),
        ["Highlighted"] = (10, 20, 30),
        ["Pushed"] = (40, 50, 60),
        ["Focused"] = (90, 100, 110),
        ["Disabled"] = (70, 70, 80),
    };

    public static IEnumerable<object[]> ButtonCases()
    {
        foreach (string state in ButtonColors.Keys)
        {
            foreach (bool defaultReference in new[] { true, false })
            {
                foreach (bool materialized in new[] { true, false })
                {
                    foreach (string authored in new[] { "unset", "true", "false" })
                    {
                        yield return new object[] { state, defaultReference, materialized, authored };
                    }
                }
            }
        }
    }

    /// <summary>
    /// The clicked state wins on the canvas, whatever the behavior-derived ButtonCategoryState says.
    /// </summary>
    [Theory]
    [MemberData(nameof(ButtonCases))]
    public void RefreshAll_ButtonStateSelected_ShowsThatStatesColor(string stateName, bool defaultReference, bool materialized, string authoredIsEnabled)
    {
        ComponentSave button = BuildFullButton(defaultReference, materialized);
        if (authoredIsEnabled != "unset")
        {
            button.DefaultState!.SetValue("IsEnabled", authoredIsEnabled == "true", "bool");
            BehaviorToolOnlyReferencesApplier.Apply(button, button.DefaultState);
        }
        StateSave clicked = button.Categories[0].States.Single(s => s.Name == stateName);

        (int, int, int) shown = ShowSelectedAndReadBackground(button, clicked);

        shown.ShouldBe(ButtonColors[stateName]);
    }

    /// <summary>With authored IsEnabled true or unset the default state shows Enabled.</summary>
    [Theory]
    [InlineData("true", "Enabled")]
    [InlineData("unset", "Enabled")]
    public void RefreshAll_DefaultSelectedOnButton_ShowsTheBehaviorDerivedState(string authoredIsEnabled, string expectedState)
    {
        ComponentSave button = BuildFullButton(defaultReference: true, materialized: true);
        if (authoredIsEnabled != "unset")
        {
            button.DefaultState!.SetValue("IsEnabled", authoredIsEnabled == "true", "bool");
            BehaviorToolOnlyReferencesApplier.Apply(button, button.DefaultState);
        }

        (int, int, int) shown = ShowSelectedAndReadBackground(button, button.DefaultState!);

        shown.ShouldBe(ButtonColors[expectedState]);
    }

    /// <summary>Clicking Disabled, then Default, then Pushed on one manager shows each state's own color.</summary>
    [Fact]
    public void Select_DisabledThenDefaultThenPushed_ShowsEachSelectedStatesColor()
    {
        ComponentSave button = BuildFullButton(defaultReference: true, materialized: true);
        StateSaveCategory category = button.Categories[0];
        CanvasSession session = new CanvasSession(button, button.DefaultState!);

        session.Select(category.States.Single(s => s.Name == "Disabled"));
        ColorOf(session.Root!).ShouldBe(ButtonColors["Disabled"]);

        session.Select(button.DefaultState!);
        ColorOf(session.Root!).ShouldBe(ButtonColors["Enabled"]);

        session.Select(category.States.Single(s => s.Name == "Pushed"));
        ColorOf(session.Root!).ShouldBe(ButtonColors["Pushed"]);
    }

    /// <summary>Editing the selected state's stored color and refreshing shows the new color.</summary>
    [Fact]
    public void Refresh_StoredColorOfSelectedStateEdited_ShowsEditedColor()
    {
        ComponentSave button = BuildFullButton(defaultReference: true, materialized: false);
        StateSave disabled = button.Categories[0].States.Single(s => s.Name == "Disabled");
        disabled.VariableLists.Clear();
        SetFill(disabled, "Background", ButtonColors["Disabled"], prefix: "");
        CanvasSession session = new CanvasSession(button, disabled);

        foreach (VariableSave variable in disabled.Variables.Where(v => v.Name == "Background.Red"))
        {
            variable.Value = 200;
        }
        session.Refresh();

        ColorOf(session.Root!).Item1.ShouldBe(200);
    }

    /// <summary>Re-pointing the selected state's own reference row and refreshing shows the new target.</summary>
    [Fact]
    public void Refresh_SelectedStatesReferenceRowRepointed_ShowsNewTarget()
    {
        ComponentSave button = BuildFullButton(defaultReference: true, materialized: false);
        StateSave disabled = button.Categories[0].States.Single(s => s.Name == "Disabled");
        CanvasSession session = new CanvasSession(button, disabled);
        ColorOf(session.Root!).ShouldBe(ButtonColors["Disabled"]);

        disabled.VariableLists.Single().ValueAsIList[0] = "Color = Components/Styles.Pushed.FillColor";
        session.Refresh();

        ColorOf(session.Root!).ShouldBe(ButtonColors["Pushed"]);
    }

    /// <summary>A default state color reference still colors states that do not set the color.</summary>
    [Fact]
    public void RefreshAll_StateThatSetsNoColorSelected_ShowsDefaultStatesReferencedColor()
    {
        ComponentSave button = BuildFullButton(defaultReference: true, materialized: false);
        StateSave quiet = new StateSave { Name = "Quiet", ParentContainer = button };
        button.Categories[0].States.Add(quiet);
        foreach (VariableSave variable in button.DefaultState!.Variables.Where(v => v.Name.StartsWith("Background.") && v.Name.EndsWith("ed")))
        {
            variable.Value = 1;
        }

        (int, int, int) shown = ShowSelectedAndReadBackground(button, quiet);

        shown.ShouldBe(ButtonColors["Enabled"]);
    }

    /// <summary>A state setting only one color channel keeps its channel and takes the other two from the default's reference.</summary>
    [Fact]
    public void RefreshAll_StateSetsOneChannelAndDefaultReferencesColor_KeepsStatesChannelAndReferencesTheRest()
    {
        ComponentSave button = BuildFullButton(defaultReference: true, materialized: false);
        StateSave redOnly = new StateSave { Name = "RedOnly", ParentContainer = button };
        redOnly.Variables.Add(new VariableSave { Name = "Background.Red", Value = 222, Type = "int", SetsValue = true });
        button.Categories[0].States.Add(redOnly);

        (int, int, int) shown = ShowSelectedAndReadBackground(button, redOnly);

        shown.ShouldBe((222, ButtonColors["Enabled"].Green, ButtonColors["Enabled"].Blue));
    }

    #endregion

    #region Panel width: state kinds and row shapes

    /// <summary>An uncategorized non-default state beats the default state's reference row.</summary>
    [Fact]
    public void RefreshAll_UncategorizedStateSetsWidthAndDefaultReferencesIt_ShowsStatesWidth()
    {
        ComponentSave panel = BuildPanel();
        StateSave alt = AddUncategorizedState(panel, "Alt", s => SetNumber(s, "Background.Width", 200f));

        ShowWidth(panel, alt).ShouldBe(200f);
    }

    /// <summary>An uncategorized state that sets nothing still follows the default's reference row.</summary>
    [Fact]
    public void RefreshAll_UncategorizedStateSetsNothing_FollowsDefaultReference()
    {
        ComponentSave panel = BuildPanel();
        StateSave alt = AddUncategorizedState(panel, "Alt", _ => { });

        ShowWidth(panel, alt).ShouldBe(50f);
    }

    /// <summary>A state in one category does not leak into a selection from another category.</summary>
    [Fact]
    public void RefreshAll_StateInOtherCategorySelected_DoesNotShowFirstCategorysValue()
    {
        ComponentSave panel = BuildPanel();
        AddCategoryState(panel, "SizeCategory", "Large", s => SetNumber(s, "Background.Width", 200f));
        StateSave tinted = AddCategoryState(panel, "TintCategory", "Tinted", s => SetNumber(s, "Reference.Height", 9f));

        ShowWidth(panel, tinted).ShouldBe(50f);
    }

    /// <summary>Two states in the same category: selecting each shows its own width.</summary>
    [Fact]
    public void Select_TwoStatesInTheSameCategory_ShowsEachOnesWidth()
    {
        ComponentSave panel = BuildPanel();
        StateSave large = AddCategoryState(panel, "SizeCategory", "Large", s => SetNumber(s, "Background.Width", 200f));
        StateSave small = AddCategoryState(panel, "SizeCategory", "Small", s => SetNumber(s, "Background.Width", 20f));
        CanvasSession session = new CanvasSession(panel, large);
        WidthOf(session).ShouldBe(200f);

        session.Select(small);

        WidthOf(session).ShouldBe(20f);
    }

    /// <summary>Selecting Default after a state restores the default state's referenced width.</summary>
    [Fact]
    public void Select_DefaultAfterState_ShowsDefaultsReferencedWidth()
    {
        ComponentSave panel = BuildPanel();
        StateSave large = AddCategoryState(panel, "SizeCategory", "Large", s => SetNumber(s, "Background.Width", 200f));
        CanvasSession session = new CanvasSession(panel, large);

        session.Select(panel.DefaultState!);
        WidthOf(session).ShouldBe(50f);

        session.Select(large);
        WidthOf(session).ShouldBe(200f);
    }

    /// <summary>Editing the selected state's width and refreshing shows the edit over the default's reference.</summary>
    [Fact]
    public void Refresh_SelectedStatesWidthEdited_ShowsEditedWidth()
    {
        ComponentSave panel = BuildPanel();
        StateSave large = AddCategoryState(panel, "SizeCategory", "Large", s => SetNumber(s, "Background.Width", 200f));
        CanvasSession session = new CanvasSession(panel, large);

        large.Variables.Single(v => v.Name == "Background.Width").Value = 250f;
        session.Refresh();

        WidthOf(session).ShouldBe(250f);
    }

    /// <summary>Editing the default row's target while a state that sets nothing is selected follows the new target.</summary>
    [Fact]
    public void Refresh_DefaultReferenceTargetEditedWhileQuietStateSelected_FollowsNewTarget()
    {
        ComponentSave panel = BuildPanel();
        StateSave alt = AddUncategorizedState(panel, "Alt", _ => { });
        CanvasSession session = new CanvasSession(panel, alt);

        panel.DefaultState!.VariableLists.Single().ValueAsIList[0] = "Width = Mid.Width";
        session.Refresh();

        WidthOf(session).ShouldBe(120f);
    }

    /// <summary>The selected state's own row beats the default state's row for the same variable.</summary>
    [Fact]
    public void RefreshAll_SelectedStateRowAndDefaultRowSetSameVariable_SelectedRowWins()
    {
        ComponentSave panel = BuildPanel();
        StateSave alt = AddUncategorizedState(panel, "Alt", s => AddReference(s, "Background", "Width = Mid.Width"));

        ShowWidth(panel, alt).ShouldBe(120f);
    }

    /// <summary>A selected state that holds only a same-component reference, with no row on the default state.</summary>
    [Fact]
    public void RefreshAll_OnlySelectedStateHasARow_AppliesIt()
    {
        ComponentSave panel = BuildPanel(defaultRow: false);
        StateSave alt = AddUncategorizedState(panel, "Alt", s => AddReference(s, "Background", "Width = Reference.Width"));

        ShowWidth(panel, alt).ShouldBe(50f);
    }

    /// <summary>Only the default state has a row and the selected state is not related to it.</summary>
    [Fact]
    public void RefreshAll_OnlyDefaultStateHasARow_RowStillAppliesWhenStateIsSelected()
    {
        ComponentSave panel = BuildPanel();
        StateSave alt = AddUncategorizedState(panel, "Alt", s => SetNumber(s, "Mid.Height", 5f));

        ShowWidth(panel, alt).ShouldBe(50f);
    }

    /// <summary>A selected-state row that reads a variable on another component.</summary>
    [Fact]
    public void RefreshAll_SelectedStateRowReadsAnotherElement_AppliesIt()
    {
        ComponentSave panel = BuildPanel(defaultRow: false, withOther: true);
        StateSave alt = AddUncategorizedState(panel, "Alt", s => AddReference(s, "Background", "Width = Components/Other.Width"));

        ShowWidth(panel, alt).ShouldBe(90f);
    }

    /// <summary>A default-state row that reads another component keeps applying when an unrelated state is selected.</summary>
    [Fact]
    public void RefreshAll_DefaultRowReadsAnotherElementAndUnrelatedStateSelected_RowStillApplies()
    {
        ComponentSave panel = BuildPanel(defaultRow: false, withOther: true);
        AddReference(panel.DefaultState!, "Background", "Width = Components/Other.Width");
        StateSave alt = AddUncategorizedState(panel, "Alt", s => SetNumber(s, "Mid.Height", 5f));

        ShowWidth(panel, alt).ShouldBe(90f);
    }

    /// <summary>A default row reading the element's own Width follows the Width a selected state sets.</summary>
    [Fact]
    public void RefreshAll_DefaultRowReadsElementWidthAndStateSetsIt_FollowsStatesWidth()
    {
        ComponentSave panel = BuildPanel(defaultRow: false);
        AddReference(panel.DefaultState!, "Background", "Width = Width");
        StateSave wide = AddUncategorizedState(panel, "Wide", s => SetNumber(s, "Width", 300f));

        ShowWidth(panel, wide).ShouldBe(300f);
    }

    /// <summary>An element-level default row loses to the selected state setting that same element variable.</summary>
    [Fact]
    public void RefreshAll_ElementLevelDefaultRowAndStateSetsThatVariable_StatesValueWins()
    {
        ComponentSave panel = BuildPanel(defaultRow: false);
        AddReference(panel.DefaultState!, null, "Width = Background.Width");
        StateSave wide = AddUncategorizedState(panel, "Wide", s => SetNumber(s, "Width", 400f));

        GraphicalUiElement root = Show(panel, wide);

        root.Width.ShouldBe(400f);
    }

    /// <summary>A commented-out row in the selected state neither applies nor removes the default's row.</summary>
    [Fact]
    public void RefreshAll_SelectedStateHasCommentedOutRow_DefaultRowStillApplies()
    {
        ComponentSave panel = BuildPanel();
        StateSave alt = AddUncategorizedState(panel, "Alt", s => AddReference(s, "Background", "// Width = Mid.Width"));

        ShowWidth(panel, alt).ShouldBe(50f);
    }

    #endregion

    #region Selected state sets both ends of a default row, and chains

    /// <summary>A chain where the selected state sets the middle: the head follows it and the tail is untouched.</summary>
    [Fact]
    public void RefreshAll_SelectedStateSetsMiddleOfChain_HeadFollowsAndTailKeepsValue()
    {
        ComponentSave chain = BuildChain(rowA: true, rowB: true);
        StateSave state = AddCategoryState(chain, "Cat", "S", s => SetNumber(s, "B.Width", 77f));

        GraphicalUiElement root = Show(chain, state);

        (WidthOfChild(root, "A"), WidthOfChild(root, "B"), WidthOfChild(root, "C")).ShouldBe((77f, 77f, 10f));
    }

    /// <summary>
    /// Oracle over default rows (A = B, B = C) and which of A, B and C the selected state sets: a variable the
    /// state sets always keeps the state's value, and a variable with no row and no set keeps the default.
    /// A variable that only follows a row is not asserted: a chain of rows evaluates in written order (#6856).
    /// Cases where the state sets both ends of a row are skipped (both-ends branch).
    /// </summary>
    [Fact]
    public void RefreshAll_RandomDefaultRowsAndSelectedSets_MatchOracle()
    {
        Random random = new Random(6858);
        for (int i = 0; i < 250; i++)
        {
            bool rowA = random.Next(2) == 0;
            bool rowB = random.Next(2) == 0;
            bool setA = random.Next(2) == 0;
            bool setB = random.Next(2) == 0;
            bool setC = random.Next(2) == 0;
            if (SetsBothEndsOfARow(rowA, rowB, setA, setB, setC))
            {
                // The live reference graph re-fires the row (see the both-ends branch).
                continue;
            }

            // The default state stores what its rows evaluate to, as the tool materializes them.
            float defaultC = 30f;
            float defaultB = rowB ? defaultC : 20f;
            float defaultA = rowA ? defaultB : 10f;
            ComponentSave chain = BuildChain(rowA, rowB, defaultA, defaultB, defaultC);
            StateSave state = AddCategoryState(chain, "Cat", "S", s =>
            {
                if (setA) SetNumber(s, "A.Width", 101f);
                if (setB) SetNumber(s, "B.Width", 102f);
                if (setC) SetNumber(s, "C.Width", 103f);
            });

            float expectedC = setC ? 103f : defaultC;
            float expectedB = setB ? 102f : rowB ? expectedC : defaultB;
            float expectedA = setA ? 101f : rowA ? expectedB : defaultA;

            GraphicalUiElement root = Show(chain, state);

            string tag = $"case {i}: rowA={rowA} rowB={rowB} setA={setA} setB={setB} setC={setC}";
            if (setA || !rowA)
            {
                WidthOfChild(root, "A").ShouldBe(expectedA, tag);
            }
            if (setB || !rowB)
            {
                WidthOfChild(root, "B").ShouldBe(expectedB, tag);
            }
            WidthOfChild(root, "C").ShouldBe(expectedC, tag);
        }
    }

    #endregion

    #region Element kinds

    /// <summary>A screen's categorized state beats the screen default's reference row.</summary>
    [Fact]
    public void RefreshAll_ScreenStateSetsWidthAndDefaultReferencesIt_ShowsStatesWidth()
    {
        ScreenSave screen = BuildScreen();
        StateSave wide = AddCategoryState(screen, "SizeCategory", "Wide", s => SetNumber(s, "Background.Width", 250f));

        ShowWidth(screen, wide).ShouldBe(250f);
    }

    /// <summary>A screen's default state shows the referenced width.</summary>
    [Fact]
    public void RefreshAll_ScreenDefaultSelected_ShowsReferencedWidth()
    {
        ScreenSave screen = BuildScreen();

        ShowWidth(screen, screen.DefaultState!).ShouldBe(50f);
    }

    /// <summary>A screen state that sets an instance of a component, against a screen default row for that instance.</summary>
    [Fact]
    public void RefreshAll_ScreenStateSetsComponentInstanceWidthAndScreenDefaultReferencesIt_ShowsStatesWidth()
    {
        ScreenSave screen = BuildScreenWithPanelInstance();
        StateSave wide = AddCategoryState(screen, "SizeCategory", "Wide", s => SetNumber(s, "PanelInstance.Width", 333f));

        GraphicalUiElement root = Show(screen, wide);

        root.GetGraphicalUiElementByName("PanelInstance")!.Width.ShouldBe(333f);
    }

    /// <summary>The same screen's default state shows the width its row references.</summary>
    [Fact]
    public void RefreshAll_ScreenWithComponentInstanceDefaultSelected_ShowsReferencedWidth()
    {
        ScreenSave screen = BuildScreenWithPanelInstance();

        GraphicalUiElement root = Show(screen, screen.DefaultState!);

        root.GetGraphicalUiElementByName("PanelInstance")!.Width.ShouldBe(60f);
    }

    /// <summary>A screen state sets an instance of a Button to Disabled through the instance's category state variable.</summary>
    [Fact]
    public void RefreshAll_ScreenStateSetsButtonInstanceToDisabled_ShowsDisabledColor()
    {
        ComponentSave button = BuildFullButton(defaultReference: true, materialized: true);
        ScreenSave screen = new ScreenSave { Name = "Menu" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Play", BaseType = "Controls/ButtonStandard", ParentContainer = screen });
        ObjectFinder.Self.GumProjectSave!.Screens.Add(screen);
        StateSave off = AddCategoryState(screen, "ModeCategory", "Off",
            s => s.Variables.Add(new VariableSave { Name = "Play.ButtonCategoryState", Value = "Disabled", Type = "ButtonCategory", SetsValue = true }));

        GraphicalUiElement root = Show(screen, off);

        RecordingRenderable background = (RecordingRenderable)root.GetGraphicalUiElementByName("Play")!
            .GetGraphicalUiElementByName("Background")!.RenderableComponent!;
        (background.Red, background.Green, background.Blue).ShouldBe(ButtonColors["Disabled"]);
    }

    /// <summary>A Standard element's own categorized state shows its width.</summary>
    [Fact]
    public void RefreshAll_StandardElementStateSelected_ShowsStatesWidth()
    {
        BuildProject();
        StandardElementSave rectangle = ObjectFinder.Self.GumProjectSave!.StandardElements.Single(s => s.Name == "Rectangle");
        StateSave wide = AddCategoryState(rectangle, "SizeCategory", "Wide", s => SetNumber(s, "Width", 88f));

        Show(rectangle, wide).Width.ShouldBe(88f);
    }

    #endregion

    #region Exposed variables

    /// <summary>
    /// A state that sets the exposed name (BackgroundWidth) must beat a default row that writes the
    /// exposed inner variable (Background.Width).
    /// </summary>
    [Fact]
    public void RefreshAll_StateSetsExposedNameAndDefaultReferencesInnerVariable_ShowsStatesWidth()
    {
        ComponentSave panel = BuildPanel();
        panel.DefaultState!.Variables.Single(v => v.Name == "Background.Width").ExposedAsName = "BackgroundWidth";
        StateSave wide = AddUncategorizedState(panel, "Wide", s => SetNumber(s, "BackgroundWidth", 222f));

        ShowWidth(panel, wide).ShouldBe(222f);
    }

    /// <summary>Control for the exposed case: with no default row the state's exposed value shows.</summary>
    [Fact]
    public void RefreshAll_StateSetsExposedNameAndDefaultHasNoRow_ShowsStatesWidth()
    {
        ComponentSave panel = BuildPanel(defaultRow: false);
        panel.DefaultState!.Variables.Single(v => v.Name == "Background.Width").ExposedAsName = "BackgroundWidth";
        StateSave wide = AddUncategorizedState(panel, "Wide", s => SetNumber(s, "BackgroundWidth", 222f));

        ShowWidth(panel, wide).ShouldBe(222f);
    }

    #endregion

    #region Setup

    private static (int, int, int) ColorOf(GraphicalUiElement root)
    {
        RecordingRenderable background = BackgroundOf(root);
        return (background.Red, background.Green, background.Blue);
    }

    private static float WidthOf(CanvasSession session) =>
        session.Root!.GetGraphicalUiElementByName("Background")!.Width;

    private static float ShowWidth(ElementSave element, StateSave selected) =>
        FindByName(Show(element, selected), "Background").Width;

    private static GraphicalUiElement FindByName(GraphicalUiElement root, string name)
    {
        Queue<GraphicalUiElement> queue = new Queue<GraphicalUiElement>();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            GraphicalUiElement current = queue.Dequeue();
            if (current.Name == name)
            {
                return current;
            }
            foreach (GraphicalUiElement child in (current.Children ?? Enumerable.Empty<IRenderableIpso>()).OfType<GraphicalUiElement>().Concat(current.ContainedElements))
            {
                queue.Enqueue(child);
            }
        }
        throw new InvalidOperationException("No element named " + name + " under " + root.Name);
    }

    /// <summary>True when the state sets a variable that has a row and also sets something that row reads, directly or through the chain.</summary>
    private static bool SetsBothEndsOfARow(bool rowA, bool rowB, bool setA, bool setB, bool setC) =>
        (setA && rowA && (setB || (rowB && setC))) || (setB && rowB && setC);

    private static float WidthOfChild(GraphicalUiElement root, string name) => FindByName(root, name).Width;

    /// <summary>A, B and C rectangles; rows A = B and B = C are optional. Stored widths are the given defaults.</summary>
    private static ComponentSave BuildChain(bool rowA, bool rowB, float a = 10f, float b = 10f, float c = 10f)
    {
        ComponentSave chain = new ComponentSave { Name = "Chain", BaseType = "Container" };
        foreach (string name in new[] { "A", "B", "C" })
        {
            chain.Instances.Add(new InstanceSave { Name = name, BaseType = "Rectangle", ParentContainer = chain });
        }
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = chain };
        chain.States.Add(defaultState);
        SetNumber(defaultState, "A.Width", a);
        SetNumber(defaultState, "B.Width", b);
        SetNumber(defaultState, "C.Width", c);
        if (rowA)
        {
            AddReference(defaultState, "A", "Width = B.Width");
        }
        if (rowB)
        {
            AddReference(defaultState, "B", "Width = C.Width");
        }
        BuildProject(chain);
        return chain;
    }

    private static void SetNumber(StateSave state, string name, float value) =>
        state.Variables.Add(new VariableSave { Name = name, Value = value, Type = "float", SetsValue = true });

    private static StateSave AddUncategorizedState(ElementSave element, string name, Action<StateSave> configure)
    {
        StateSave state = new StateSave { Name = name, ParentContainer = element };
        configure(state);
        element.States.Add(state);
        return state;
    }

    private static StateSave AddCategoryState(ElementSave element, string categoryName, string name, Action<StateSave> configure)
    {
        StateSaveCategory category = element.Categories.FirstOrDefault(c => c.Name == categoryName)
            ?? new StateSaveCategory { Name = categoryName };
        if (!element.Categories.Contains(category))
        {
            element.Categories.Add(category);
        }
        StateSave state = new StateSave { Name = name, ParentContainer = element };
        configure(state);
        category.States.Add(state);
        return state;
    }

    /// <summary>Overload that adds a row on the element itself (owner null) or on an instance.</summary>
    private static void AddReference(StateSave state, string? owner, string line)
    {
        string name = owner == null ? "VariableReferences" : owner + ".VariableReferences";
        VariableListSave<string>? list = state.VariableLists.OfType<VariableListSave<string>>().FirstOrDefault(l => l.Name == name);
        if (list == null)
        {
            list = new VariableListSave<string> { Type = "string", Name = name };
            state.VariableLists.Add(list);
        }
        list.Value.Add(line);
    }

    /// <summary>
    /// Panel: Background, Reference (width 50) and Mid (width 120) rectangles. The default state's
    /// Background width is a stored 10 and, when asked, follows Reference.Width.
    /// </summary>
    private static ComponentSave BuildPanel(bool defaultRow = true, bool withOther = false)
    {
        ComponentSave panel = new ComponentSave { Name = "Panel", BaseType = "Container" };
        foreach (string name in new[] { "Background", "Reference", "Mid" })
        {
            panel.Instances.Add(new InstanceSave { Name = name, BaseType = "Rectangle", ParentContainer = panel });
        }

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = panel };
        panel.States.Add(defaultState);
        SetNumber(defaultState, "Reference.Width", 50f);
        SetNumber(defaultState, "Mid.Width", 120f);
        SetNumber(defaultState, "Background.Width", 10f);
        if (defaultRow)
        {
            AddReference(defaultState, "Background", "Width = Reference.Width");
        }

        List<ComponentSave> components = new List<ComponentSave> { panel };
        if (withOther)
        {
            ComponentSave other = new ComponentSave { Name = "Other", BaseType = "Container" };
            StateSave otherDefault = new StateSave { Name = "Default", ParentContainer = other };
            other.States.Add(otherDefault);
            SetNumber(otherDefault, "Width", 90f);
            components.Add(other);
        }

        BuildProject(components.ToArray());
        return panel;
    }

    private static ScreenSave BuildScreen()
    {
        BuildProject();
        ScreenSave screen = new ScreenSave { Name = "Screen" };
        foreach (string name in new[] { "Background", "Reference" })
        {
            screen.Instances.Add(new InstanceSave { Name = name, BaseType = "Rectangle", ParentContainer = screen });
        }
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        SetNumber(defaultState, "Reference.Width", 50f);
        SetNumber(defaultState, "Background.Width", 10f);
        AddReference(defaultState, "Background", "Width = Reference.Width");
        ObjectFinder.Self.GumProjectSave!.Screens.Add(screen);
        return screen;
    }

    private static ScreenSave BuildScreenWithPanelInstance()
    {
        ComponentSave panel = BuildPanel(defaultRow: false);
        ScreenSave screen = new ScreenSave { Name = "Screen" };
        screen.Instances.Add(new InstanceSave { Name = "PanelInstance", BaseType = "Panel", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Reference", BaseType = "Rectangle", ParentContainer = screen });
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        SetNumber(defaultState, "Reference.Width", 60f);
        AddReference(defaultState, "PanelInstance", "Width = Reference.Width");
        ObjectFinder.Self.GumProjectSave!.Screens.Add(screen);
        return screen;
    }

    /// <summary>
    /// A Button with the five Forms states in ButtonCategory. Each state's color comes from a style
    /// instance in Styles; the default state stores Enabled's color and, when asked, references it.
    /// <paramref name="materialized"/> false leaves each state with only its reference row.
    /// </summary>
    private static ComponentSave BuildFullButton(bool defaultReference, bool materialized)
    {
        Dictionary<string, string> styleNames = new()
        {
            ["Enabled"] = "Primary",
            ["Highlighted"] = "Highlighted",
            ["Pushed"] = "Pushed",
            ["Focused"] = "Focused",
            ["Disabled"] = "DarkGray",
        };

        ComponentSave styles = new ComponentSave { Name = "Styles", BaseType = "Container" };
        StateSave stylesDefault = new StateSave { Name = "Default", ParentContainer = styles };
        styles.States.Add(stylesDefault);
        foreach ((string state, string styleName) in styleNames)
        {
            styles.Instances.Add(new InstanceSave { Name = styleName, BaseType = "Rectangle", ParentContainer = styles });
            SetFill(stylesDefault, styleName, ButtonColors[state]);
        }

        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        behavior.ToolOnlyVariableReferences.Add("ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        ComponentSave button = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        button.Behaviors.Add(new Gum.DataTypes.Behaviors.ElementBehaviorReference { BehaviorName = "ButtonBehavior" });
        button.Instances.Add(new InstanceSave { Name = "Background", BaseType = "NineSlice", ParentContainer = button });

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = button };
        button.States.Add(defaultState);
        SetFill(defaultState, "Background", ButtonColors["Enabled"], prefix: "");
        defaultState.Variables.Add(new VariableSave { Name = "ButtonCategoryState", Type = "ButtonCategory", SetsValue = false });
        if (defaultReference)
        {
            AddReference(defaultState, "Background", "Color = Components/Styles.Primary.FillColor");
        }

        StateSaveCategory category = new StateSaveCategory { Name = "ButtonCategory" };
        foreach ((string state, string styleName) in styleNames)
        {
            StateSave stateSave = new StateSave { Name = state, ParentContainer = button };
            if (materialized)
            {
                SetFill(stateSave, "Background", ButtonColors[state], prefix: "");
            }
            AddReference(stateSave, "Background", $"Color = Components/Styles.{styleName}.FillColor");
            category.States.Add(stateSave);
        }
        button.Categories.Add(category);

        GumProjectSave project = BuildProject(styles, button);
        project.Behaviors.Add(behavior);
        return button;
    }

    #endregion
}
