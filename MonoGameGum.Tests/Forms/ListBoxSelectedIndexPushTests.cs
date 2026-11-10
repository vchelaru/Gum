using Gum.Forms.Controls;
using Gum.Mvvm;
using Shouldly;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A bound SelectedIndex reaches the view model whenever its value changes, whatever the cause (#7849).
/// </summary>
public class ListBoxSelectedIndexPushTests : BaseTestClass
{
    #region ListBox

    [Fact]
    public void SelectedIndexSetInCode_ShouldPushToViewModel()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = Bind(listBox);

        listBox.SelectedIndex = 2;

        viewModel.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void SelectedIndexSetInCode_ShouldPushSelectedObject()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = Bind(listBox);

        listBox.SelectedIndex = 1;

        viewModel.Selected.ShouldBe("B");
    }

    [Fact]
    public void SelectedObjectSetInCode_ShouldPushSelectedIndex()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = Bind(listBox);

        listBox.SelectedObject = "C";

        viewModel.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void InsertAboveSelection_ShouldPushShiftedIndex()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = Bind(listBox);
        listBox.ListBoxItems[1].IsSelected = true;

        listBox.Items!.Insert(0, "Z");

        viewModel.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void RemoveAboveSelection_ShouldPushShiftedIndex()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = Bind(listBox);
        listBox.ListBoxItems[2].IsSelected = true;

        listBox.Items!.RemoveAt(0);

        viewModel.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void MoveAcrossSelection_ShouldPushShiftedIndex()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = Bind(listBox);
        listBox.ListBoxItems[2].IsSelected = true;

        ((ObservableCollection<object>)listBox.Items!).Move(0, 2);

        viewModel.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void SelectedIndexChanges_ShouldNotifyOncePerChange()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        int[] count = TrackSelectedIndexNotifications(listBox);

        listBox.SelectedIndex = 1;
        listBox.ListBoxItems[2].IsSelected = true;
        listBox.Items!.Insert(0, "Z");
        listBox.Items!.Add("D");

        // Three changes (1, 2, 3); the Add below the selection leaves the index alone.
        count[0].ShouldBe(3);
    }

    [Fact]
    public void ViewModelSet_ShouldSelectRowAndKeepViewModelValue()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = Bind(listBox);
        int[] count = TrackSelectedIndexNotifications(listBox);

        viewModel.SelectedIndex = 2;

        listBox.SelectedIndex.ShouldBe(2);
        viewModel.SelectedIndex.ShouldBe(2);
        count[0].ShouldBe(1);
    }

    #endregion

    #region ComboBox

    [Fact]
    public void ComboBox_SelectedIndexSetInCode_ShouldPushToViewModel()
    {
        ComboBox comboBox = CreateComboBox("A", "B", "C");
        SelectionViewModel viewModel = Bind(comboBox);

        comboBox.SelectedIndex = 2;

        viewModel.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void ComboBox_InsertAboveSelection_ShouldPushShiftedIndex()
    {
        ComboBox comboBox = CreateComboBox("A", "B", "C");
        SelectionViewModel viewModel = Bind(comboBox);
        comboBox.ListBox.ListBoxItems[1].IsSelected = true;

        comboBox.Items!.Insert(0, "Z");

        viewModel.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void ComboBox_MoveAcrossSelection_ShouldPushShiftedIndex()
    {
        ComboBox comboBox = CreateComboBox("A", "B", "C");
        SelectionViewModel viewModel = Bind(comboBox);
        comboBox.ListBox.ListBoxItems[2].IsSelected = true;

        ((ObservableCollection<object>)comboBox.Items!).Move(0, 2);

        viewModel.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void ComboBox_SelectedIndexChanges_ShouldNotifyOncePerChange()
    {
        ComboBox comboBox = CreateComboBox("A", "B", "C");
        int[] count = TrackSelectedIndexNotifications(comboBox);

        comboBox.SelectedIndex = 1;
        comboBox.Items!.Insert(0, "Z");

        count[0].ShouldBe(2);
    }

    #endregion

    private static SelectionViewModel Bind(FrameworkElement element)
    {
        SelectionViewModel viewModel = new();
        element.BindingContext = viewModel;
        element.SetBinding(nameof(ListBox.SelectedIndex), nameof(viewModel.SelectedIndex));
        element.SetBinding(nameof(ListBox.SelectedObject), nameof(viewModel.Selected));
        return viewModel;
    }

    private static int[] TrackSelectedIndexNotifications(INotifyPropertyChanged element)
    {
        int[] count = new int[1];
        element.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ListBox.SelectedIndex))
            {
                count[0]++;
            }
        };
        return count;
    }

    private static ListBox CreateListBox(params string[] items)
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        foreach (string item in items)
        {
            listBox.Items!.Add(item);
        }
        return listBox;
    }

    private static ComboBox CreateComboBox(params string[] items)
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        foreach (string item in items)
        {
            comboBox.Items!.Add(item);
        }
        return comboBox;
    }

    private class SelectionViewModel : ViewModel
    {
        public string? Selected
        {
            get => Get<string?>();
            set => Set(value);
        }

        public int SelectedIndex
        {
            get => Get<int>();
            set => Set(value);
        }
    }
}
