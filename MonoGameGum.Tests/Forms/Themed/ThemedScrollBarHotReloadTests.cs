using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms.Themed;

/// <summary>
/// A tool-authored scrolling control in every tool theme that the Preview's hot reload updates while it runs
/// (rows pasted into the editor): the vertical scroll bar still spans the control and its thumb travels from
/// the control's top to its bottom once the bar appears. The reload used to re-apply the ScrollBar's
/// Vertical orientation state (128 px Absolute) over the container's own bar Height override. #8108
/// </summary>
public class ThemedScrollBarHotReloadTests : FileThemeInputTestBase
{
    private const float ControlHeight = 150;
    private const float MaxEdgeGap = 8;

    // Initialize() sorts the screen list, so the base class's "drop the last screens" trim would remove a theme's own screen.
    private (GumProjectSave Project, ScreenSave Screen)? added;

    public override void Dispose()
    {
        added?.Project.Screens.Remove(added.Value.Screen);
        base.Dispose();
    }

    public static IEnumerable<object[]> ThemeAndControl() =>
        AllThemes().Select(row => (string)row[0]).SelectMany(theme =>
            new[] { "ListBox", "ItemsControl", "ScrollViewer" }.Select(control => new object[] { theme, control }));

    private static void AddRow(ScreenSave screen, string rowType, int index)
    {
        screen.Instances.Add(new InstanceSave { Name = "Row" + index, BaseType = rowType, ParentContainer = screen });
        screen.DefaultState!.Variables.Add(new VariableSave { Name = "Row" + index + ".Parent", Type = "string", Value = "Box.InnerPanelInstance", SetsValue = true });
        screen.DefaultState.Variables.Add(new VariableSave { Name = "Row" + index + ".Text", Type = "string", Value = "Row " + index, SetsValue = true });
    }

    private static void SetBox(ScreenSave screen, string name, string type, object value) =>
        screen.DefaultState!.Variables.Add(new VariableSave { Name = "Box." + name, Type = type, Value = value, SetsValue = true });

    private static float ButtonSize(ScrollViewer viewer, string name)
    {
        GraphicalUiElement? button = viewer.VerticalScrollBar!.Visual.GetGraphicalUiElementByName(name);
        return button != null && button.AbsoluteVisible ? button.AbsoluteHeight : 0;
    }

    private void AssertBarSpansControl(ScrollViewer viewer, string label)
    {
        GraphicalUiElement bar = viewer.VerticalScrollBar!.Visual;
        GraphicalUiElement thumb = bar.GetGraphicalUiElementByName("ThumbInstance")!;
        float top = viewer.Visual.AbsoluteTop;
        float bottom = top + viewer.Visual.AbsoluteHeight;

        bar.AbsoluteHeight.ShouldBeGreaterThanOrEqualTo(viewer.Visual.AbsoluteHeight - 2 * MaxEdgeGap, $"{label}: bar {bar.AbsoluteHeight} in a {viewer.Visual.AbsoluteHeight} control");

        viewer.VerticalScrollBarValue = 0;
        Driver.Step(2);
        (thumb.AbsoluteTop - top).ShouldBeLessThanOrEqualTo(MaxEdgeGap + ButtonSize(viewer, "UpButtonInstance"), $"{label}: thumb at the top of the list");

        viewer.VerticalScrollBarValue = viewer.VerticalScrollBarMaximum;
        Driver.Step(2);
        (bottom - (thumb.AbsoluteTop + thumb.AbsoluteHeight)).ShouldBeLessThanOrEqualTo(MaxEdgeGap + ButtonSize(viewer, "DownButtonInstance"), $"{label}: thumb at the bottom of the list");
        viewer.VerticalScrollBarValue = 0;
        Driver.Step(2);
    }

    [Theory]
    [MemberData(nameof(ThemeAndControl))]
    public void RowsAddedByHotReload_ShouldKeepTheScrollBarSpanningTheControl(string theme, string control)
    {
        GumProjectSave project = LoadTheme(theme);
        ScreenSave screen = new ScreenSave { Name = "LiveScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = ComponentName(project, control), ParentContainer = screen });
        SetBox(screen, "Width", "float", 200f);
        SetBox(screen, "Height", "float", ControlHeight);
        SetBox(screen, "WidthUnits", "DimensionUnitType", (int)DimensionUnitType.Absolute);
        SetBox(screen, "HeightUnits", "DimensionUnitType", (int)DimensionUnitType.Absolute);
        string rowType = ComponentName(project, "ListBoxItem");
        int rows = 0;
        for (; rows < 3; rows++) { AddRow(screen, rowType, rows); }
        project.Screens.Add(screen);
        added = (project, screen);
        project.Initialize();
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        ScrollViewer viewer = (ScrollViewer)((InteractiveGue)screenVisual.GetGraphicalUiElementByName("Box")!).FormsControlAsObject!;

        // The editor adds one row at a time; the bar appears once they overflow, then keeps growing the list.
        while (rows < 30)
        {
            AddRow(screen, rowType, rows++);
            project.Initialize();
            Gum.GumHotReloadManager.ApplyDiff(new[] { screenVisual }, project, RenderingLibrary.SystemManagers.Default);
            Driver.Step(2);
            if (viewer.VerticalScrollBar!.Visual.AbsoluteVisible && (rows == 8 || rows == 29))
            {
                AssertBarSpansControl(viewer, $"{theme} {control} with {rows} rows after hot reload");
            }
        }
        viewer.VerticalScrollBar!.Visual.AbsoluteVisible.ShouldBeTrue($"{theme} {control}: the bar appeared");
    }

    [Fact]
    public void ScreenOverrideOfTheBarHeight_ShouldSurviveAHotReload()
    {
        // The screen's value for Box.VerticalScrollBarInstance.Height beats the ListBox's own, as it does on a restart.
        GumProjectSave project = LoadTheme("DarkPro");
        ScreenSave screen = new ScreenSave { Name = "LiveScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = ComponentName(project, "ListBox"), ParentContainer = screen });
        SetBox(screen, "Width", "float", 200f);
        SetBox(screen, "Height", "float", ControlHeight);
        SetBox(screen, "WidthUnits", "DimensionUnitType", (int)DimensionUnitType.Absolute);
        SetBox(screen, "HeightUnits", "DimensionUnitType", (int)DimensionUnitType.Absolute);
        SetBox(screen, "VerticalScrollBarInstance.Height", "float", -10f);
        string rowType = ComponentName(project, "ListBoxItem");
        for (int i = 0; i < 30; i++) { AddRow(screen, rowType, i); }
        project.Screens.Add(screen);
        added = (project, screen);
        project.Initialize();
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        ListBox viewer = (ListBox)((InteractiveGue)screenVisual.GetGraphicalUiElementByName("Box")!).FormsControlAsObject!;
        float before = viewer.VerticalScrollBar!.Visual.AbsoluteHeight;

        Gum.GumHotReloadManager.ApplyDiff(new[] { screenVisual }, project, RenderingLibrary.SystemManagers.Default);
        Driver.Step(2);

        before.ShouldBe(viewer.Visual.AbsoluteHeight - 10);
        viewer.VerticalScrollBar.Visual.AbsoluteHeight.ShouldBe(before);
    }
}
