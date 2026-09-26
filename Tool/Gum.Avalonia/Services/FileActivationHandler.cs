using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Gum.Startup;

namespace Gum.Avalonia.Services;

/// <summary>
/// Forwards the platform's open-document activations (a <c>.gumx</c> double-clicked in the macOS
/// Finder, #5130) to <see cref="IProjectOpenRequestRouter"/>.
/// </summary>
internal sealed class FileActivationHandler
{
    // Lazy: the handler is attached in App.Initialize, before the tool's services are needed.
    private readonly Lazy<IProjectOpenRequestRouter> _router;

    public FileActivationHandler(Lazy<IProjectOpenRequestRouter> router)
    {
        _router = router;
    }

    public void HandleActivated(object? sender, ActivatedEventArgs e)
    {
        if (e is not FileActivatedEventArgs fileArgs)
        {
            return;
        }

        List<string> paths = new List<string>();
        foreach (IStorageItem item in fileArgs.Files)
        {
            if (item is IStorageFile && item.TryGetLocalPath() is { } path)
            {
                paths.Add(path);
            }
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            _ = RouteAsync(paths);
        }
        else
        {
            Dispatcher.UIThread.Post(() => _ = RouteAsync(paths));
        }
    }

    private async Task RouteAsync(List<string> paths)
    {
        try
        {
            await _router.Value.RequestOpenAsync(paths);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("Opening a project from the OS failed: " + exception);
        }
    }
}
