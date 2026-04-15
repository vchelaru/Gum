using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfDataUi.DataTypes;

namespace WpfDataUi.Controls;

/// <summary>
/// IDataUi displayer for a <see cref="List{String}"/> of file paths. Provides Add / Remove /
/// Move-Up / Move-Down buttons wired to a shared <see cref="FilePickingLogic"/> so the file
/// chooser matches the single-file <see cref="FileSelectionDisplay"/>. Order of the list is
/// preserved because it is semantically significant (e.g. last-write-wins merge order for
/// localization files).
/// </summary>
public partial class MultiFileDisplay : UserControl, IDataUi
{
    #region Fields

    static readonly SolidColorBrush DefaultValueBackground = new SolidColorBrush(Color.FromRgb(180, 255, 180)) { Opacity = 0.5 };

    readonly FilePickingLogic _filePickingLogic;
    InstanceMember? _instanceMember;
    List<string> _entries;

    // NOTE: static dictionary is process-global. If two MultiFileDisplay controls ever bind
    // to different InstanceMembers with the same Name simultaneously (e.g. a secondary
    // property inspector), they would cross-pollinate. Not a concern with today's
    // single-project-properties-grid usage, but worth revisiting if the control gets reused.
    //
    // Pending selection survives across:
    //   (a) the PropertyChanged-triggered refresh of the current control,
    //   (b) the subsequent grid rebuild that constructs BOTH a new InstanceMember and a
    //       new MultiFileDisplay bound to it.
    // We can't key on InstanceMember identity because (b) produces a different instance.
    // Name is stable. Scope stays tight because we clear the entry on the next idle tick
    // after Commit, so stale values can't leak across element switches.
    static readonly Dictionary<string, int> s_pendingSelectIndexByName = new();

    #endregion

    #region Properties

    public InstanceMember? InstanceMember
    {
        get => _instanceMember;
        set
        {
            bool instanceMemberChanged = _instanceMember != value;
            if (_instanceMember != null && instanceMemberChanged)
            {
                _instanceMember.PropertyChanged -= HandlePropertyChange;
            }
            _instanceMember = value;
            if (_instanceMember != null && instanceMemberChanged)
            {
                _instanceMember.PropertyChanged += HandlePropertyChange;
            }

            if (instanceMemberChanged)
            {
                // Clear stale green background from a previous pooled use.
                this.ListBox.ClearValue(ListBox.BackgroundProperty);
            }

            Refresh();
        }
    }

    public bool SuppressSettingProperty { get; set; }

    /// <summary>
    /// OpenFileDialog filter string forwarded to the underlying <see cref="FilePickingLogic"/>.
    /// </summary>
    public string Filter
    {
        get => _filePickingLogic.Filter;
        set => _filePickingLogic.Filter = value;
    }

    #endregion

    #region Construction

    public MultiFileDisplay()
    {
        _filePickingLogic = new FilePickingLogic();
        _entries = new List<string>();

        InitializeComponent();
    }

    #endregion

    #region IDataUi

    public void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        SuppressSettingProperty = true;

        this.Label.Text = InstanceMember?.DisplayName ?? string.Empty;

        HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
        HintTextBlock.Text = InstanceMember?.DetailText;

        TrySetValueOnUi(InstanceMember?.Value);
        RefreshIsEnabled();

        Dispatcher.BeginInvoke(() =>
        {
            if (DataUiGrid.GetOverridesIsDefaultStyling(this))
            {
                return;
            }

            if (InstanceMember?.IsDefault == true)
            {
                this.ListBox.Background = DefaultValueBackground;
            }
            else
            {
                this.ListBox.ClearValue(BackgroundProperty);
            }
        });

        SuppressSettingProperty = false;
    }

    public ApplyValueResult TrySetValueOnUi(object value)
    {
        _entries.Clear();
        if (value is List<string> incoming)
        {
            _entries.AddRange(incoming);
        }
        RebindListBox();
        return ApplyValueResult.Success;
    }

    public ApplyValueResult TryGetValueOnUi(out object result)
    {
        result = new List<string>(_entries);
        return ApplyValueResult.Success;
    }

    #endregion

    #region Event Handlers

    private void HandlePropertyChange(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InstanceMember.Value) ||
            e.PropertyName == nameof(InstanceMember.DetailText))
        {
            this.Refresh();
        }
    }

    private void AddButtonClicked(object? sender, RoutedEventArgs e)
    {
        string? selected = _filePickingLogic.ShowOpenDialog();
        if (string.IsNullOrEmpty(selected))
        {
            return;
        }

        _entries.Add(selected);
        Commit();
    }

    private void RemoveButtonClicked(object? sender, RoutedEventArgs e)
    {
        RemoveSelected();
    }

    private void MoveUpButtonClicked(object? sender, RoutedEventArgs e)
    {
        MoveSelected(-1);
    }

    private void MoveDownButtonClicked(object? sender, RoutedEventArgs e)
    {
        MoveSelected(1);
    }

    private void ListBox_PreviewMouseLeftButtonDown(object? sender, MouseButtonEventArgs e)
    {
        if (_instanceMember != null)
        {
            if (_instanceMember?.Name is string n) s_pendingSelectIndexByName.Remove(n);
        }
    }

    private void ListBox_KeyDown(object? sender, KeyEventArgs e)
    {
        // Arrow keys / clicks = real user navigation → the latched pending value is stale.
        if (_instanceMember != null)
        {
            if (_instanceMember?.Name is string n) s_pendingSelectIndexByName.Remove(n);
        }

        if (InstanceMember?.IsReadOnly == true)
        {
            return;
        }

        if (e.Key == Key.Delete)
        {
            RemoveSelected();
            e.Handled = true;
        }
    }

    #endregion

    #region Private Helpers

    private void RemoveSelected()
    {
        int index = ListBox.SelectedIndex;
        if (index < 0 || index >= _entries.Count)
        {
            return;
        }

        _entries.RemoveAt(index);
        Commit();
    }

    private void MoveSelected(int direction)
    {
        int index = ListBox.SelectedIndex;
        int newIndex = index + direction;
        if (index < 0 || newIndex < 0 || newIndex >= _entries.Count)
        {
            return;
        }

        string value = _entries[index];
        _entries.RemoveAt(index);
        _entries.Insert(newIndex, value);
        Commit(newIndex);
    }

    private void Commit(int? selectIndex = null)
    {
        // Latch the desired selection by MEMBER NAME — the grid rebuild produces a new
        // InstanceMember instance, so identity-based keys don't survive. Scheduled for
        // cleanup at ContextIdle so all rebuild passes in this input cycle see it, but
        // it can't linger into a future element switch.
        string? name = _instanceMember?.Name;
        if (selectIndex is int idx && name != null)
        {
            s_pendingSelectIndexByName[name] = idx;
            Dispatcher.BeginInvoke(
                () => s_pendingSelectIndexByName.Remove(name),
                System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
        this.TrySetValueOnInstance();
        RebindListBox();
    }

    private void RebindListBox()
    {
        int? pending = TryPeekPendingSelect();
        ListBox.ItemsSource = null;
        ListBox.ItemsSource = _entries;
        if (pending is int p && p >= 0 && p < _entries.Count)
        {
            ListBox.SelectedIndex = p;
        }
        RefreshButtonVisibility();
    }

    private void ListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        RefreshButtonVisibility();
    }

    private void RefreshButtonVisibility()
    {
        bool hasSelection = ListBox.SelectedIndex >= 0;
        bool hasMultiple = _entries.Count > 1;
        RemoveButton.Visibility = hasSelection ? Visibility.Visible : Visibility.Collapsed;
        MoveUpButton.Visibility = hasSelection && hasMultiple ? Visibility.Visible : Visibility.Collapsed;
        MoveDownButton.Visibility = hasSelection && hasMultiple ? Visibility.Visible : Visibility.Collapsed;
    }

    private int? TryPeekPendingSelect()
    {
        var name = _instanceMember?.Name;
        if (name != null && s_pendingSelectIndexByName.TryGetValue(name, out var idx))
        {
            return idx;
        }
        return null;
    }

    private void RefreshIsEnabled()
    {
        this.IsEnabled = InstanceMember?.IsReadOnly != true;
    }

    #endregion
}
