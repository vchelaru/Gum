using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using RenderingLibrary;
using Gum.Mvvm;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A ListBox subclass (generated code in a game) sets its template and bindings from
/// ReactToVisualChanged, which runs during construction. Everything that works for a ListBox
/// bound after construction must work for one bound from inside its constructor.
/// </summary>
public class ListBoxBindingSetDuringConstructionTests : BaseTestClass
{
    #region Construction reads

    [Fact]
    public void ReactToVisualChanged_ReadingSelectionMembers_ShouldNotThrowWithDefaultConstructor()
    {
        Should.NotThrow(() => new ReadingListBox());
    }

    [Fact]
    public void ReactToVisualChanged_ReadingSelectionMembers_ShouldNotThrowWithVisualConstructor()
    {
        Should.NotThrow(() => new ReadingListBox(new Gum.Forms.DefaultVisuals.V3.ListBoxVisual(tryCreateFormsObject: false)));
    }

    [Fact]
    public void ReactToVisualChanged_ReadingSelectionMembers_ShouldSeeEmptySelection()
    {
        ReadingListBox listBox = new();

        listBox.SelectedIndexSeenDuringConstruction.ShouldBe(-1);
        listBox.SelectedObjectSeenDuringConstruction.ShouldBeNull();
        listBox.SelectedItemsCountSeenDuringConstruction.ShouldBe(0);
        listBox.ItemsCountSeenDuringConstruction.ShouldBe(0);
    }

    #endregion

    #region Bindings set during construction

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ItemsBinding_ContextAssignedAfterConstruction_ShouldFillRows(bool useVisualConstructor)
    {
        TowerList listBox = CreateTowerList(useVisualConstructor);
        TowerViewModel viewModel = new("A", "B", "C");

        listBox.BindingContext = viewModel;

        listBox.Items!.Count.ShouldBe(3);
        listBox.ListBoxItems.Count.ShouldBe(3);
    }

    [Fact]
    public void ItemsBinding_ContextAssignedAfterConstruction_ShouldUseTemplateRowType()
    {
        TowerList listBox = CreateTowerList(false);

        listBox.BindingContext = new TowerViewModel("A", "B");

        listBox.ListBoxItems[0].ShouldBeOfType<TowerItem>();
        listBox.ListBoxItems[1].ShouldBeOfType<TowerItem>();
    }

    [Fact]
    public void ItemsBinding_ContextAssignedAfterConstruction_ShouldGiveEachRowItsDataAsBindingContext()
    {
        TowerList listBox = CreateTowerList(false);

        listBox.BindingContext = new TowerViewModel("A", "B");

        listBox.ListBoxItems[0].BindingContext.ShouldBe("A");
        listBox.ListBoxItems[1].BindingContext.ShouldBe("B");
    }

    [Fact]
    public void ItemsBinding_ItemAddedToViewModelCollection_ShouldAddRow()
    {
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new("A");
        listBox.BindingContext = viewModel;

        viewModel.AvailableTowers.Add("B");

        listBox.ListBoxItems.Count.ShouldBe(2);
        listBox.ListBoxItems[1].ShouldBeOfType<TowerItem>();
    }

    [Fact]
    public void ItemsBinding_ItemRemovedFromViewModelCollection_ShouldRemoveRow()
    {
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new("A", "B", "C");
        listBox.BindingContext = viewModel;

        viewModel.AvailableTowers.RemoveAt(1);

        listBox.ListBoxItems.Count.ShouldBe(2);
        listBox.Items!.Count.ShouldBe(2);
    }

    [Fact]
    public void SelectedIndexBinding_ViewModelValueSetBeforeContextAssigned_ShouldSelectRow()
    {
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new("A", "B", "C") { SelectedTower = 2 };

        listBox.BindingContext = viewModel;

        listBox.SelectedIndex.ShouldBe(2);
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void SelectedIndexBinding_ViewModelValueSetAfterContextAssigned_ShouldSelectRow()
    {
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new("A", "B", "C");
        listBox.BindingContext = viewModel;

        viewModel.SelectedTower = 1;

        listBox.SelectedIndex.ShouldBe(1);
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void SelectedIndexBinding_ViewModelValueNegativeOne_ShouldLeaveNothingSelected()
    {
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new("A", "B") { SelectedTower = -1 };

        listBox.BindingContext = viewModel;

        listBox.SelectedIndex.ShouldBe(-1);
        listBox.SelectedItems.Count.ShouldBe(0);
    }

    [Fact]
    public void SelectedIndexBinding_ControlSelectionChanged_ShouldPushToViewModel()
    {
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new("A", "B", "C");
        listBox.BindingContext = viewModel;

        listBox.SelectedIndex = 2;

        viewModel.SelectedTower.ShouldBe(2);
    }

    [Fact]
    public void SelectedIndexBinding_RowIsSelectedChanged_ShouldPushToViewModel()
    {
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new("A", "B", "C");
        listBox.BindingContext = viewModel;

        listBox.ListBoxItems[1].IsSelected = true;

        viewModel.SelectedTower.ShouldBe(1);
    }

    [Fact]
    public void SelectedIndexBinding_ViewModelCollectionClearedWhileSelected_ShouldPushMinusOne()
    {
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new("A", "B", "C") { SelectedTower = 1 };
        listBox.BindingContext = viewModel;

        viewModel.AvailableTowers.Clear();

        listBox.SelectedIndex.ShouldBe(-1);
        viewModel.SelectedTower.ShouldBe(-1);
    }

    [Fact]
    public void SelectedIndexBinding_RowInsertedAboveSelection_ShouldPushShiftedIndex()
    {
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new("A", "B", "C") { SelectedTower = 1 };
        listBox.BindingContext = viewModel;

        viewModel.AvailableTowers.Insert(0, "Z");

        viewModel.SelectedTower.ShouldBe(2);
        listBox.SelectedObject.ShouldBe("B");
    }

    [Fact]
    public void ContextReplaced_NewViewModel_ShouldReplaceRowsAndSelection()
    {
        TowerList listBox = CreateTowerList(false);
        listBox.BindingContext = new TowerViewModel("A", "B", "C") { SelectedTower = 2 };

        listBox.BindingContext = new TowerViewModel("X", "Y") { SelectedTower = 0 };

        listBox.ListBoxItems.Count.ShouldBe(2);
        listBox.SelectedObject.ShouldBe("X");
        listBox.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void ContextInheritedFromParent_ShouldFillRowsAndSelect()
    {
        TowerList listBox = CreateTowerList(false);
        Gum.GueDeriving.ContainerRuntime parent = new();
        parent.AddChild(listBox.Visual);
        parent.BindingContext = new TowerViewModel("A", "B", "C") { SelectedTower = 1 };

        listBox.ListBoxItems.Count.ShouldBe(3);
        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void SelectedIndexBinding_ViewModelIndexSetBeforeItemsArrive_ShouldSelectRowWhenItemsArrive()
    {
        // The view model already says "tower 1", and the towers are loaded afterward.
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new() { SelectedTower = 1 };
        listBox.BindingContext = viewModel;

        viewModel.AvailableTowers.Add("A");
        viewModel.AvailableTowers.Add("B");

        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void VisualSwap_AfterBinding_ShouldKeepRowsAndSelection()
    {
        TowerList listBox = CreateTowerList(false);
        listBox.BindingContext = new TowerViewModel("A", "B", "C") { SelectedTower = 1 };

        listBox.Visual = new Gum.Forms.DefaultVisuals.V3.ListBoxVisual(tryCreateFormsObject: false);

        listBox.ListBoxItems.Count.ShouldBe(3);
        listBox.SelectedIndex.ShouldBe(1);
        listBox.ListBoxItems[0].ShouldBeOfType<TowerItem>();
    }

    [Fact]
    public void SelectedIndexBinding_ViewModelIndexSetBeforeItemsArrive_ShouldKeepViewModelValue()
    {
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new() { SelectedTower = 1 };
        listBox.BindingContext = viewModel;

        viewModel.SelectedTower.ShouldBe(1);
    }

    [Fact]
    public void SelectedIndexBinding_ViewModelResetToNegativeOneBeforeItemsArrive_ShouldNotSelectWhenItemsArrive()
    {
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new() { SelectedTower = 1 };
        listBox.BindingContext = viewModel;
        viewModel.SelectedTower = -1;

        viewModel.AvailableTowers.Add("A");
        viewModel.AvailableTowers.Add("B");

        listBox.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void FrameworkElementTemplate_ChangedWithBoundSelection_ShouldNotWriteTransientValueToViewModel()
    {
        ListBox listBox = CreateBoundListBox();
        TowerViewModel viewModel = new("A", "B", "C") { SelectedTower = 1, SelectedName = "B" };
        listBox.BindingContext = viewModel;
        List<string> writes = new();
        viewModel.PropertyChanged += (_, args) =>
            writes.Add($"{args.PropertyName}:{viewModel.SelectedTower}:{viewModel.SelectedName}");

        listBox.FrameworkElementTemplate = new FrameworkElementTemplate(typeof(TowerItem));

        writes.ShouldBeEmpty();
        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void FrameworkElementTemplate_ChangedWhileRowSelected_ShouldStillRaiseSelectionChanged()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        listBox.Items = new ObservableCollection<string> { "A", "B", "C" };
        listBox.SelectedIndex = 1;
        int raised = 0;
        listBox.SelectionChanged += (_, _) => raised++;

        listBox.FrameworkElementTemplate = new FrameworkElementTemplate(typeof(TowerItem));

        raised.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void SelectedIndex_ClearedWhileObjectHeld_ShouldNotSelectObjectWhenItArrives()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        ObservableCollection<string> items = new();
        listBox.Items = items;
        listBox.SetSelectedObject("Z", deferIfNotFound: true);

        listBox.SelectedIndex = -1;
        items.Add("Z");

        listBox.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void SelectedObject_ClearedWhileIndexHeld_ShouldNotSelectIndexWhenItemsArrive()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        ObservableCollection<string> items = new();
        listBox.Items = items;
        listBox.SetSelectedIndex(1, deferIfPastEnd: true);

        listBox.SelectedObject = null;
        items.Add("A");
        items.Add("B");

        listBox.SelectedIndex.ShouldBe(-1);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void SelectedIndexBinding_BindingContextReplacedWhileIndexHeld_ShouldUseNewViewModelValue(int newValue)
    {
        ListBox listBox = CreateBoundListBox(bindSelectedObject: false);
        listBox.BindingContext = new TowerViewModel { SelectedTower = 5 };
        TowerViewModel second = new("A", "B", "C") { SelectedTower = newValue };

        listBox.BindingContext = second;
        second.AvailableTowers.Add("D");
        second.AvailableTowers.Add("E");
        second.AvailableTowers.Add("F");

        listBox.SelectedIndex.ShouldBe(newValue);
    }

    [Fact]
    public void SelectedObjectBinding_BindingContextReplacedWhileObjectHeld_ShouldUseNewViewModelValue()
    {
        ListBox listBox = CreateBoundListBox();
        listBox.BindingContext = new TowerViewModel { SelectedName = "Z" };
        TowerViewModel second = new("A", "B") { SelectedName = "B", SelectedTower = 1 };

        listBox.BindingContext = second;
        second.AvailableTowers.Add("Z");

        listBox.SelectedObject.ShouldBe("B");
        listBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void SelectedIndexBinding_ViewModelIndexStillPastEndAfterItemsArrive_ShouldSelectWhenEnoughItemsArrive()
    {
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new() { SelectedTower = 2 };
        listBox.BindingContext = viewModel;

        viewModel.AvailableTowers.Add("A");
        listBox.SelectedIndex.ShouldBe(-1);
        viewModel.AvailableTowers.Add("B");
        viewModel.AvailableTowers.Add("C");

        listBox.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void SelectedIndex_PastEndWithoutBinding_ShouldStayIgnoredWhenItemsArrive()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        ObservableCollection<string> items = new();
        listBox.Items = items;

        listBox.SelectedIndex = 1;
        items.Add("A");
        items.Add("B");

        listBox.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void ComboBoxSelectedIndexBinding_ViewModelIndexSetBeforeItemsArrive_ShouldSelectWhenItemsArrive()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.SetBinding(nameof(ComboBox.Items), nameof(TowerViewModel.AvailableTowers));
        comboBox.SetBinding(nameof(ComboBox.SelectedIndex), nameof(TowerViewModel.SelectedTower));
        TowerViewModel viewModel = new() { SelectedTower = 1 };
        comboBox.BindingContext = viewModel;

        viewModel.AvailableTowers.Add("A");
        viewModel.AvailableTowers.Add("B");

        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.SelectedObject.ShouldBe("B");
    }

    [Fact]
    public void SelectedObjectBinding_ViewModelObjectSetBeforeItemsArrive_ShouldSelectWhenItemsArrive()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        listBox.SetBinding(nameof(ListBox.Items), nameof(TowerViewModel.AvailableTowers));
        listBox.SetBinding(nameof(ListBox.SelectedObject), nameof(TowerViewModel.SelectedName));
        TowerViewModel viewModel = new() { SelectedName = "B" };
        listBox.BindingContext = viewModel;

        viewModel.AvailableTowers.Add("A");
        viewModel.AvailableTowers.Add("B");

        listBox.SelectedObject.ShouldBe("B");
    }

    [Fact]
    public void FrameworkElementTemplate_ChangedWhileRowSelected_ShouldKeepSelection()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        listBox.Items = new ObservableCollection<string> { "A", "B", "C" };
        listBox.SelectedIndex = 1;

        listBox.FrameworkElementTemplate = new FrameworkElementTemplate(typeof(TowerItem));

        listBox.ListBoxItems.Count.ShouldBe(3);
        listBox.SelectedIndex.ShouldBe(1);
        listBox.SelectedObject.ShouldBe("B");
        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void VisualTemplate_ChangedWhileRowSelected_ShouldKeepSelection()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        listBox.Items = new ObservableCollection<string> { "A", "B", "C" };
        listBox.SelectedIndex = 2;

        listBox.VisualTemplate = new VisualTemplate(() => new ListBoxItem().Visual);

        listBox.ListBoxItems.Count.ShouldBe(3);
        listBox.SelectedIndex.ShouldBe(2);
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void ClickTemplatedRow_ShouldSelectItAndPushToViewModel()
    {
        TowerList listBox = CreateTowerList(false);
        TowerViewModel viewModel = new("A", "B", "C");
        listBox.BindingContext = viewModel;
        InteractiveGue row = listBox.ListBoxItems[2].Visual;
        float x = row.GetAbsoluteX() + row.AbsoluteWidth / 2;
        float y = row.GetAbsoluteY() + row.AbsoluteHeight / 2;
        Moq.Mock<ICursor> cursor = new();
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
        listBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
        viewModel.SelectedTower.ShouldBe(2);
    }

    [Fact]
    public void SelectedRow_ShouldApplySelectedVisualState()
    {
        TowerList listBox = CreateTowerList(false);
        listBox.BindingContext = new TowerViewModel("A", "B", "C");

        listBox.SelectedIndex = 1;

        listBox.ListBoxItems[1].IsSelected.ShouldBeTrue();
        listBox.ListBoxItems[1].Visual.Categories.ContainsKey("ListBoxItemCategory");
        listBox.ListBoxItems[0].IsSelected.ShouldBeFalse();
    }

    #endregion

    #region Helpers

    private static ListBox CreateBoundListBox(bool bindSelectedObject = true)
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        listBox.SetBinding(nameof(ListBox.Items), nameof(TowerViewModel.AvailableTowers));
        listBox.SetBinding(nameof(ListBox.SelectedIndex), nameof(TowerViewModel.SelectedTower));
        if (bindSelectedObject)
        {
            listBox.SetBinding(nameof(ListBox.SelectedObject), nameof(TowerViewModel.SelectedName));
        }
        return listBox;
    }

    private static TowerList CreateTowerList(bool useVisualConstructor)
    {
        TowerList listBox = useVisualConstructor
            ? new TowerList(new Gum.Forms.DefaultVisuals.V3.ListBoxVisual(tryCreateFormsObject: false))
            : new TowerList();
        listBox.AddToRoot();
        return listBox;
    }

    private class TowerViewModel : ViewModel
    {
        public ObservableCollection<string> AvailableTowers { get; } = new();

        public int SelectedTower
        {
            get => Get<int>();
            set => Set(value);
        }

        public string? SelectedName
        {
            get => Get<string?>();
            set => Set(value);
        }

        public TowerViewModel(params string[] towers)
        {
            foreach (string tower in towers)
            {
                AvailableTowers.Add(tower);
            }
        }
    }

    private class TowerItem : ListBoxItem
    {
        public TowerItem() : base() { }
    }

    /// <summary>What the generated partial class plus the user's CustomInitialize amounts to.</summary>
    private class TowerList : ListBox
    {
        public TowerList() : base() { }

        public TowerList(InteractiveGue visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();
            FrameworkElementTemplate = new FrameworkElementTemplate(typeof(TowerItem));
            SetBinding(nameof(Items), nameof(TowerViewModel.AvailableTowers));
            SetBinding(nameof(SelectedIndex), nameof(TowerViewModel.SelectedTower));
        }
    }

    /// <summary>Reads selection members from inside construction, as a custom initializer might.</summary>
    private class ReadingListBox : ListBox
    {
        public int SelectedIndexSeenDuringConstruction { get; private set; } = int.MinValue;
        public object? SelectedObjectSeenDuringConstruction { get; private set; } = new object();
        public int SelectedItemsCountSeenDuringConstruction { get; private set; } = -1;
        public int ItemsCountSeenDuringConstruction { get; private set; } = -1;

        public ReadingListBox() : base() { }

        public ReadingListBox(InteractiveGue visual) : base(visual) { }

        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();
            SelectedIndexSeenDuringConstruction = SelectedIndex;
            SelectedObjectSeenDuringConstruction = SelectedObject;
            SelectedItemsCountSeenDuringConstruction = SelectedItems.Count;
            ItemsCountSeenDuringConstruction = Items?.Count ?? 0;
        }
    }

    #endregion
}
