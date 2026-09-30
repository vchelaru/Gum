using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Gum.Avalonia.Diagnostics;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Dialogs;

/// <summary>
/// Avalonia implementation of <see cref="IDialogService"/>. The contract is synchronous (callers
/// expect the answer on return), while Avalonia's dialogs and file pickers are asynchronous, so
/// each call shows the dialog and then runs a nested dispatcher loop until it closes.
/// </summary>
public class AvaloniaDialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DialogViewRegistry _viewRegistry;
    private readonly List<DialogWindow> _openDialogs;
    private readonly IFilePickers _filePickers;
    private readonly Func<Window?> _mainWindow;

    /// <summary>Creates the service over the view registry and the container that builds view models.</summary>
    public AvaloniaDialogService(IServiceProvider serviceProvider, DialogViewRegistry viewRegistry)
        : this(serviceProvider, viewRegistry, new StorageProviderFilePickers(), () => AppMainWindow)
    {
    }

    /// <summary>
    /// Tests pass <paramref name="filePickers"/> to see which window a picker is opened over, and
    /// <paramref name="mainWindow"/> because the headless app has no main window.
    /// </summary>
    internal AvaloniaDialogService(IServiceProvider serviceProvider, DialogViewRegistry viewRegistry,
        IFilePickers filePickers, Func<Window?> mainWindow)
    {
        _serviceProvider = serviceProvider;
        _viewRegistry = viewRegistry;
        _openDialogs = new List<DialogWindow>();
        _filePickers = filePickers;
        _mainWindow = mainWindow;
    }

    /// <inheritdoc/>
    public MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null)
    {
        style ??= MessageDialogStyle.Ok;
        MessageDialogViewModel viewModel = new MessageDialogViewModel
        {
            AffirmativeText = style.AffirmativeText,
            NegativeText = style.NegativeText,
            Title = title,
            Message = message,
        };

        return ShowModal(viewModel) switch
        {
            true => MessageDialogResult.Affirmative,
            false => MessageDialogResult.Negative,
            _ => MessageDialogResult.Canceled,
        };
    }

    /// <inheritdoc/>
    public bool Show<T>(T dialogViewModel) where T : DialogViewModel => ShowModal(dialogViewModel) is true;

    /// <inheritdoc/>
    public bool Show<T>(Action<T>? initializer, out T viewModel) where T : DialogViewModel
    {
        viewModel = _serviceProvider.GetRequiredService<T>();
        initializer?.Invoke(viewModel);
        return Show(viewModel);
    }

    /// <inheritdoc/>
    public string? GetUserString(string message, string? title = null, GetUserStringOptions? options = null)
    {
        GetUserStringDialogViewModel viewModel = new GetUserStringDialogViewModel(options)
        {
            AffirmativeText = "Ok",
            NegativeText = "Cancel",
            Title = title,
            Message = message,
        };
        viewModel.Validate();
        return ShowModal(viewModel) is true ? viewModel.Value : null;
    }

    /// <inheritdoc/>
    public List<string>? OpenFile(OpenFileDialogOptions? options = null)
    {
        options ??= new OpenFileDialogOptions();
        Window? owner = CurrentOwner;
        if (owner == null)
        {
            return null;
        }

        IReadOnlyList<string> paths = RunOnUiThread(() => _filePickers.OpenFileAsync(owner, new OpenFilePickerRequest(
            options.Title, options.Multiselect, ParseFilter(options.Filter), ExistingDirectory(options.InitialDirectory))));
        return paths.Count == 0 ? null : paths.ToList();
    }

    /// <inheritdoc/>
    public string? SaveFile(SaveFileDialogOptions? options = null)
    {
        options ??= new SaveFileDialogOptions();
        Window? owner = CurrentOwner;
        if (owner == null)
        {
            return null;
        }

        return RunOnUiThread(() => _filePickers.SaveFileAsync(owner, new SaveFilePickerRequest(
            options.Title, options.FileName, ParseFilter(options.Filter), ExistingDirectory(options.InitialDirectory))));
    }

    /// <inheritdoc/>
    public string? OpenFolder(OpenFolderDialogOptions? options = null)
    {
        options ??= new OpenFolderDialogOptions();
        Window? owner = CurrentOwner;
        if (owner == null)
        {
            return null;
        }

        return RunOnUiThread(() => _filePickers.OpenFolderAsync(owner, new OpenFolderPickerRequest(
            options.Title, ExistingDirectory(options.InitialDirectory))));
    }

    /// <summary>
    /// Turns a WPF-style filter ("PNG Files (*.png)|*.png|All Files (*.*)|*.*") into picker types.
    /// Pure, so it is unit-testable.
    /// </summary>
    public static List<FilePickerFileType> ParseFilter(string filter)
    {
        List<FilePickerFileType> types = new List<FilePickerFileType>();
        string[] parts = filter.Split('|');
        for (int i = 0; i + 1 < parts.Length; i += 2)
        {
            types.Add(new FilePickerFileType(parts[i])
            {
                Patterns = parts[i + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            });
        }
        return types;
    }

    /// <summary>The dialogs this service has open, oldest first. Exposed for tests.</summary>
    internal IReadOnlyList<DialogWindow> OpenDialogs => _openDialogs;

    /// <summary>
    /// The window a new dialog or file picker is modal over: the topmost open dialog, else the main
    /// window. Avalonia's ShowDialog disables only its owner, so owning a prompt by the main window
    /// left the dialog that raised it clickable (#5538).
    /// </summary>
    private Window? CurrentOwner => _openDialogs.LastOrDefault(dialog => dialog.IsVisible) ?? _mainWindow();

    private static Window? AppMainWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    private bool? ShowModal(DialogViewModel viewModel)
    {
        string dialogKind = viewModel.GetType().Name;
        UiFreezeWatchdog.RecordStep($"ShowModal({dialogKind}): begin");

        Control view = _viewRegistry.CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);
        Window? owner = CurrentOwner;
        window.FitHeightToScreen(owner);
        // An unattended run's window sits off-screen without focus; its dialogs stay there with it.
        window.ShowActivated = owner?.ShowActivated ?? true;

        using CancellationTokenSource closed = new CancellationTokenSource();
        _openDialogs.Add(window);
        window.Closed += (_, _) =>
        {
            _openDialogs.Remove(window);
            closed.Cancel();
        };
        if (owner is { IsVisible: true })
        {
            UiFreezeWatchdog.RecordStep($"ShowModal({dialogKind}): calling ShowDialog(owner)");
            _ = window.ShowDialog(owner);
        }
        else
        {
            UiFreezeWatchdog.RecordStep($"ShowModal({dialogKind}): calling Show() (no owner)");
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }

        // The contract is synchronous: pump the dispatcher until the dialog closes.
        UiFreezeWatchdog.RecordStep($"ShowModal({dialogKind}): entering nested MainLoop");
        Dispatcher.UIThread.MainLoop(closed.Token);
        UiFreezeWatchdog.RecordStep($"ShowModal({dialogKind}): MainLoop returned");
        return window.Result;
    }

    private static T RunOnUiThread<T>(Func<Task<T>> start)
    {
        Task<T> task = start();
        using CancellationTokenSource done = new CancellationTokenSource();
        task.ContinueWith(_ => done.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
        if (!task.IsCompleted)
        {
            Dispatcher.UIThread.MainLoop(done.Token);
        }
        return task.GetAwaiter().GetResult();
    }

    private static string? ExistingDirectory(string? directory) =>
        !string.IsNullOrEmpty(directory) && System.IO.Directory.Exists(directory) ? directory : null;
}
