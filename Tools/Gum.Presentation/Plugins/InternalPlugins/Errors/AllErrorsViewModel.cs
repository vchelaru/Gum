using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Services;

namespace Gum.Plugins.Errors;

public partial class AllErrorsViewModel : ViewModel
{
    private readonly IClipboardService _clipboardService;

    public ObservableCollection<ErrorViewModel> Errors { get; } = [];

    public ErrorViewModel? SelectedItem
    {
        get => Get<ErrorViewModel?>();
        set
        {
            if (Set(value))
            {
                CopySelectedErrorCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string CountDescription => Errors.Count switch
    {
        0 => "0 Errors",
        1 => "1 Error",
        _ => $"{Errors.Count} Errors"
    };

    public AllErrorsViewModel(IClipboardService clipboardService)
    {
        _clipboardService = clipboardService;
        Errors.CollectionChanged += ErrorsOnCollectionChanged;
    }

    private void ErrorsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        NotifyPropertyChanged(nameof(CountDescription));
        CopyAllErrorsCommand.NotifyCanExecuteChanged();
    }

    // Clipboard.SetText throws on empty text, so blank rows must not reach it.
    private bool CanCopySelectedError() => !string.IsNullOrEmpty(SelectedItem?.ClipboardText);

    [RelayCommand(CanExecute = nameof(CanCopySelectedError))]
    private void CopySelectedError()
    {
        if (SelectedItem != null)
        {
            _clipboardService.SetText(SelectedItem.ClipboardText);
        }
    }

    private bool CanCopyAllErrors() => Errors.Any(item => !string.IsNullOrEmpty(item.ClipboardText));

    [RelayCommand(CanExecute = nameof(CanCopyAllErrors))]
    private void CopyAllErrors()
    {
        _clipboardService.SetText(
            string.Join(Environment.NewLine, Errors.Select(item => item.ClipboardText)));
    }
}
