using System;
using System.Windows.Input;
using Gum.Mvvm;
using Microsoft.Xaml.Behaviors.Core;

namespace Gum.Services.Dialogs;

public abstract class DialogViewModel : ViewModel
{
    public event EventHandler<bool>? RequestClose;
    public string? AffirmativeText { get => Get<string>(); set => Set(value); }
    public string? NegativeText { get => Get<string>(); set => Set(value); }

    public ICommand AffirmativeCommand { get; }
    public ICommand NegativeCommand { get; }

    protected DialogViewModel()
    {
        AffirmativeCommand = new ActionCommand(Affirmative);
        NegativeCommand = new ActionCommand(Negative);
    }

    private void Affirmative()
    {
        OnAffirmative();
        RequestClose?.Invoke(this, true);
    }

    private void Negative()
    {
        OnNegative();
        RequestClose?.Invoke(this, false);
    }
    
    protected virtual void OnAffirmative(){}
    protected virtual void OnNegative(){}
}