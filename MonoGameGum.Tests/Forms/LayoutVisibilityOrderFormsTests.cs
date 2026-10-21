using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Forms controls filled while they, or something above them, are hidden must end with the layout of the same
/// controls filled while visible (#6307).
/// </summary>
public class LayoutVisibilityOrderFormsTests : BaseTestClass
{
    public enum Hiding
    {
        /// <summary>Only the control is hidden.</summary>
        ControlOnly,
        /// <summary>The container above the control is hidden and the control is not.</summary>
        ContainerOnly,
        /// <summary>Both are hidden and the container is shown first.</summary>
        BothContainerShownFirst,
        /// <summary>Both are hidden and the control is shown first.</summary>
        BothControlShownFirst
    }

    private static string Describe(string name, GraphicalUiElement element)
    {
        return $"{name} {element.AbsoluteLeft},{element.AbsoluteTop} {element.AbsoluteWidth}x{element.AbsoluteHeight}{Environment.NewLine}";
    }

    /// <summary>Builds a control inside a container, fills it with the given hiding, and describes the result.</summary>
    private static string Run<T>(Hiding? hiding, Func<T> create, Action<T> fill, Func<T, IEnumerable<GraphicalUiElement>> parts)
        where T : FrameworkElement
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.Absolute;
        container.HeightUnits = DimensionUnitType.Absolute;
        container.Width = 300;
        container.Height = 300;

        T control = create();
        container.AddChild(control.Visual);

        switch (hiding)
        {
            case Hiding.ControlOnly:
                control.Visual.Visible = false;
                break;
            case Hiding.ContainerOnly:
                container.Visible = false;
                break;
            case Hiding.BothContainerShownFirst:
            case Hiding.BothControlShownFirst:
                container.Visible = false;
                control.Visual.Visible = false;
                break;
        }

        fill(control);

        switch (hiding)
        {
            case Hiding.ControlOnly:
                control.Visual.Visible = true;
                break;
            case Hiding.ContainerOnly:
                container.Visible = true;
                break;
            case Hiding.BothContainerShownFirst:
                container.Visible = true;
                control.Visual.Visible = true;
                break;
            case Hiding.BothControlShownFirst:
                control.Visual.Visible = true;
                container.Visible = true;
                break;
        }

        StringBuilder builder = new();
        builder.Append(Describe("container", container));
        builder.Append(Describe("control", control.Visual));
        int index = 0;
        foreach (GraphicalUiElement part in parts(control))
        {
            builder.Append(Describe("part" + index++, part));
        }
        return builder.ToString();
    }

    private static void AssertSameAsVisible<T>(Func<T> create, Action<T> fill, Func<T, IEnumerable<GraphicalUiElement>> parts)
        where T : FrameworkElement
    {
        string expected = Run(hiding: null, create, fill, parts);
        expected.ShouldContain("part2", customMessage: "the control should have produced parts to compare");
        List<string> failures = new();
        foreach (Hiding hiding in Enum.GetValues<Hiding>())
        {
            string actual = Run(hiding, create, fill, parts);
            if (actual != expected)
            {
                failures.Add($"{hiding}:{Environment.NewLine}{actual}expected:{Environment.NewLine}{expected}");
            }
        }

        failures.Count.ShouldBe(0, string.Join(Environment.NewLine, failures.Take(1)));
    }

    [Fact]
    public void StackPanelWithLabels_FilledWhileHidden_ShouldLayOutLikeOneFilledWhileVisible()
    {
        List<Label> labels = new();
        AssertSameAsVisible(
            () => new StackPanel(),
            panel =>
            {
                labels.Clear();
                for (int i = 0; i < 3; i++)
                {
                    Label label = new();
                    label.Text = "Row " + i;
                    panel.AddChild(label);
                    labels.Add(label);
                }
            },
            panel => labels.Select(label => (GraphicalUiElement)label.Visual));
    }

    [Fact]
    public void StackPanelWithLabels_FilledWhileTheStackPanelAndItsParentAreHidden_ShouldPutTheLabelsBelowEachOther()
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.Absolute;
        container.HeightUnits = DimensionUnitType.Absolute;
        container.Width = 300;
        container.Height = 300;
        StackPanel panel = new();
        container.AddChild(panel.Visual);
        container.Visible = false;
        panel.Visual.Visible = false;
        List<Label> labels = new();
        for (int i = 0; i < 3; i++)
        {
            Label label = new();
            label.Text = "Row " + i;
            panel.AddChild(label);
            labels.Add(label);
        }

        panel.Visual.Visible = true;
        container.Visible = true;

        labels[0].Visual.AbsoluteTop.ShouldBe(panel.Visual.AbsoluteTop);
        labels[1].Visual.AbsoluteTop.ShouldBe(labels[0].Visual.AbsoluteTop + labels[0].Visual.AbsoluteHeight);
        labels[2].Visual.AbsoluteTop.ShouldBe(labels[1].Visual.AbsoluteTop + labels[1].Visual.AbsoluteHeight);
    }

    [Fact]
    public void StackPanelWithWrappingLabels_TextChangedWhileHidden_ShouldLayOutLikeOneChangedWhileVisible()
    {
        List<Label> labels = new();
        AssertSameAsVisible(
            () => new StackPanel(),
            panel =>
            {
                labels.Clear();
                for (int i = 0; i < 3; i++)
                {
                    Label label = new();
                    label.Visual.WidthUnits = DimensionUnitType.Absolute;
                    label.Visual.Width = 60;
                    label.Text = "x";
                    panel.AddChild(label);
                    labels.Add(label);
                }
                labels[0].Text = "A longer line of text that has to wrap";
                labels[2].Text = "Another long line of text that wraps too";
            },
            panel => labels.Select(label => (GraphicalUiElement)label.Visual));
    }

    [Fact]
    public void ListBox_ItemsAddedWhileHidden_ShouldLayOutLikeItemsAddedWhileVisible()
    {
        AssertSameAsVisible(
            () => new ListBox(),
            listBox =>
            {
                for (int i = 0; i < 4; i++)
                {
                    listBox.Items.Add("Item " + i);
                }
            },
            listBox => listBox.ListBoxItems.Select(item => (GraphicalUiElement)item.Visual));
    }

    [Fact]
    public void ItemsControl_ItemsAddedWhileHidden_ShouldLayOutLikeItemsAddedWhileVisible()
    {
        AssertSameAsVisible(
            () => new ItemsControl(),
            itemsControl =>
            {
                for (int i = 0; i < 4; i++)
                {
                    itemsControl.Items.Add("Item " + i);
                }
            },
            itemsControl => itemsControl.InnerPanel.Children.Select(child => (GraphicalUiElement)child));
    }

    [Fact]
    public void StackPanel_NestedInAStackPanel_FilledWhileHidden_ShouldLayOutLikeOneFilledWhileVisible()
    {
        List<Label> labels = new();
        AssertSameAsVisible(
            () => new StackPanel(),
            outerPanel =>
            {
                labels.Clear();
                Label title = new();
                title.Text = "Title";
                outerPanel.AddChild(title);
                labels.Add(title);
                StackPanel inner = new();
                outerPanel.AddChild(inner);
                inner.Visual.Visible = false;
                for (int i = 0; i < 3; i++)
                {
                    Label label = new();
                    label.Text = "Row " + i;
                    inner.AddChild(label);
                    labels.Add(label);
                }
                inner.Visual.Visible = true;
                Label footer = new();
                footer.Text = "Footer";
                outerPanel.AddChild(footer);
                labels.Add(footer);
            },
            panel => labels.Select(label => (GraphicalUiElement)label.Visual));
    }
}
