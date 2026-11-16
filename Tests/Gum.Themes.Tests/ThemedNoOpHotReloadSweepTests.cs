using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;

namespace Gum.Themes.Tests;

/// <summary>
/// Splash damage for #8130 and #8131: a no-op reload over a code-created Forms control changes no position, size,
/// unit, visibility or text, in every packaged (code-only) theme.
/// </summary>
public class ThemedNoOpHotReloadSweepTests
{
    public static IEnumerable<object[]> ThemeAndControl() =>
        MenuFocusTests.Themes().SelectMany(row => new[]
        {
            "Button", "CheckBox", "RadioButton", "ToggleButton", "Label", "TextBox", "TextBoxMultiLine", "PasswordBox",
            "ListBox", "ListBoxFewItems", "ComboBox", "Slider", "ScrollBarVertical", "ScrollBarHorizontal", "ScrollViewer",
            "ScrollViewerBoth", "ItemsControl", "TabControl", "Menu", "StackPanel", "Splitter", "Window",
            "ScrollViewerNested", "ListBoxInScrollViewer",
        }.Select(control => new object[] { row[0], row[1], row[2], control }));

    private static FrameworkElement Create(string control)
    {
        switch (control)
        {
            case "Button": return new Button { Text = "Hello" };
            case "CheckBox": return new CheckBox { Text = "Check" };
            case "RadioButton": return new RadioButton { Text = "Radio" };
            case "ToggleButton": return new ToggleButton { Text = "Toggle" };
            case "Label": return new Label { Text = "A label" };
            case "TextBox": return new TextBox { Text = "typed" };
            case "TextBoxMultiLine":
                return new TextBox { TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Text = string.Join("\n", Enumerable.Range(0, 30)) };
            case "PasswordBox": return new PasswordBox { Password = "secret" };
            case "ListBox": return FillList(new ListBox(), 40);
            case "ListBoxFewItems": return FillList(new ListBox(), 3);
            case "ComboBox":
            {
                ComboBox combo = new();
                for (int i = 0; i < 6; i++) { combo.Items!.Add("choice " + i); }
                combo.SelectedIndex = 1;
                return combo;
            }
            case "Slider": return new Slider { Minimum = 0, Maximum = 10, Value = 3 };
            case "ScrollBarVertical": return new ScrollBar { Minimum = 0, Maximum = 100, ViewportSize = 20 };
            case "ScrollBarHorizontal": return new ScrollBar { Orientation = Orientation.Horizontal, Minimum = 0, Maximum = 100, ViewportSize = 20 };
            case "ScrollViewer": return FillViewer(new ScrollViewer(), 30);
            case "ScrollViewerBoth":
            {
                ScrollViewer viewer = FillViewer(new ScrollViewer(), 30);
                viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                return viewer;
            }
            case "ItemsControl":
            {
                ItemsControl items = new();
                for (int i = 0; i < 30; i++) { items.Items!.Add("item " + i); }
                return items;
            }
            case "TabControl":
            {
                TabControl tabs = new();
                tabs.AddTab("One", new Label { Text = "first page" });
                tabs.AddTab("Two", new Label { Text = "second page" });
                return tabs;
            }
            case "Menu":
            {
                Menu menu = new();
                MenuItem file = new() { Header = "File" };
                file.Items!.Add(new MenuItem { Header = "Open" });
                menu.Items!.Add(file);
                menu.Items!.Add(new MenuItem { Header = "Edit" });
                return menu;
            }
            case "StackPanel":
            {
                StackPanel panel = new();
                panel.AddChild(new Button { Text = "a" });
                panel.AddChild(new Label { Text = "b" });
                return panel;
            }
            case "Splitter": return new Splitter();
            case "Window":
            {
                Window window = new() { Width = 200, Height = 150 };
                window.AddChild(new Label { Text = "inside" });
                return window;
            }
            case "ScrollViewerNested":
            {
                ScrollViewer outer = FillViewer(new ScrollViewer(), 3);
                outer.AddChild(FillViewer(new ScrollViewer { Width = 150, Height = 80 }, 20));
                return outer;
            }
            case "ListBoxInScrollViewer":
            {
                ScrollViewer outer = new();
                outer.AddChild(FillList(new ListBox { Width = 150, Height = 80 }, 20));
                return outer;
            }
            default: throw new ArgumentException(control);
        }
    }

    private static ListBox FillList(ListBox list, int count)
    {
        for (int i = 0; i < count; i++) { list.Items!.Add("item " + i); }
        return list;
    }

    private static ScrollViewer FillViewer(ScrollViewer viewer, int count)
    {
        for (int i = 0; i < count; i++) { viewer.AddChild(new Button { Width = 400, Text = "item " + i }); }
        return viewer;
    }

    private static void Snapshot(GraphicalUiElement element, string path, Dictionary<string, string> into)
    {
        if (!element.AbsoluteVisible) { return; } // a hidden element's layout is deferred until it shows
        string text = (element as TextRuntime)?.Text ?? "";
        into[path] = $"{element.X},{element.Y},{element.Width},{element.Height},{element.XUnits},{element.YUnits},{element.WidthUnits},{element.HeightUnits}"
            + $" abs {element.AbsoluteLeft:0.##},{element.AbsoluteTop:0.##},{element.AbsoluteWidth:0.##},{element.AbsoluteHeight:0.##} vis {element.Visible}/{element.AbsoluteVisible} [{text}]";
        for (int i = 0; i < element.Children.Count; i++)
        {
            GraphicalUiElement child = element.Children[i];
            Snapshot(child, path + "/" + (child.Name ?? "?") + i, into);
        }
    }

    [Theory]
    [MemberData(nameof(ThemeAndControl))]
    public void ApplyDiff_NoOp_ShouldChangeNothingOnACodeCreatedControl(string theme, Action configure, Action register, string control)
    {
        Dictionary<Type, VisualTemplate> previous = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configure();
            register();
            ContainerRuntime host = new() { Width = 400, Height = 400, WidthUnits = DimensionUnitType.Absolute, HeightUnits = DimensionUnitType.Absolute };
            FrameworkElement element = Create(control);
            host.AddChild(element.Visual);
            if (control is not ("Window" or "Menu" or "StackPanel" or "Splitter" or "TabControl" or "Label"))
            {
                element.Visual.Width = 200;
                element.Visual.Height = 120;
                element.Visual.WidthUnits = DimensionUnitType.Absolute;
                element.Visual.HeightUnits = DimensionUnitType.Absolute;
            }
            host.UpdateLayout();
            Dictionary<string, string> before = new();
            Snapshot(host, "", before);

            GumHotReloadManager.ApplyDiff(new[] { (GraphicalUiElement)host }, new GumProjectSave(), SystemManagers.Default);
            host.UpdateLayout();

            Dictionary<string, string> after = new();
            Snapshot(host, "", after);
            string[] changed = before.Where(pair => !after.TryGetValue(pair.Key, out string? value) || value != pair.Value)
                .Select(pair => $"{pair.Key}: {pair.Value} -> {(after.TryGetValue(pair.Key, out string? now) ? now : "gone")}").ToArray();
            changed.ShouldBeEmpty($"{theme} {control}");
            after.Count.ShouldBe(before.Count, $"{theme} {control}");
        }
        finally
        {
            FrameworkElement.DefaultFormsTemplates.Clear();
            foreach (KeyValuePair<Type, VisualTemplate> pair in previous) { FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value; }
        }
    }
}
