using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Internal parts of composite controls (scroll bar buttons and thumbs, slider thumb, ...) are not
/// tab stops of their own (issue #7422): Tab visits the controls a user would expect.
/// </summary>
public class CompositeControlTabStopTests : BaseTestClass
{
    private static string Describe(FrameworkElement element) =>
        element.GetType().Name + ":" + element.Visual.Name;

    /// <summary>Tabs (looping) from <paramref name="start"/> and returns each focused control, ending when the walk returns to the start.</summary>
    private static List<string> TabSequence(FrameworkElement start, GraphicalUiElement root, int maxSteps = 60)
    {
        List<string> sequence = new() { Describe(start) };
        FrameworkElement current = start;
        for (int i = 0; i < maxSteps; i++)
        {
            current.HandleTab(TabDirection.Down, loop: true);
            FrameworkElement? next = FrameworkElementTreeExtensions
                .ProjectToFrameworkElements(root.Descendants().Prepend(root))
                .FirstOrDefault(f => f.IsFocused);
            if (next == null) { sequence.Add("<none>"); break; }
            if (next == start) { break; }
            sequence.Add(Describe(next));
            current = next;
        }
        return sequence;
    }

    private static (StackPanel root, Button first) NewScreen()
    {
        StackPanel root = new();
        root.AddToRoot();
        Button first = new();
        first.Visual.Name = "First";
        root.AddChild(first);
        return (root, first);
    }

    [Fact]
    public void HandleTab_ShouldVisitOnlyContent_WhenScrollViewerHasVerticalScrollBar()
    {
        (StackPanel root, Button first) = NewScreen();
        ScrollViewer viewer = new();
        viewer.GamepadTabbingFocusBehavior = TabbingFocusBehavior.SkipOnTab;
        viewer.Visual.Height = 110;
        root.AddChild(viewer);
        List<Button> content = new();
        for (int i = 0; i < 10; i++)
        {
            Button b = new();
            b.Visual.Name = "Content" + i;
            viewer.AddChild(b);
            content.Add(b);
        }
        root.Visual.UpdateLayout();
        viewer.VerticalScrollBarMaximum.ShouldBeGreaterThan(0);

        List<string> sequence = TabSequence(first, root.Visual);

        sequence.ShouldBe(new[] { "Button:First" }.Concat(content.Select(c => "Button:" + c.Visual.Name)).ToList());
    }

    [Fact]
    public void HandleTab_ShouldNotVisitInternalParts_WhenScreenHasOneOfEachCompositeControl()
    {
        (StackPanel root, Button first) = NewScreen();
        ScrollViewer viewer = new();
        viewer.Visual.Name = "Viewer";
        viewer.GamepadTabbingFocusBehavior = TabbingFocusBehavior.SkipOnTab;
        viewer.Visual.Height = 110;
        root.AddChild(viewer);
        for (int i = 0; i < 10; i++) { viewer.AddChild(new Button()); }
        ListBox listBox = new(); listBox.Visual.Name = "ListBox"; listBox.Visual.Height = 100; root.AddChild(listBox);
        for (int i = 0; i < 20; i++) { listBox.Items.Add("item" + i); }
        ComboBox combo = new(); combo.Visual.Name = "Combo"; root.AddChild(combo);
        combo.Items.Add("a");
        Slider slider = new(); slider.Visual.Name = "Slider"; root.AddChild(slider);
        Splitter splitter = new(); splitter.Visual.Name = "Splitter"; root.AddChild(splitter);
        TextBox textBox = new(); textBox.Visual.Name = "TextBox"; root.AddChild(textBox);
        PasswordBox password = new(); password.Visual.Name = "Password"; root.AddChild(password);
        CheckBox check = new(); check.Visual.Name = "Check"; root.AddChild(check);
        Menu menu = new(); menu.Visual.Name = "Menu"; root.AddChild(menu);
        MenuItem mi = new(); mi.Header = "File"; menu.Items.Add(mi);
        MenuItem mi2 = new(); mi2.Header = "Open"; mi.Items.Add(mi2);
        ColorPicker picker = new(); picker.Visual.Name = "Picker"; root.AddChild(picker);
        root.Visual.UpdateLayout();

        List<string> sequence = TabSequence(first, root.Visual);

        sequence.ShouldNotContain(s => s.StartsWith("ScrollBar") || s.StartsWith("Thumb"));
        sequence.ShouldContain("ListBox:ListBox");
        sequence.ShouldContain("Slider:Slider");
    }

    [Fact]
    public void HandleTab_ShouldSkipSliderThumb_WhenSliderIsSkippedOnTab()
    {
        (StackPanel root, Button first) = NewScreen();
        Slider slider = new();
        slider.GamepadTabbingFocusBehavior = TabbingFocusBehavior.SkipOnTab;
        root.AddChild(slider);
        Button last = new(); last.Visual.Name = "Last"; root.AddChild(last);
        root.Visual.UpdateLayout();

        TabSequence(first, root.Visual).ShouldBe(new List<string> { "Button:First", "Button:Last" });
    }
}
