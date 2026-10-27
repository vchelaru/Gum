using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #6878 sweep B, reference shapes after a reload: the control's next state change, a second reload,
/// a removed or retargeted target, a state that also sets the referenced variable, and nesting.
/// Builds on the chain builder of <see cref="HotReloadReferenceChainTests"/>.
/// </summary>
public class HotReloadSweepBReferenceShapesTests : BaseTestClass
{
    private const int Old = HotReloadReferenceChainTests.Old;
    private const int New = HotReloadReferenceChainTests.New;

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static ICursor Cursor => FrameworkElement.MainCursor;

    private static GraphicalUiElement LoadFirst(ChainCell cell) =>
        HotReloadFormsProjects.Load(HotReloadReferenceChainTests.Build(cell, second: false));

    private static Button ButtonIn(GraphicalUiElement screen) =>
        HotReloadFormsProjects.Control<Button>(HotReloadFormsProjects.Child(screen, "Button1"));

    private static void Hover(Button button, bool over)
    {
        Cursor.VisualOver = over ? (InteractiveGue)button.Visual : null;
        button.UpdateState();
    }

    public static IEnumerable<object[]> Placements() =>
        new[] { ChainPlacement.OneComponent, ChainPlacement.AcrossComponents }.Select(p => new object[] { p });

    // ---- Chain, then the next state change ----

    [Theory]
    [MemberData(nameof(Placements))]
    public void ApplyDiff_ChainEditedThenHoveredAndUnhovered_ReaderKeepsTheEndOfTheChain(ChainPlacement placement)
    {
        ChainCell cell = new ChainCell(3, placement, ChainEdit.TailOnly);
        GraphicalUiElement screen = LoadFirst(cell);
        Button button = ButtonIn(screen);
        GraphicalUiElement reader = HotReloadReferenceChainTests.ReaderVisual(screen, ChainReader.ButtonInScreen);

        HotReloadFormsProjects.Reload(screen, HotReloadReferenceChainTests.Build(cell, second: true));
        Hover(button, true);
        HotReloadFormsProjects.Red(reader).ShouldBe(New);
        Hover(button, false);

        HotReloadFormsProjects.Red(reader).ShouldBe(New);
    }

    [Theory]
    [MemberData(nameof(Placements))]
    public void ApplyDiff_ChainRetargetedThenHoveredAndUnhovered_ReaderKeepsTheNewTarget(ChainPlacement placement)
    {
        ChainCell cell = new ChainCell(3, placement, ChainEdit.MiddleRetargeted);
        GraphicalUiElement screen = LoadFirst(cell);
        Button button = ButtonIn(screen);
        GraphicalUiElement reader = HotReloadReferenceChainTests.ReaderVisual(screen, ChainReader.ButtonInScreen);

        HotReloadFormsProjects.Reload(screen, HotReloadReferenceChainTests.Build(cell, second: true));
        Hover(button, true);
        Hover(button, false);

        HotReloadFormsProjects.Red(reader).ShouldBe(HotReloadReferenceChainTests.Accent);
    }

    [Fact]
    public void ApplyDiff_ChainEditedThenDisabledAndReEnabled_ReaderKeepsTheEndOfTheChain()
    {
        ChainCell cell = new ChainCell(3, ChainPlacement.AcrossComponents, ChainEdit.TailOnly);
        GraphicalUiElement screen = LoadFirst(cell);
        Button button = ButtonIn(screen);
        GraphicalUiElement reader = HotReloadReferenceChainTests.ReaderVisual(screen, ChainReader.ButtonInScreen);

        HotReloadFormsProjects.Reload(screen, HotReloadReferenceChainTests.Build(cell, second: true));
        button.IsEnabled = false;
        HotReloadFormsProjects.Red(reader).ShouldBe(New);
        button.IsEnabled = true;

        HotReloadFormsProjects.Red(reader).ShouldBe(New);
    }

    [Theory]
    [InlineData(ChainReaderState.Highlighted)]
    [InlineData(ChainReaderState.Disabled)]
    public void ApplyDiff_ChainEditedBackToTheOldValueInAState_ReaderShowsTheOldValueAgain(ChainReaderState state)
    {
        ChainCell cell = new ChainCell(3, ChainPlacement.OneComponent, ChainEdit.TailOnly, State: state);
        GraphicalUiElement screen = LoadFirst(cell);
        HotReloadReferenceChainTests.EnterState(screen, cell);
        GraphicalUiElement reader = HotReloadReferenceChainTests.ReaderVisual(screen, ChainReader.ButtonInScreen);

        HotReloadFormsProjects.Reload(screen, HotReloadReferenceChainTests.Build(cell, second: true));
        HotReloadFormsProjects.Red(reader).ShouldBe(New, "sanity");
        HotReloadFormsProjects.Reload(screen, HotReloadReferenceChainTests.Build(cell with { Edit = ChainEdit.TailOnly }, second: false));

        HotReloadFormsProjects.Red(reader).ShouldBe(Old);
    }

    // ---- Target removed, then the next state change ----

    [Theory]
    [InlineData(ChainReaderState.Highlighted)]
    [InlineData(ChainReaderState.Disabled)]
    public void ApplyDiff_TailRemovedWhileInAState_FollowersKeepTheirLastValueThroughStateChanges(ChainReaderState state)
    {
        ChainCell cell = new ChainCell(3, ChainPlacement.OneComponent, ChainEdit.TailOnly, State: state);
        GraphicalUiElement screen = LoadFirst(cell);
        HotReloadReferenceChainTests.EnterState(screen, cell);
        Button button = ButtonIn(screen);
        GraphicalUiElement reader = HotReloadReferenceChainTests.ReaderVisual(screen, ChainReader.ButtonInScreen);
        GumProjectSave second = HotReloadReferenceChainTests.Build(cell, second: true);
        ComponentSave styles = second.Components.Single(c => c.Name == "Styles");
        styles.Instances.RemoveAll(i => i.Name == "Sw3");
        styles.DefaultState!.Variables.RemoveAll(v => v.Name.StartsWith("Sw3."));

        HotReloadFormsProjects.Reload(screen, second);
        HotReloadFormsProjects.Red(reader).ShouldBe(Old);
        if (state == ChainReaderState.Disabled)
        {
            button.IsEnabled = true;
        }
        else
        {
            Hover(button, false);
        }

        HotReloadFormsProjects.Red(reader).ShouldBe(Old);
    }

    [Fact]
    public void ApplyDiff_TargetStyleComponentRemovedFromTheProject_ReaderKeepsItsSavedValueWithoutThrowing()
    {
        ChainCell cell = new ChainCell(2, ChainPlacement.AcrossComponents, ChainEdit.TailOnly);
        GraphicalUiElement screen = LoadFirst(cell);
        GraphicalUiElement reader = HotReloadReferenceChainTests.ReaderVisual(screen, ChainReader.ButtonInScreen);
        GumProjectSave second = HotReloadReferenceChainTests.Build(cell, second: true);
        second.Components.RemoveAll(c => c.Name == "Chain2");

        Should.NotThrow(() => HotReloadFormsProjects.Reload(screen, second));

        HotReloadFormsProjects.Red(reader).ShouldBe(Old);
    }

    // ---- Cross element and same element inside the screen ----

    [Fact]
    public void ApplyDiff_ScreenInstanceReadsAnotherScreenInstanceThatReadsTheChain_ShowsTheEnd()
    {
        ChainCell cell = new ChainCell(3, ChainPlacement.OneComponent, ChainEdit.TailOnly, ChainReader.RectangleOnScreen);
        GumProjectSave Build(bool second)
        {
            GumProjectSave project = HotReloadReferenceChainTests.Build(cell, second);
            ScreenSave screenSave = project.Screens.Single();
            screenSave.Instances.Add(new InstanceSave { Name = "Follower", BaseType = "ColoredRectangle", ParentContainer = screenSave });
            StateSave state = screenSave.DefaultState!;
            foreach (string channel in new[] { "Red", "Green", "Blue" })
            {
                state.Variables.Add(new VariableSave
                {
                    Name = "Follower." + channel, Type = "int", Value = channel == "Red" ? Old : 0, SetsValue = true
                });
            }
            VariableListSave<string> rows = new VariableListSave<string> { Type = "string", Name = "Follower.VariableReferences" };
            rows.Value.Add("Color = Swatch.Color");
            state.VariableLists.Add(rows);
            return project;
        }
        GraphicalUiElement screen = HotReloadFormsProjects.Load(Build(second: false));
        GraphicalUiElement follower = HotReloadFormsProjects.Child(screen, "Follower");

        HotReloadFormsProjects.Reload(screen, Build(second: true));

        HotReloadFormsProjects.Red(follower).ShouldBe(New);
    }

    // ---- Nesting ----

    [Theory]
    [MemberData(nameof(Placements))]
    public void ApplyDiff_ComponentInsideComponentInsideScreen_InnermostButtonShowsTheEndOfTheChain(ChainPlacement placement)
    {
        ChainCell cell = new ChainCell(3, placement, ChainEdit.TailOnly);
        GumProjectSave Build(bool second)
        {
            GumProjectSave project = HotReloadReferenceChainTests.Build(cell, second);
            ComponentSave outer = new ComponentSave { Name = "Controls/Outer", BaseType = "Container" };
            outer.States.Add(new StateSave { Name = "Default", ParentContainer = outer });
            outer.Instances.Add(new InstanceSave { Name = "Panel1", BaseType = "Controls/Panel", ParentContainer = outer });
            project.Components.Add(outer);
            ScreenSave screenSave = project.Screens.Single();
            screenSave.Instances.Add(new InstanceSave { Name = "Outer1", BaseType = outer.Name, ParentContainer = screenSave });
            project.Initialize();
            return project;
        }
        GraphicalUiElement screen = HotReloadFormsProjects.Load(Build(second: false));
        GraphicalUiElement deepButton = HotReloadFormsProjects.Child(
            HotReloadFormsProjects.Child(HotReloadFormsProjects.Child(screen, "Outer1"), "Panel1"), "Button1");
        GraphicalUiElement background = HotReloadFormsProjects.Child(deepButton, "Background");
        HotReloadFormsProjects.Red(background).ShouldBe(Old, "sanity");
        Button button = HotReloadFormsProjects.Control<Button>(deepButton);

        HotReloadFormsProjects.Reload(screen, Build(second: true));
        Hover(button, true);
        Hover(button, false);

        HotReloadFormsProjects.Red(background).ShouldBe(New);
    }

    // ---- A state that also sets the referenced variable ----

    private const int HoverLiteralFirst = 31;
    private const int HoverLiteralSecond = 131;
    private const int PushedRowFirst = 41;
    private const int PushedRowSecond = 141;

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

    private static void SetRow(StateSave state, string row)
    {
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "Background.VariableReferences" };
        list.Value.Add(row);
        state.VariableLists.Add(list);
    }

    /// <summary>
    /// Styles.Sw1 feeds the default state's Background through a reference. Highlighted also sets
    /// Background.Red as a literal. Pushed sets it through its own reference to Styles.Sw2.
    /// </summary>
    private static GumProjectSave BuildStateAlsoSets(bool second)
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new GumProjectSave();
        foreach (string standard in new[] { "Container", "ColoredRectangle", "Text" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!.Clone());
            standardElement.DefaultState!.ParentContainer = standardElement;
            project.StandardElements.Add(standardElement);
        }
        BehaviorSave behavior = new BehaviorSave { Name = StandardFormsBehaviorNames.ButtonBehaviorName };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        project.Behaviors.Add(behavior);

        ComponentSave styles = new ComponentSave { Name = "Styles", BaseType = "Container" };
        styles.States.Add(new StateSave { Name = "Default", ParentContainer = styles });
        foreach ((string name, int red) in new[] { ("Sw1", second ? 110 : 10), ("Sw2", second ? PushedRowSecond : PushedRowFirst) })
        {
            styles.Instances.Add(new InstanceSave { Name = name, BaseType = "ColoredRectangle", ParentContainer = styles });
            SetVariable(styles.DefaultState!, name + ".Red", "int", red);
            SetVariable(styles.DefaultState!, name + ".Green", "int", 0);
            SetVariable(styles.DefaultState!, name + ".Blue", "int", 0);
        }
        project.Components.Add(styles);

        ComponentSave button = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        button.Instances.Add(new InstanceSave { Name = "Background", BaseType = "ColoredRectangle", ParentContainer = button });
        button.Instances.Add(new InstanceSave { Name = "TextInstance", BaseType = "Text", ParentContainer = button });
        button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = StandardFormsBehaviorNames.ButtonBehaviorName });
        SetRed(button.DefaultState!, 1);
        SetRow(button.DefaultState!, "Color = Components/Styles.Sw1.Color");
        StateSaveCategory category = new StateSaveCategory { Name = "ButtonCategory" };
        StateSave enabled = new StateSave { Name = "Enabled", ParentContainer = button };
        StateSave highlighted = new StateSave { Name = "Highlighted", ParentContainer = button };
        SetRed(highlighted, second ? HoverLiteralSecond : HoverLiteralFirst);
        StateSave pushed = new StateSave { Name = "Pushed", ParentContainer = button };
        SetRed(pushed, second ? PushedRowSecond : PushedRowFirst);
        SetRow(pushed, "Color = Components/Styles.Sw2.Color");
        category.States.Add(enabled);
        category.States.Add(highlighted);
        category.States.Add(pushed);
        button.Categories.Add(category);
        project.Components.Add(button);

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Button1", BaseType = button.Name, ParentContainer = screen });
        project.Screens.Add(screen);
        project.Initialize();
        return project;
    }

    [Fact]
    public void ApplyDiff_ReferenceFeedsTheDefaultStateAndNoStateIsActive_ShowsTheNewTargetValue()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(BuildStateAlsoSets(second: false));
        GraphicalUiElement background = HotReloadFormsProjects.Child(HotReloadFormsProjects.Child(screen, "Button1"), "Background");
        HotReloadFormsProjects.Red(background).ShouldBe(10, "sanity");

        HotReloadFormsProjects.Reload(screen, BuildStateAlsoSets(second: true));

        HotReloadFormsProjects.Red(background).ShouldBe(110);
    }

    [Fact]
    public void ApplyDiff_HighlightedStateAlsoSetsTheReferencedVariable_ActiveStateWinsWithItsNewLiteral()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(BuildStateAlsoSets(second: false));
        GraphicalUiElement visual = HotReloadFormsProjects.Child(screen, "Button1");
        GraphicalUiElement background = HotReloadFormsProjects.Child(visual, "Background");
        Hover(HotReloadFormsProjects.Control<Button>(visual), true);
        HotReloadFormsProjects.Red(background).ShouldBe(HoverLiteralFirst, "sanity");

        HotReloadFormsProjects.Reload(screen, BuildStateAlsoSets(second: true));

        HotReloadFormsProjects.Red(background).ShouldBe(HoverLiteralSecond);
    }

    [Fact]
    public void ApplyDiff_ReloadedThenHighlightedThenPushed_EachStateShowsItsOwnNewValue()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(BuildStateAlsoSets(second: false));
        GraphicalUiElement visual = HotReloadFormsProjects.Child(screen, "Button1");
        GraphicalUiElement background = HotReloadFormsProjects.Child(visual, "Background");
        Button button = HotReloadFormsProjects.Control<Button>(visual);

        HotReloadFormsProjects.Reload(screen, BuildStateAlsoSets(second: true));
        Hover(button, true);
        HotReloadFormsProjects.Red(background).ShouldBe(HoverLiteralSecond);
        visual.SetProperty("ButtonCategoryState", "Pushed");

        HotReloadFormsProjects.Red(background).ShouldBe(PushedRowSecond);
    }

    [Fact]
    public void ApplyDiff_PushedStateHasItsOwnReferenceAndIsActiveWhenReloaded_ShowsTheNewReferencedValue()
    {
        GraphicalUiElement screen = HotReloadFormsProjects.Load(BuildStateAlsoSets(second: false));
        GraphicalUiElement visual = HotReloadFormsProjects.Child(screen, "Button1");
        GraphicalUiElement background = HotReloadFormsProjects.Child(visual, "Background");
        visual.SetProperty("ButtonCategoryState", "Pushed");
        HotReloadFormsProjects.Red(background).ShouldBe(PushedRowFirst, "sanity");

        HotReloadFormsProjects.Reload(screen, BuildStateAlsoSets(second: true));

        HotReloadFormsProjects.Red(background).ShouldBe(PushedRowSecond);
    }
}
