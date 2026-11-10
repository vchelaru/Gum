using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Input;
using Gum.Mvvm;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
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

    [Theory]
    [InlineData("ViewModelSetIndex")]
    [InlineData("ViewModelSetObject")]
    [InlineData("ControlSetIndex")]
    [InlineData("ControlSetObject")]
    [InlineData("RowIsSelected")]
    [InlineData("Add")]
    [InlineData("InsertAbove")]
    [InlineData("RemoveAbove")]
    [InlineData("RemoveSelected")]
    [InlineData("Move")]
    [InlineData("Clear")]
    [InlineData("SelectionModeMultiple")]
    [InlineData("SelectionModeMultipleThenSingle")]
    public void SelectionChange_ShouldNotifyEachPropertyAtMostOnce(string operation)
    {
        ListBox listBox = CreateListBox("A", "B", "C", "D");
        SelectionViewModel viewModel = Bind(listBox);
        listBox.SelectedIndex = 2;
        int[] controlIndex = TrackNotifications(listBox, nameof(ListBox.SelectedIndex));
        int[] controlObject = TrackNotifications(listBox, nameof(ListBox.SelectedObject));
        int[] viewModelIndex = TrackNotifications(viewModel, nameof(SelectionViewModel.SelectedIndex));
        int[] viewModelObject = TrackNotifications(viewModel, nameof(SelectionViewModel.Selected));

        switch (operation)
        {
            case "ViewModelSetIndex": viewModel.SelectedIndex = 1; break;
            case "ViewModelSetObject": viewModel.Selected = "D"; break;
            case "ControlSetIndex": listBox.SelectedIndex = 0; break;
            case "ControlSetObject": listBox.SelectedObject = "B"; break;
            case "RowIsSelected": listBox.ListBoxItems[3].IsSelected = true; break;
            case "Add": listBox.Items!.Add("E"); break;
            case "InsertAbove": listBox.Items!.Insert(0, "Z"); break;
            case "RemoveAbove": listBox.Items!.RemoveAt(0); break;
            case "RemoveSelected": listBox.Items!.RemoveAt(2); break;
            case "Move": ((ObservableCollection<object>)listBox.Items!).Move(0, 3); break;
            case "Clear": listBox.Items!.Clear(); break;
            case "SelectionModeMultiple": listBox.SelectionMode = SelectionMode.Multiple; break;
            case "SelectionModeMultipleThenSingle":
                listBox.SelectionMode = SelectionMode.Multiple;
                listBox.ListBoxItems[0].IsSelected = true;
                controlIndex[0] = controlObject[0] = viewModelIndex[0] = viewModelObject[0] = 0;
                listBox.SelectionMode = SelectionMode.Single;
                break;
        }

        viewModel.SelectedIndex.ShouldBe(listBox.SelectedIndex);
        viewModel.Selected.ShouldBe(listBox.SelectedObject);
        controlIndex[0].ShouldBeLessThanOrEqualTo(1);
        controlObject[0].ShouldBeLessThanOrEqualTo(1);
        viewModelIndex[0].ShouldBeLessThanOrEqualTo(1);
        viewModelObject[0].ShouldBeLessThanOrEqualTo(1);
    }

    [Fact]
    public void ClickRow_ShouldNotifyEachPropertyOnce()
    {
        ListBox listBox = CreateListBox("A", "B", "C", "D");
        SelectionViewModel viewModel = Bind(listBox);
        listBox.SelectedIndex = 0;
        int[] controlIndex = TrackNotifications(listBox, nameof(ListBox.SelectedIndex));
        int[] controlObject = TrackNotifications(listBox, nameof(ListBox.SelectedObject));
        InteractiveGue row = listBox.ListBoxItems[2].Visual;
        float x = row.GetAbsoluteX() + row.AbsoluteWidth / 2;
        float y = row.GetAbsoluteY() + row.AbsoluteHeight / 2;
        Mock<ICursor> cursor = new();
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(x);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(y);
        cursor.Setup(c => c.X).Returns((int)x);
        cursor.Setup(c => c.Y).Returns((int)y);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.PrimaryPush).Returns(true);
        cursor.SetupProperty(c => c.VisualOver);
        cursor.SetupProperty(c => c.WindowPushed);
        FrameworkElement.MainCursor = cursor.Object;

        GueInteractiveExtensionMethods.DoUiActivityRecursively(listBox.Visual, cursor.Object, null!, 0);

        listBox.SelectedIndex.ShouldBe(2);
        viewModel.SelectedIndex.ShouldBe(2);
        viewModel.Selected.ShouldBe("C");
        controlIndex[0].ShouldBe(1);
        controlObject[0].ShouldBe(1);
    }

    [Fact]
    public void Rebind_ControlChangeBackToLastPushedValue_ShouldPush()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        Bind(listBox);
        listBox.SelectedIndex = 2;
        SelectionViewModel secondViewModel = new() { SelectedIndex = 0, Selected = "A" };
        listBox.BindingContext = secondViewModel;

        listBox.SelectedIndex = 2;

        secondViewModel.SelectedIndex.ShouldBe(2);
        secondViewModel.Selected.ShouldBe("C");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Rebind_ShouldNotWriteToPreviousViewModel(bool bindIndexFirst)
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel first = new();
        BindBoth(listBox, first, bindIndexFirst);
        listBox.SelectedIndex = 2;
        SelectionViewModel second = new() { SelectedIndex = 0 };

        listBox.BindingContext = second;

        first.SelectedIndex.ShouldBe(2);
        first.Selected.ShouldBe("C");
    }

    [Fact]
    public void VisualSwap_LaterChanges_ShouldPush()
    {
        ListBox listBox = CreateListBox("A", "B", "C");
        SelectionViewModel viewModel = Bind(listBox);
        listBox.SelectedIndex = 2;
        listBox.Visual = new ListBoxVisual(tryCreateFormsObject: false);

        listBox.SelectedIndex = 1;
        viewModel.Selected.ShouldBe("B");
        listBox.Items!.Insert(0, "Z");

        viewModel.SelectedIndex.ShouldBe(2);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ComboBox_Rebind_ShouldNotWriteToPreviousViewModel(bool bindIndexFirst)
    {
        ComboBox comboBox = CreateComboBox("A", "B", "C");
        SelectionViewModel first = new();
        BindBoth(comboBox, first, bindIndexFirst);
        comboBox.SelectedIndex = 2;
        SelectionViewModel second = new() { SelectedIndex = 0 };

        comboBox.BindingContext = second;

        first.SelectedIndex.ShouldBe(2);
        first.Selected.ShouldBe("C");
    }

    #endregion

    private static void BindBoth(FrameworkElement element, SelectionViewModel viewModel, bool bindIndexFirst)
    {
        element.BindingContext = viewModel;
        if (bindIndexFirst)
        {
            element.SetBinding(nameof(ListBox.SelectedIndex), nameof(viewModel.SelectedIndex));
            element.SetBinding(nameof(ListBox.SelectedObject), nameof(viewModel.Selected));
        }
        else
        {
            element.SetBinding(nameof(ListBox.SelectedObject), nameof(viewModel.Selected));
            element.SetBinding(nameof(ListBox.SelectedIndex), nameof(viewModel.SelectedIndex));
        }
    }

    private static SelectionViewModel Bind(FrameworkElement element)
    {
        SelectionViewModel viewModel = new();
        element.BindingContext = viewModel;
        element.SetBinding(nameof(ListBox.SelectedIndex), nameof(viewModel.SelectedIndex));
        element.SetBinding(nameof(ListBox.SelectedObject), nameof(viewModel.Selected));
        return viewModel;
    }

    private static int[] TrackSelectedIndexNotifications(INotifyPropertyChanged element) =>
        TrackNotifications(element, nameof(ListBox.SelectedIndex));

    private static int[] TrackNotifications(INotifyPropertyChanged source, string propertyName)
    {
        int[] count = new int[1];
        source.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == propertyName)
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
