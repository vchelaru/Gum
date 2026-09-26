using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.VariableGrid;
using Gum.ToolStates;
using Gum.Undo;
using Microsoft.Extensions.DependencyInjection;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.VariableGrid;

/// <summary>
/// The tool's own Variables tab (the head's singleton grid manager, plugin and view, so every
/// refresh the tool routes to the tab lands here) over a temp project, hosted in a headless window
/// and driven the way a user drives it: typing into fields, pressing toggles, picking combo items,
/// right-click menus. Results are read back from the rows, the <see cref="ElementSave"/> and the
/// saved element file.
/// </summary>
internal sealed class VariableGridHarness : IDisposable
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    private readonly PluginManager _pluginManager;
    private readonly HeadlessWindowDriver _driver;

    public VariableGridHarness()
    {
        _pluginManager = Services.GetRequiredService<PluginManager>();
        GridManager = Services.GetRequiredService<PropertyGridManager>();
        Plugin = _pluginManager.InitializedPlugins.OfType<MainVariableGridPlugin>().Single();
        // An earlier test's failure must not read as this one's; a disabled tab ignores every event.
        ThrowIfPluginFailed();
        View = (VariablesTabView)ToolStartup.VariablesTab.Content;

        Project = new ToolProjectFixture("GumVariableGrid");
        try
        {
            _driver = new HeadlessWindowDriver(View, width: 520, height: 1600, framesFolderName: "GumVariableGrid", contentOutlivesTest: true);
        }
        catch
        {
            Project.Dispose();
            throw;
        }
    }

    /// <summary>The temp project, its builders and the scripted dialogs.</summary>
    public ToolProjectFixture Project { get; }

    /// <summary>Input, context menus and pixel reads for the tab's window.</summary>
    public HeadlessWindowDriver Input => _driver;

    public PropertyGridManager GridManager { get; }

    public MainVariableGridPlugin Plugin { get; }

    public VariablesTabView View { get; }

    public DataUiGrid Grid => (DataUiGrid)View.VariablesGrid;

    public MainControlViewModel ViewModel => GridManager.VariableViewModel;

    public ISelectedState SelectedState => Project.SelectedState;

    public IUndoManager UndoManager => Project.UndoManager;

    public ScriptedDialogService Dialogs => Project.Dialogs;

    #region Selection

    /// <summary>Selects <paramref name="element"/> (and its default state), as a tree click does.</summary>
    public void Select(ElementSave element)
    {
        SelectedState.SelectedElement = element;
        Settle();
    }

    /// <summary>Selects <paramref name="instance"/> in its element.</summary>
    public void Select(InstanceSave instance)
    {
        SelectedState.SelectedInstance = instance;
        Settle();
    }

    /// <summary>Selects several instances of one element, as a ctrl+click in the tree does.</summary>
    public void Select(params InstanceSave[] instances)
    {
        SelectedState.SelectedInstances = instances;
        Settle();
    }

    /// <summary>Selects <paramref name="state"/> of the selected element, as a click in the States tab does.</summary>
    public void Select(StateSave state)
    {
        SelectedState.SelectedStateSave = state;
        Settle();
    }

    /// <summary>Undoes the last change to the selected element, as Ctrl+Z does.</summary>
    public void Undo()
    {
        UndoManager.PerformUndo();
        Settle();
    }

    /// <summary>Redoes, as Ctrl+Y does.</summary>
    public void Redo()
    {
        UndoManager.PerformRedo();
        Settle();
    }

    /// <summary>Lays the window out and fails when a tool event crashed the tab's plugin.</summary>
    public void Settle()
    {
        _driver.Layout();
        ThrowIfPluginFailed();
    }

    /// <summary>
    /// Fails with the plugin's own exception when a tool event crashed it: the plugin manager
    /// disables a plugin that throws, after which the tab silently stops following the tool.
    /// </summary>
    public void ThrowIfPluginFailed()
    {
        PluginContainer container = _pluginManager.PluginContainers[Plugin];
        if (!container.IsEnabled)
        {
            throw new InvalidOperationException($"The Variables tab plugin was disabled: {container.FailureDetails}", container.FailureException);
        }
    }

    #endregion

    #region Reading the grid

    /// <summary>The names of the rows shown, in order, category by category.</summary>
    public List<string> ShownMemberNames() =>
        Grid.Categories.SelectMany(category => category.Members).Select(member => member.Name).ToList();

    /// <summary>The names of the categories shown, in order.</summary>
    public List<string> ShownCategoryNames() => Grid.Categories.Select(category => category.Name).ToList();

    /// <summary>
    /// The row model for <paramref name="memberName"/>: an exact name, or the unqualified name of an
    /// instance's row ("X" for "Label.X"); the first match when several categories show it.
    /// </summary>
    public InstanceMember Member(string memberName)
    {
        List<InstanceMember> members = Grid.Categories.SelectMany(category => category.Members).ToList();
        return members.FirstOrDefault(member => member.Name == memberName)
            ?? members.FirstOrDefault(member => member.Name.EndsWith("." + memberName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"The grid shows no {memberName} row; it shows [{string.Join(", ", ShownMemberNames())}].");
    }

    /// <summary>The live row host for <paramref name="memberName"/>.</summary>
    public SingleDataUiContainer Row(string memberName)
    {
        _driver.Layout();
        InstanceMember member = Member(memberName);
        return Grid.LiveContainers.FirstOrDefault(row => row.Member == member)
            ?? throw new InvalidOperationException($"The {memberName} row has no live editor.");
    }

    /// <summary>The editor of <paramref name="memberName"/>'s row, as <typeparamref name="T"/>.</summary>
    public T Editor<T>(string memberName) where T : Control =>
        Row(memberName).Displayer as T
        ?? throw new InvalidOperationException($"The {memberName} row shows a {Row(memberName).Displayer?.GetType().Name ?? "nothing"}, not a {typeof(T).Name}.");

    /// <summary>The text field of a text-box row.</summary>
    public TextBox TextField(string memberName) => Editor<TextBoxDisplay>(memberName).TextBox;

    /// <summary>What a text-box row's field shows.</summary>
    public string FieldText(string memberName) => TextField(memberName).Text ?? "";

    /// <summary>The combo box of a combo row.</summary>
    public ComboBox Combo(string memberName) =>
        Row(memberName).GetVisualDescendants().OfType<ComboBox>().FirstOrDefault()
        ?? throw new InvalidOperationException($"The {memberName} row has no combo box.");

    /// <summary>The toggle buttons of an option row (units, origins, alignment).</summary>
    public List<ToggleButton> Toggles(string memberName) => Editor<ToggleButtonOptionDisplay>(memberName).Buttons.ToList();

    /// <summary>Whether the row shows the set-value marker (its value is set in the selected state, not inherited).</summary>
    public bool ShowsSetMarker(string memberName) => !Member(memberName).IsDefault;

    #endregion

    #region Gestures

    /// <summary>Types <paramref name="text"/> into a text-box row and presses Enter.</summary>
    public void TypeAndEnter(string memberName, string text)
    {
        _driver.TypeAndEnter(TextField(memberName), text);
        ThrowIfPluginFailed();
    }

    /// <summary>Types <paramref name="text"/> into a text-box row, then moves focus away (tab-out commit).</summary>
    public void TypeAndLeave(string memberName, string text)
    {
        _driver.TypeInto(TextField(memberName), text);
        _driver.Press(Key.Tab, PhysicalKey.Tab);
        ThrowIfPluginFailed();
    }

    /// <summary>Types <paramref name="lines"/> into a string-list row (VariableReferences) and applies them with Ctrl+Enter.</summary>
    public void TypeLinesAndApply(string memberName, params string[] lines)
    {
        TextBox box = Editor<StringListTextBoxDisplay>(memberName).EditorTextBox;
        _driver.Click(box);
        box.Focus();
        box.SelectAll();
        // The box takes new lines as typed text; Enter alone would add a line, not apply.
        _driver.Window.KeyTextInput(string.Join("\n", lines));
        _driver.Press(Key.Enter, PhysicalKey.Enter, RawInputModifiers.Control);
        ThrowIfPluginFailed();
    }

    /// <summary>
    /// Opens a combo row's drop-down and picks <paramref name="item"/>, which commits it as a click
    /// in the list does. The drop-down's popup is its own top level, which the window's pointer
    /// input does not reach, so the pick is the selection the click makes while the list is open.
    /// </summary>
    public void PickComboItem(string memberName, string item)
    {
        ComboBox combo = Combo(memberName);
        combo.BringIntoView();
        combo.IsDropDownOpen = true;
        _driver.Layout();
        object match = combo.Items.Cast<object?>().FirstOrDefault(candidate => candidate?.ToString() == item)
            ?? throw new InvalidOperationException($"The {memberName} combo has no \"{item}\"; it has [{string.Join(", ", combo.Items.Cast<object?>())}].");
        combo.SelectedItem = match;
        combo.IsDropDownOpen = false;
        Settle();
    }

    /// <summary>Presses the toggle of an option row whose tooltip or value names <paramref name="option"/>.</summary>
    public void PressToggle(string memberName, object value)
    {
        ToggleButtonOptionDisplay display = Editor<ToggleButtonOptionDisplay>(memberName);
        ToggleButton button = display.Buttons.FirstOrDefault(candidate => Equals(ToggleValue(candidate), value))
            ?? throw new InvalidOperationException($"The {memberName} row has no toggle for {value}.");
        _driver.Click(button);
        Settle();
    }

    private static object? ToggleValue(ToggleButton button) => (button.Tag as ToggleButtonOption)?.Value ?? button.Tag;

    /// <summary>Right-clicks <paramref name="memberName"/>'s row and picks <paramref name="header"/>.</summary>
    public void PickRowMenuItem(string memberName, string header)
    {
        _driver.RightClick(Row(memberName));
        _driver.PickContextMenuItem(header);
        Settle();
    }

    /// <summary>The headers of <paramref name="memberName"/>'s right-click menu.</summary>
    public List<string> RowMenu(string memberName)
    {
        _driver.RightClick(Row(memberName));
        List<string> headers = _driver.ContextMenuHeaders();
        _driver.OpenContextMenu?.Close();
        _driver.Layout();
        return headers;
    }

    #endregion

    #region Saved data

    /// <summary>Reads <paramref name="element"/>'s file back from disk, as the tool last autosaved it.</summary>
    public T ReadSaved<T>(T element) where T : ElementSave, new()
    {
        string folder = element switch
        {
            ScreenSave => "Screens",
            ComponentSave => "Components",
            _ => "Standards",
        };
        string extension = element switch
        {
            ScreenSave => GumProjectSave.ScreenExtension,
            ComponentSave => GumProjectSave.ComponentExtension,
            _ => GumProjectSave.StandardExtension,
        };
        string path = Path.Combine(Project.ProjectFolder, folder, element.Name + "." + extension);
        return ElementReference.DeserializeElement<T>(path, GumProjectSave.NativeVersion);
    }

    /// <summary>The value <paramref name="element"/>'s <paramref name="state"/> (default when null) stores for <paramref name="variableName"/>.</summary>
    public static object? StoredValue(ElementSave element, string variableName, StateSave? state = null) =>
        (state ?? element.GetDefaultStateOrThrow()).GetVariableSave(variableName) is { SetsValue: true } variable ? variable.Value : null;

    #endregion

    public void Dispose()
    {
        try
        {
            ViewModel.VariableFilterText = string.Empty;
            _driver.Dispose();
        }
        finally
        {
            Project.Dispose();
            // Leave the singleton grid empty for the next test, as the tool is with nothing selected.
            GridManager.RefreshEntireGrid(force: true);
        }
    }
}
