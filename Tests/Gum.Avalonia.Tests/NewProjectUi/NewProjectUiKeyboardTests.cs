using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Tests.Harness;
using Gum.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests.NewProjectUi;

/// <summary>
/// The New Project dialog from the keyboard: Tab order, Enter (the OK button is the window's default button, so it
/// creates from any text box), Escape (Cancel, or stop the work while busy), Space on check boxes.
/// </summary>
[Trait("Category", "NewProjectUi")]
public class NewProjectUiKeyboardTests
{
    private static string? Describe(IInputElement? focused)
    {
        string? automationName = focused is Control control ? global::Avalonia.Automation.AutomationProperties.GetName(control) : null;
        return focused switch
        {
            FluentIcons.Avalonia.FluentIcon => null,
            TextBox box when box.Name == null && box.GetVisualAncestors().OfType<ComboBox>().Any() => null,
            TextBox { Text: var text } when text != null && !text.Contains(Path.DirectorySeparatorChar) => "Project name",
            TextBox => "Location",
            ComboBox combo when combo.ItemsSource is IEnumerable<string> formats && formats.Contains(NewProjectUiText.JsonFormat) => "File format",
            ComboBox combo when combo.ItemsSource is IEnumerable<NewProjectPlatformOption> => "Platform",
            ComboBox => "Theme",
            CheckBox { Content: StackPanel label } => label.GetVisualDescendants().OfType<TextBlock>().First().Text,
            CheckBox { Content: string text } => text,
            Button { Name: DialogWindow.AffirmativeButtonName } => "OK",
            Button { Name: DialogWindow.NegativeButtonName } => "Cancel",
            Control when !string.IsNullOrEmpty(automationName) => automationName,
            Button { Content: string text } => text,
            _ => null,
        };
    }

    // Tab (or Shift+Tab) from the name box until the focus is back on it; the controls reached, in order.
    private static List<string> WalkFocus(NewProjectUiFixture fixture, bool backwards = false)
    {
        fixture.Dialog.NameBox.Focus();
        fixture.Window.Layout();
        List<string> visited = new List<string>();
        for (int i = 0; i < 60; i++)
        {
            string? description = Describe(fixture.Window.Window.FocusManager!.GetFocusedElement());
            if (description != null)
            {
                if (visited.Count > 0 && description == visited[0])
                {
                    return visited;
                }
                visited.Add(description);
            }
            fixture.Window.Press(Key.Tab, PhysicalKey.Tab, backwards ? RawInputModifiers.Shift : RawInputModifiers.None);
        }
        throw new InvalidOperationException("The focus never came back to the first control: " + string.Join(", ", visited));
    }

    [AvaloniaFact]
    public void Tab_VisitsEveryControlOnce_InReadingOrder_AndWrapsAround_GumOnly()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.CheckBoxLabeled(NewProjectUiText.IncludeDemo);

        List<string> visited = WalkFocus(fixture);

        visited.ShouldBe(new[]
        {
            "Project name", "File format", "Location", "Browse for a folder", NewProjectUiText.IncludeForms, "Theme",
            NewProjectUiText.IncludeDemo, NewProjectUiText.CreateCodeProject, "Folders", "Command line", "OK", "Cancel",
        });
        visited.Distinct().Count().ShouldBe(visited.Count, "no control twice");
    }

    [AvaloniaFact]
    public void Tab_WithACodeProjectAndFormsOff_SkipsTheHiddenThemeAndDemo_AndAddsThePlatformControls()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.SetChecked(NewProjectUiText.IncludeForms, false);
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);

        List<string> visited = WalkFocus(fixture);

        visited.ShouldBe(new[]
        {
            "Project name", "File format", "Location", "Browse for a folder", NewProjectUiText.IncludeForms, NewProjectUiText.CreateCodeProject,
            "Platform", NewProjectUiText.Stable, NewProjectUiText.RestorePackages, NewProjectUiText.RecommendedPackages,
            "Folders", "Command line", "OK", "Cancel",
        });
    }

    [AvaloniaFact]
    public void ShiftTab_VisitsTheSameControls_InReverse()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        List<string> forward = WalkFocus(fixture);

        List<string> backward = WalkFocus(fixture, backwards: true);

        backward.ShouldBe(new[] { forward[0] }.Concat(Enumerable.Reverse(forward.Skip(1))).ToList());
    }

    [AvaloniaFact]
    public void Tab_OnTheRestoreFailurePanel_ReachesRetryContinueBackAndCancel_NotTheLockedForm()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.StandInRestore(NewProjectUiFixture.RestoreFailed());
        fixture.Settle();
        fixture.Dialog.ClickOkWithoutWaitingForClose();
        fixture.WaitUntil(() => fixture.ViewModel.IsRestoreFailed, "the restore failure");
        fixture.ButtonLabeled("Retry")!.Focus();
        fixture.Window.Layout();

        List<string> visited = new List<string>();
        for (int i = 0; i < 12; i++)
        {
            string? description = Describe(fixture.Window.Window.FocusManager!.GetFocusedElement());
            if (description != null && (visited.Count == 0 || description != visited[0]))
            {
                visited.Add(description);
            }
            else if (description != null)
            {
                break;
            }
            fixture.Window.Press(Key.Tab, PhysicalKey.Tab);
        }

        visited.ShouldBe(new[] { "Retry", "Continue without restore", "Back", "Cancel" });
    }

    [AvaloniaFact]
    public void Enter_InTheNameBox_WithAValidGumOnlyForm_AcceptsTheDialog()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("Game");
        fixture.Settle();

        fixture.Window.Press(Key.Enter, PhysicalKey.Enter);

        fixture.Dialog.Result.ShouldBe(true);
        fixture.ViewModel.IsCodeProjectCreated.ShouldBeFalse("a Gum-only project is created by the tool after the dialog closes");
        fixture.ViewModel.GumProjectPath.ShouldBe(Path.Combine(fixture.Location, "Game", "Game.gumj"));
    }

    [AvaloniaFact]
    public void Enter_InTheLocationBox_WithACodeProject_CreatesTheFilesAndAccepts()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        List<string> restored = fixture.StandInRestore(NewProjectUiFixture.RestoreOk());
        fixture.Dialog.TypeName("Game");
        fixture.Settle();
        fixture.Dialog.SetChecked(NewProjectUiText.CreateCodeProject, true);
        fixture.Dialog.TypeLocation(fixture.Location);
        fixture.Settle();

        fixture.Window.Press(Key.Enter, PhysicalKey.Enter);
        fixture.WaitUntil(() => fixture.Dialog.Result != null, "the dialog to close");

        fixture.Dialog.Result.ShouldBe(true);
        restored.ShouldHaveSingleItem();
        File.Exists(Path.Combine(fixture.GameFolder("Game"), "Game.csproj")).ShouldBeTrue();
    }

    [AvaloniaFact]
    public void Enter_OnTheFocusedCancelButton_Cancels()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.CancelButton.Focus();
        fixture.Window.Layout();

        fixture.Window.Press(Key.Enter, PhysicalKey.Enter);

        fixture.Dialog.Result.ShouldBe(false, "Enter presses the focused button, not the default one");
    }

    [AvaloniaFact]
    public void Enter_OnTheFoldersHeading_OpensTheSection_AndDoesNotCreate()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        NewProjectDialogView.Disclosure folders = fixture.Window.Find<NewProjectDialogView.Disclosure>(d => d.Name == NewProjectDialogView.FoldersExpanderName);
        ((Control)folders.Children[0]).Focus();
        fixture.Window.Layout();

        fixture.Window.Press(Key.Enter, PhysicalKey.Enter);

        folders.IsExpanded.ShouldBeTrue();
        fixture.Dialog.Result.ShouldBeNull();
        fixture.Window.Press(Key.Space, PhysicalKey.Space);
        folders.IsExpanded.ShouldBeFalse("Space closes it again");
    }

    [AvaloniaFact]
    public void Escape_WhenIdle_CancelsFromAnyControl()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.TypeName("Typed");
        fixture.Dialog.OpenFolders();
        fixture.Window.Press(Key.Escape, PhysicalKey.Escape);

        fixture.Dialog.Result.ShouldBe(false);
        fixture.ViewModel.IsCodeProjectCreated.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void Escape_WithADropDownFocused_CancelsToo()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.FileFormatBox.Focus();
        fixture.Window.Layout();

        fixture.Window.Press(Key.Escape, PhysicalKey.Escape);

        fixture.Dialog.Result.ShouldBe(false);
    }

    [AvaloniaTheory]
    [InlineData(NewProjectUiText.IncludeForms)]
    [InlineData(NewProjectUiText.CreateCodeProject)]
    public void Space_OnAFocusedCheckBox_TogglesIt_AndTheCommandLineFollows(string label)
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        string before = fixture.Dialog.OpenCommandLine();
        CheckBox box = fixture.Dialog.CheckBoxLabeled(label);
        bool wasChecked = box.IsChecked == true;
        box.Focus();
        fixture.Window.Layout();

        fixture.Window.Press(Key.Space, PhysicalKey.Space);

        fixture.Dialog.CheckBoxLabeled(label).IsChecked.ShouldBe(!wasChecked);
        fixture.Dialog.CommandLine.ShouldNotBe(before);

        fixture.Window.Press(Key.Space, PhysicalKey.Space);
        fixture.Dialog.CommandLine.ShouldBe(before, "toggling twice is back where it started");
    }

    [AvaloniaFact]
    public void ArrowKeysOnTheClosedFileFormatDropDown_ChangeTheFormat_AndTheProjectPath()
    {
        using NewProjectUiFixture fixture = new NewProjectUiFixture();
        fixture.Dialog.FileFormatBox.Focus();
        fixture.Window.Layout();

        fixture.Window.Press(Key.Down, PhysicalKey.ArrowDown);

        fixture.Dialog.FileFormatBox.SelectedItem.ShouldBe(NewProjectUiText.XmlFormat);
        fixture.ViewModel.GumProjectPath.ShouldEndWith(".gumx");
        fixture.Window.Press(Key.Up, PhysicalKey.ArrowUp);
        fixture.ViewModel.GumProjectPath.ShouldEndWith(".gumj");
    }
}
