using Gum.Mvvm;

namespace ImportFromGumxPlugin.ViewModels;

public enum ElementItemType
{
    Component,
    Screen,
    Behavior,
    Standard
}

public enum InclusionState
{
    NotIncluded,
    AutoIncluded,
    Explicit
}

public class ImportPreviewItemViewModel : ViewModel
{
    /// <summary>
    /// Display name, e.g. "Controls/Button" or "ButtonBehavior"
    /// </summary>
    public string Name
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

    public ElementItemType ElementType
    {
        get => Get<ElementItemType>();
        set => Set(value);
    }

    /// <summary>
    /// The inclusion state driven by either user interaction or the VM's dependency analysis.
    /// Setting this automatically updates IsChecked.
    /// </summary>
    public InclusionState InclusionState
    {
        get => Get<InclusionState>();
        set
        {
            Set(value);
            NotifyPropertyChanged(nameof(IsChecked));
        }
    }

    /// <summary>
    /// Three-state bool bound to the WPF CheckBox's IsChecked:
    ///   true  = Explicit   (user-selected)
    ///   null  = AutoIncluded (transitive dependency, set programmatically)
    ///   false = NotIncluded
    ///
    /// The setter is called by WPF when the user clicks the checkbox.
    /// It advances the state: NotIncluded → Explicit → NotIncluded (auto may override back to null).
    /// The VM is responsible for setting null (AutoIncluded) programmatically; user clicks never produce null.
    /// </summary>
    public bool? IsChecked
    {
        get => InclusionState switch
        {
            InclusionState.Explicit => true,
            InclusionState.AutoIncluded => null,
            _ => false
        };
        set
        {
            // Value comes from user click. WPF cycles true → false → null (indeterminate),
            // but the view's code-behind intercepts null and forces false instead.
            // So the only values we receive here are true and false.
            if (value == true)
            {
                InclusionState = InclusionState.Explicit;
            }
            else
            {
                InclusionState = InclusionState.NotIncluded;
                // If the VM decides this item is still a transitive dependency,
                // it will set InclusionState = AutoIncluded after this setter returns.
            }
        }
    }

    /// <summary>
    /// Human-readable reason this item is auto-included, e.g. "used by Controls/Button".
    /// Shown as a tooltip in the UI.
    /// </summary>
    public string AutoIncludedReason
    {
        get => Get<string>() ?? string.Empty;
        set => Set(value);
    }

    public override string ToString() => Name;
}
