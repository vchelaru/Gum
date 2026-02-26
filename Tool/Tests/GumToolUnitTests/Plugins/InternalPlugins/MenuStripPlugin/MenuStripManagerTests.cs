using Gum.Commands;
using Gum.Managers;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Moq.AutoMock;
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
    private readonly Mock<IDeleteLogic> _deleteLogic;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly MenuStripManager _menuStripManager;

    public MenuStripManagerTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _undoManager = new Mock<IUndoManager>();
        _editCommands = new Mock<IEditCommands>();
        _dialogService = new Mock<IDialogService>();
        _fileCommands = new Mock<IFileCommands>();
        _deleteLogic = new Mock<IDeleteLogic>();
        _projectManager = new Mock<IProjectManager>();

        _menuStripManager = new MenuStripManager(
            _selectedState.Object,
            _undoManager.Object,
            _editCommands.Object,
            _dialogService.Object,
            _fileCommands.Object,
            _deleteLogic.Object,
            _projectManager.Object
        );
    }

    /// <summary>
    /// Helper to run an action on an STA thread, which is required for WPF controls.
    /// </summary>
    private static void RunOnSta(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { caught = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (caught != null)
            throw new System.Reflection.TargetInvocationException(caught);
    }

    [Fact]
    public void PopulateMenu_ShouldCreateSixTopLevelMenuItems()
    {
        RunOnSta(() =>
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
        });
    }

    [Fact]
    public void RefreshUI_WithSelectedState_ShouldUpdateRemoveStateMenuItem()
    {
        RunOnSta(() =>
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
                .First(mi => (mi.Header as string).StartsWith("State"));

            stateItem.Header.ShouldBe("State Running");
            stateItem.IsEnabled.ShouldBeTrue();

            // Now clear selection and refresh again
            _selectedState.Setup(s => s.SelectedStateSave).Returns((StateSave?)null);
            _selectedState.Setup(s => s.SelectedStateCategorySave).Returns((StateSaveCategory?)null);
            _menuStripManager.RefreshUI();

            stateItem.Header.ShouldBe("<no state selected>");
            stateItem.IsEnabled.ShouldBeFalse();
        });
    }

    [Fact]
    public void AddMenuItem_WhenParentExists_ShouldAddChildItem()
    {
        RunOnSta(() =>
        {
            var menu = new Menu();
            _menuStripManager.PopulateMenu(menu);

            var result = _menuStripManager.AddMenuItem(new[] { "Edit", "Properties" });

            result.Header.ShouldBe("Properties");
            var editMenu = (MenuItem)menu.Items[1];
            editMenu.Items.OfType<MenuItem>()
                .ShouldContain(mi => mi.Header as string == "Properties");
        });
    }

    [Fact]
    public void AddMenuItem_WhenParentDoesNotExist_ShouldInsertBeforeHelp()
    {
        RunOnSta(() =>
        {
            var menu = new Menu();
            _menuStripManager.PopulateMenu(menu);

            var result = _menuStripManager.AddMenuItem(new[] { "Tools", "My Tool" });

            menu.Items.Count.ShouldBe(7);
            ((MenuItem)menu.Items[5]).Header.ShouldBe("Tools");
            ((MenuItem)menu.Items[6]).Header.ShouldBe("Help");
            result.Header.ShouldBe("My Tool");
        });
    }

    [Fact]
    public void RefreshUI_WithSelectedElement_ShouldUpdateRemoveElementMenuItem()
    {
        RunOnSta(() =>
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
        });
    }

    [Fact]
    public void GetItem_ShouldReturnNull_WhenItemDoesNotExist()
    {
        RunOnSta(() =>
        {
            var menu = new Menu();
            _menuStripManager.PopulateMenu(menu);

            var result = _menuStripManager.GetItem("Nonexistent");

            result.ShouldBeNull();
        });
    }

    [Theory]
    [InlineData("File")]
    [InlineData("Edit")]
    [InlineData("View")]
    [InlineData("Content")]
    [InlineData("Plugins")]
    [InlineData("Help")]
    public void GetItem_ReturnsItem_ForExistingTopLevelMenus(string menuName)
    {
        RunOnSta(() =>
        {
            var menu = new Menu();
            _menuStripManager.PopulateMenu(menu);

            var result = _menuStripManager.GetItem(menuName);

            result.ShouldNotBeNull();
            (result.Header as string).ShouldBe(menuName);
        });
    }

    [Fact]
    public void PopulateMenu_CalledTwice_DoesNotDuplicateItems()
    {
        RunOnSta(() =>
        {
            var menu = new Menu();
            _menuStripManager.PopulateMenu(menu);
            var countAfterFirst = menu.Items.Count;

            _menuStripManager.PopulateMenu(menu);
            var countAfterSecond = menu.Items.Count;

            countAfterSecond.ShouldBe(countAfterFirst);
        });
    }
}
