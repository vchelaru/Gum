using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfDataUi.DataTypes;
using static System.Net.Mime.MediaTypeNames;

namespace WpfDataUi.Controls;

/// <summary>
/// Interaction logic for ListBoxDisplay.xaml
/// </summary>
public partial class ListBoxDisplay : UserControl, IDataUi
{
    InstanceMember? _instanceMember;

    int? indexEditing = -1;
    int? IndexEditing
    {
        get => indexEditing;
        set
        {
            indexEditing = value;
            ListBox.IsEnabled = indexEditing == null;
        }
    }

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
                // A pooled instance (or this control's very first bind, since the backing
                // field starts at -1 rather than null) may not be "not editing" for this
                // InstanceMember. Reset so the next Add appends instead of indexing an
                // in-progress edit position that may not exist in the new list.
                NewEntryGrid.Visibility = Visibility.Collapsed;
                NotEditingEntryStackPanel.Visibility = Visibility.Visible;
                IndexEditing = null;
            }

            Refresh();

        }
    }

    readonly ListBoxDisplayLogic _listLogic = new ListBoxDisplayLogic();

    public ListBoxDisplay()
    {
        InitializeComponent();
    }

    public bool SuppressSettingProperty { get; set; }

    static SolidColorBrush DefaultValueBackground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(180, 255, 180)){Opacity = 0.5f};

    public void Refresh(bool forceRefreshEvenIfFocused = false)
    {
        bool canRefresh = true;

        if(canRefresh)
        {
            SuppressSettingProperty = true;

            //mTextBoxLogic.RefreshDisplay();

            this.Label.Text = InstanceMember?.DisplayName;
            this.RefreshContextMenu(ListBox.ContextMenu);
            //this.RefreshContextMenu(StackPanel.ContextMenu);

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


            HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
            HintTextBlock.Text = InstanceMember?.DetailText;
            TrySetValueOnUi(InstanceMember?.Value!);
            RefreshIsEnabled();

            SuppressSettingProperty = false;
        }
    }

    public ApplyValueResult TryGetValueOnUi(out object? result)
    {
        if (_listLogic.TryReadList(ListBox.Items, InstanceMember?.PropertyType, out object? list))
        {
            result = list;
            return ApplyValueResult.Success;
        }

        result = null;
        return ApplyValueResult.NotSupported;
    }

    public ApplyValueResult TrySetValueOnUi(object value)
    {
        // Edit a copy so nothing reaches the member until it is committed.
        ListBox.ItemsSource = _listLogic.CreateEditableCopy(value, InstanceMember?.PropertyType);
        return ApplyValueResult.Success;
    }

    private void RefreshIsEnabled()
    {
        if (InstanceMember?.IsReadOnly == true)
        {
            this.IsEnabled = false;
        }
        else
        {
            this.IsEnabled = true;
        }
    }

    private void AddButtonClicked(object? sender, RoutedEventArgs e)
    {
        ShowTextBoxUi();
    }

    private void ShowTextBoxUi()
    {
        NewEntryGrid.Visibility = Visibility.Visible;
        NotEditingEntryStackPanel.Visibility = Visibility.Collapsed;
        NewTextBox.Focus();
    }

    private void ListBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (InstanceMember?.IsReadOnly == true)
            return;

        var isCtrlDown =
            (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));

        if (e.Key == Key.Delete)
        {
            var selectedItem = ListBox.SelectedIndex;

            if(selectedItem > -1)
            {
                var listToRemoveFrom = ListBox.ItemsSource as IList;

                if(listToRemoveFrom != null && ListBox.SelectedIndex < listToRemoveFrom.Count)
                {
                    listToRemoveFrom.RemoveAt(ListBox.SelectedIndex);
                }
            }
            this.TrySetValueOnInstance();

            TryDoManualRefresh();
        }
        else if(e.Key == Key.C && isCtrlDown)
        {
            var selectedItem = ListBox.SelectedItem as string;

            if(!string.IsNullOrEmpty(selectedItem))
            {
                Clipboard.SetText(selectedItem);
            }
        }
        else if(e.Key == Key.V && isCtrlDown)
        {
            var text = Clipboard.GetText();

            if(!string.IsNullOrEmpty(text))
            {
                HandleAddTextItem(text);
            }
        }
    }

    private void OkButton_Click(object? sender, RoutedEventArgs e)
    {
        HandleAddTextItem(NewTextBox.Text);
    }

    private void HandleAddTextItem(string text)
    {
        var listToAddTo = ListBox.ItemsSource as IList;
        if (listToAddTo != null)
        {
            string? error = _listLogic.AddOrReplace(listToAddTo, IndexEditing, text);
            if (error != null)
            {
                MessageBox.Show(error);
            }
        }
        NewTextBox.Text = null;
        NewEntryGrid.Visibility = Visibility.Collapsed;
        NotEditingEntryStackPanel.Visibility = Visibility.Visible;
        this.TrySetValueOnInstance();
        if(IndexEditing != null)
        {
            IndexEditing = null;
            this.Refresh();
        }

        TryDoManualRefresh();
    }



    private void TryDoManualRefresh()
    {
        var itemSourceList = ListBox.ItemsSource as IList;

        var needsManualRefresh = !(itemSourceList is INotifyCollectionChanged);
        if (needsManualRefresh)
        {
            ListBox.ItemsSource = null;
            TrySetValueOnUi(InstanceMember?.Value!);
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        HandleCancelItem();
    }

    private void HandleCancelItem()
    {
        NewTextBox.Text = null;
        NewEntryGrid.Visibility = Visibility.Collapsed;
        NotEditingEntryStackPanel.Visibility = Visibility.Visible;
        IndexEditing = null;
    }

    private void NewTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if(e.Key == Key.Enter)
        {
            e.Handled = true;
            HandleAddTextItem(NewTextBox.Text);

        }
        else if(e.Key == Key.Escape)
        {
            e.Handled = true;
            HandleCancelItem();
        }
    }

    private void HandlePropertyChange(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InstanceMember.Value) ||
            e.PropertyName == nameof(InstanceMember.DetailText))
        {
            this.Refresh();
        }
    }

    private void ListBox_MouseDoubleClick(object? sender, MouseButtonEventArgs e)
    {
        if (InstanceMember?.IsReadOnly == true)
            return;

        IndexEditing = ((ListBox)sender!).SelectedIndex;
        if(IndexEditing < 0)
        {
            IndexEditing = null;
        }
        else
        {
            ShowTextBoxUi();

            this.NewTextBox.Text = ListBoxDisplayLogic.StripAngleBrackets(ListBox.SelectedItem?.ToString());
        }
    }
}
