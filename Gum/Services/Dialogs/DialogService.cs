using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace Gum.Services.Dialogs;

public interface IDialogService
{
    MessageDialogResult ShowMessage(string message, string? title = null, MessageDialogStyle? style = null);
    public bool Show<T>(T dialogViewModel) where T : DialogViewModel;
    bool Show<T>(Action<T>? initializer, out T viewModel) where T : DialogViewModel;
    string? GetUserString(string message, string? title = null, GetUserStringOptions? options = null);
    List<string>? OpenFile(OpenFileDialogOptions? options = null);
}

internal class DialogService : IDialogService
{
    private readonly IServiceProvider _serviceProvider;

    public DialogService(IServiceProvider serviceProvider)
    {
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
        Window? owner = null;
        if (Application.Current.MainWindow is { } mainWindow)
        {
            // EnsureHandle forces the Win32 HWND to be created if it doesn't exist yet.
            // IsLoaded/IsVisible may be false during startup (before app.Run() processes
            // the first render), but a valid HWND is all that's needed for ShowDialog()
            // to disable the owner and enforce z-order.
            new WindowInteropHelper(mainWindow).EnsureHandle();
            owner = mainWindow;
        }

        DialogWindow window = new()
        {
            DataContext = dialogViewModel
        };

        if(owner != null)
        {
            window.Owner = owner;
            if (owner.ActualHeight > 0)
            {
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.MaxHeight = owner.ActualHeight;
            }
            else
            {
                // Owner exists but hasn't been laid out yet (e.g. dialog shown during startup).
                // Still set Owner for z-order/modal behavior, but center on screen since
                // CenterOwner and MaxHeight both require a valid ActualHeight.
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

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

        if (options != null && options.HasRefactorCheckbox)
        {
            options.IsRefactorChecked = vm.IsRefactorChecked;
        }

        return affirmative ? vm.Value : null;
    }

    public List<string>? OpenFile(OpenFileDialogOptions? options = null)
    {
        options ??= new OpenFileDialogOptions();
        
        OpenFileDialog openFileDialog = new()
        {
            Multiselect = options.Multiselect,
            Filter = options.Filter ?? "All Files (*.*)|*.*",
            Title = options.Title ?? "Open File",
            InitialDirectory = options.InitialDirectory ?? string.Empty,
        };

        return openFileDialog.ShowDialog() is true ? openFileDialog.FileNames.ToList() : null;
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

    public static T? ShowChoices<T>(this IDialogService dialogService, string message, Dictionary<T, string> options,
        string? title = null,
        bool canCancel = false) where T : notnull
    {
        dialogService.Show(Configure, out ChoiceDialogViewModel dialog);
        return (T?)dialog.SelectedKey;

        void Configure(ChoiceDialogViewModel d)
        {
            d.SetOptions(options);
            d.Title = title ?? "Gum";
            d.Message = message;
            d.CanCancel = canCancel;
        }
    }
}