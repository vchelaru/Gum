using System;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Services.Dialogs;

public interface IDialogService
{
    MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null);
    public bool Show<T>(T dialogViewModel) where T : DialogViewModel;
    bool Show<T>(Action<T>? initializer, out T viewModel) where T : DialogViewModel;
    string? GetUserString(string message, string? title = null, GetUserStringOptions? options = null);
}

internal class DialogService : IDialogService
{
    private readonly IMainWindowHandleProvider _handleProvider;
    private readonly IServiceProvider _serviceProvider;
    
    public DialogService(IMainWindowHandleProvider mainWindowHandleProvider,
        IServiceProvider serviceProvider)
    {
        _handleProvider = mainWindowHandleProvider;
        _serviceProvider = serviceProvider;
    }
    
    public MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null)
    {
        style ??= MessageDialogStyle.Ok;
        
        MessageDialogViewModel vm = new()
        {
            AffirmativeText = style.AffirmativeText,
            NegativeText = style.NegativeText,
            Title = title,
            Message = message,
        };

        bool? affirmative = false;
        
        DialogWindow window = CreateDialogWindow(vm);
        vm.RequestClose += (_, e) =>
        {
            affirmative = e;
            window.Close();
        };
        
        window.ShowDialog();

        return affirmative switch
        {
            true => MessageDialogResult.Affirmative,
            false => MessageDialogResult.Negative,
            _ => MessageDialogResult.Canceled
        };
    }

    private DialogWindow CreateDialogWindow(DialogViewModel dialogViewModel)
    {
        DialogWindow window = new() { DataContext = dialogViewModel };
        
        // this lets wpf center the new window on the winforms window
        _ = new WindowInteropHelper(window)
        {
            Owner = _handleProvider.GetMainWindowHandle()
        };

        
        return window;
    }

    public bool Show<T>(T dialogViewModel) where T : DialogViewModel
    {
        bool? affirmative = null;
        DialogWindow window = CreateDialogWindow(dialogViewModel);
        dialogViewModel.RequestClose += (_, e) =>
        {
            affirmative = e;
            window.Close();
        };
        
        window.ShowDialog();
        return affirmative is true;
    }

    public bool Show<T>(Action<T>? initializer, out T viewModel) where T : DialogViewModel
    {
        viewModel = _serviceProvider.GetRequiredService<T>();
        initializer?.Invoke(viewModel);
        return Show(viewModel);
    }

    public string? GetUserString(string message, string? title = null, GetUserStringOptions? options = null)
    {
        GetUserStringDialogViewModel vm = new(options)
        {
            AffirmativeText = "Ok",
            NegativeText = "Cancel",
            Title = title,
            Message = message,
        };
        
        DialogWindow window = CreateDialogWindow(vm);

        bool affirmative = false;
        vm.RequestClose += (_, e) =>
        {
            affirmative = e;
            window.Close();
        };
        
        window.ShowDialog();
        
        return affirmative ? vm.Value : null;
    }
}

public static class IDialogServiceExt
{
    public static bool ShowYesNoMessage(this IDialogService dialogService, string message, string? title = null)
    {
        return dialogService.ShowMessage(message, title, MessageDialogStyle.YesNo) is MessageDialogResult.Affirmative;
    }

    public static bool Show<T>(this IDialogService dialogService) where T : DialogViewModel
    {
        return dialogService.Show<T>(null, out _);
    }

    public static bool Show<T>(this IDialogService dialogService, Action<T> initializer) where T : DialogViewModel
    {
        return dialogService.Show<T>(initializer, out _);
    }
}