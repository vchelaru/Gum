using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Pins the SizingCategory (Fixed / FitChildren) on the tool's Add Forms theme ListBox components,
/// and that each theme's ComboBox uses FitChildren so its dropdown follows the item count (#5793).
/// </summary>
public class ListBoxSizingCategoryTests : BaseTestClass
{
    public static IEnumerable<object[]> ThemeGumxPaths()
    {
        yield return new object[] { "FormsTemplate" };
        foreach (string theme in new[] { "Bubblegum", "DarkPro", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95" })
        {
            yield return new object[] { theme };
        }
    }

    [Theory]
    [MemberData(nameof(ThemeGumxPaths))]
    public void ComboBox_OpenWithFewItems_ShouldSizeDropDownToItems(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        ComponentSave comboBoxSave = project.Components.Single(c => c.Name.EndsWith("Controls/ComboBox"));
        ComboBox comboBox = (ComboBox)((InteractiveGue)comboBoxSave.ToGraphicalUiElement()).FormsControlAsObject;
        comboBox.AddToRoot();
        comboBox.Items.Add("a");
        comboBox.Items.Add("b");
        comboBox.Items.Add("c");

        comboBox.IsDropDownOpen = true;

        GraphicalUiElement listBoxVisual = comboBox.ListBox.Visual;
        float itemsHeight = listBoxVisual.GetGraphicalUiElementByName("InnerPanelInstance")!.AbsoluteHeight;
        itemsHeight.ShouldBeGreaterThan(0);
        GraphicalUiElement fixedVisual = CreateListBox(project, "Fixed", itemCount: 0, out _);
        (listBoxVisual.AbsoluteHeight - itemsHeight).ShouldBe(FixedInset(fixedVisual));
    }

    [Theory]
    [MemberData(nameof(ThemeGumxPaths))]
    public void ComboBox_ReopenAfterAddingItems_ShouldGrowDropDownUpToFixedHeight(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        ComponentSave comboBoxSave = project.Components.Single(c => c.Name.EndsWith("Controls/ComboBox"));
        ComboBox comboBox = (ComboBox)((InteractiveGue)comboBoxSave.ToGraphicalUiElement()).FormsControlAsObject;
        comboBox.AddToRoot();
        comboBox.Items.Add("a");
        comboBox.IsDropDownOpen = true;
        float oneItemHeight = comboBox.ListBox.Visual.AbsoluteHeight;
        comboBox.IsDropDownOpen = false;

        for (int i = 0; i < 100; i++)
        {
            comboBox.Items.Add("Item " + i);
        }
        comboBox.IsDropDownOpen = true;

        GraphicalUiElement fixedVisual = CreateListBox(project, "Fixed", itemCount: 0, out _);
        comboBox.ListBox.Visual.AbsoluteHeight.ShouldBe(fixedVisual.AbsoluteHeight);
        comboBox.ListBox.Visual.AbsoluteHeight.ShouldBeGreaterThan(oneItemHeight);
    }

    [Theory]
    [MemberData(nameof(ThemeGumxPaths))]
    public void ListBox_FitChildren_ShouldCapAtFixedHeight_WhenItemsOverflow(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        GraphicalUiElement fixedVisual = CreateListBox(project, "Fixed", itemCount: 100, out _);
        GraphicalUiElement fitVisual = CreateListBox(project, "FitChildren", itemCount: 100, out _);

        fitVisual.AbsoluteHeight.ShouldBe(fixedVisual.AbsoluteHeight);
        float clipHeight = fitVisual.GetGraphicalUiElementByName("ClipContainerInstance")!.AbsoluteHeight;
        fitVisual.GetGraphicalUiElementByName("InnerPanelInstance")!.AbsoluteHeight.ShouldBeGreaterThan(clipHeight);
    }

    [Theory]
    [MemberData(nameof(ThemeGumxPaths))]
    public void ListBox_FitChildren_ShouldSizeToItems(string theme)
    {
        GumProjectSave project = LoadTheme(theme);
        GraphicalUiElement fixedVisual = CreateListBox(project, "Fixed", itemCount: 3, out _);
        GraphicalUiElement fitVisual = CreateListBox(project, "FitChildren", itemCount: 3, out _);

        float itemsHeight = fitVisual.GetGraphicalUiElementByName("InnerPanelInstance")!.AbsoluteHeight;
        itemsHeight.ShouldBeGreaterThan(0);
        fitVisual.GetGraphicalUiElementByName("ClipContainerInstance")!.AbsoluteHeight.ShouldBe(itemsHeight);
        // Same inset around the items as the Fixed layout keeps around its clip container.
        (fitVisual.AbsoluteHeight - itemsHeight).ShouldBe(FixedInset(fixedVisual));
    }

    private static float FixedInset(GraphicalUiElement fixedVisual) =>
        fixedVisual.AbsoluteHeight - fixedVisual.GetGraphicalUiElementByName("ClipContainerInstance")!.AbsoluteHeight;

    private static GraphicalUiElement CreateListBox(GumProjectSave project, string sizingState, int itemCount, out ListBox listBox)
    {
        ComponentSave listBoxSave = project.Components.Single(c => c.Name.EndsWith("Controls/ListBox"));
        GraphicalUiElement visual = listBoxSave.ToGraphicalUiElement();
        visual.SetProperty("SizingCategoryState", sizingState);
        listBox = (ListBox)((InteractiveGue)visual).FormsControlAsObject;
        for (int i = 0; i < itemCount; i++)
        {
            listBox.Items.Add("Item " + i);
        }
        visual.UpdateLayout();
        return visual;
    }

    private static GumProjectSave LoadTheme(string theme)
    {
        string templates = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates");
        string gumx = theme == "FormsTemplate"
            ? Path.Combine(templates, "FormsTemplate", "GumProject.gumx")
            : Path.Combine(templates, "FormsThemes", theme, "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        return project;
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
