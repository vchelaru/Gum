using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Managers;
using Gum.Menus;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GumToolUnitTests.Plugins.InternalPlugins.MenuStripPlugin;

public class MenuStripManagerTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IUndoManager> _undoManager;
    private readonly Mock<IEditCommands> _editCommands;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly Mock<IMessenger> _messenger;
    private readonly Mock<IFileSystemRevealService> _fileSystemRevealService;
    private readonly Mock<IDispatcher> _dispatcher;
    private readonly MenuStripManager _menuStripManager;

    public MenuStripManagerTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _undoManager = new Mock<IUndoManager>();
        _editCommands = new Mock<IEditCommands>();
        _dialogService = new Mock<IDialogService>();
        _fileCommands = new Mock<IFileCommands>();
        _projectManager = new Mock<IProjectManager>();
        _messenger = new Mock<IMessenger>();
        _fileSystemRevealService = new Mock<IFileSystemRevealService>();
        _dispatcher = new Mock<IDispatcher>();
        _dispatcher.Setup(d => d.Post(It.IsAny<Action>())).Callback<Action>(action => action());

        _menuStripManager = CreateManager();
    }

    private MenuStripManager CreateManager() =>
        new MenuStripManager(new StandardMenuModelBuilder(
            _selectedState.Object,
            _undoManager.Object,
            _editCommands.Object,
            _dialogService.Object,
            _fileCommands.Object,
            _projectManager.Object,
            _messenger.Object,
            _fileSystemRevealService.Object,
            _dispatcher.Object));

    [StaFact]
    public void PopulateMenu_ShouldCreateSixTopLevelMenuItems()
    {
        var menu = new Menu();

        _menuStripManager.PopulateMenu(menu);

        menu.Items.Count.ShouldBe(6);
        ((MenuItem)menu.Items[0]).Header.ShouldBe("File");
        ((MenuItem)menu.Items[1]).Header.ShouldBe("Edit");
        ((MenuItem)menu.Items[2]).Header.ShouldBe("View");
        ((MenuItem)menu.Items[3]).Header.ShouldBe("Content");
        ((MenuItem)menu.Items[4]).Header.ShouldBe("Plugins");
        ((MenuItem)menu.Items[5]).Header.ShouldBe("Help");
    }

    [StaFact]
    public void RefreshUI_WithSelectedState_ShouldUpdateRemoveStateMenuItem()
    {
        var menu = new Menu();
        _selectedState.Setup(s => s.SelectedStateSave)
            .Returns(new StateSave { Name = "Running" });

        _menuStripManager.PopulateMenu(menu);
        _menuStripManager.RefreshUI();

        // Navigate to Edit > Remove > State item
        var editMenu = (MenuItem)menu.Items[1];
        var removeMenu = editMenu.Items
            .OfType<MenuItem>()
            .First(mi => mi.Header as string == "Remove");
        var stateItem = removeMenu.Items
            .OfType<MenuItem>()
            .First(mi => ((string)mi.Header).StartsWith("State"));

        stateItem.Header.ShouldBe("State Running");
        stateItem.IsEnabled.ShouldBeTrue();

        // Now clear selection and refresh again
        _selectedState.Setup(s => s.SelectedStateSave).Returns((StateSave?)null);
        _selectedState.Setup(s => s.SelectedStateCategorySave).Returns((StateSaveCategory?)null);
        _menuStripManager.RefreshUI();

        stateItem.Header.ShouldBe("<no state selected>");
        stateItem.IsEnabled.ShouldBeFalse();
    }

    [StaFact]
    public void NewProjectClicked_DelegatesSavingToNewProject_WithoutForcingASave()
    {
        // NewProject owns the save-location prompt so it can honour a cancelled dialog. Forcing a
        // save here as well re-prompts even when the user backed out.
        Menu menu = new();
        _menuStripManager.PopulateMenu(menu);

        MenuItem fileMenu = (MenuItem)menu.Items[0];
        MenuItem newProjectItem = fileMenu.Items
            .OfType<MenuItem>()
            .First(mi => mi.Header as string == "New Project");

        newProjectItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        _fileCommands.Verify(f => f.NewProject(), Times.Once);
        _fileCommands.Verify(f => f.ForceSaveProject(It.IsAny<bool>()), Times.Never);
    }

    [StaFact]
    public void AddMenuItem_WhenParentExists_ShouldAddChildItem()
    {
        var menu = new Menu();
        _menuStripManager.PopulateMenu(menu);

        var result = AddMenuItem(_menuStripManager, new[] { "Edit", "Properties" });

        result.Header.ShouldBe("Properties");
        var editMenu = (MenuItem)menu.Items[1];
        editMenu.Items.OfType<MenuItem>()
            .ShouldContain(mi => mi.Header as string == "Properties");
    }

    [StaFact]
    public void AddMenuItem_WhenParentDoesNotExist_ShouldInsertBeforeHelp()
    {
        var menu = new Menu();
        _menuStripManager.PopulateMenu(menu);

        var result = AddMenuItem(_menuStripManager, new[] { "Tools", "My Tool" });

        menu.Items.Count.ShouldBe(7);
        ((MenuItem)menu.Items[5]).Header.ShouldBe("Tools");
        ((MenuItem)menu.Items[6]).Header.ShouldBe("Help");
        result.Header.ShouldBe("My Tool");
    }

    [StaFact]
    public void RefreshUI_WithSelectedElement_ShouldUpdateRemoveElementMenuItem()
    {
        var menu = new Menu();
        _selectedState.Setup(s => s.SelectedElement)
            .Returns(new ComponentSave { Name = "MyButton" });

        _menuStripManager.PopulateMenu(menu);
        _menuStripManager.RefreshUI();

        var editMenu = (MenuItem)menu.Items[1];
        var removeMenu = editMenu.Items
            .OfType<MenuItem>()
            .First(mi => mi.Header as string == "Remove");
        var elementItem = removeMenu.Items
            .OfType<MenuItem>()
            .First(mi => mi.Header as string == "MyButton");

        elementItem.IsEnabled.ShouldBeTrue();

        // StandardElementSave should NOT enable the remove item
        _selectedState.Setup(s => s.SelectedElement)
            .Returns(new StandardElementSave { Name = "Text" });
        _menuStripManager.RefreshUI();

        elementItem.Header.ShouldBe("<no element selected>");
        elementItem.IsEnabled.ShouldBeFalse();
    }

    [StaFact]
    public void PopulateMenu_ShouldAddOpenSettingsFolderItemToHelpMenu()
    {
        var menu = new Menu();

        _menuStripManager.PopulateMenu(menu);

        var helpMenu = (MenuItem)menu.Items[5];
        helpMenu.Items.OfType<MenuItem>()
            .ShouldContain(mi => mi.Header as string == "Open Settings Folder...");
    }

    [StaFact]
    public void GetItem_ShouldReturnNull_WhenItemDoesNotExist()
    {
        var menu = new Menu();
        _menuStripManager.PopulateMenu(menu);

        var result = GetItem(_menuStripManager, "Nonexistent");

        result.ShouldBeNull();
    }

    [StaTheory]
    [InlineData("File")]
    [InlineData("Edit")]
    [InlineData("View")]
    [InlineData("Content")]
    [InlineData("Plugins")]
    [InlineData("Help")]
    public void GetItem_ReturnsItem_ForExistingTopLevelMenus(string menuName)
    {
        var menu = new Menu();
        _menuStripManager.PopulateMenu(menu);

        var result = GetItem(_menuStripManager, menuName);

        result.ShouldNotBeNull();
        (result.Header as string).ShouldBe(menuName);
    }

    [StaFact]
    public void PopulateMenu_CalledTwice_DoesNotDuplicateItems()
    {
        var menu = new Menu();
        _menuStripManager.PopulateMenu(menu);
        var countAfterFirst = menu.Items.Count;

        _menuStripManager.PopulateMenu(menu);
        var countAfterSecond = menu.Items.Count;

        countAfterSecond.ShouldBe(countAfterFirst);
    }

    [StaFact]
    public void ContentMenu_OrdersByLayoutTable_RegardlessOfRegistrationOrder()
    {
        Menu firstMenu = new Menu();
        _menuStripManager.PopulateMenu(firstMenu);

        // Register in one order
        AddMenuItem(_menuStripManager, new[] { "Content", "View Font Cache" });
        AddMenuItem(_menuStripManager, new[] { "Content", "Clear Font Cache" });
        AddMenuItem(_menuStripManager, new[] { "Content", "Import" });
        AddMenuItem(_menuStripManager, new[] { "Content", "Add Forms Components" });
        AddMenuItem(_menuStripManager, new[] { "Content", "Force re-create all font files" });
        AddMenuItem(_menuStripManager, new[] { "Content", "Re-create missing font files" });

        object[] firstHeaders = HeadersOf(firstMenu, "Content");

        MenuStripManager secondManager = CreateManager();
        Menu secondMenu = new Menu();
        secondManager.PopulateMenu(secondMenu);

        // Register in a different order
        AddMenuItem(secondManager, new[] { "Content", "Add Forms Components" });
        AddMenuItem(secondManager, new[] { "Content", "Re-create missing font files" });
        AddMenuItem(secondManager, new[] { "Content", "Import" });
        AddMenuItem(secondManager, new[] { "Content", "Force re-create all font files" });
        AddMenuItem(secondManager, new[] { "Content", "Clear Font Cache" });
        AddMenuItem(secondManager, new[] { "Content", "View Font Cache" });

        object[] secondHeaders = HeadersOf(secondMenu, "Content");

        firstHeaders.ShouldBe(secondHeaders);

        object[] expected = new object[]
        {
            "Find file references...",
            "<separator>",
            "Add Forms Components",
            "Import",
            "<separator>",
            "Clear Font Cache",
            "Re-create missing font files",
            "Force re-create all font files",
            "View Font Cache",
        };
        firstHeaders.ShouldBe(expected);
    }

    [StaFact]
    public void ContentMenu_AfterRemoveAndReadd_RestoresLayoutPosition()
    {
        Menu menu = new Menu();
        _menuStripManager.PopulateMenu(menu);

        MenuItem addForms = AddMenuItem(_menuStripManager, new[] { "Content", "Add Forms Components" });
        AddMenuItem(_menuStripManager, new[] { "Content", "Import" });
        AddMenuItem(_menuStripManager, new[] { "Content", "Clear Font Cache" });
        AddMenuItem(_menuStripManager, new[] { "Content", "View Font Cache" });

        // Simulate the project-load handler removing the item from the model when the project
        // already has forms imported; the rendered menu follows the model.
        MenuItemModel content = _menuStripManager.Model.GetItem("Content")!;
        content.Items.Remove(content.Items.First(item => item.Header == "Add Forms Components"));
        HeadersOf(menu, "Content").ShouldNotContain("Add Forms Components");

        // Now simulate re-adding it on a subsequent project load (forms not present).
        MenuItem readded = AddMenuItem(_menuStripManager, new[] { "Content", "Add Forms Components" });
        readded.ShouldNotBeSameAs(addForms);

        object[] headers = HeadersOf(menu, "Content");

        // The re-added item must land back in its forms group, not at the end.
        int formsIndex = Array.IndexOf(headers, (object)"Add Forms Components");
        int importIndex = Array.IndexOf(headers, (object)"Import");
        int clearFontIndex = Array.IndexOf(headers, (object)"Clear Font Cache");

        formsIndex.ShouldBeLessThan(importIndex);
        importIndex.ShouldBeLessThan(clearFontIndex);
    }

    [StaFact]
    public void AddMenuItem_ReturnsTheSameWpfItem_AcrossLayoutPasses()
    {
        Menu menu = new Menu();
        _menuStripManager.PopulateMenu(menu);

        MenuItem clearFontCache = AddMenuItem(_menuStripManager, new[] { "Content", "Clear Font Cache" });
        // A later registration re-applies the Content layout, which reorders the model's children.
        AddMenuItem(_menuStripManager, new[] { "Content", "Import" });

        MenuItem content = GetItem(_menuStripManager, "Content")!;
        content.Items.OfType<MenuItem>().ShouldContain(item => ReferenceEquals(item, clearFontCache));
    }

    [StaFact]
    public void ModelChanges_FlowIntoTheRenderedItem()
    {
        Menu menu = new Menu();
        _menuStripManager.PopulateMenu(menu);

        MenuItemModel model = _menuStripManager.Model.AddMenuItem(new[] { "View", "Show Thing" });
        MenuItem rendered = _menuStripManager.GetMenuItem(model);

        model.Header = "Hide Thing";
        model.IsEnabled = false;

        rendered.Header.ShouldBe("Hide Thing");
        rendered.IsEnabled.ShouldBeFalse();
    }

    [StaFact]
    public void CheckableItem_TogglesOnceThroughTheModel()
    {
        Menu menu = new Menu();
        _projectManager.SetupProperty(p => p.UseStandardsPalette, (bool?)false);
        _menuStripManager.PopulateMenu(menu);

        MenuItem viewMenu = GetItem(_menuStripManager, "View")!;
        MenuItem palette = viewMenu.Items.OfType<MenuItem>()
            .First(mi => (mi.Header as string)!.StartsWith("Standards palette"));
        palette.IsChecked.ShouldBeFalse();

        palette.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        palette.IsChecked.ShouldBeTrue();
        _projectManager.VerifySet(p => p.UseStandardsPalette = true, Times.Once);
    }

    [StaFact]
    public void ContentMenu_UnknownItem_GoesToEndWithSeparator()
    {
        Menu menu = new Menu();
        _menuStripManager.PopulateMenu(menu);

        AddMenuItem(_menuStripManager, new[] { "Content", "Add Forms Components" });
        AddMenuItem(_menuStripManager, new[] { "Content", "Clear Font Cache" });
        AddMenuItem(_menuStripManager, new[] { "Content", "Some Third-Party Plugin Item" });

        object[] headers = HeadersOf(menu, "Content");

        headers[^2].ShouldBe("<separator>");
        headers[^1].ShouldBe("Some Third-Party Plugin Item");
    }

    // Plugins add through the model; these mirror that path and hand back the rendered WPF item.
    private static MenuItem AddMenuItem(MenuStripManager manager, IEnumerable<string> path) =>
        manager.GetMenuItem(manager.Model.AddMenuItem(path));

    private static MenuItem? GetItem(MenuStripManager manager, string name)
    {
        MenuItemModel? model = manager.Model.GetItem(name);
        return model == null ? null : manager.GetMenuItem(model);
    }

    private static object[] HeadersOf(Menu menu, string topMenuName)
    {
        MenuItem parent = menu.Items.OfType<MenuItem>()
            .First(mi => mi.Header as string == topMenuName);

        List<object> headers = new List<object>();
        foreach (object item in parent.Items)
        {
            if (item is Separator)
            {
                headers.Add("<separator>");
            }
            else if (item is MenuItem mi)
            {
                headers.Add(mi.Header);
            }
        }
        return headers.ToArray();
    }
}
