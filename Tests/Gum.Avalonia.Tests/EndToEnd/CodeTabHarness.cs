using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using CodeOutputPlugin.ViewModels;
using Gum.Avalonia.Plugins.CodeOutput;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// The head's Code tab (preview, settings grid, Generate button) in a window of its own, next to a
/// <see cref="ProjectTreeHarness"/> over the same temp project. The project is saved and reopened
/// through the tool's load path first, so the Code Output plugin reads the code settings and
/// .csproj in the project's folder. Code is written under <see cref="CodeFolder"/>, inside the
/// project folder. The fixture's dispose opens a new project, which puts the plugin's code settings
/// back to the defaults (pinned by <c>CodeSettingsOfAFinishedScenario_DoNotReachTheNextTestsProject</c>).
/// </summary>
internal sealed class CodeTabHarness : IDisposable
{
    public const string TabTitle = "Code";

    /// <summary>The Code Project Root the scenarios type, relative to the project folder.</summary>
    public const string CodeFolderName = "Code";

    private static IServiceProvider Services => TestAppBuilder.Services;

    private readonly HeadlessWindowDriver _driver;
    private readonly bool _wasSelected;

    /// <param name="beforeLoad">Runs after the project is first saved and before it is reopened, for files the load should see (a .csproj, a .codsj).</param>
    public CodeTabHarness(Action<string>? beforeLoad = null)
    {
        Tree = new ProjectTreeHarness();
        try
        {
            Tab = TabManager.AllTabs.SingleOrDefault(candidate => candidate.Title == TabTitle)
                ?? throw new InvalidOperationException(
                    $"The tool has no {TabTitle} tab. Tabs: {string.Join(", ", TabManager.AllTabs.Select(candidate => candidate.Title))}");
            View = (CodeOutputView)Tab.Content;
            beforeLoad?.Invoke(Tree.Project.ProjectFolder);
            Tree.Project.SaveAndReload();
            Tree.TreeManager.RefreshUi();
            _wasSelected = Tab.IsSelected;
            // The preview only follows the selection while the tab is the one shown.
            Tab.IsSelected = true;
            _driver = new HeadlessWindowDriver(View, width: 1000, height: 800, framesFolderName: "GumCodeTab");
        }
        catch
        {
            if (Tab != null)
            {
                Tab.IsSelected = _wasSelected;
            }
            Tree.Dispose();
            throw;
        }
    }

    public ProjectTreeHarness Tree { get; }

    public ToolProjectFixture Project => Tree.Project;

    public AvaloniaPluginTab Tab { get; }

    public CodeOutputView View { get; }

    public CodeWindowViewModel ViewModel => (CodeWindowViewModel)(View.DataContext
        ?? throw new InvalidOperationException("The Code tab has no view model."));

    public HeadlessWindowDriver Input => _driver;

    public AvaloniaTabManager TabManager => (AvaloniaTabManager)Services.GetRequiredService<ITabManager>();

    /// <summary>The folder the scenarios generate into.</summary>
    public string CodeFolder => Path.Combine(Project.ProjectFolder, CodeFolderName);

    /// <summary>What the preview shows.</summary>
    public string Preview
    {
        get
        {
            _driver.Layout();
            return View.CodeTextBox.Text ?? "";
        }
    }

    /// <summary>A file under <see cref="CodeFolder"/>; <paramref name="relativePath"/> uses '/'.</summary>
    public string CodeFile(string relativePath) =>
        Path.Combine(CodeFolder, relativePath.Replace('/', Path.DirectorySeparatorChar));

    #region Settings grid

    public DataUiGrid Grid => View.SettingsGrid;

    public List<string> ShownMemberNames()
    {
        _driver.Layout();
        return Grid.Categories.SelectMany(category => category.Members).Select(member => member.Name).ToList();
    }

    public InstanceMember Member(string memberName) =>
        Grid.Categories.SelectMany(category => category.Members).FirstOrDefault(member => member.Name == memberName)
            ?? throw new InvalidOperationException($"The Code tab shows no {memberName} row; it shows [{string.Join(", ", ShownMemberNames())}].");

    public SingleDataUiContainer Row(string memberName)
    {
        _driver.Layout();
        InstanceMember member = Member(memberName);
        SingleDataUiContainer row = Grid.LiveContainers.FirstOrDefault(container => container.Member == member)
            ?? throw new InvalidOperationException($"The {memberName} row has no live editor.");
        row.BringIntoView();
        _driver.Layout();
        return row;
    }

    /// <summary>Types into a single-line text row and presses Enter.</summary>
    public void TypeAndEnter(string memberName, string text)
    {
        _driver.TypeAndEnter(TextBoxOf(memberName), text);
        _driver.Layout();
    }

    /// <summary>Types into a text row (single or multi-line) and tabs out, which commits it.</summary>
    public void TypeAndLeave(string memberName, string text)
    {
        _driver.TypeInto(TextBoxOf(memberName), text);
        _driver.Press(Key.Tab, PhysicalKey.Tab);
        _driver.Layout();
    }

    /// <summary>Opens a combo row and picks <paramref name="item"/>, as a click in its list does.</summary>
    public void PickComboItem(string memberName, string item)
    {
        ComboBox combo = Row(memberName).GetVisualDescendants().OfType<ComboBox>().FirstOrDefault()
            ?? throw new InvalidOperationException($"The {memberName} row has no combo box.");
        combo.IsDropDownOpen = true;
        _driver.Layout();
        object match = combo.Items.Cast<object?>().FirstOrDefault(candidate => candidate?.ToString() == item)
            ?? throw new InvalidOperationException($"The {memberName} combo has no \"{item}\"; it has [{string.Join(", ", combo.Items.Cast<object?>())}].");
        combo.SelectedItem = match;
        combo.IsDropDownOpen = false;
        _driver.Layout();
    }

    /// <summary>Clicks a check box row.</summary>
    public void ClickCheckBox(string memberName)
    {
        CheckBox box = Row(memberName).GetVisualDescendants().OfType<CheckBox>().FirstOrDefault()
            ?? throw new InvalidOperationException($"The {memberName} row has no check box.");
        _driver.Click(box);
        _driver.Layout();
    }

    private TextBox TextBoxOf(string memberName) =>
        Row(memberName).GetVisualDescendants().OfType<TextBox>().FirstOrDefault()
            ?? throw new InvalidOperationException($"The {memberName} row has no text box.");

    #endregion

    #region Gestures

    /// <summary>Selects <paramref name="element"/> with a click on its tree row.</summary>
    public void Select(ElementSave element)
    {
        Tree.Click(Tree.NodeFor(element));
        _driver.Layout();
    }

    /// <summary>
    /// Types the Code Project Root, picks the output library and instantiation type, and switches
    /// the selected element to manual generation: the setup a user does before the first Generate.
    /// </summary>
    public void SetUpManualGeneration(string library = "Gum Forms (recommended)", string instantiation = "Reference loaded Gum Project")
    {
        // The tab offers setup instead of its settings when any folder above the project holds a
        // .csproj, which a stray one in the temp folder does on some machines.
        if (ShowsButton("Manual"))
        {
            ClickButton("Manual");
        }
        TypeAndEnter("Code Project Root", CodeFolderName);
        PickComboItem("Output Library", library);
        PickComboItem("Object Instantiation Type", instantiation);
        PickComboItem("Generation Behavior", "GenerateManually");
    }

    /// <summary>Clicks a toggle or button of the tab by its text ("Selected", "All", "Object", "State", "Manual", "Auto").</summary>
    public void ClickButton(string text)
    {
        _driver.Layout();
        Button button = View.GetVisualDescendants().OfType<Button>()
            .FirstOrDefault(candidate => candidate.Content as string == text && candidate.IsEffectivelyVisible)
            ?? throw new InvalidOperationException($"The Code tab shows no {text} button.");
        _driver.Click(button);
        _driver.Layout();
    }

    /// <summary>Whether the tab shows a button with this text.</summary>
    public bool ShowsButton(string text)
    {
        _driver.Layout();
        return View.GetVisualDescendants().OfType<Button>()
            .Any(candidate => candidate.Content as string == text && candidate.IsEffectivelyVisible);
    }

    /// <summary>Clicks Generate; the message it shows is answered with OK.</summary>
    public void ClickGenerate()
    {
        _driver.Layout();
        Project.Dialogs.AnswerNextMessage(Gum.Services.Dialogs.MessageDialogResult.Affirmative);
        _driver.Click(View.GenerateButton);
        _driver.Layout();
    }

    /// <summary>Saves and opens the project again through the tool's load path; element objects taken before are stale.</summary>
    public void Reopen()
    {
        Project.SaveAndReload();
        Tree.TreeManager.RefreshUi();
        _driver.Layout();
    }

    public void Undo()
    {
        Tree.Undo();
        _driver.Layout();
    }

    public void Redo()
    {
        Tree.Redo();
        _driver.Layout();
    }

    #endregion

    public void AssertOracles()
    {
        _driver.Layout();
        Tree.AssertOracles();
    }

    public void Dispose()
    {
        try
        {
            // The tab's view model is the tool's for the session; put back what a scenario toggles.
            ViewModel.WhichElementsToGenerate = WhichElementsToGenerate.SelectedOnly;
            ViewModel.WhatToView = WhatToView.SelectedElement;
            Tab.IsSelected = _wasSelected;
            _driver.Dispose();
        }
        finally
        {
            Tree.Dispose();
        }
    }
}
