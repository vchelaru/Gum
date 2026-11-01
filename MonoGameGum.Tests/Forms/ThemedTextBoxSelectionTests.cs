using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The selection highlight of every packaged theme's text boxes has a visible size once text is
/// selected. A runtime control starts in its default state, not its authored "Single" line-mode
/// state, so the default state itself has to size the highlight to the line (#7318).
/// </summary>
public class ThemedTextBoxSelectionTests : BaseTestClass
{
    [Theory]
    [InlineData("Bubblegum", "TextBox")]
    [InlineData("DarkPro", "TextBox")]
    [InlineData("Editor", "TextBox")]
    [InlineData("ForestGlade", "TextBox")]
    [InlineData("Hazard", "TextBox")]
    [InlineData("Meadow", "TextBox")]
    [InlineData("Neon", "TextBox")]
    [InlineData("Retro95", "TextBox")]
    [InlineData("Bubblegum", "PasswordBox")]
    [InlineData("DarkPro", "PasswordBox")]
    [InlineData("Editor", "PasswordBox")]
    [InlineData("ForestGlade", "PasswordBox")]
    [InlineData("Hazard", "PasswordBox")]
    [InlineData("Meadow", "PasswordBox")]
    [InlineData("Neon", "PasswordBox")]
    [InlineData("Retro95", "PasswordBox")]
    public void SelectAll_ThemedControl_SelectionHighlightHasVisibleHeight(string theme, string control)
    {
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", theme, "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        ScreenSave screen = new ScreenSave { Name = "SelectionScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = $"{theme}/Controls/{control}", ParentContainer = screen });
        project.Screens.Add(screen);
        project.Initialize();
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.UpdateLayout();
        GraphicalUiElement boxVisual = screenVisual.GetGraphicalUiElementByName("Box")!;
        TextBoxBase box = (TextBoxBase)((InteractiveGue)boxVisual).FormsControlAsObject;
        if (box is TextBox textBox) textBox.Text = "Select me please";
        else ((PasswordBox)box).Password = "Select me please";
        box.IsFocused = true;

        box.SelectAll();
        screenVisual.UpdateLayout();

        GraphicalUiElement selection = boxVisual.GetGraphicalUiElementByName("SelectionInstance")!;
        selection.Visible.ShouldBeTrue();
        selection.GetAbsoluteWidth().ShouldBeGreaterThan(0f);
        selection.GetAbsoluteHeight().ShouldBeGreaterThan(0f);
    }

    /// <summary>
    /// The selected text is drawn over the highlight without being recolored, so an opaque highlight must
    /// stay at least 3:1 away from the text color (#7324). A translucent highlight takes its look from
    /// the box background underneath, so it is left to that background's own contrast with the text.
    /// </summary>
    [Theory]
    [InlineData("", "TextBox")]
    [InlineData("", "PasswordBox")]
    [InlineData("Bubblegum", "TextBox")]
    [InlineData("DarkPro", "TextBox")]
    [InlineData("Editor", "TextBox")]
    [InlineData("ForestGlade", "TextBox")]
    [InlineData("Hazard", "TextBox")]
    [InlineData("Meadow", "TextBox")]
    [InlineData("Neon", "TextBox")]
    [InlineData("Retro95", "TextBox")]
    [InlineData("Bubblegum", "PasswordBox")]
    [InlineData("DarkPro", "PasswordBox")]
    [InlineData("Editor", "PasswordBox")]
    [InlineData("ForestGlade", "PasswordBox")]
    [InlineData("Hazard", "PasswordBox")]
    [InlineData("Meadow", "PasswordBox")]
    [InlineData("Neon", "PasswordBox")]
    [InlineData("Retro95", "PasswordBox")]
    public void SelectionHighlight_ThemedControl_HasContrastWithText(string theme, string control)
    {
        string themeFolder = theme.Length == 0 ? "FormsTemplate" : Path.Combine("FormsThemes", theme);
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", themeFolder, "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ComponentSave component = project.Components.Single(item => item.Name.EndsWith($"Controls/{control}"));

        (double[] selectionColor, int alpha) = ResolveColor(project, component, "SelectionInstance");
        if (alpha < 255)
        {
            return;
        }

        (double[] textColor, _) = ResolveColor(project, component, "TextInstance");

        double ratio = ContrastRatio(selectionColor, textColor);

        ratio.ShouldBeGreaterThanOrEqualTo(3.0, $"{theme} {control}: text {string.Join(",", textColor)} over highlight {string.Join(",", selectionColor)}");
    }

    /// <summary>
    /// The color an instance ends up with: the one its variable reference points at (a style token such
    /// as Styles.Selection, followed through further references), else the channels set on it directly.
    /// </summary>
    private static (double[] Rgb, int Alpha) ResolveColor(GumProjectSave project, ComponentSave component, string instanceName)
    {
        StateSave state = component.DefaultState;
        VariableListSave<string>? references = state.VariableLists
            .OfType<VariableListSave<string>>()
            .FirstOrDefault(item => item.Name == instanceName + ".VariableReferences");
        string? reference = references?.ValueAsIList.Cast<string>()
            .FirstOrDefault(line => line.StartsWith("FillColor =") || line.StartsWith("Color ="));
        if (reference != null)
        {
            // "FillColor = Components/Hazard/Styles.Selection.FillColor"
            string target = reference.Substring(reference.IndexOf('=') + 1).Trim();
            string[] pieces = target.Split('.');
            string componentName = pieces[0].Substring("Components/".Length);
            ComponentSave styles = project.Components.Single(item => item.Name == componentName);
            (double[] rgb, int tokenAlpha) = ResolveColor(project, styles, pieces[1]);
            // An alpha set directly on the instance wins over the token's.
            object? ownAlpha = state.GetValue(instanceName + ".FillAlpha") ?? state.GetValue(instanceName + ".Alpha");
            return (rgb, ownAlpha is int value ? value : tokenAlpha);
        }

        // Filled shapes name their channels FillRed; nine slices and text name them Red.
        string prefix = state.GetValue(instanceName + ".FillRed") != null || state.GetValue(instanceName + ".FillAlpha") != null ? ".Fill" : ".";
        int Channel(string name) => state.GetValue(instanceName + prefix + name) is int value ? value : 255;
        return (new double[] { Channel("Red"), Channel("Green"), Channel("Blue") }, Channel("Alpha"));
    }

    private static double ContrastRatio(double[] a, double[] b)
    {
        double la = Luminance(a);
        double lb = Luminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(double[] rgb)
    {
        double Linear(double v)
        {
            v /= 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Linear(rgb[0]) + 0.7152 * Linear(rgb[1]) + 0.0722 * Linear(rgb[2]);
    }

    private static string LocateRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GumFull.sln")))
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not locate GumFull.sln by walking up from {AppContext.BaseDirectory}.");
    }
}
