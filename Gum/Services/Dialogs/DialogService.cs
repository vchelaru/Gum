using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger _logger;
    
    public DialogService(IServiceProvider serviceProvider, ILogger<DialogService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;       
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
        if (Application.Current.MainWindow is { IsLoaded: true } mainWindow)
        {
            owner = mainWindow; 
        }
        else
        {
            _logger.LogWarning("Showing dialog before main window is loaded.");
        }

        DialogWindow window = new()
        {
            DataContext = dialogViewModel, 
            Owner = owner,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            MaxHeight = Application.Current.MainWindow!.ActualHeight
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