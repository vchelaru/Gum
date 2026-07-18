using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.Managers;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;
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

        _menuStripManager = new MenuStripManager(
            _selectedState.Object,
            _undoManager.Object,
            _editCommands.Object,
            _dialogService.Object,
            _fileCommands.Object,
            _projectManager.Object,
            _messenger.Object
        );
    }

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
    public void AddMenuItem_WhenParentExists_ShouldAddChildItem()
    {
        var menu = new Menu();
        _menuStripManager.PopulateMenu(menu);

        var result = _menuStripManager.AddMenuItem(new[] { "Edit", "Properties" });

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

        var result = _menuStripManager.AddMenuItem(new[] { "Tools", "My Tool" });

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
    public void GetItem_ShouldReturnNull_WhenItemDoesNotExist()
    {
        var menu = new Menu();
        _menuStripManager.PopulateMenu(menu);

        var result = _menuStripManager.GetItem("Nonexistent");

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

        var result = _menuStripManager.GetItem(menuName);

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
        _menuStripManager.AddMenuItem(new[] { "Content", "View Font Cache" });
        _menuStripManager.AddMenuItem(new[] { "Content", "Clear Font Cache" });
        _menuStripManager.AddMenuItem(new[] { "Content", "Import from .gumx..." });
        _menuStripManager.AddMenuItem(new[] { "Content", "Add Forms Components" });
        _menuStripManager.AddMenuItem(new[] { "Content", "Force re-create all font files" });
        _menuStripManager.AddMenuItem(new[] { "Content", "Re-create missing font files" });

        object[] firstHeaders = HeadersOf(firstMenu, "Content");

        MenuStripManager secondManager = new MenuStripManager(
            _selectedState.Object,
            _undoManager.Object,
            _editCommands.Object,
            _dialogService.Object,
            _fileCommands.Object,
            _projectManager.Object,
            _messenger.Object);
        Menu secondMenu = new Menu();
        secondManager.PopulateMenu(secondMenu);

        // Register in a different order
        secondManager.AddMenuItem(new[] { "Content", "Add Forms Components" });
        secondManager.AddMenuItem(new[] { "Content", "Re-create missing font files" });
        secondManager.AddMenuItem(new[] { "Content", "Import from .gumx..." });
        secondManager.AddMenuItem(new[] { "Content", "Force re-create all font files" });
        secondManager.AddMenuItem(new[] { "Content", "Clear Font Cache" });
        secondManager.AddMenuItem(new[] { "Content", "View Font Cache" });

        object[] secondHeaders = HeadersOf(secondMenu, "Content");

        firstHeaders.ShouldBe(secondHeaders);

        object[] expected = new object[]
        {
            "Find file references...",
            "<separator>",
            "Add Forms Components",
            "Import from .gumx...",
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

        _menuStripManager.AddMenuItem(new[] { "Content", "Add Forms Components" });
        _menuStripManager.AddMenuItem(new[] { "Content", "Import from .gumx..." });
        _menuStripManager.AddMenuItem(new[] { "Content", "Clear Font Cache" });
        _menuStripManager.AddMenuItem(new[] { "Content", "View Font Cache" });

        MenuItem content = (MenuItem)menu.Items.OfType<MenuItem>()
            .First(mi => mi.Header as string == "Content");
        MenuItem addForms = content.Items.OfType<MenuItem>()
            .First(mi => mi.Header as string == "Add Forms Components");

        // Simulate the project-load handler removing the item when the project
        // already has forms imported.
        content.Items.Remove(addForms);

        // Now simulate re-adding it on a subsequent project load (forms not present).
        // The Forms plugin uses AddMenuItemTo which appends directly into the parent's
        // Items collection, so mimic that path here and then re-apply the layout.
        MenuItem readded = new MenuItem { Header = "Add Forms Components" };
        content.Items.Add(readded);
        _menuStripManager.ApplyLayout("Content");

        object[] headers = HeadersOf(menu, "Content");

        // The re-added item must land back in its forms group, not at the end.
        int formsIndex = Array.IndexOf(headers, (object)"Add Forms Components");
        int importIndex = Array.IndexOf(headers, (object)"Import from .gumx...");
        int clearFontIndex = Array.IndexOf(headers, (object)"Clear Font Cache");

        formsIndex.ShouldBeLessThan(importIndex);
        importIndex.ShouldBeLessThan(clearFontIndex);
    }

    [StaFact]
    public void ContentMenu_UnknownItem_GoesToEndWithSeparator()
    {
        Menu menu = new Menu();
        _menuStripManager.PopulateMenu(menu);

        _menuStripManager.AddMenuItem(new[] { "Content", "Add Forms Components" });
        _menuStripManager.AddMenuItem(new[] { "Content", "Clear Font Cache" });
        _menuStripManager.AddMenuItem(new[] { "Content", "Some Third-Party Plugin Item" });

        object[] headers = HeadersOf(menu, "Content");

        headers[^2].ShouldBe("<separator>");
        headers[^1].ShouldBe("Some Third-Party Plugin Item");
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
