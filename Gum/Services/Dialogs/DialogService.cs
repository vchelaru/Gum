using System;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Services.Dialogs;

public interface IDialogService
{
    MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null);
    public bool Show<T>(T dialogViewModel) where T : DialogViewModel;
    bool Show<T>(out T viewModel) where T : DialogViewModel;
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

    public bool Show<T>(out T viewModel) where T : DialogViewModel
    {
        viewModel = _serviceProvider.GetService<T>() ?? Locator.GetRequiredService<T>();
        return Show(viewModel);
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
        return dialogService.Show<T>(out _);
    }
}