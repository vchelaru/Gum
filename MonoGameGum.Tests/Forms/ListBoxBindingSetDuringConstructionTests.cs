using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Mvvm;
using Gum.Wireframe;
using Shouldly;
using System;
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

    #endregion

    #region Helpers

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
