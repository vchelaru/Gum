using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Gum.Mvvm;

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

    /// <summary>
    /// When true, pressing the access-key letter of <see cref="AffirmativeText"/> or
    /// <see cref="NegativeText"/> (the letter after an underscore, as in "_Yes") answers the dialog
    /// without Alt. Only for dialogs with nothing to type into, such as the delete confirmation.
    /// </summary>
    protected bool AnswersOnAccessKeyAlone { get; set; }

    /// <summary>
    /// Runs the affirmative or negative command whose access key is <paramref name="letter"/>
    /// (case-insensitive), if this dialog opted in through <see cref="AnswersOnAccessKeyAlone"/>.
    /// Returns whether the dialog was answered.
    /// </summary>
    public bool TryAnswerFromAccessKey(char letter)
    {
        if (!AnswersOnAccessKeyAlone)
        {
            return false;
        }
        char key = char.ToUpperInvariant(letter);
        if (AccessKeyOf(AffirmativeText) == key && AffirmativeCommand.CanExecute(null))
        {
            AffirmativeCommand.Execute(null);
            return true;
        }
        if (AccessKeyOf(NegativeText) == key && NegativeCommand.CanExecute(null))
        {
            NegativeCommand.Execute(null);
            return true;
        }
        return false;
    }

    private static char? AccessKeyOf(string? text)
    {
        int underscore = text?.IndexOf('_') ?? -1;
        return underscore >= 0 && underscore + 1 < text!.Length ? char.ToUpperInvariant(text[underscore + 1]) : null;
    }

    public virtual bool CanExecuteAffirmative() => true;
    public virtual bool CanExecuteNegative() => true;
}