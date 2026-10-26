using Gum.DataTypes;
using Gum.DataTypes.Variables;
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
/// The canvas counterparts of <c>StateVersusReferenceRowPrecedenceTests</c> (MonoGameGum.Tests), which
/// pin what a running game shows when the default state has a reference row (<c>X = Source.X</c>) and a
/// categorized state sets <c>X</c> to a plain value with no row of its own (#6851, PR #6858). Same data,
/// same expected value: where the game shows the state's value, so does the canvas.
/// </summary>
public class StateVersusReferenceRowCanvasTests : BaseTestClass
{
    private readonly Func<string, ISystemManagers, IRenderable?>? _previousRenderableFunc;

    public StateVersusReferenceRowCanvasTests()
    {
        GumExpressionService.Initialize();
        _previousRenderableFunc = ElementSaveExtensions.CustomCreateGraphicalComponentFunc;
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = (_, _) => new RecordingRenderable();
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = _previousRenderableFunc;
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.VariableChangedThroughReference = null;
        base.Dispose();
    }

    [Fact]
    public void RefreshAll_PlainChannelsOverDefaultColorReferenceRow_ShowsTheStatesColor()
    {
        ComponentSave swatch = BuildSwatch();

        (int, int, int) shown = ShowSelectedAndReadBackground(swatch, swatch.Categories[0].States.Single(s => s.Name == "Dark"));

        shown.ShouldBe(DarkGray);
    }

    [Fact]
    public void RefreshAll_DisabledWithPlainColorOverDefaultCrossElementColorRow_ShowsTheDisabledColor()
    {
        ComponentSave button = BuildButtonWithoutStateReferenceRows();

        (int, int, int) shown = ShowSelectedAndReadBackground(button, button.Categories[0].States.Single(s => s.Name == "Disabled"));

        shown.ShouldBe(DarkGray);
    }

    /// <summary>
    /// The game keeps the Disabled color when Styles changes (ApplyAllVariableReferences then RefreshStyles),
    /// because RefreshStyles applies the categorized state again after the default state's new value.
    /// </summary>
    [Fact]
    public void RefreshAll_StylesEditedWhileDisabledSelected_KeepsTheDisabledColor()
    {
        ComponentSave button = BuildButtonWithoutStateReferenceRows();
        StateSave disabled = button.Categories[0].States.Single(s => s.Name == "Disabled");
        CanvasSession session = new CanvasSession(button, disabled);
        StateSave stylesDefault = ObjectFinder.Self.GetComponent("Styles")!.DefaultState!;
        stylesDefault.GetVariableSave("Primary.FillRed")!.Value = 200;
        stylesDefault.GetVariableSave("Primary.FillGreen")!.Value = 100;
        stylesDefault.GetVariableSave("Primary.FillBlue")!.Value = 50;

        session.Refresh();

        RecordingRenderable background = BackgroundOf(session.Root!);
        (background.Red, background.Green, background.Blue).ShouldBe(DarkGray);
    }

    /// <summary>
    /// In the game, changing the referenced source after the state was applied makes the row re-drive
    /// the variable over the state's plain value (the width becomes the source's 80). The canvas drops the
    /// default row for a variable the selected state sets, so it keeps 200 and disagrees with the game.
    /// </summary>
    [Fact]
    public void RefreshAll_ReferencedWidthEditedWhileLargeSelected_MatchesTheGame()
    {
        ComponentSave panel = BuildPanelWithSizeCategory();
        StateSave large = panel.Categories[0].States.Single(s => s.Name == "Large");
        CanvasSession session = new CanvasSession(panel, large);
        panel.DefaultState!.GetVariableSave("Reference.Width")!.Value = 80f;

        session.Refresh();

        session.Root!.GetGraphicalUiElementByName("Background")!.Width.ShouldBe(80f);
    }

    /// <summary>The Color counterpart of the width case above: the game re-drives the color from the changed source.</summary>
    [Fact]
    public void RefreshAll_ReferencedColorEditedWhileDarkSelected_MatchesTheGame()
    {
        ComponentSave swatch = BuildSwatch();
        StateSave dark = swatch.Categories[0].States.Single(s => s.Name == "Dark");
        CanvasSession session = new CanvasSession(swatch, dark);
        StateSave defaultState = swatch.DefaultState!;
        defaultState.GetVariableSave("Source.Red")!.Value = 200;
        defaultState.GetVariableSave("Source.Green")!.Value = 100;
        defaultState.GetVariableSave("Source.Blue")!.Value = 50;

        session.Refresh();

        RecordingRenderable background = BackgroundOf(session.Root!);
        (background.Red, background.Green, background.Blue).ShouldBe((200, 100, 50));
    }

    // Default: Background (Container child, teal) follows Source's color through "Color = Source.Color";
    // Dark sets Background to dark gray by value and has no row.
    private static ComponentSave BuildSwatch()
    {
        ComponentSave swatch = new ComponentSave { Name = "Swatch", BaseType = "Container" };
        swatch.Instances.Add(new InstanceSave { Name = "Background", BaseType = "NineSlice", ParentContainer = swatch });
        swatch.Instances.Add(new InstanceSave { Name = "Source", BaseType = "NineSlice", ParentContainer = swatch });

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = swatch };
        swatch.States.Add(defaultState);
        SetFill(defaultState, "Source", Teal, prefix: "");
        SetFill(defaultState, "Background", Teal, prefix: "");
        AddReference(defaultState, "Background", "Color = Source.Color");

        StateSaveCategory category = new StateSaveCategory { Name = "ShadeCategory" };
        StateSave dark = new StateSave { Name = "Dark", ParentContainer = swatch };
        SetFill(dark, "Background", DarkGray, prefix: "");
        category.States.Add(dark);
        swatch.Categories.Add(category);

        BuildProject(swatch);
        return swatch;
    }

    // The Forms Button template with Styles.Primary teal; the default state's Background color follows
    // it through a cross-element row, and Enabled and Disabled set their colors by value with no row.
    private static ComponentSave BuildButtonWithoutStateReferenceRows()
    {
        ComponentSave button = BuildButton(defaultHasColorReference: true);
        foreach (StateSave state in button.Categories[0].States)
        {
            state.VariableLists.Clear();
        }
        return button;
    }
}
