using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Shell;
using Gum.Dialogs;
using Gum.ProjectServices;
using Gum.Services;
using Gum.Services.Dialogs;
using GumFormsPlugin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests.Harness;

/// <summary>
/// A New Project dialog in the head's own window over a temp folder, driven with real input through
/// <see cref="NewProjectDialogDriver"/>. It stands in for nothing but what a test sets on
/// <see cref="SwitchableNewProjectService"/> (restore, file creation); dispose closes the window, puts the zoom, theme
/// and stand-ins back and deletes the folder.
/// </summary>
internal sealed class NewProjectUiFixture : IDisposable
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(60);

    private readonly SwitchableNewProjectService _switchable;
    private readonly IAppScaleProvider _scale;
    private readonly double _originalFontSize;
    private IDisposable? _fontSizeFollower;

    /// <param name="service">The service behind the dialog; the test container's by default.</param>
    /// <param name="detector">The existing-code-project check; the real one by default.</param>
    public NewProjectUiFixture(INewProjectService? service = null, IExistingCodeProjectDetector? detector = null)
    {
        IServiceProvider services = TestAppBuilder.Services;
        Root = Path.Combine(Path.GetTempPath(), "GumNewProjectUi", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        Location = Path.Combine(Root, "Out");
        Directory.CreateDirectory(Location);

        _switchable = services.GetRequiredService<SwitchableNewProjectService>();
        _scale = services.GetRequiredService<IAppScaleProvider>();
        _originalFontSize = _scale.BaseFontSize;

        ViewModel = new NewProjectDialogViewModel(
            ActivatorUtilities.CreateInstance<ThemeSelectionViewModel>(services),
            service ?? _switchable,
            detector ?? services.GetRequiredService<IExistingCodeProjectDetector>(),
            services.GetRequiredService<IDialogService>())
        {
            Location = Location,
        };
        Window = new DialogWindowDriver(ViewModel);
        Dialog = new NewProjectDialogDriver(Window);
        Settle();
    }

    /// <summary>The temp folder everything the test makes lives in.</summary>
    public string Root { get; }

    /// <summary>An existing folder to create projects in, the dialog's starting Location.</summary>
    public string Location { get; }

    public NewProjectDialogViewModel ViewModel { get; }

    public DialogWindowDriver Window { get; }

    public NewProjectDialogDriver Dialog { get; }

    /// <summary>The test container's service, to stand in for the restore or file creation.</summary>
    public SwitchableNewProjectService Service => _switchable;

    /// <summary>The game folder the dialog would create for <paramref name="name"/> in <see cref="Location"/>.</summary>
    public string GameFolder(string name) => Path.Combine(Location, name);

    /// <summary>Makes a folder under <see cref="Root"/>.</summary>
    public string MakeFolder(params string[] parts)
    {
        string path = Path.Combine(new[] { Root }.Concat(parts).ToArray());
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>Waits, by the real clock, until <paramref name="condition"/> holds, pumping the window.</summary>
    public void WaitUntil(Func<bool> condition, string what)
    {
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (stopwatch.Elapsed > WaitLimit)
            {
                throw new TimeoutException($"Waited {WaitLimit.TotalSeconds:0} s for {what}. Busy: {ViewModel.IsBusy}. Error: {ViewModel.ErrorMessage}. Restore failure: {ViewModel.RestoreFailureText}. Validation: {ViewModel.ValidationMessage}. Refusal: {ViewModel.RefusalText}.");
            }
            Thread.Sleep(5);
            Window.Layout();
        }
    }

    /// <summary>Waits until the warnings and the refusal reflect the latest inputs.</summary>
    public void Settle()
    {
        for (int i = 0; i < 3; i++)
        {
            Task latest = ViewModel.ChecksRefresh;
            WaitUntil(() => latest.IsCompleted, "the folder checks");
            Window.Layout();
            if (ReferenceEquals(latest, ViewModel.ChecksRefresh))
            {
                return;
            }
        }
    }

    /// <summary>Every text a visible TextBlock shows.</summary>
    public List<string> VisibleTexts()
    {
        Window.Layout();
        return Window.Window.GetVisualDescendants().OfType<TextBlock>()
            .Where(block => block.IsEffectivelyVisible && !string.IsNullOrEmpty(block.Text))
            .Select(block => block.Text!).ToList();
    }

    /// <summary>Whether a visible text block shows exactly <paramref name="text"/>.</summary>
    public bool IsShown(string text) => VisibleTexts().Contains(text);

    /// <summary>Whether a visible text block's text starts with <paramref name="prefix"/>.</summary>
    public bool IsShownStartingWith(string prefix) => VisibleTexts().Any(text => text.StartsWith(prefix, StringComparison.Ordinal));

    /// <summary>The visible button labeled <paramref name="text"/>, or null.</summary>
    public Button? ButtonLabeled(string text)
    {
        Window.Layout();
        return Window.Window.GetVisualDescendants().OfType<Button>()
            .FirstOrDefault(button => button.IsEffectivelyVisible && (button.Content as string)?.Replace("_", "") == text);
    }

    public void ClickButton(string text)
    {
        Button button = ButtonLabeled(text) ?? throw new InvalidOperationException($"No visible {text} button.");
        Window.Click(button);
    }

    /// <summary>Sets the tool's zoom (the base font size) for this window until dispose.</summary>
    public void SetZoom(double size)
    {
        _fontSizeFollower ??= AppWideWindowInput.FollowBaseFontSize(Window.Window, _scale);
        _scale.BaseFontSize = size;
        Window.Layout();
        Dispatcher.UIThread.RunJobs();
        Window.Layout();
    }

    /// <summary>Shows the window in the light or dark theme.</summary>
    public void SetTheme(ThemeVariant theme)
    {
        Window.Window.RequestedThemeVariant = theme;
        Window.Layout();
    }

    /// <summary>A stand-in restore that records the .csproj it ran on and returns what <paramref name="results"/> lists, in order, then the last one again.</summary>
    public List<string> StandInRestore(params RestoreResult[] results)
    {
        List<string> restored = new List<string>();
        int index = 0;
        _switchable.Restore = (csproj, onOutputLine, _) =>
        {
            restored.Add(csproj);
            onOutputLine?.Invoke("restore (stand-in)");
            RestoreResult result = results[Math.Min(index, results.Length - 1)];
            index++;
            return Task.FromResult(result);
        };
        return restored;
    }

    public static RestoreResult RestoreOk() => new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 };

    public static RestoreResult RestoreFailed(string tail = "NU1101: no such package") =>
        new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = tail };

    /// <summary>The paths of every file under <paramref name="folder"/>, relative with forward slashes.</summary>
    public static List<string> RelativeFiles(string folder) =>
        Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(folder, path).Replace('\\', '/')).OrderBy(path => path, StringComparer.Ordinal).ToList();

    /// <summary>Where <paramref name="control"/> sits in the window, in the window's coordinates.</summary>
    public Rect BoundsOf(Visual control) =>
        new Rect(control.TranslatePoint(new Point(0, 0), Window.Window) ?? throw new InvalidOperationException("The control is not in the window."), control.Bounds.Size);

    /// <summary>The rows the Folders tree shows in the window, in order.</summary>
    public List<string> FolderRows() => Dialog.OpenFolders();

    /// <summary>Every folder the tree shows under <paramref name="root"/> (relative, <c>/</c>) and the marked project file.</summary>
    public static void CollectLayout(NewProjectLayoutNode root, List<string> folders, List<string> mainFiles) =>
        Collect(root, string.Empty, folders, mainFiles);

    private static void Collect(NewProjectLayoutNode node, string prefix, List<string> folders, List<string> mainFiles)
    {
        foreach (NewProjectLayoutNode child in node.Children)
        {
            string path = prefix.Length == 0 ? child.Name : prefix + "/" + child.Name;
            if (child.IsFolder)
            {
                folders.Add(path);
                Collect(child, path, folders, mainFiles);
            }
            else if (child.IsMainFile)
            {
                mainFiles.Add(path);
            }
        }
    }

    public void Dispose()
    {
        _switchable.Restore = null;
        _switchable.CreateFiles = null;
        Window.Dispose();
        _fontSizeFollower?.Dispose();
        _scale.BaseFontSize = _originalFontSize;
        Dispatcher.UIThread.RunJobs();
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
