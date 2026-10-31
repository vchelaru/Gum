using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Dialogs.Views;
using Gum.Dialogs;
using Shouldly;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// The New Project dialog's controls in the head's own window, found and used the way a person does: type into the
/// name and location boxes, tick check boxes, pick from the drop-downs with the arrow keys, click Browse, OK and
/// Cancel, and read the folder tree and the command line the view shows.
/// </summary>
internal sealed class NewProjectDialogDriver
{

    private static readonly TimeSpan CreationTimeout = TimeSpan.FromSeconds(90);

    private readonly DialogWindowDriver _window;

    public NewProjectDialogDriver(DialogWindowDriver window)
    {
        _window = window;
    }

    /// <summary>The dialog's view model, for what the view does not show.</summary>
    public NewProjectDialogViewModel ViewModel => (NewProjectDialogViewModel)_window.Window.DataContext!;

    /// <summary>What the window closed with, or null while it is open.</summary>
    public bool? Result => _window.Result;

    // The two text boxes, the name above the location (the drop-downs carry hidden ones).
    private List<TextBox> TextBoxesTopDown() => _window.FindAll<TextBox>().Where(box => box.IsEffectivelyVisible && box.GetVisualAncestors().OfType<ComboBox>().Count() == 0)
        .OrderBy(box => box.TranslatePoint(new Point(0, 0), _window.Window)!.Value.Y).ToList();

    public TextBox NameBox => TextBoxesTopDown()[0];

    public TextBox LocationBox => TextBoxesTopDown()[1];

    public ComboBox FileFormatBox => _window.Find<ComboBox>(combo => combo.ItemsSource is IEnumerable<string> items && items.Contains(NewProjectDialogViewModel.JsonFormat));

    public ComboBox ThemeBox => _window.Find<ComboBox>(combo => combo.ItemsSource is IEnumerable<string> items && !items.Contains(NewProjectDialogViewModel.JsonFormat));

    public ComboBox PlatformBox => _window.Find<ComboBox>(combo => combo.ItemsSource is IEnumerable<NewProjectPlatformOption>);

    public Button OkButton => _window.AffirmativeButton;

    public Button CancelButton => _window.Find<Button>(button => button.Name == DialogWindow.NegativeButtonName);

    /// <summary>Types <paramref name="name"/> over the project name.</summary>
    public void TypeName(string name) => _window.TypeInto(NameBox, name);

    /// <summary>Types <paramref name="location"/> over the Location box.</summary>
    public void TypeLocation(string location) => _window.TypeInto(LocationBox, location);

    /// <summary>The check box whose label reads <paramref name="label"/>.</summary>
    public CheckBox CheckBoxLabeled(string label) => _window.Find<CheckBox>(box => LabelOf(box) == label);

    /// <summary>Clicks the check box labeled <paramref name="label"/> when it is not already in the wanted state.</summary>
    public void SetChecked(string label, bool isChecked)
    {
        CheckBox box = CheckBoxLabeled(label);
        if (box.IsChecked != isChecked)
        {
            _window.Click(box);
        }
        CheckBoxLabeled(label).IsChecked.ShouldBe(isChecked, $"\"{label}\" after the click");
    }

    /// <summary>Picks the entry of a closed drop-down for which <paramref name="isTarget"/> holds, with the Up and Down keys.</summary>
    public void Choose(ComboBox combo, Func<object?, bool> isTarget)
    {
        combo.Focus();
        _window.Layout();
        for (int i = 0; i < combo.ItemCount; i++)
        {
            _window.Press(Key.Up, PhysicalKey.ArrowUp);
        }
        for (int i = 0; i < combo.ItemCount && !isTarget(combo.SelectedItem); i++)
        {
            _window.Press(Key.Down, PhysicalKey.ArrowDown);
        }
        isTarget(combo.SelectedItem).ShouldBeTrue($"the drop-down reached the wanted entry; it shows {combo.SelectedItem}");
    }

    /// <summary>Picks the file format labeled <paramref name="format"/>.</summary>
    public void ChooseFileFormat(string format) => Choose(FileFormatBox, item => (item as string) == format);

    /// <summary>Picks the Forms theme named <paramref name="theme"/>.</summary>
    public void ChooseTheme(string theme) => Choose(ThemeBox, item => (item as string) == theme);

    /// <summary>Picks the platform whose <c>--platform</c> name is <paramref name="platformName"/>.</summary>
    public void ChoosePlatform(string platformName) => Choose(PlatformBox, item => (item as NewProjectPlatformOption)?.Name == platformName);

    /// <summary>Clicks Browse.</summary>
    public void ClickBrowse() => _window.ClickButton("Browse…");

    /// <summary>Opens the Folders section and all its folders; returns the rows the tree shows, in order.</summary>
    public List<string> OpenFolders()
    {
        OpenSection(NewProjectDialogView.FoldersExpanderName);
        ViewModel.SetFolderTreeExpanded(true);
        _window.Layout();
        return _window.Find<TreeView>().GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text ?? string.Empty).ToList();
    }

    /// <summary>Opens the Command line section; returns the command the view shows.</summary>
    public string OpenCommandLine()
    {
        OpenSection(NewProjectDialogView.CommandLineExpanderName);
        return CommandLine;
    }

    /// <summary>The <c>gumcli new</c> command the open Command line section shows.</summary>
    public string CommandLine
    {
        get
        {
            SelectableTextBlock command = _window.Find<SelectableTextBlock>(text => text.Text != null && text.Text.StartsWith("gumcli new"));
            command.IsEffectivelyVisible.ShouldBeTrue("the Command line section is open");
            return command.Text!;
        }
    }

    /// <summary>Clicks OK and waits for the window to close (a code project creates its files first).</summary>
    public void ClickOk()
    {
        ClickSettled(OkButton);
        Wait.Until(() => _window.Result != null, CreationTimeout, () =>
            $"the dialog to close. OK enabled: {OkButton.IsEffectivelyEnabled}. Busy: {ViewModel.IsBusy}. Error: {ViewModel.ErrorMessage}. Restore failure: {ViewModel.RestoreFailureText}. Validation: {ViewModel.ValidationMessage}. Refusal: {ViewModel.RefusalText}.",
            _window);
        _window.Result.ShouldBe(true);
    }

    /// <summary>Clicks OK and returns at once, for a flow that stays open (busy, a failure panel).</summary>
    public void ClickOkWithoutWaiting() => ClickSettled(OkButton);

    /// <summary>Presses and releases a key in the window.</summary>
    public void Press(Key key, PhysicalKey physicalKey) => _window.Press(key, physicalKey);

    /// <summary>Clicks the button whose text is <paramref name="text"/>.</summary>
    public void ClickButton(string text) => _window.ClickButton(text);

    /// <summary>Clicks Cancel; the window closes.</summary>
    public void ClickCancel()
    {
        ClickSettled(CancelButton);
        _window.Result.ShouldBe(false);
    }

    // A person clicks a footer button long after the folder checks of the last edit finished. The checks run on a
    // worker and their banner can grow the window (moving the footer) at any moment, so a click made right after an
    // edit would land on the old position and be lost: wait for them first.
    private void ClickSettled(Control button)
    {
        for (int i = 0; i < 3; i++)
        {
            System.Threading.Tasks.Task latest = ViewModel.ChecksRefresh;
            Wait.Until(() => latest.IsCompleted, CreationTimeout, () => "the folder checks", _window);
            _window.Layout();
            if (ReferenceEquals(latest, ViewModel.ChecksRefresh))
            {
                break;
            }
        }
        _window.Click(button);
    }

    private void OpenSection(string name)
    {
        NewProjectDialogView.Disclosure section = _window.Find<NewProjectDialogView.Disclosure>(disclosure => disclosure.Name == name);
        if (!section.IsExpanded)
        {
            _window.Click((Control)section.Children[0]);
        }
        section.IsExpanded.ShouldBeTrue($"{name} opens on a click");
        _window.Layout();
    }

    private static string? LabelOf(CheckBox box) => box.Content switch
    {
        string text => text,
        Control label => label.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault()?.Text,
        _ => null,
    };

    private static class Wait
    {
        public static void Until(Func<bool> condition, TimeSpan timeout, Func<string> what, DialogWindowDriver window)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (!condition())
            {
                if (stopwatch.Elapsed > timeout)
                {
                    throw new TimeoutException($"Waited {timeout.TotalSeconds:0} s for {what()}");
                }
                Thread.Sleep(20);
                Dispatcher.UIThread.RunJobs();
                window.Layout();
            }
        }
    }
}
