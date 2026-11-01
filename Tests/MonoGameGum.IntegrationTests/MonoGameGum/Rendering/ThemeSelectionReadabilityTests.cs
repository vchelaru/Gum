using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using Gum.DataTypes;
using Gum.Input;
using Moq;
using Shouldly;
using Xunit;
using V3Styling = Gum.Forms.DefaultVisuals.V3.Styling;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// The text of a selected, highlighted or focused item has to stay readable in the pixels that are drawn (#7366): in each
/// state the text is measured against the background actually behind it, for the V3 defaults, every code-only theme and
/// every tool template. The background is the most common color rendered inside the text's own rectangle and the text color is the one
/// the Text draws with. Set GUM_THEME_TABLE to a file path to append every measurement, and
/// GUM_SCREENSHOT_DIR to also write the scene PNGs.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ThemeSelectionReadabilityTests : BaseTestClass
{
    private const int Width = 640;
    private const int Height = 300;
    private const double MinimumTextContrast = 4.5;

    // Only the Standard ListBox rows (and the drop-down rows that share the ListBoxItem visual) are asserted. Every other
    // theme and surface is measured and recorded but left pending: the rest of #7366 was closed as not planned (#7408).
    private static bool IsAsserted(string themeName, string surface) =>
        themeName == "standard" && (surface.StartsWith("ListBoxItem") || surface.StartsWith("ComboBox drop-down item"));

    private sealed record Measurement(string Surface, double Contrast, XnaColor Background, XnaColor Text, double Minimum = MinimumTextContrast);

    [Theory]
    [MemberData(nameof(MenuFocusRenderTests.Pairs), MemberType = typeof(MenuFocusRenderTests))]
    public void CodeOnly_SelectionSurfaces_TextIsReadable(string themeName, Action? applyTheme, string templateFolder, string componentPrefix, XnaColor background)
    {
        _ = templateFolder;
        _ = componentPrefix;
        List<Measurement> measurements = new();
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        V3Styling previousStyle = V3Styling.ActiveStyle;
        var previousFontCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        using (MenuFocusRenderTests.MenuLease game = new(null))
        {
            try
            {
                applyTheme?.Invoke();
                measurements = RunScene(themeName + "-codeonly", themeName, componentPrefix: null, background);
            }
            finally
            {
                FrameworkElement.DefaultFormsTemplates.Clear();
                foreach (KeyValuePair<Type, VisualTemplate> pair in previousTemplates)
                {
                    FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value;
                }
                V3Styling.ActiveStyle = previousStyle;
                CustomSetPropertyOnRenderable.InMemoryFontCreator = previousFontCreator;
            }
        }
        AssertReadable(themeName, themeName + " code-only", measurements);
    }

    [Theory]
    [MemberData(nameof(MenuFocusRenderTests.Pairs), MemberType = typeof(MenuFocusRenderTests))]
    public void Tool_SelectionSurfaces_TextIsReadable(string themeName, Action? applyTheme, string templateFolder, string componentPrefix, XnaColor background)
    {
        _ = applyTheme;
        List<Measurement> measurements = new();
        MenuFocusRenderTests.WithToolProject(templateFolder, mutateProject: null, () =>
        {
            measurements = RunScene(themeName + "-tool", themeName, componentPrefix, background);
        });
        AssertReadable(themeName, themeName + " tool", measurements);
    }

    private static T FromComponent<T>(string componentName) where T : FrameworkElement
    {
        ComponentSave component = global::Gum.Managers.ObjectFinder.Self.GetComponent(componentName).ShouldNotBeNull(componentName);
        GraphicalUiElement visual = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        return (visual as InteractiveGue)?.FormsControlAsObject.ShouldBeOfType<T>(componentName)!;
    }

    // A mouse cursor parked far outside the capture, so no control reacts to it.
    private static void InstallOffscreenCursor()
    {
        Mock<ICursor> cursor = new();
        cursor.SetupProperty(c => c.VisualOver);
        cursor.SetupProperty(c => c.WindowPushed);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.X).Returns(-1000);
        cursor.Setup(c => c.Y).Returns(-1000);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(-1000);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(-1000);
        FormsUtilities.SetCursor(cursor.Object);
    }

    private static void AssertReadable(string themeName, string label, List<Measurement> measurements)
    {
        string? tablePath = Environment.GetEnvironmentVariable("GUM_THEME_TABLE");
        if (!string.IsNullOrEmpty(tablePath))
        {
            lock (typeof(ThemeSelectionReadabilityTests))
            {
                File.AppendAllLines(tablePath, measurements.Select(m =>
                    $"{label}\t{m.Surface}\t{m.Contrast:0.00}\tbg {m.Background.R},{m.Background.G},{m.Background.B}\ttext {m.Text.R},{m.Text.G},{m.Text.B}"));
            }
        }
        measurements.ShouldNotBeEmpty(label);
        List<string> failures = measurements
            .Where(m => m.Contrast < m.Minimum && IsAsserted(themeName, m.Surface))
            .Select(m => $"{m.Surface}: {m.Contrast:0.00}:1 (text {m.Text.R},{m.Text.G},{m.Text.B} on {m.Background.R},{m.Background.G},{m.Background.B})")
            .ToList();
        failures.ShouldBeEmpty($"{label}: below its minimum contrast");
    }

    private static List<Measurement> RunScene(string pngPrefix, string themeName, string? componentPrefix, XnaColor background)
    {
        InstallOffscreenCursor();
        GraphicsDevice gd = SystemManagers.Default.Renderer.GraphicsDevice;
        List<Measurement> results = new();

        if (componentPrefix != null)
        {
            // What generated code registers for a project: list items come from the project's ListBoxItem component.
            string itemComponent = componentPrefix + "ListBoxItem";
            FrameworkElement.DefaultFormsTemplates[typeof(ListBoxItem)] = new VisualTemplate((_, createForms) =>
            {
                global::Gum.GueDeriving.ContainerRuntime visual = new();
                global::Gum.Managers.ObjectFinder.Self.GetRequiredElementSave(itemComponent).SetGraphicalUiElement(visual, SystemManagers.Default);
                if (createForms)
                {
                    visual.FormsControlAsObject = new ListBoxItem(visual);
                }
                return visual;
            });
        }

        T Make<T>(string name, Func<T> codeOnly) where T : FrameworkElement =>
            componentPrefix == null ? codeOnly() : FromComponent<T>(componentPrefix + name);

        void Place(FrameworkElement element, float x, float y, float? width = null, float? height = null)
        {
            GraphicalUiElement visual = element.Visual;
            visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            element.X = x;
            element.Y = y;
            if (width != null)
            {
                element.Width = width.Value;
                visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            }
            if (height != null)
            {
                element.Height = height.Value;
                visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            }
        }

        // Scene 1: list item states, a closed combo box, a text box and a password box with a selection.
        ListBox list = Make("ListBox", () => new ListBox());
        Place(list, 10, 10, 200, 190);
        string[] states = { "Enabled", "Highlighted", "Selected", "SelectedHighlighted", "Focused" };
        for (int i = 0; i < states.Length; i++)
        {
            list.Items!.Add("Item " + (i + 1));
        }
        list.AddToRoot();
        list.Visual.UpdateLayout();
        List<(string State, ListBoxItem Item)> itemStates = new();
        for (int i = 0; i < states.Length; i++)
        {
            ListBoxItem item = list.ListBoxItems[i];
            if (item.Visual.Categories.TryGetValue("ListBoxItemCategory", out var category) && category.States.Any(s => s.Name == states[i]))
            {
                item.Visual.SetProperty("ListBoxItemCategoryState", states[i]);
                itemStates.Add((states[i], item));
            }
        }

        ComboBox combo = Make("ComboBox", () => new ComboBox());
        Place(combo, 230, 10, 180);
        combo.Items!.Add("Combo A");
        combo.Items!.Add("Combo B");
        combo.SelectedIndex = 0;
        combo.AddToRoot();

        TextBox textBox = Make("TextBox", () => new TextBox());
        Place(textBox, 230, 70, 200);
        textBox.Text = "Select this text";
        textBox.AddToRoot();

        PasswordBox passwordBox = Make("PasswordBox", () => new PasswordBox());
        Place(passwordBox, 230, 120, 200);
        passwordBox.Password = "secret text";
        passwordBox.AddToRoot();
        list.Visual.UpdateLayout();

        XnaColor[] pixels = MenuFocusRenderTests.Capture(gd, background, pngPrefix + "-list.png", Width, Height);
        foreach ((string state, ListBoxItem item) in itemStates)
        {
            results.Add(MeasureText($"ListBoxItem {state}", pixels, item.Visual.GetGraphicalUiElementByName("TextInstance")));
        }
        results.Add(MeasureText("ComboBox closed", pixels, combo.Visual.GetGraphicalUiElementByName("TextInstance")));

        textBox.SelectionStart = 0;
        textBox.SelectionLength = 6;
        textBox.IsFocused = true;
        textBox.Visual.UpdateLayout();
        pixels = MenuFocusRenderTests.Capture(gd, background, pngPrefix + "-textbox.png", Width, Height);
        results.Add(MeasureSelection("TextBox selection (focused)", pixels, textBox.Visual));
        results.Add(MeasureSelectionArea("TextBox selection area vs field", pixels, textBox.Visual, themeName));

        passwordBox.IsFocused = true;
        passwordBox.SelectionStart = 0;
        passwordBox.SelectionLength = 6;
        passwordBox.Visual.UpdateLayout();
        pixels = MenuFocusRenderTests.Capture(gd, background, pngPrefix + "-passwordbox.png", Width, Height);
        results.Add(MeasureSelection("PasswordBox selection (focused)", pixels, passwordBox.Visual));
        results.Add(MeasureSelectionArea("PasswordBox selection area vs field", pixels, passwordBox.Visual, themeName));

        // Scene 2: the open combo box drop-down with its selected item.
        passwordBox.IsFocused = false;
        list.RemoveFromRoot();
        textBox.RemoveFromRoot();
        passwordBox.RemoveFromRoot();
        combo.SelectedIndex = 1;
        combo.IsDropDownOpen = true;
        combo.Visual.UpdateLayout();
        pixels = MenuFocusRenderTests.Capture(gd, background, pngPrefix + "-combo-open.png", Width, Height);
        ListBox dropDown = combo.ListBox;
        for (int i = 0; i < dropDown.ListBoxItems.Count; i++)
        {
            ListBoxItem item = dropDown.ListBoxItems[i];
            results.Add(MeasureText($"ComboBox drop-down item {i} ({(item.IsSelected ? "selected" : "resting")})", pixels, item.Visual.GetGraphicalUiElementByName("TextInstance")));
        }
        combo.IsDropDownOpen = false;
        combo.RemoveFromRoot();

        return results;
    }

    private static Measurement MeasureSelection(string surface, XnaColor[] pixels, GraphicalUiElement textBoxVisual)
    {
        GraphicalUiElement text = textBoxVisual.GetGraphicalUiElementByName("TextInstance").ShouldNotBeNull(surface);
        GraphicalUiElement selection = textBoxVisual.GetGraphicalUiElementByName("SelectionInstance").ShouldNotBeNull(surface);
        selection.Visible.ShouldBeTrue($"{surface}: selection highlight is shown");
        int left = (int)Math.Max(text.AbsoluteLeft, selection.AbsoluteLeft);
        int top = (int)Math.Max(text.AbsoluteTop, selection.AbsoluteTop);
        int right = (int)Math.Min(text.AbsoluteLeft + text.GetAbsoluteWidth(), selection.AbsoluteLeft + selection.GetAbsoluteWidth());
        int bottom = (int)Math.Min(text.AbsoluteTop + text.GetAbsoluteHeight(), selection.AbsoluteTop + selection.GetAbsoluteHeight());
        return MeasureRect(surface, pixels, left, top, right, bottom, text);
    }

    // How visible the highlight itself is: the drawn selection fill against the field's own fill (sampled
    // inside the box, right of the text). Asserted for Hazard, whose dark field made the highlight faint; recorded
    // for the other themes.
    private static Measurement MeasureSelectionArea(string surface, XnaColor[] pixels, GraphicalUiElement textBoxVisual, string themeName)
    {
        Measurement selected = MeasureSelection(surface, pixels, textBoxVisual);
        int x = (int)(textBoxVisual.AbsoluteLeft + textBoxVisual.GetAbsoluteWidth() - 10);
        int y = (int)(textBoxVisual.AbsoluteTop + textBoxVisual.GetAbsoluteHeight() / 2);
        XnaColor field = pixels[y * Width + x];
        return new Measurement(surface, ContrastRatio(selected.Background, field), selected.Background, field,
            1.0);
    }

    private static Measurement MeasureText(string surface, XnaColor[] pixels, GraphicalUiElement? text)
    {
        text.ShouldNotBeNull(surface);
        return MeasureRect(surface, pixels, (int)text.AbsoluteLeft, (int)text.AbsoluteTop,
            (int)(text.AbsoluteLeft + text.GetAbsoluteWidth()), (int)(text.AbsoluteTop + text.GetAbsoluteHeight()), text);
    }

    private static Measurement MeasureRect(string surface, XnaColor[] pixels, int left, int top, int right, int bottom, GraphicalUiElement textElement)
    {
        left = Math.Clamp(left, 0, Width - 1);
        right = Math.Clamp(right, left + 1, Width);
        top = Math.Clamp(top, 0, Height - 1);
        bottom = Math.Clamp(bottom, top + 1, Height);
        Dictionary<uint, int> counts = new();
        for (int y = top; y < bottom; y++)
        {
            for (int x = left; x < right; x++)
            {
                XnaColor p = pixels[y * Width + x];
                uint key = ((uint)p.R << 16) | ((uint)p.G << 8) | p.B;
                counts[key] = counts.GetValueOrDefault(key) + 1;
            }
        }
        uint backgroundKey = counts.OrderByDescending(pair => pair.Value).First().Key;
        XnaColor background = FromKey(backgroundKey);
        // The text color is what the Text draws with after state application; thin glyphs (a password's dots)
        // never reach full color in a pixel, so the farthest pixel only stands in when it is not a Text.
        XnaColor text = background;
        if (textElement.RenderableComponent is global::RenderingLibrary.Graphics.Text renderable)
        {
            text = new XnaColor(renderable.Red, renderable.Green, renderable.Blue);
        }
        else
        {
            double farthest = 1.0;
            foreach (uint key in counts.Keys)
            {
                XnaColor candidate = FromKey(key);
                double candidateRatio = ContrastRatio(candidate, background);
                if (candidateRatio > farthest)
                {
                    farthest = candidateRatio;
                    text = candidate;
                }
            }
        }
        return new Measurement(surface, ContrastRatio(text, background), background, text);
    }

    private static XnaColor FromKey(uint key) => new XnaColor((int)((key >> 16) & 255), (int)((key >> 8) & 255), (int)(key & 255));

    private static double Luminance(XnaColor c)
    {
        static double Channel(byte value)
        {
            double s = value / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    private static double ContrastRatio(XnaColor a, XnaColor b)
    {
        double la = Luminance(a);
        double lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }
}
