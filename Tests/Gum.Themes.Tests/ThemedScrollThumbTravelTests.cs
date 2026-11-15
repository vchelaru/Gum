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
/// A long list in every packaged (code-only) theme, for every control that scrolls: the vertical scroll bar
/// spans the control and its thumb travels from the control top to its bottom as the list scrolls. #8106
/// </summary>
public class ThemedScrollThumbTravelTests
{
    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { "V3", (Action)(() => { }), (Action)(() => { }) };
        yield return new object[] { "Bubblegum", (Action)Bubblegum.BubblegumTheme.ConfigureStyling, (Action)Bubblegum.BubblegumTheme.RegisterVisuals };
        yield return new object[] { "DarkPro", (Action)DarkPro.DarkProTheme.ConfigureStyling, (Action)DarkPro.DarkProTheme.RegisterVisuals };
        yield return new object[] { "Editor", (Action)Editor.EditorTheme.ConfigureStyling, (Action)Editor.EditorTheme.RegisterVisuals };
        yield return new object[] { "ForestGlade", (Action)ForestGlade.ForestGladeTheme.ConfigureStyling, (Action)ForestGlade.ForestGladeTheme.RegisterVisuals };
        yield return new object[] { "Hazard", (Action)Hazard.HazardTheme.ConfigureStyling, (Action)Hazard.HazardTheme.RegisterVisuals };
        yield return new object[] { "Meadow", (Action)Meadow.MeadowTheme.ConfigureStyling, (Action)Meadow.MeadowTheme.RegisterVisuals };
        yield return new object[] { "Neon", (Action)Neon.NeonTheme.ConfigureStyling, (Action)Neon.NeonTheme.RegisterVisuals };
        yield return new object[] { "Retro95", (Action)Retro95.Retro95Theme.ConfigureStyling, (Action)Retro95.Retro95Theme.RegisterVisuals };
        yield return new object[] { "Template", (Action)Template.TemplateTheme.ConfigureStyling, (Action)Template.TemplateTheme.RegisterVisuals };
    }

    private const float MaxEdgeGap = 8;

    [Theory]
    [MemberData(nameof(Themes))]
    public void ScrollThumb_OfALongList_ShouldTravelTheWholeControl(string theme, Action configure, Action register)
    {
        Dictionary<Type, VisualTemplate> previous = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configure();
            register();
            foreach (string kind in new[] { "ListBox", "ScrollViewer", "ItemsControl" })
            {
                ScrollViewer viewer = kind == "ScrollViewer" ? new ScrollViewer() : kind == "ListBox" ? new ListBox() : new ItemsControl();
                for (int i = 0; i < 500; i++)
                {
                    if (viewer is ItemsControl items) { items.Items!.Add("Item " + i); }
                    else { viewer.AddChild(new Label { Text = "Item " + i }); }
                }
                viewer.Visual.Width = 200;
                viewer.Visual.Height = 150;
                viewer.Visual.WidthUnits = DimensionUnitType.Absolute;
                viewer.Visual.HeightUnits = DimensionUnitType.Absolute;
                viewer.Visual.UpdateLayout();

                GraphicalUiElement bar = viewer.VerticalScrollBar!.Visual;
                GraphicalUiElement thumb = bar.GetGraphicalUiElementByName("ThumbInstance")!;
                float top = viewer.Visual.AbsoluteTop;
                float bottom = top + viewer.Visual.AbsoluteHeight;
                string label = $"{theme} {kind}";

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
