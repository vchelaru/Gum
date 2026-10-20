using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Items authored in the Gum tool (children of a ListBox or Menu InnerPanel, or of a MenuItem's
/// SubItemContainerInstance) reach the control's Items when the project is loaded, and still do
/// after a Visual swap. Characterization of the control's own Items collection (#6243).
/// </summary>
public class AuthoredItemsFromProjectTests : BaseTestClass
{
    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    #region Loaded from project

    [Fact]
    public void ListBox_ShouldHaveAuthoredRowsInItemsAndListBoxItems_WhenLoadedFromProject()
    {
        ListBox sut = LoadControl<ListBox>(
            "Controls/ListBox", "Controls/ListBoxItem", "InnerPanelInstance", 2);

        sut.Items!.Count.ShouldBe(2);
        sut.Items[0].ShouldBeOfType<ListBoxItem>();
        sut.ListBoxItems.Count.ShouldBe(2);
        sut.InnerPanel.Children.Count.ShouldBe(2);
    }

    [Fact]
    public void ListBox_ShouldSelectAuthoredRow_WhenLoadedFromProject()
    {
        ListBox sut = LoadControl<ListBox>(
            "Controls/ListBox", "Controls/ListBoxItem", "InnerPanelInstance", 2);

        sut.ListBoxItems[1].IsSelected = true;

        sut.SelectedIndex.ShouldBe(1);
        sut.SelectedObject.ShouldBe(sut.ListBoxItems[1]);
    }

    [Fact]
    public void Menu_ShouldHaveAuthoredItemsInMenuItemsOnly_WhenLoadedFromProject()
    {
        Menu sut = LoadControl<Menu>(
            "Controls/Menu", "Controls/MenuItem", "InnerPanelInstance", 2);

        // Children the project loader adds after the Menu exists reach MenuItems but not Items.
        sut.MenuItems.Count.ShouldBe(2);
        sut.Items!.Count.ShouldBe(0);
        sut.InnerPanel.Children.Count.ShouldBe(2);
    }

    [Fact]
    public void MenuItem_ShouldHaveAuthoredSubItemsInItems_WhenLoadedFromProject()
    {
        MenuItem sut = LoadControl<MenuItem>(
            "Controls/MenuItem", "Controls/MenuItem", "SubItemContainerInstance", 2);
        Gum.GumService.Default.DeferredQueue.ProcessPending();

        sut.Items!.Count.ShouldBe(2);
        sut.Items[0].ShouldBeOfType<MenuItem>();
    }

    #endregion

    #region Visual swap on a project-loaded control

    [Fact]
    public void ListBox_ShouldCarryProjectRowsAndCodeItemsToNewPanel_WhenLoadedControlSwappedToPlainVisual()
    {
        ListBox sut = LoadControl<ListBox>(
            "Controls/ListBox", "Controls/ListBoxItem", "InnerPanelInstance", 2);
        sut.Items!.Add("code");
        ListBoxVisual newVisual = new(tryCreateFormsObject: false);

        sut.Visual = newVisual;

        // Rows the project loader adds after the ListBox exists are ordinary Items, so they follow
        // the control like code-added items do.
        sut.Items.Count.ShouldBe(3);
        sut.Items[2].ShouldBe("code");
        sut.ListBoxItems.Count.ShouldBe(3);
        newVisual.InnerPanelInstance.Children.Count.ShouldBe(3);
    }

    [Fact]
    public void ListBox_ShouldAdoptNewAuthoredRowsFirst_WhenLoadedControlSwappedToVisualWithAuthoredRows()
    {
        ListBox sut = LoadControl<ListBox>(
            "Controls/ListBox", "Controls/ListBoxItem", "InnerPanelInstance", 2);
        sut.Items!.Add("code");
        ListBoxVisual newVisual = new(tryCreateFormsObject: false);
        ListBoxItem newAuthored = new();
        newVisual.InnerPanelInstance.Children.Add(newAuthored.Visual);

        sut.Visual = newVisual;

        sut.Items.Count.ShouldBe(4);
        sut.Items[0].ShouldBe(newAuthored);
        sut.Items[3].ShouldBe("code");
        sut.ListBoxItems.Count.ShouldBe(4);
    }

    [Fact]
    public void Menu_ShouldAdoptNewAuthoredItems_WhenLoadedControlSwappedToVisualWithAuthoredItems()
    {
        Menu sut = LoadControl<Menu>(
            "Controls/Menu", "Controls/MenuItem", "InnerPanelInstance", 2);
        MenuVisual newVisual = new(tryCreateFormsObject: false);
        MenuItem newAuthored = new();
        newVisual.InnerPanelInstance.Children.Add(newAuthored.Visual);

        sut.Visual = newVisual;

        sut.Items!.Count.ShouldBe(1);
        sut.Items[0].ShouldBe(newAuthored);
        sut.MenuItems.Count.ShouldBe(1);
    }

    [Fact]
    public void MenuItem_ShouldAdoptNewAuthoredSubItems_WhenLoadedControlSwappedToVisualWithSubItems()
    {
        MenuItem sut = LoadControl<MenuItem>(
            "Controls/MenuItem", "Controls/MenuItem", "SubItemContainerInstance", 2);
        Gum.GumService.Default.DeferredQueue.ProcessPending();
        MenuItem newAuthored = new();
        ContainerRuntime newVisual = new();
        ContainerRuntime container = new() { Name = "SubItemContainerInstance" };
        newVisual.AddChild(container);
        container.Children.Add(newAuthored.Visual);

        sut.Visual = newVisual;

        sut.Items!.Count.ShouldBe(1);
        sut.Items[0].ShouldBe(newAuthored);
    }

    #endregion

    #region Default collection, code-built visuals

    [Fact]
    public void Menu_ShouldAdoptAuthoredItemsAheadOfCodeItems_WhenVisualSwapped()
    {
        Menu sut = new();
        sut.Items!.Add("Edit");
        MenuVisual newVisual = new(tryCreateFormsObject: false);
        MenuItem authoredItem = new();
        newVisual.InnerPanelInstance.Children.Add(authoredItem.Visual);

        sut.Visual = newVisual;

        sut.Items.Count.ShouldBe(2);
        sut.Items[0].ShouldBe(authoredItem);
        sut.Items[1].ShouldBe("Edit");
        sut.MenuItems.Count.ShouldBe(2);
        sut.MenuItems[0].ShouldBe(authoredItem);
    }

    [Fact]
    public void Menu_ShouldDropOldAuthoredItems_WhenVisualSwapped()
    {
        MenuVisual oldVisual = new(tryCreateFormsObject: false);
        MenuItem oldAuthored = new();
        oldVisual.InnerPanelInstance.Children.Add(oldAuthored.Visual);
        Menu sut = new(oldVisual);

        sut.Visual = new MenuVisual(tryCreateFormsObject: false);

        sut.Items!.Count.ShouldBe(0);
        sut.MenuItems.Count.ShouldBe(0);
    }

    #endregion

    #region Helpers

    private static T LoadControl<T>(
        string controlType, string childType, string containerName, int childCount) where T : FrameworkElement
    {
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsTemplate", "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        ScreenSave screen = new ScreenSave { Name = "ItemsScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        screen.Instances.Add(new InstanceSave { Name = "ControlInstance", BaseType = controlType, ParentContainer = screen });
        for (int i = 0; i < childCount; i++)
        {
            string childName = "Child" + i;
            screen.Instances.Add(new InstanceSave { Name = childName, BaseType = childType, ParentContainer = screen });
            defaultState.Variables.Add(new VariableSave
            {
                Name = childName + ".Parent",
                Type = "string",
                Value = "ControlInstance." + containerName,
                SetsValue = true
            });
        }
        project.Screens.Add(screen);
        project.Initialize();

        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.UpdateLayout();

        GraphicalUiElement controlVisual = screenVisual.GetGraphicalUiElementByName("ControlInstance")!;
        return ((InteractiveGue)controlVisual).FormsControlAsObject.ShouldBeOfType<T>();
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

    #endregion
}
