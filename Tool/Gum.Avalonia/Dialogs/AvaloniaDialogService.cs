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

    /// <summary>Creates the service over the view registry and the container that builds view models.</summary>
    public AvaloniaDialogService(IServiceProvider serviceProvider, DialogViewRegistry viewRegistry)
    {
        _serviceProvider = serviceProvider;
        _viewRegistry = viewRegistry;
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
        Window? owner = MainWindow;
        if (owner == null)
        {
            return null;
        }

        IReadOnlyList<IStorageFile> files = RunOnUiThread(() => owner.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = options.Title,
            AllowMultiple = options.Multiselect,
            FileTypeFilter = ParseFilter(options.Filter),
            SuggestedStartLocation = StartLocation(owner, options.InitialDirectory),
        }));

        List<string> paths = files.Select(f => f.Path.LocalPath).Where(p => !string.IsNullOrEmpty(p)).ToList();
        return paths.Count == 0 ? null : paths;
    }

    /// <inheritdoc/>
    public string? SaveFile(SaveFileDialogOptions? options = null)
    {
        options ??= new SaveFileDialogOptions();
        Window? owner = MainWindow;
        if (owner == null)
        {
            return null;
        }

        IStorageFile? file = RunOnUiThread(() => owner.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = options.Title,
            SuggestedFileName = options.FileName,
            FileTypeChoices = ParseFilter(options.Filter),
            SuggestedStartLocation = StartLocation(owner, options.InitialDirectory),
        }));
        return file?.Path.LocalPath;
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

    private static Window? MainWindow =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;

    private bool? ShowModal(DialogViewModel viewModel)
    {
        Control view = _viewRegistry.CreateView(viewModel);
        DialogWindow window = new DialogWindow(viewModel, view);
        Window? owner = MainWindow;

        using CancellationTokenSource closed = new CancellationTokenSource();
        window.Closed += (_, _) => closed.Cancel();
        if (owner is { IsVisible: true })
        {
            _ = window.ShowDialog(owner);
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            window.Show();
        }

        // The contract is synchronous: pump the dispatcher until the dialog closes.
        Dispatcher.UIThread.MainLoop(closed.Token);
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

    private static IStorageFolder? StartLocation(Window owner, string? directory)
    {
        if (string.IsNullOrEmpty(directory) || !System.IO.Directory.Exists(directory))
        {
            return null;
        }
        return RunOnUiThread(() => owner.StorageProvider.TryGetFolderFromPathAsync(directory));
    }
}
