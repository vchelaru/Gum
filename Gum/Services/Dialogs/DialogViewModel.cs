using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Gum.Mvvm;
using Microsoft.Xaml.Behaviors.Core;

namespace Gum.Services.Dialogs;

public abstract class DialogViewModel : ViewModel
{
    public event EventHandler<bool>? RequestClose;
    public string? AffirmativeText { get => Get<string>(); set => Set(value); }
    public string? NegativeText { get => Get<string>(); set => Set(value); }

    public RelayCommand AffirmativeCommand { get; }
    public RelayCommand NegativeCommand { get; }

    protected DialogViewModel()
    {
        AffirmativeText = "OK";
        NegativeText = "Cancel";
        
        AffirmativeCommand = new RelayCommand(OnAffirmative, CanExecuteAffirmative);
        NegativeCommand = new RelayCommand(OnNegative, CanExecuteNegative);
    }

    public virtual void OnAffirmative()
    {
        RequestClose?.Invoke(this, true);
    }

    protected virtual void OnNegative()
    {
        RequestClose?.Invoke(this, false);
    }

    public virtual bool CanExecuteAffirmative() => true;
    public virtual bool CanExecuteNegative() => true;
}