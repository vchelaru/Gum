using Gum.Services.Dialogs;
using System;

namespace StateAnimationPlugin.ViewModels;

internal class AddAnimationDialogViewModel : DialogViewModel
{
    public Func<string, string?>? Validator { get; set; }

    public string Name
    {
        get => Get<string>();
        set
        {
            if (Set(value))
            {
                AffirmativeCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool Loops
    {
        get => Get<bool>();
        set => Set(value);
    }

    public AddAnimationDialogViewModel()
    {
        Name = string.Empty;
        Loops = false;
    }

    public override bool CanExecuteAffirmative() =>
        !string.IsNullOrWhiteSpace(Name) && Validator?.Invoke(Name) is null;
}
