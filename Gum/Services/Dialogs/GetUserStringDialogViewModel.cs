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

    public GetUserStringDialogViewModel(GetUserStringOptions? options = null)
    {
        Value = options?.InitialValue;
        Validator = options?.Validator;
        PreSelect = options?.PreSelect ?? false;
    }

    protected override string? Validate(string? value) => Validator?.Invoke(value) ?? base.Validate(value);
}

public class GetUserStringOptions
{
    public Func<string?,string?>? Validator { get; set; }
    public string? InitialValue { get; set; }
    public bool PreSelect { get; set; }
}