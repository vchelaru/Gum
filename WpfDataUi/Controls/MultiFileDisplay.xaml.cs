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
    int? _pendingSelectIndex;

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
        _entries = new List<string>();
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
        _pendingSelectIndex = null;
    }

    private void ListBox_KeyDown(object? sender, KeyEventArgs e)
    {
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
        // Latch the desired selection BEFORE TrySetValueOnInstance fires, because the
        // variable-grid may rebuild this control (or call Refresh → RebindListBox) in
        // response, which would otherwise drop the selection. RebindListBox reapplies
        // _pendingSelectIndex on every bind; we clear it once the user interacts.
        _pendingSelectIndex = selectIndex;
        this.TrySetValueOnInstance();
        RebindListBox();
    }

    private void RebindListBox()
    {
        ListBox.ItemsSource = null;
        ListBox.ItemsSource = _entries;
        if (_pendingSelectIndex is int pending && pending >= 0 && pending < _entries.Count)
        {
            ListBox.SelectedIndex = pending;
        }
    }

    private void RefreshIsEnabled()
    {
        this.IsEnabled = InstanceMember?.IsReadOnly != true;
    }

    #endregion
}
