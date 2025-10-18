using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Gum.Services.Dialogs;

public class ChoiceDialogViewModel : DialogViewModel
{
    public string? Title { get => Get<string>(); set => Set(value); }
    public string Message { get => Get<string>(); set => Set(value); }
    public bool CanCancel
    {
        get => Get<bool>();
        set
        {
            if (Set(value) && value)
            {
                NegativeText = "Cancel";
            }
        }
    }

    public ReadOnlyObservableCollection<string> OptionValues { get; }
    private readonly ObservableCollection<string> _optionValues = [];
    private Dictionary<object, string> _options = [];

    public void SetOptions<T>(IDictionary<T, string> options) where T : notnull
    {
        _options = options.ToDictionary(kvp => (object)kvp.Key, kvp => kvp.Value);
        _optionValues.Clear();
        _optionValues.AddRange(options.Values);
        SelectedValue = options.Values.FirstOrDefault() ??
                        throw new InvalidOperationException("Must have at least one option.");
    }

    public ChoiceDialogViewModel()
    {
        OptionValues = new ReadOnlyObservableCollection<string>(_optionValues);
        AffirmativeText = "Confirm";
        NegativeText = null;
        CanCancel = false;
    }

    public string? SelectedValue
    {
        get => Get<string>();
        set => Set(value);
    }

    protected override void OnNegative()
    {
        if (!CanCancel)
        {
            return;
        }

        SelectedValue = null;
        base.OnNegative();
    }

    public object? SelectedKey => SelectedValue is null ? null :
        _options.FirstOrDefault(kvp => kvp.Value == SelectedValue).Key;
}

public class DialogChoices<TKey> : Dictionary<TKey, string>
    where TKey : notnull
{
    public DialogChoices() { }

    public DialogChoices(IDictionary<TKey, string> dictionary)
        : base(dictionary) { }

    public DialogChoices(IEnumerable<KeyValuePair<TKey, string>> pairs)
        : base()
    {
        foreach (var pair in pairs)
            this[pair.Key] = pair.Value;
    }
}