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
/// A shallow layout (<c>UpdateLayout(true, 0)</c>) of a hidden element, such as a control's hidden FocusedIndicator,
/// must not move anything visible, and showing the element afterwards must give the tree it would have given
/// without the shallow layout (#6366). Only visible elements are compared: the shallow layout refreshes the stored
/// rectangles of hidden elements on purpose.
/// </summary>
public class HiddenShallowLayoutFormsTests : BaseTestClass
{
    private const int MaxTargetsPerControl = 10;

    public static IEnumerable<object[]> ControlNames()
    {
        foreach (string name in new[]
        {
            "ScrollViewer", "ScrollViewerSizedAndFocused", "ListBox", "ListBox30Focused", "ListBox40",
            "Button", "CheckBox", "ComboBox", "TextBox", "Slider", "RadioButton", "ToggleButton", "PasswordBox"
        })
        {
            yield return new object[] { name };
        }
    }

    private static (ContainerRuntime Container, FrameworkElement Control) Create(string name)
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.Absolute;
        container.HeightUnits = DimensionUnitType.Absolute;
        container.Width = 300;
        container.Height = 300;

        FrameworkElement control;
        switch (name)
        {
            case "ScrollViewer":
                control = new ScrollViewer();
                break;
            case "ScrollViewerSizedAndFocused":
            {
                ScrollViewer scrollViewer = new();
                scrollViewer.Visual.Width = 200;
                scrollViewer.Visual.Height = 150;
                for (int i = 0; i < 30; i++)
                {
                    Label label = new();
                    label.Text = "Row " + i;
                    scrollViewer.AddChild(label);
                }
                scrollViewer.IsFocused = true;
                control = scrollViewer;
                break;
            }
            case "ListBox":
                control = new ListBox();
                break;
            case "ListBox30Focused":
            case "ListBox40":
            {
                ListBox listBox = new();
                listBox.Visual.Width = 200;
                listBox.Visual.Height = 150;
                int count = name == "ListBox40" ? 40 : 30;
                for (int i = 0; i < count; i++)
                {
                    listBox.Items!.Add("Item " + i);
                }
                listBox.IsFocused = name == "ListBox30Focused";
                control = listBox;
                break;
            }
            case "Button": control = new Button(); break;
            case "CheckBox": control = new CheckBox(); break;
            case "ComboBox": control = new ComboBox(); break;
            case "TextBox": control = new TextBox(); break;
            case "Slider": control = new Slider(); break;
            case "RadioButton": control = new RadioButton(); break;
            case "ToggleButton": control = new ToggleButton(); break;
            case "PasswordBox": control = new PasswordBox(); break;
            default: throw new ArgumentException(name);
        }

        container.AddChild(control.Visual);
        container.UpdateLayout();
        return (container, control);
    }

    /// <summary>The element and every descendant, depth first.</summary>
    private static List<GraphicalUiElement> Walk(GraphicalUiElement root)
    {
        List<GraphicalUiElement> all = new();
        void Add(GraphicalUiElement element)
        {
            all.Add(element);
            foreach (GraphicalUiElement child in element.Children)
            {
                Add(child);
            }
        }
        Add(root);
        return all;
    }

    /// <summary>The absolute rectangle of every element that is visible itself and under visible ancestors only.</summary>
    private static string SnapshotVisible(GraphicalUiElement root)
    {
        StringBuilder builder = new();
        void Add(GraphicalUiElement element, string path)
        {
            builder.Append(path).Append(' ')
                .Append(element.AbsoluteLeft).Append(',').Append(element.AbsoluteTop).Append(' ')
                .Append(element.AbsoluteWidth).Append('x').Append(element.AbsoluteHeight).AppendLine();
            int index = 0;
            foreach (GraphicalUiElement child in element.Children)
            {
                if (child.Visible)
                {
                    Add(child, path + "." + index);
                }
                index++;
            }
        }
        Add(root, "r");
        return builder.ToString();
    }

    /// <summary>The hidden elements; <paramref name="skipScrollBars"/> leaves out the ones whose visibility the owning control manages.</summary>
    private static List<int> HiddenIndexes(GraphicalUiElement root, bool skipScrollBars = false)
    {
        List<GraphicalUiElement> all = Walk(root);
        List<int> hidden = new();
        for (int i = 1; i < all.Count; i++)
        {
            if (!all[i].Visible && !(skipScrollBars && all[i].GetType().Name == "ScrollBarVisual"))
            {
                hidden.Add(i);
            }
        }
        return hidden;
    }

    [Theory]
    [MemberData(nameof(ControlNames))]
    public void ShallowLayoutOfAHiddenElement_ShouldLeaveEveryVisibleElementWhereItWas(string name)
    {
        (ContainerRuntime container, FrameworkElement control) = Create(name);
        List<GraphicalUiElement> all = Walk(container);
        List<int> hidden = HiddenIndexes(container);
        hidden.Count.ShouldBeGreaterThan(0, "the control should have hidden elements to lay out");

        string before = SnapshotVisible(container);
        foreach (int index in hidden)
        {
            all[index].UpdateLayout(true, 0);
            SnapshotVisible(container).ShouldBe(before, $"{name}: shallow layout of hidden element {index} ({all[index].Name})");
        }
    }

    /// <summary>
    /// Each side is described right after it is built and shown: creating a second focused control takes focus
    /// from the first and hides its FocusedIndicator again. A ScrollViewer decides its scroll bars' visibility
    /// itself, so forcing one visible is not a case of showing an element and is left out.
    /// </summary>
    [Theory]
    [MemberData(nameof(ControlNames))]
    public void ShowingAHiddenElementAfterAShallowLayout_ShouldGiveTheTreeShowingItWithoutOne(string name)
    {
        int hiddenCount = HiddenIndexes(Create(name).Container, skipScrollBars: true).Count;
        hiddenCount.ShouldBeGreaterThan(0, "the control should have hidden elements to show");

        // A list with dozens of items has hundreds of hidden elements; every few of them are enough.
        int stride = Math.Max(1, hiddenCount / MaxTargetsPerControl);
        List<string> failures = new();
        for (int hiddenNumber = 0; hiddenNumber < hiddenCount; hiddenNumber++)
        {
            if (hiddenNumber % stride != 0 || !SweepScale.Includes(hiddenNumber / stride))
            {
                continue;
            }

            (ContainerRuntime shallowContainer, _) = Create(name);
            GraphicalUiElement shallowTarget = Walk(shallowContainer)[HiddenIndexes(shallowContainer, skipScrollBars: true)[hiddenNumber]];
            shallowTarget.UpdateLayout(true, 0);
            shallowTarget.Visible = true;
            string shownAfterShallow = SnapshotVisible(shallowContainer);

            (ContainerRuntime plainContainer, _) = Create(name);
            Walk(plainContainer)[HiddenIndexes(plainContainer, skipScrollBars: true)[hiddenNumber]].Visible = true;
            string shownPlain = SnapshotVisible(plainContainer);

            if (shownAfterShallow != shownPlain)
            {
                failures.Add($"hidden element {hiddenNumber} ({shallowTarget.Name})");
            }
        }

        failures.Count.ShouldBe(0, $"{name}: {string.Join("; ", failures.Take(5))}");
    }
}
