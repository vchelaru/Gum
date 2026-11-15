using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using Bubblegum = Gum.Themes.Bubblegum;
using DarkPro = Gum.Themes.DarkPro;
using Editor = Gum.Themes.Editor;
using ForestGlade = Gum.Themes.ForestGlade;
using Hazard = Gum.Themes.Hazard;
using Meadow = Gum.Themes.Meadow;
using Neon = Gum.Themes.Neon;
using Retro95 = Gum.Themes.Retro95;
using Template = Gum.Themes.Template;

namespace Gum.Themes.Tests;

/// <summary>
/// Every packaged (code-only) theme, every scrolling control: items are added one at a time to a running control
/// until the vertical scroll bar appears, and from then on it spans the control and its thumb travels the control's
/// whole height. A bar that appears later is the case a bar present from the start does not cover. #8108
/// </summary>
public class ThemedScrollBarAppearsLaterTests
{
    public static IEnumerable<object[]> Themes() => ThemedScrollThumbTravelTests.Themes();

    private const float MaxEdgeGap = 8;

    [Theory]
    [MemberData(nameof(Themes))]
    public void ScrollBar_AppearingAfterItemsAreAdded_ShouldSpanTheControl(string theme, Action configure, Action register)
    {
        Dictionary<Type, VisualTemplate> previous = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configure();
            register();
            foreach (string kind in new[] { "ListBox", "ScrollViewer", "ItemsControl" })
            {
                ScrollViewer viewer = kind == "ScrollViewer" ? new ScrollViewer() : kind == "ListBox" ? new ListBox() : new ItemsControl();
                viewer.Visual.Width = 200;
                viewer.Visual.Height = 150;
                viewer.Visual.WidthUnits = DimensionUnitType.Absolute;
                viewer.Visual.HeightUnits = DimensionUnitType.Absolute;
                viewer.Visual.UpdateLayout();
                string label = $"{theme} {kind}";

                int count = 0;
                while (!viewer.VerticalScrollBar!.Visual.AbsoluteVisible && count < 100)
                {
                    count++;
                    if (viewer is ItemsControl items) { items.Items!.Add("Item " + count); }
                    else { viewer.AddChild(new Label { Text = "Item " + count }); }
                    viewer.Visual.UpdateLayout();
                }
                viewer.VerticalScrollBar!.Visual.AbsoluteVisible.ShouldBeTrue(label + ": the bar appeared");
                for (int i = 0; i < 20; i++)
                {
                    if (viewer is ItemsControl items) { items.Items!.Add("More " + i); }
                    else { viewer.AddChild(new Label { Text = "More " + i }); }
                }
                viewer.Visual.UpdateLayout();

                GraphicalUiElement bar = viewer.VerticalScrollBar!.Visual;
                GraphicalUiElement thumb = bar.GetGraphicalUiElementByName("ThumbInstance")!;
                float top = viewer.Visual.AbsoluteTop;
                float bottom = top + viewer.Visual.AbsoluteHeight;
                bar.AbsoluteHeight.ShouldBeGreaterThanOrEqualTo(viewer.Visual.AbsoluteHeight - 2 * MaxEdgeGap, label);
                (thumb.AbsoluteTop - top).ShouldBeLessThanOrEqualTo(MaxEdgeGap + ButtonSize(bar, "UpButtonInstance"), label + " at the top");
                viewer.VerticalScrollBarValue = viewer.VerticalScrollBarMaximum;
                viewer.Visual.UpdateLayout();
                (bottom - (thumb.AbsoluteTop + thumb.AbsoluteHeight)).ShouldBeLessThanOrEqualTo(MaxEdgeGap + ButtonSize(bar, "DownButtonInstance"), label + " at the bottom");
            }
        }
        finally
        {
            FrameworkElement.DefaultFormsTemplates.Clear();
            foreach (var pair in previous) { FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value; }
        }
    }

    private static float ButtonSize(GraphicalUiElement bar, string name)
    {
        GraphicalUiElement? button = bar.GetGraphicalUiElementByName(name);
        return button != null && button.AbsoluteVisible ? button.AbsoluteHeight : 0;
    }
}
