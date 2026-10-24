using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The submenu popup panel suspends layout around bulk row changes without breaking a caller's own
/// suspension, hidden ancestors or nested submenus (#6705).
/// </summary>
public class MenuItemPopupSuspendTests : BaseTestClass
{
    private static (ContainerRuntime container, MenuItem file) Create()
    {
        ContainerRuntime container = new();
        container.Width = 400;
        container.WidthUnits = DimensionUnitType.Absolute;
        container.Height = 40;
        container.HeightUnits = DimensionUnitType.Absolute;
        Gum.GumService.Default.Root.Children!.Add(container);
        Menu menu = new();
        container.Children!.Add(menu.Visual);
        MenuItem file = new() { Header = "File" };
        menu.Items!.Add(file);
        file.Items!.Add("First");
        container.UpdateLayout();
        return (container, file);
    }

    private static List<GraphicalUiElement> Rows(MenuItem owner)
    {
        List<GraphicalUiElement> rows = new();
        foreach (GraphicalUiElement popup in FrameworkElement.PopupRoot.Children!)
        {
            if (popup is InteractiveGue gue && gue.FormsControlAsObject is ScrollViewer sv)
            {
                foreach (GraphicalUiElement row in sv.InnerPanel.Children!)
                {
                    if ((row as InteractiveGue)?.FormsControlAsObject is MenuItem m && m.ParentMenuItem == owner)
                    {
                        rows.Add(row);
                    }
                }
            }
        }
        return rows;
    }

    private static void ShouldStack(List<GraphicalUiElement> rows, int count)
    {
        rows.Count.ShouldBe(count);
        rows[0].AbsoluteHeight.ShouldBeGreaterThan(0);
        for (int i = 1; i < rows.Count; i++)
        {
            rows[i].GetAbsoluteTop().ShouldBe(rows[i - 1].GetAbsoluteBottom(), 0.01f);
        }
        GraphicalUiElement panel = (GraphicalUiElement)rows[0].Parent!;
        panel.AbsoluteHeight.ShouldBe(rows[^1].GetAbsoluteBottom() - rows[0].GetAbsoluteTop(), 0.01f);
    }

    [Fact]
    public void CallerSuspendedPanel_StaysSuspendedAcrossRefresh_AndLaysOutOnCallerResume()
    {
        (_, MenuItem file) = Create();
        file.IsSelected = true;
        GraphicalUiElement panel = (GraphicalUiElement)Rows(file)[0].Parent!;
        panel.SuspendLayout();

        file.Items!.Add("Second");
        file.Items!.Add("Third");

        panel.IsLayoutSuspended.ShouldBeTrue();
        panel.ResumeLayout();
        ShouldStack(Rows(file), 3);
    }

    [Fact]
    public void OpenedWhileAncestorHidden_LaysOutWhenShown()
    {
        (ContainerRuntime container, MenuItem file) = Create();
        file.Items!.Add("Second");
        file.Items!.Add("Third");
        container.Visible = false;
        file.IsSelected = true;
        file.Items!.Add("Fourth");
        container.Visible = true;
        ShouldStack(Rows(file), 4);
    }

    [Fact]
    public void NestedSubmenu_RowsStack()
    {
        (_, MenuItem file) = Create();
        MenuItem sub = new() { Header = "Sub" };
        for (int i = 0; i < 5; i++)
        {
            sub.Items!.Add("S" + i);
        }
        file.Items!.Add(sub);
        file.IsSelected = true;
        sub.IsSelected = true;
        ShouldStack(Rows(file), 2);
        ShouldStack(Rows(sub), 5);
        sub.Items!.Add("S5");
        ShouldStack(Rows(sub), 6);
    }
}
