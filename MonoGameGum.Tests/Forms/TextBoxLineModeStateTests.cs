using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// When a TextBox or PasswordBox applies its visual's LineModeCategory state: a default
/// single-line control applies "Single" when the visual defines it, a control configured for
/// multiple lines applies "Multi"/"MultiNoWrap", and a visual authored in "Multi" keeps it (#7325).
/// </summary>
public class TextBoxLineModeStateTests : BaseTestClass
{
    #region Helpers

    private enum Authored { Default, Multi }

    private sealed record Loaded(GraphicalUiElement Screen, GraphicalUiElement BoxVisual, TextBoxBase Box);

    private static string LocateTemplates()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "GumFull.sln")))
            {
                return Path.Combine(directory.FullName, "Tools", "Gum.ProjectServices", "Templates");
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not locate GumFull.sln by walking up from {AppContext.BaseDirectory}.");
    }

    /// <param name="selectionHeightOnlyInSingle">Removes the selection height variables from the
    /// component's default state, leaving them only in "Single" (what a user-authored theme can do).</param>
    private static Loaded LoadThemed(string project, string control, Authored authored, bool selectionHeightOnlyInSingle = false)
    {
        string root = LocateTemplates();
        bool isTheme = project != "FormsTemplate";
        string gumx = isTheme
            ? Path.Combine(root, "FormsThemes", project, "GumProject.gumx")
            : Path.Combine(root, project, "GumProject.gumx");
        GumProjectSave gumProject = GumProjectSave.Load(gumx, out _) ?? throw new InvalidOperationException(gumx);
        gumProject.Initialize();
        ObjectFinder.Self.GumProjectSave = gumProject;
        string baseType = isTheme ? $"{project}/Controls/{control}" : $"Controls/{control}";

        if (selectionHeightOnlyInSingle)
        {
            ComponentSave component = gumProject.Components.First(item => item.Name == baseType);
            component.DefaultState!.Variables.RemoveAll(item =>
                item.Name == "SelectionInstance.Height" || item.Name == "SelectionInstance.HeightUnits");
            StateSave single = component.Categories.First(item => item.Name == "LineModeCategory")
                .States.First(item => item.Name == "Single");
            single.SetValue("SelectionInstance.Height", -4f, "float");
            single.SetValue("SelectionInstance.HeightUnits", DimensionUnitType.RelativeToParent, "DimensionUnitType");
        }

        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        ScreenSave screen = new ScreenSave { Name = "LineModeScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Box", BaseType = baseType, ParentContainer = screen });
        if (authored == Authored.Multi)
        {
            screen.DefaultState!.SetValue("Box.LineModeCategoryState", "Multi", "LineModeCategory");
        }
        gumProject.Screens.Add(screen);
        gumProject.Initialize();
        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement()!;
        screenVisual.UpdateLayout();
        GraphicalUiElement boxVisual = screenVisual.GetGraphicalUiElementByName("Box")!;
        return new Loaded(screenVisual, boxVisual, (TextBoxBase)((InteractiveGue)boxVisual).FormsControlAsObject!);
    }

    private static VerticalAlignment TextAlignment(GraphicalUiElement boxVisual) =>
        ((RenderingLibrary.Graphics.Text)boxVisual.GetGraphicalUiElementByName("TextInstance")!.RenderableComponent).VerticalAlignment;

    // The Single state sizes the text to its children; Multi sizes it to the container.
    private static DimensionUnitType TextWidthUnits(GraphicalUiElement boxVisual) =>
        boxVisual.GetGraphicalUiElementByName("TextInstance")!.WidthUnits;

    private static float SelectAllHeight(Loaded loaded)
    {
        if (loaded.Box is TextBox textBox) textBox.Text = "Select me please";
        else ((PasswordBox)loaded.Box).Password = "Select me please";
        loaded.Box.IsFocused = true;
        loaded.Box.SelectAll();
        loaded.Screen.UpdateLayout();
        return loaded.BoxVisual.GetGraphicalUiElementByName("SelectionInstance")!.AbsoluteHeight;
    }

    #endregion

    #region Characterization: code-only visuals

    [Fact]
    public void CodeOnlyTextBox_Default_IsCenteredSingleLine()
    {
        TextBox box = new();
        TextAlignment(box.Visual).ShouldBe(VerticalAlignment.Center);
    }

    [Fact]
    public void CodeOnlyPasswordBox_Default_IsCenteredSingleLine()
    {
        PasswordBox box = new();
        TextAlignment(box.Visual).ShouldBe(VerticalAlignment.Center);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CodeOnlyTextBox_MultiLineConfiguration_IsTopAligned(bool acceptsReturn, bool wrap)
    {
        TextBox box = new();
        box.AcceptsReturn = acceptsReturn;
        if (wrap) box.TextWrapping = Gum.Forms.TextWrapping.Wrap;
        TextAlignment(box.Visual).ShouldBe(VerticalAlignment.Top);
    }

    [Fact]
    public void CodeOnlyTextBox_BackToSingleLine_IsCentered()
    {
        TextBox box = new();
        box.AcceptsReturn = true;
        box.AcceptsReturn = false;
        TextAlignment(box.Visual).ShouldBe(VerticalAlignment.Center);
    }

    #endregion

    #region Characterization: themed projects

    [Theory]
    [InlineData("Hazard", "TextBox")]
    [InlineData("Hazard", "PasswordBox")]
    [InlineData("FormsTemplate", "TextBox")]
    [InlineData("FormsTemplate", "PasswordBox")]
    public void Themed_Default_SelectionHasHeightAndTextSizedToChildren(string project, string control)
    {
        Loaded loaded = LoadThemed(project, control, Authored.Default);

        SelectAllHeight(loaded).ShouldBeGreaterThan(0f);
        TextWidthUnits(loaded.BoxVisual).ShouldBe(DimensionUnitType.RelativeToChildren);
    }

    [Theory]
    [InlineData("Hazard")]
    [InlineData("FormsTemplate")]
    public void ThemedTextBox_AcceptsReturn_TextSizedToContainer(string project)
    {
        Loaded loaded = LoadThemed(project, "TextBox", Authored.Default);
        loaded.Box.AcceptsReturn = true;
        TextWidthUnits(loaded.BoxVisual).ShouldBe(DimensionUnitType.RelativeToParent);
    }

    [Theory]
    [InlineData("Hazard", "TextBox")]
    [InlineData("FormsTemplate", "TextBox")]
    public void Themed_AuthoredMultiDefault_StaysMulti(string project, string control)
    {
        Loaded loaded = LoadThemed(project, control, Authored.Multi);

        TextWidthUnits(loaded.BoxVisual).ShouldBe(DimensionUnitType.RelativeToParent);
        loaded.Box.AcceptsReturn.ShouldBeFalse();
    }

    #endregion

    #region Bug: Single-only variables never apply to a default control

    [Theory]
    [InlineData("Hazard", "TextBox")]
    [InlineData("FormsTemplate", "TextBox")]
    public void Themed_SelectionHeightOnlyInSingle_DefaultControlShowsSelection(string project, string control)
    {
        Loaded loaded = LoadThemed(project, control, Authored.Default, selectionHeightOnlyInSingle: true);

        SelectAllHeight(loaded).ShouldBeGreaterThan(0f);
        // The height is authored only in "Single", so it applies only if that state is applied.
        loaded.BoxVisual.GetGraphicalUiElementByName("SelectionInstance")!.HeightUnits
            .ShouldBe(DimensionUnitType.RelativeToParent);
    }

    [Theory]
    [InlineData("Hazard", "TextBox")]
    [InlineData("FormsTemplate", "TextBox")]
    public void Themed_SelectionHeightOnlyInSingle_AuthoredMultiStaysMulti(string project, string control)
    {
        Loaded loaded = LoadThemed(project, control, Authored.Multi, selectionHeightOnlyInSingle: true);

        TextWidthUnits(loaded.BoxVisual).ShouldBe(DimensionUnitType.RelativeToParent);
    }

    #endregion
}
