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
    InstanceMember mInstanceMember;

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

    public InstanceMember InstanceMember
    { 
        get => mInstanceMember; 
        set
        {
            bool instanceMemberChanged = mInstanceMember != value;
            if (mInstanceMember != null && instanceMemberChanged)
            {
                mInstanceMember.PropertyChanged -= HandlePropertyChange;
            }
            mInstanceMember = value;
            if (mInstanceMember != null && instanceMemberChanged)
            {
                mInstanceMember.PropertyChanged += HandlePropertyChange;
            }
            Refresh();

        }
    }

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

            this.Label.Text = InstanceMember.DisplayName;
            this.RefreshContextMenu(ListBox.ContextMenu);
            //this.RefreshContextMenu(StackPanel.ContextMenu);

            if (InstanceMember.IsDefault)
            {
                this.ListBox.Background = DefaultValueBackground;
            }
            else
            {
                this.ListBox.ClearValue(BackgroundProperty);
            }

            //HintTextBlock.Visibility = !string.IsNullOrEmpty(InstanceMember?.DetailText) ? Visibility.Visible : Visibility.Collapsed;
            //HintTextBlock.Text = InstanceMember?.DetailText;
            TrySetValueOnUi(InstanceMember?.Value);
            //RefreshIsEnabled();

            SuppressSettingProperty = false;
        }
    }

    public ApplyValueResult TryGetValueOnUi(out object result)
    {
        // todo - need to make this more flexible, but for now let's just support strings:
        var propertyType = InstanceMember?.PropertyType;
        if(propertyType == typeof(List<string>))
        {
            var value = new List<string>();

            foreach(var item in ListBox.Items)
            {
                value.Add(item?.ToString());
            }

            result = value;

            return ApplyValueResult.Success;

        }
        else if(propertyType == typeof(List<int>))
        {
            var value = new List<int>();

            foreach(var item in ListBox.Items)
            {
                if(int.TryParse(item?.ToString(), out int intResult))
                {
                    value.Add(intResult);
                }
            }

            result = value;

            return ApplyValueResult.Success;
        }
        // do the same as above, but this time for List<float>
        else if(propertyType == typeof(List<float>))
        {
            var value = new List<float>();
            foreach(var item in ListBox.Items)
            {
                if(float.TryParse(item?.ToString(), out float floatResult))
                {
                    value.Add(floatResult);
                }
            }
            result = value;
            return ApplyValueResult.Success;
        }
        else if (propertyType == typeof(List<Vector2>))
        {
            var value = new List<Vector2>();
            foreach (var item in ListBox.Items)
            {
                if (TryParse(item?.ToString(), out Vector2? vectorResult))
                {
                    value.Add(vectorResult.Value);
                }
            }
            result = value;
            return ApplyValueResult.Success;
        }
        else
        {
            result = null;
            return ApplyValueResult.NotSupported;
        }
    }

    public ApplyValueResult TrySetValueOnUi(object value)
    {
        if(value is List<string> valueAsList)
        {
            var newList = new List<string>();
            newList.AddRange(valueAsList);
            ListBox.ItemsSource = newList;
        }
        else if(InstanceMember?.PropertyType == typeof(List<string>))
        {
            var newList = new List<string>();
            ListBox.ItemsSource = newList;
        }
        else if(value is List<int> valueAsIntList)
        {
            var newList = new List<int>();
            newList.AddRange(valueAsIntList);
            ListBox.ItemsSource = newList;
        }
        else if(InstanceMember?.PropertyType == typeof(List<int>))
        {
            var newList = new List<int>();
            ListBox.ItemsSource = newList;
        }
        else if(value is List<float> valueAsFloatList)
        {
            var newList = new List<float>();
            newList.AddRange(valueAsFloatList);
            ListBox.ItemsSource = newList;
        }
        else if (InstanceMember?.PropertyType == typeof(List<float>))
        {
            var newList = new List<float>();
            ListBox.ItemsSource = newList;
        }
        else if(value is List<Vector2> valueAsVectorList)
        {
            var newList = new List<Vector2>();
            newList.AddRange(valueAsVectorList);
            ListBox.ItemsSource = newList;
        }
        else if (InstanceMember?.PropertyType == typeof(List<Vector2>))
        {
            var newList = new List<Vector2>();
            ListBox.ItemsSource = newList;
        }
        else if(value is not null)
        {
            var newList = Activator.CreateInstance(value.GetType()) as IList;

            foreach(var item in value as IList)
            {
                newList.Add(item);
            }
            ListBox.ItemsSource = newList;
        }
        else
        {
            // what do we do here?
            if(InstanceMember?.PropertyType != null)
            {
                var newList = Activator.CreateInstance(InstanceMember.PropertyType) as IList;
                ListBox.ItemsSource = newList;
            }
            else
            {
                throw new InvalidOperationException(
                    "Could not set UI value on ListBoxDisplay in TrySetValueOnUi because the value is null and the InstanceMember does not specify a property type");
            }

        }
        return ApplyValueResult.Success;
    }

    private void AddButtonClicked(object sender, RoutedEventArgs e)
    {
        ShowTextBoxUi();
    }

    private void ShowTextBoxUi()
    {
        NewEntryGrid.Visibility = Visibility.Visible;
        NotEditingEntryStackPanel.Visibility = Visibility.Collapsed;
        NewTextBox.Focus();
    }

    private void ListBox_KeyDown(object sender, KeyEventArgs e)
    {
        var isCtrlDown =
            (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));

        if (e.Key == Key.Delete)
        {
            var selectedItem = ListBox.SelectedIndex;

            if(selectedItem > -1)
            {
                var listToRemoveFrom = ListBox.ItemsSource as IList;

                if(ListBox.SelectedIndex < listToRemoveFrom.Count)
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

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        HandleAddTextItem(NewTextBox.Text);
    }

    private void HandleAddTextItem(string text)
    {
        var listToAddTo = ListBox.ItemsSource as IList;
        if (listToAddTo != null)
        {
            if(listToAddTo is List<string> stringList)
            {
                if (IndexEditing == null)
                {
                    stringList.Add(text);
                }
                else
                {
                    stringList[IndexEditing.Value] = text;
                }
            }
            else if (listToAddTo is List<int> intList)
            {
                if (int.TryParse(text, out int intResult))
                {
                    if(IndexEditing == null)
                    {
                        intList.Add(intResult);
                    }
                    else
                    {
                        intList[IndexEditing.Value] = intResult;
                    }
                }
            }
            else if (listToAddTo is List<float> floatList)
            {
                if (float.TryParse(text, out float floatResult))
                {
                    if (IndexEditing == null)
                    {
                        floatList.Add(floatResult);
                    }
                    else
                    {
                        floatList[IndexEditing.Value] = floatResult;
                    }
                }
            }
            else if(listToAddTo is List<System.Numerics.Vector2> vector2List)
            {
                Vector2? toAdd = null;

                if(TryParse(text, out toAdd))
                {
                    if (IndexEditing == null)
                    {
                        vector2List.Add(toAdd.Value);
                    }
                    else
                    {
                        vector2List[IndexEditing.Value] = toAdd.Value;
                    }
                }
                else
                {
                    MessageBox.Show("Could not parse the values. Value must be two numbers separated by a comma, such as \"10,20\"");
                }
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

    private static bool TryParse(string text, out Vector2? parsedValue)
    {
        parsedValue = null;

        if(text?.StartsWith("<") == true)
        {
            text = text.Substring(1);
        }
        if(text?.EndsWith(">") == true)
        {
            text = text.Substring(0, text.Length - 1);
        }

        if (text?.Contains(",") == true)
        {
            var splitValues = text.Split(',');

            if (splitValues.Length == 2)
            {
                if (float.TryParse(splitValues[0], out float firstValue) &&
                    float.TryParse(splitValues[1], out float secondValue))
                {
                    parsedValue = new Vector2(firstValue, secondValue);
                }
            }
        }
        return parsedValue != null;
    }

    private void TryDoManualRefresh()
    {
        var itemSourceList = ListBox.ItemsSource as IList;

        var needsManualRefresh = !(itemSourceList is INotifyCollectionChanged);
        if (needsManualRefresh)
        {
            ListBox.ItemsSource = null;
            TrySetValueOnUi(InstanceMember?.Value);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
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

    private void NewTextBox_KeyDown(object sender, KeyEventArgs e)
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

    private void HandlePropertyChange(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(InstanceMember.Value))
        {
            this.Refresh();

        }
    }

    private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        IndexEditing = ((ListBox)sender).SelectedIndex;
        if(IndexEditing < 0)
        {
            IndexEditing = null;
        }
        else
        {
            ShowTextBoxUi();

            var textToAssign = ListBox.SelectedItem?.ToString();

            if (textToAssign?.StartsWith("<") == true)
            {
                textToAssign = textToAssign.Substring(1);
            }
            if (textToAssign?.EndsWith(">") == true)
            {
                textToAssign = textToAssign.Substring(0, textToAssign.Length - 1);
            }

            this.NewTextBox.Text = textToAssign;
        }
    }
}
