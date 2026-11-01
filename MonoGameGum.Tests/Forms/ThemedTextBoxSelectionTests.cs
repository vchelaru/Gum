using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System;
using System.IO;
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
