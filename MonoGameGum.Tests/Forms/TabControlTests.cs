using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;
#if !SKIA_LINKED_TESTS
using Microsoft.Xna.Framework.Input;
using MonoGameGum.Tests.Input;
using GamePad = Gum.Input.GamePad;
#endif

namespace MonoGameGum.Tests.Forms;

public class TabControlTests : BaseTestClass
{
    private class TabViewModel
    {
        public string Title { get; set; } = "";
        public override string ToString() => Title;
    }

    private class OwnerViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<TabViewModel> Tabs { get; } = new ObservableCollection<TabViewModel>();

        int _selectedIndex;
        public int SelectedIndex
        {
            get => _selectedIndex;
            set { _selectedIndex = value; Raise(); }
        }

        TabViewModel? _selected;
        public TabViewModel? Selected
        {
            get => _selected;
            set { _selected = value; Raise(); }
        }

        private void Raise([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private static TabControl CreateWithTitles(params string[] titles)
    {
        TabControl tabControl = new TabControl();
        ObservableCollection<TabViewModel> items = new ObservableCollection<TabViewModel>(
            titles.Select(t => new TabViewModel { Title = t }));
        tabControl.Items = items;
        return tabControl;
    }

    private static void ShouldBePaired(TabControl tabControl)
    {
        tabControl.TabItems.Count.ShouldBe(tabControl.Items!.Count);
        tabControl.Pages.Count.ShouldBe(tabControl.Items.Count);
        for (int i = 0; i < tabControl.Items.Count; i++)
        {
            tabControl.Pages[i].BindingContext.ShouldBe(tabControl.Items[i]);
            tabControl.TabItems[i].BindingContext.ShouldBe(tabControl.Items[i]);
            tabControl.ContentPanel!.Children.IndexOf(tabControl.Pages[i].Visual).ShouldBe(i);
            tabControl.InnerPanel.Children.IndexOf(tabControl.TabItems[i].Visual).ShouldBe(i);
        }
    }

    #region Pairing

    [Fact]
    public void Add_ShouldCreateTabAndPagePerItem()
    {
        TabControl tabControl = CreateWithTitles("A", "B", "C");

        ShouldBePaired(tabControl);
        tabControl.TabItems[1].Header.ShouldBe(tabControl.Items![1]);
    }

    [Fact]
    public void Insert_ShouldKeepTabsAndPagesPaired()
    {
        TabControl tabControl = CreateWithTitles("A", "C");

        tabControl.Items!.Insert(1, new TabViewModel { Title = "B" });

        ShouldBePaired(tabControl);
    }

    [Fact]
    public void Remove_ShouldRemoveTabAndPage()
    {
        TabControl tabControl = CreateWithTitles("A", "B", "C");
        TabPage removedPage = tabControl.Pages[1];

        tabControl.Items!.RemoveAt(1);

        ShouldBePaired(tabControl);
        removedPage.Visual.Parent.ShouldBeNull();
    }

    [Fact]
    public void Move_ShouldKeepTabsAndPagesPaired()
    {
        TabControl tabControl = CreateWithTitles("A", "B", "C");
        ObservableCollection<TabViewModel> items = (ObservableCollection<TabViewModel>)tabControl.Items!;

        items.Move(0, 2);

        ShouldBePaired(tabControl);
        tabControl.TabItems[2].BindingContext.ShouldBe(items[2]);
    }

    [Fact]
    public void Clear_ShouldRemoveEveryTabAndPage()
    {
        TabControl tabControl = CreateWithTitles("A", "B");

        tabControl.Items!.Clear();

        tabControl.TabItems.Count.ShouldBe(0);
        tabControl.Pages.Count.ShouldBe(0);
        tabControl.ContentPanel!.Children.Count.ShouldBe(0);
    }

    [Fact]
    public void ContentTemplate_ShouldCreatePageContentWithItemAsBindingContext()
    {
        TabControl tabControl = new TabControl();
        List<object?> contexts = new List<object?>();
        tabControl.ContentTemplate = new VisualTemplate(item =>
        {
            contexts.Add(item);
            return new LabelVisual();
        });
        TabViewModel first = new TabViewModel { Title = "A" };

        tabControl.Items!.Add(first);

        contexts.ShouldBe(new object?[] { first });
        tabControl.Pages[0].ContentContainer.Children.Count.ShouldBe(1);
        tabControl.Pages[0].BindingContext.ShouldBe(first);
    }

    #endregion

    #region Sugar

    [Fact]
    public void AddTab_ShouldAddOneTabItemToItemsWithContentInPage()
    {
        TabControl tabControl = new TabControl();
        Button content = new Button();

        TabItem tab = tabControl.AddTab("Settings", content);

        tabControl.Items!.Count.ShouldBe(1);
        tabControl.Items[0].ShouldBe(tab);
        tab.Header.ShouldBe("Settings");
        tabControl.Pages.Count.ShouldBe(1);
        content.Visual.Parent.ShouldBe(tabControl.Pages[0].ContentContainer);
    }

    [Fact]
    public void TabItemContent_ShouldBePlacedInPage_WhenTabAddedToItems()
    {
        TabControl tabControl = new TabControl();
        Label content = new Label();
        TabItem tab = new TabItem { Header = "One", Content = content };

        tabControl.Items!.Add(tab);

        tabControl.TabItems.Single().ShouldBe(tab);
        content.Visual.Parent.ShouldBe(tabControl.Pages[0].ContentContainer);

        tabControl.SelectedIndex = 0;
        tabControl.SelectedObject.ShouldBe(tab);
    }

    [Fact]
    public void TabItemContent_ShouldBeUsedAsPage_WhenContentIsTabPage()
    {
        TabControl tabControl = new TabControl();
        TabPage page = new TabPage();

        tabControl.AddTab("One", page);

        tabControl.Pages.Single().ShouldBe(page);
    }

    #endregion

    #region Selection

    [Fact]
    public void SelectedIndex_ShouldBeMinusOne_WhenItemsEmpty()
    {
        TabControl tabControl = new TabControl();

        tabControl.SelectedIndex.ShouldBe(-1);
        tabControl.SelectedObject.ShouldBeNull();
    }

    [Fact]
    public void SelectedIndex_ShouldStayMinusOne_WhenTabsAdded_AsListBoxDoes()
    {
        TabControl tabControl = CreateWithTitles("A", "B");

        tabControl.SelectedIndex.ShouldBe(-1);
        tabControl.TabItems.Select(t => t.IsSelected).ShouldBe(new[] { false, false });
        tabControl.Pages.Select(p => p.Visual.Visible).ShouldBe(new[] { false, false });
    }

    [Fact]
    public void SelectedIndex_ShouldSyncIsSelectedAndPageVisibility()
    {
        TabControl tabControl = CreateWithTitles("A", "B", "C");

        tabControl.SelectedIndex = 2;

        tabControl.TabItems.Select(t => t.IsSelected).ShouldBe(new[] { false, false, true });
        tabControl.Pages.Select(p => p.IsSelected).ShouldBe(new[] { false, false, true });
        tabControl.Pages.Select(p => p.Visual.Visible).ShouldBe(new[] { false, false, true });
        tabControl.SelectedObject.ShouldBe(tabControl.Items![2]);
    }

    [Fact]
    public void SelectedIndex_ShouldBeIgnored_WhenOutOfRange()
    {
        TabControl tabControl = CreateWithTitles("A", "B");
        tabControl.SelectedIndex = 1;

        tabControl.SelectedIndex = 5;

        tabControl.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void SelectedObject_ShouldSelectMatchingTab()
    {
        TabControl tabControl = CreateWithTitles("A", "B");

        tabControl.SelectedObject = tabControl.Items![1];

        tabControl.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void TabItemIsSelected_ShouldSelectTabAndUnselectOthers()
    {
        TabControl tabControl = CreateWithTitles("A", "B", "C");

        tabControl.TabItems[1].IsSelected = true;

        tabControl.SelectedIndex.ShouldBe(1);
        tabControl.TabItems[0].IsSelected.ShouldBeFalse();
        tabControl.Pages[1].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void SelectionChanged_ShouldReportAddedAndRemovedItems()
    {
        TabControl tabControl = CreateWithTitles("A", "B");
        tabControl.SelectedIndex = 0;
        List<SelectionChangedEventArgs> raised = new List<SelectionChangedEventArgs>();
        tabControl.SelectionChanged += (_, args) => raised.Add(args);

        tabControl.SelectedIndex = 1;

        raised.Count.ShouldBe(1);
        raised[0].AddedItems.Cast<object>().ShouldBe(new[] { tabControl.Items![1] });
        raised[0].RemovedItems.Cast<object>().ShouldBe(new[] { tabControl.Items[0] });
    }

    [Fact]
    public void SelectionChanged_ShouldReportClick_WhenTabSelectedDirectly()
    {
        TabControl tabControl = CreateWithTitles("A", "B");
        tabControl.SelectedIndex = 0;
        List<SelectionChangedEventArgs> raised = new List<SelectionChangedEventArgs>();
        tabControl.SelectionChanged += (_, args) => raised.Add(args);

        tabControl.TabItems[1].IsSelected = true;

        raised.Count.ShouldBe(1);
        raised[0].AddedItems.Cast<object>().ShouldBe(new[] { tabControl.Items![1] });
        raised[0].RemovedItems.Cast<object>().ShouldBe(new[] { tabControl.Items[0] });
    }

    [Fact]
    public void RemovingSelectedTab_ShouldLeaveNothingSelected_AsListBoxDoes()
    {
        TabControl tabControl = CreateWithTitles("A", "B", "C");
        tabControl.SelectedIndex = 1;
        object removed = tabControl.Items![1]!;
        List<SelectionChangedEventArgs> raised = new List<SelectionChangedEventArgs>();
        tabControl.SelectionChanged += (_, args) => raised.Add(args);

        tabControl.Items.RemoveAt(1);

        tabControl.SelectedIndex.ShouldBe(-1);
        tabControl.SelectedObject.ShouldBeNull();
        tabControl.Pages.Select(p => p.IsSelected).ShouldBe(new[] { false, false });
        raised.Count.ShouldBe(1);
        raised[0].RemovedItems.Cast<object>().ShouldBe(new[] { removed });
        raised[0].AddedItems.Count.ShouldBe(0);
    }

    [Fact]
    public void ReplacingSelectedItem_ShouldLeaveNothingSelected_AsListBoxDoes()
    {
        TabControl tabControl = CreateWithTitles("A", "B");
        tabControl.SelectedIndex = 1;

        tabControl.Items![1] = new TabViewModel { Title = "C" };

        tabControl.SelectedIndex.ShouldBe(-1);
        tabControl.TabItems[1].IsSelected.ShouldBeFalse();
        tabControl.Pages[1].IsSelected.ShouldBeFalse();
    }

    [Fact]
    public void SelectedIndex_ShouldFollowMovedTab()
    {
        TabControl tabControl = CreateWithTitles("A", "B", "C");
        tabControl.SelectedIndex = 0;
        ObservableCollection<TabViewModel> items = (ObservableCollection<TabViewModel>)tabControl.Items!;
        TabViewModel selected = items[0];

        items.Move(0, 2);

        tabControl.SelectedIndex.ShouldBe(2);
        tabControl.SelectedObject.ShouldBe(selected);
    }

    #endregion

    #region Binding

    [Fact]
    public void Binding_ShouldBeTwoWay_ForSelectedIndexAndSelectedObject()
    {
        OwnerViewModel vm = new OwnerViewModel();
        vm.Tabs.Add(new TabViewModel { Title = "A" });
        vm.Tabs.Add(new TabViewModel { Title = "B" });
        TabControl tabControl = new TabControl { BindingContext = vm };
        tabControl.SetBinding(nameof(TabControl.Items), nameof(OwnerViewModel.Tabs));
        tabControl.SetBinding(nameof(TabControl.SelectedIndex), nameof(OwnerViewModel.SelectedIndex));
        tabControl.SetBinding(nameof(TabControl.SelectedObject), nameof(OwnerViewModel.Selected));

        vm.SelectedIndex = 1;
        tabControl.SelectedIndex.ShouldBe(1);
        vm.Selected.ShouldBe(vm.Tabs[1]);

        tabControl.TabItems[0].IsSelected = true;
        vm.SelectedIndex.ShouldBe(0);
        vm.Selected.ShouldBe(vm.Tabs[0]);

        vm.Selected = vm.Tabs[1];
        tabControl.SelectedIndex.ShouldBe(1);
    }

    private class CountingViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<TabViewModel> Tabs { get; } = new ObservableCollection<TabViewModel>();
        public int IndexSets;
        public int SelectedSets;

        // Starts where an empty TabControl is, so any difference later is one the control failed to push.
        int _index = -1;
        public int SelectedIndex
        {
            get => _index;
            set { IndexSets++; if (_index != value) { _index = value; Raise(); } }
        }

        TabViewModel? _selected;
        public TabViewModel? Selected
        {
            get => _selected;
            set { SelectedSets++; if (_selected != value) { _selected = value; Raise(); } }
        }

        private void Raise([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private static TabControl BindCounting(CountingViewModel vm)
    {
        TabControl tabControl = new TabControl { BindingContext = vm };
        tabControl.SetBinding(nameof(TabControl.Items), nameof(CountingViewModel.Tabs));
        tabControl.SetBinding(nameof(TabControl.SelectedIndex), nameof(CountingViewModel.SelectedIndex));
        tabControl.SetBinding(nameof(TabControl.SelectedObject), nameof(CountingViewModel.Selected));
        return tabControl;
    }

    private static CountingViewModel CreateCountingViewModel(int count)
    {
        CountingViewModel vm = new CountingViewModel();
        for (int i = 0; i < count; i++)
        {
            vm.Tabs.Add(new TabViewModel { Title = "Tab" + i });
        }
        return vm;
    }

    [Fact]
    public void InsertBeforeSelectedTab_ShouldPushShiftedIndex()
    {
        CountingViewModel vm = CreateCountingViewModel(3);
        TabControl tabControl = BindCounting(vm);
        tabControl.SelectedIndex = 1;

        vm.Tabs.Insert(0, new TabViewModel { Title = "New" });

        tabControl.SelectedIndex.ShouldBe(2);
        vm.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void RemoveBeforeSelectedTab_ShouldPushShiftedIndex()
    {
        CountingViewModel vm = CreateCountingViewModel(3);
        TabControl tabControl = BindCounting(vm);
        tabControl.SelectedIndex = 2;

        vm.Tabs.RemoveAt(0);

        tabControl.SelectedIndex.ShouldBe(1);
        vm.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void MoveUnselectedTabPastSelectedTab_ShouldPushShiftedIndex()
    {
        CountingViewModel vm = CreateCountingViewModel(3);
        TabControl tabControl = BindCounting(vm);
        tabControl.SelectedIndex = 1;

        vm.Tabs.Move(0, 2);

        tabControl.SelectedIndex.ShouldBe(0);
        vm.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void SelectionChange_ShouldPushEachPropertyOnce()
    {
        CountingViewModel vm = CreateCountingViewModel(3);
        TabControl tabControl = BindCounting(vm);
        vm.IndexSets = 0;
        vm.SelectedSets = 0;

        tabControl.SelectedIndex = 2;

        vm.IndexSets.ShouldBe(1);
        vm.SelectedSets.ShouldBe(1);
    }

    [Fact]
    public void RandomOperations_ShouldKeepTabsPagesAndViewModelInSync()
    {
        CountingViewModel vm = new CountingViewModel();
        TabControl tabControl = new TabControl { BindingContext = vm };
        tabControl.ContentTemplate = new VisualTemplate(_ => new LabelVisual());
        tabControl.SetBinding(nameof(TabControl.Items), nameof(CountingViewModel.Tabs));
        tabControl.SetBinding(nameof(TabControl.SelectedIndex), nameof(CountingViewModel.SelectedIndex));
        tabControl.SetBinding(nameof(TabControl.SelectedObject), nameof(CountingViewModel.Selected));

        System.Random random = new System.Random(6408);
        int nextId = 0;
        ObservableCollection<TabViewModel> items = vm.Tabs;
        for (int op = 0; op < 400; op++)
        {
            int kind = random.Next(100);
            string step;
            if (items.Count == 0 || kind < 30)
            {
                int at = random.Next(items.Count + 1);
                step = $"op{op} insert@{at}";
                items.Insert(at, new TabViewModel { Title = "Tab" + nextId++ });
            }
            else if (kind < 50)
            {
                int at = random.Next(items.Count);
                step = $"op{op} remove@{at}";
                items.RemoveAt(at);
            }
            else if (kind < 65)
            {
                int from = random.Next(items.Count);
                int to = random.Next(items.Count);
                step = $"op{op} move {from}->{to}";
                items.Move(from, to);
            }
            else if (kind < 80)
            {
                int at = random.Next(items.Count);
                step = $"op{op} replace@{at}";
                items[at] = new TabViewModel { Title = "Tab" + nextId++ };
            }
            else if (kind < 83)
            {
                step = $"op{op} clear";
                items.Clear();
            }
            else
            {
                int at = random.Next(-1, items.Count);
                step = $"op{op} select {at}";
                tabControl.SelectedIndex = at;
            }

            ShouldBePairedWithSelection(tabControl, items, step);
            vm.SelectedIndex.ShouldBe(tabControl.SelectedIndex, step + " view model index");
            vm.Selected.ShouldBeSameAs(tabControl.SelectedObject, step + " view model selection");
        }
    }

    private static void ShouldBePairedWithSelection(TabControl tabControl, IList<TabViewModel> items, string step)
    {
        tabControl.TabItems.Count.ShouldBe(items.Count, step);
        tabControl.Pages.Count.ShouldBe(items.Count, step);
        for (int i = 0; i < items.Count; i++)
        {
            tabControl.Pages[i].BindingContext.ShouldBeSameAs(items[i], step + " page " + i);
            tabControl.TabItems[i].BindingContext.ShouldBeSameAs(items[i], step + " tab " + i);
            tabControl.TabItems[i].Header.ShouldBeSameAs(items[i], step + " header " + i);
            tabControl.ContentPanel!.Children.IndexOf(tabControl.Pages[i].Visual).ShouldBe(i, step + " page slot " + i);
            tabControl.InnerPanel.Children.IndexOf(tabControl.TabItems[i].Visual).ShouldBe(i, step + " tab slot " + i);
            tabControl.Pages[i].IsSelected.ShouldBe(tabControl.TabItems[i].IsSelected, step + " page selection " + i);
        }
        tabControl.ContentPanel!.Children.Count.ShouldBe(items.Count, step + " stray pages");
        if (tabControl.SelectedIndex > -1)
        {
            tabControl.SelectedObject.ShouldBeSameAs(items[tabControl.SelectedIndex], step + " selected object");
        }
    }

    [Fact]
    public void Binding_ShouldReplaceAuthoredTabs_AsListBoxDoes()
    {
        TabControlVisual visual = new TabControlVisual(tryCreateFormsObject: false);
        TabItemVisual authoredTab = new TabItemVisual();
        TabPageVisual authoredPage = new TabPageVisual();
        visual.InnerPanelInstance.Children.Add(authoredTab);
        visual.ContentPanelInstance.Children.Add(authoredPage);
        TabControl tabControl = new TabControl(visual);
        tabControl.TabItems.Count.ShouldBe(1);

        OwnerViewModel vm = new OwnerViewModel();
        vm.Tabs.Add(new TabViewModel { Title = "Bound" });
        tabControl.BindingContext = vm;
        tabControl.SetBinding(nameof(TabControl.Items), nameof(OwnerViewModel.Tabs));

        tabControl.TabItems.Count.ShouldBe(1);
        tabControl.TabItems[0].BindingContext.ShouldBe(vm.Tabs[0]);
        tabControl.Pages.Count.ShouldBe(1);
        tabControl.Pages[0].ShouldNotBe(authoredPage.FormsControl);
        authoredPage.Parent.ShouldBeNull();
        tabControl.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void AuthoredTabs_ShouldBeAdoptedAndPairedByPosition()
    {
        TabControlVisual visual = new TabControlVisual(tryCreateFormsObject: false);
        TabItemVisual tab0 = new TabItemVisual();
        TabItemVisual tab1 = new TabItemVisual();
        TabPageVisual page0 = new TabPageVisual();
        TabPageVisual page1 = new TabPageVisual();
        visual.InnerPanelInstance.Children.Add(tab0);
        visual.InnerPanelInstance.Children.Add(tab1);
        visual.ContentPanelInstance.Children.Add(page0);
        visual.ContentPanelInstance.Children.Add(page1);

        TabControl tabControl = new TabControl(visual);

        tabControl.Items!.Count.ShouldBe(2);
        tabControl.Pages.ShouldBe(new[] { page0.FormsControl, page1.FormsControl });
        // No selection until the tool's SelectedIndex default (or code) sets one, as with ListBox.
        tabControl.SelectedIndex.ShouldBe(-1);

        tabControl.SelectedIndex = 0;
        page0.Visible.ShouldBeTrue();
        page1.Visible.ShouldBeFalse();

        tabControl.SelectedIndex = 1;
        page0.Visible.ShouldBeFalse();
        page1.Visible.ShouldBeTrue();
    }

    #endregion

    #region Pages parented after the control is built

    // A screen parents its TabItems into InnerPanelInstance and its TabPages into ContentPanelInstance
    // after the TabControl exists (GumPreview), so the panels change at any time.

    private static (TabControlVisual Visual, TabControl Control) CreateEmptyControl()
    {
        TabControlVisual visual = new TabControlVisual(tryCreateFormsObject: false);
        return (visual, new TabControl(visual));
    }

    private static TabPageVisual[] AddPages(TabControlVisual visual, int count)
    {
        TabPageVisual[] pages = new TabPageVisual[count];
        for (int i = 0; i < count; i++)
        {
            pages[i] = new TabPageVisual();
            pages[i].Parent = visual.ContentPanelInstance;
        }
        return pages;
    }

    private static void AddTabs(TabControlVisual visual, int count)
    {
        for (int i = 0; i < count; i++)
        {
            new TabItemVisual().Parent = visual.InnerPanelInstance;
        }
    }

    [Fact]
    public void TabWithNoPage_ShouldGetAPlaceholderPage()
    {
        (TabControlVisual visual, TabControl tabControl) = CreateEmptyControl();

        AddTabs(visual, 1);

        tabControl.TabItems.Count.ShouldBe(1);
        tabControl.Pages.Count.ShouldBe(1);
        visual.ContentPanelInstance.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void PagesAddedAfterTabs_ShouldPairByPosition()
    {
        (TabControlVisual visual, TabControl tabControl) = CreateEmptyControl();
        AddTabs(visual, 3);

        TabPageVisual[] pages = AddPages(visual, 3);

        tabControl.Pages.ShouldBe(pages.Select(p => p.FormsControl).ToArray());
        visual.ContentPanelInstance.Children.Count.ShouldBe(3);

        tabControl.SelectedIndex = 1;
        pages.Select(p => p.Visible).ShouldBe(new[] { false, true, false });
    }

    [Fact]
    public void PagesAddedBeforeTabs_ShouldPairByPosition()
    {
        (TabControlVisual visual, TabControl tabControl) = CreateEmptyControl();
        TabPageVisual[] pages = AddPages(visual, 2);

        AddTabs(visual, 2);

        tabControl.Pages.ShouldBe(pages.Select(p => p.FormsControl).ToArray());
        visual.ContentPanelInstance.Children.Count.ShouldBe(2);

        tabControl.SelectedIndex = 0;
        pages.Select(p => p.Visible).ShouldBe(new[] { true, false });
    }

    [Fact]
    public void PageRemovedFromPanel_ShouldRepairPairingByPosition()
    {
        (TabControlVisual visual, TabControl tabControl) = CreateEmptyControl();
        AddTabs(visual, 3);
        TabPageVisual[] pages = AddPages(visual, 3);

        pages[1].Parent = null;

        tabControl.Pages.Count.ShouldBe(3);
        tabControl.Pages[0].ShouldBe(pages[0].FormsControl);
        tabControl.Pages[1].ShouldBe(pages[2].FormsControl);
        tabControl.Pages.ShouldNotContain(pages[1].FormsControl);
        visual.ContentPanelInstance.Children.Count.ShouldBe(3);

        tabControl.SelectedIndex = 1;
        pages[2].Visible.ShouldBeTrue();
    }

    [Fact]
    public void PageMovedInPanel_ShouldRepairPairingByPosition()
    {
        (TabControlVisual visual, TabControl tabControl) = CreateEmptyControl();
        AddTabs(visual, 3);
        TabPageVisual[] pages = AddPages(visual, 3);

        visual.ContentPanelInstance.Children.Move(0, 2);

        tabControl.Pages.ShouldBe(new[] { pages[1].FormsControl, pages[2].FormsControl, pages[0].FormsControl });
        tabControl.SelectedIndex = 0;
        pages.Select(p => p.Visible).ShouldBe(new[] { false, true, false });
    }

    [Fact]
    public void OnlyPageRemoved_ShouldLeaveTheTabWithAPlaceholder()
    {
        (TabControlVisual visual, TabControl tabControl) = CreateEmptyControl();
        AddTabs(visual, 1);
        TabPageVisual page = AddPages(visual, 1)[0];

        page.Parent = null;

        tabControl.Pages.Count.ShouldBe(1);
        tabControl.Pages[0].ShouldNotBe(page.FormsControl);
        visual.ContentPanelInstance.Children.Count.ShouldBe(1);
    }

    [Fact]
    public void PagesAddedAfterPagesWereRead_ShouldReplacePlaceholders()
    {
        (TabControlVisual visual, TabControl tabControl) = CreateEmptyControl();
        AddTabs(visual, 2);
        tabControl.Pages.Count.ShouldBe(2);
        visual.ContentPanelInstance.Children.Count.ShouldBe(2);

        TabPageVisual[] pages = AddPages(visual, 2);

        tabControl.Pages.ShouldBe(pages.Select(p => p.FormsControl).ToArray());
        visual.ContentPanelInstance.Children.ToArray().ShouldBe(pages);
    }

    [Fact]
    public void TabItemWithoutContentAddedToItems_ShouldGetAPlaceholderPageAtOnce()
    {
        TabControl tabControl = new TabControl();

        tabControl.Items!.Add(new TabItem { Header = "Empty" });

        tabControl.ContentPanel!.Children.Count.ShouldBe(1);
        tabControl.Pages.Single().Visual.Parent.ShouldBe(tabControl.ContentPanel);
    }

    [Fact]
    public void PagesAddedAfterVisualSwap_ShouldPairWithTheNewPanelOnly()
    {
        TabControlVisual first = new TabControlVisual(tryCreateFormsObject: false);
        TabControl tabControl = new TabControl(first);
        TabControlVisual second = new TabControlVisual(tryCreateFormsObject: false);
        tabControl.Visual = second;
        AddTabs(second, 1);

        TabPageVisual page = AddPages(second, 1)[0];
        AddPages(first, 1);

        tabControl.Pages.ShouldBe(new[] { page.FormsControl });
    }

    #endregion

    #region Input

    private static Mock<IInputReceiverKeyboardMonoGame> CreateKeyboardTyping(Gum.Forms.Input.Keys key)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyTyped(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Gum.Forms.Input.Keys>());
        return keyboard;
    }

    [Fact]
    public void RightKey_ShouldSelectNextEnabledTab()
    {
        TabControl tabControl = CreateWithTitles("A", "B", "C");
        tabControl.SelectedIndex = 0;
        tabControl.TabItems[1].IsEnabled = false;
        FrameworkElement.KeyboardsForUiControl.Add(CreateKeyboardTyping(Gum.Forms.Input.Keys.Right).Object);

        tabControl.OnFocusUpdate();

        tabControl.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void LeftKey_ShouldStayOnFirstTab()
    {
        TabControl tabControl = CreateWithTitles("A", "B");
        tabControl.SelectedIndex = 0;
        FrameworkElement.KeyboardsForUiControl.Add(CreateKeyboardTyping(Gum.Forms.Input.Keys.Left).Object);

        tabControl.OnFocusUpdate();

        tabControl.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void LeftKey_ShouldSelectPreviousTab()
    {
        TabControl tabControl = CreateWithTitles("A", "B");
        tabControl.SelectedIndex = 1;
        FrameworkElement.KeyboardsForUiControl.Add(CreateKeyboardTyping(Gum.Forms.Input.Keys.Left).Object);

        tabControl.OnFocusUpdate();

        tabControl.SelectedIndex.ShouldBe(0);
    }

#if !SKIA_LINKED_TESTS // uses Microsoft.Xna GamePadState
    [Fact]
    public void DPadRight_ShouldSelectNextTab()
    {
        TabControl tabControl = CreateWithTitles("A", "B");
        tabControl.SelectedIndex = 0;
        tabControl.IsFocused = true;

        GamePad gamepad = new();
        GamePadState pressedState = new GamePadState(
            new GamePadThumbSticks(new Microsoft.Xna.Framework.Vector2(0, 0), new Microsoft.Xna.Framework.Vector2(0, 0)),
            new GamePadTriggers(0, 0),
            new GamePadButtons(0),
            new GamePadDPad(
                ButtonState.Released,
                ButtonState.Released,
                ButtonState.Released,
                ButtonState.Pressed));

        InteractiveGue.CurrentGameTime = 0;
        gamepad.Activity(new GamePadState(), 0);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);

        InteractiveGue.CurrentGameTime = 1;
        gamepad.Activity(pressedState, 1);
        tabControl.OnFocusUpdate();

        tabControl.SelectedIndex.ShouldBe(1);
    }
#endif

    #endregion
}
