using System.ComponentModel;

namespace AvaloniaDataUi;

/// <summary>
/// Whether a <see cref="DataUiGrid"/> shows its category headers, for the category views to bind to.
/// A grid hides them when it has <see cref="DataUiGrid.HideLoneCategoryHeader"/> set and only one category.
/// </summary>
internal sealed class CategoryHeaderState : INotifyPropertyChanged
{
    private bool _areHidden;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>True when the category headers are not shown.</summary>
    public bool AreHidden
    {
        get => _areHidden;
        set
        {
            if (value == _areHidden)
            {
                return;
            }
            _areHidden = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AreHidden)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AreShown)));
        }
    }

    /// <summary>True when the category headers are shown.</summary>
    public bool AreShown => !_areHidden;
}
