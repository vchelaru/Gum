using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Gum.Mvvm;

namespace Gum.Services.Dialogs;


public abstract class GetUserStringDialogBaseViewModel : DialogViewModel
{
    public virtual string? Title { get => Get<string>(); set => Set(value); }
    public virtual string Message { get => Get<string>(); set => Set(value); }
    public string? Value { get => Get<string>(); set => Set(value); }
    public string? Error { get => Get<string>(); protected set => Set(value); }
    public bool PreSelect { get; protected set; }
    public string? Prefix { get => Get<string>(); set => Set(value); }
    
    protected GetUserStringDialogBaseViewModel()
    {
        AffirmativeText = "OK";
        NegativeText = "Cancel";
        
        PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Value):
                    Validate();
                    break;
                case nameof(Error):
                    AffirmativeCommand.NotifyCanExecuteChanged();
                    break;
            }
        };
    }
    
    public void Validate() => Error = Validate(Value);
    
    protected virtual string? Validate(string? value) => EnsureNotEmpty(value);

    public override bool CanExecuteAffirmative() => Error is null;
    
    private static string? EnsureNotEmpty(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "Cannot be empty." : null;
    }
}

public sealed class GetUserStringDialogViewModel : GetUserStringDialogBaseViewModel
{
    private Func<string?,string?>? Validator { get; }

    public bool HasRefactorCheckbox { get; }
    public string RefactorCheckboxLabel { get; }
    public string? RefactorDetails { get; }
    public bool IsRefactorChecked { get => Get<bool>(); set => Set(value); }

    [DependsOn(nameof(IsRefactorChecked))]
    public bool HasRefactorDetailsVisible => IsRefactorChecked && !string.IsNullOrEmpty(RefactorDetails);

    public GetUserStringDialogViewModel(GetUserStringOptions? options = null)
    {
        Value = options?.InitialValue;
        Validator = options?.Validator;
        PreSelect = options?.PreSelect ?? false;
        HasRefactorCheckbox = options?.HasRefactorCheckbox ?? false;
        RefactorCheckboxLabel = options?.RefactorCheckboxLabel ?? "Also update referencing elements";
        RefactorDetails = options?.RefactorDetails;
        IsRefactorChecked = options?.IsRefactorChecked ?? false;
    }

    protected override string? Validate(string? value) => Validator?.Invoke(value) ?? base.Validate(value);
}

public class GetUserStringOptions
{
    public Func<string?,string?>? Validator { get; set; }
    public string? InitialValue { get; set; }
    public bool PreSelect { get; set; }
    public bool HasRefactorCheckbox { get; set; }
    public string RefactorCheckboxLabel { get; set; } = "Also update referencing elements";
    public string? RefactorDetails { get; set; }
    /// <summary>
    /// Set before showing the dialog to specify the default checked state.
    /// After the dialog closes, read this to determine whether the user checked the checkbox.
    /// </summary>
    public bool IsRefactorChecked { get; set; }
}