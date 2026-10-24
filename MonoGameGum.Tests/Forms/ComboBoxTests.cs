using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Mvvm;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using MonoGameGum.Tests.Input;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public  class ComboBoxTests : BaseTestClass
{
    [Fact]
    public void Visual_Assignment_ShouldSetVisualCorrectly()
    {
        ComboBox comboBox = new ();

        comboBox.Visual = new CGComboBox(tryCreateFormsObject:false);
    }

    [Fact]
    public void ListBox_SetToNull_ThrowsAndKeepsCurrentListBox()
    {
        ComboBox comboBox = new();
        ListBox originalListBox = comboBox.ListBox;

        Should.Throw<ArgumentNullException>(() => comboBox.ListBox = null!);

        comboBox.ListBox.ShouldBeSameAs(originalListBox);
    }

    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        ComboBox sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    [Fact]
    public void Clicking_ShouldOpenComboBox_IfInBounds()
    {
        ComboBox comboBox = new ComboBox();

        comboBox.AddToRoot();

        comboBox.IsDropDownOpen.ShouldBe(false);

        Mock<ICursor> cursor = new();
        // combo boxes open on a push
        cursor.Setup(x => x.PrimaryPush).Returns(true);
        cursor.Setup(x => x.WindowPushed).Returns(comboBox.Visual);
        cursor.Setup(x => x.VisualOver).Returns(comboBox.Visual);
        Gum.Forms.FormsUtilities.SetCursor(cursor.Object);

        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        comboBox.IsDropDownOpen.ShouldBe(true);

        // Set it up so it's inside the window, but outside the bounds of the combo box
        cursor.Setup(x => x.X).Returns((int)(GraphicalUiElement.CanvasWidth-1));
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns((int)(GraphicalUiElement.CanvasWidth - 1));
        cursor.Setup(x => x.VisualOver).Returns((InteractiveGue?)null);
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        comboBox.IsDropDownOpen.ShouldBe(false);

    }

    [Fact]
    public void Items_AssignTypedCollection_ShouldSyncInternalListBoxItems()
    {
        // A typed ObservableCollection<string> assigned to ComboBox.Items was a
        // bug vector: HandleCollectionNewItemCreated would try to insert a ListBoxItem
        // into the typed collection, throwing ArgumentException.
        var items = new ObservableCollection<string> { "A", "B", "C" };
        ComboBox comboBox = new();

        comboBox.Items = items;

        comboBox.ListBox!.Items!.Count.ShouldBe(3);
        comboBox.ListBox!.ListBoxItems.Count.ShouldBe(3);
    }

    [Fact]
    public void IsDropDownOpen_ShouldNotResetListBoxItemBindingContext()
    {
        ComboBox comboBox = new();

        comboBox.AddToRoot();

        comboBox.Visual.EffectiveManagers.ShouldNotBeNull(
            "because this is needed to effectively test removal");

        TestViewModel viewModel = new();
        viewModel.Items.Add("1");
        viewModel.Items.Add("2");
        viewModel.Items.Add("3");

        comboBox.BindingContext = viewModel;
        comboBox.SetBinding(
            nameof(comboBox.Items),
            nameof(viewModel.Items));

        comboBox.ListBox!.Items!.Count.ShouldBe(3);
        comboBox.ListBox!.ListBoxItems.Count.ShouldBe(3);
        comboBox.ListBox!.ListBoxItems[0]!.BindingContext.ShouldBe("1");

        comboBox.IsDropDownOpen = true;

        comboBox.ListBox!.ListBoxItems[0]!.BindingContext.ShouldBe("1");

        comboBox.IsDropDownOpen = false;

        comboBox.ListBox!.ListBoxItems[0]!.BindingContext.ShouldBe("1");

    }

    [Fact]
    public void ComboBox_EnterPressed_Opens()
    {
        ComboBox comboBox = new ComboBox();
        comboBox.AddToRoot();
        comboBox.IsDropDownOpen.ShouldBeFalse();

        Mock<Gum.Input.IInputReceiverKeyboardMonoGame> keyboard = new Mock<Gum.Input.IInputReceiverKeyboardMonoGame>();
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeyPushed(Gum.Forms.Input.Keys.Enter)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>()
            .Setup(k => k.KeysTyped).Returns(new List<Gum.Forms.Input.Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);

        comboBox.OnFocusUpdate();

        comboBox.IsDropDownOpen.ShouldBeTrue();
    }

    #region Selection and Text

    [Fact]
    public void Text_NothingSelected_ShouldBeEmpty()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");

        comboBox.SelectedIndex.ShouldBe(-1);
        comboBox.SelectedObject.ShouldBeNull();
        string.IsNullOrEmpty(comboBox.Text).ShouldBeTrue();
    }

    [Fact]
    public void SelectedIndex_Set_ShouldUpdateTextAndSelectedObject()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");

        comboBox.SelectedIndex = 1;

        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.SelectedObject.ShouldBe("Banana");
        comboBox.Text.ShouldBe("Banana");
    }

    [Fact]
    public void SelectedObject_Set_ShouldUpdateTextAndSelectedIndex()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");

        comboBox.SelectedObject = "Cherry";

        comboBox.SelectedIndex.ShouldBe(2);
        comboBox.SelectedObject.ShouldBe("Cherry");
        comboBox.Text.ShouldBe("Cherry");
    }

    [Fact]
    public void SelectedIndex_ChangedTwice_ShouldShowLatestItem()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");

        comboBox.SelectedIndex = 1;
        comboBox.SelectedIndex = 2;

        comboBox.SelectedIndex.ShouldBe(2);
        comboBox.SelectedObject.ShouldBe("Cherry");
        comboBox.Text.ShouldBe("Cherry");
    }

    [Fact]
    public void SelectedIndex_SetToNegativeOne_ShouldClearTextAndSelection()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        comboBox.SelectedIndex = -1;

        comboBox.SelectedIndex.ShouldBe(-1);
        comboBox.SelectedObject.ShouldBeNull();
        string.IsNullOrEmpty(comboBox.Text).ShouldBeTrue();
    }

    [Fact]
    public void SelectedObject_SetToNull_ShouldClearTextAndSelection()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        comboBox.SelectedObject = null;

        comboBox.SelectedIndex.ShouldBe(-1);
        string.IsNullOrEmpty(comboBox.Text).ShouldBeTrue();
    }

    [Fact]
    public void SelectedObject_SetToValueNotInItems_ShouldClearSelection()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        comboBox.SelectedObject = "Durian";

        comboBox.SelectedIndex.ShouldBe(-1);
        comboBox.SelectedObject.ShouldBeNull();
        string.IsNullOrEmpty(comboBox.Text).ShouldBeTrue();
    }

    [Theory]
    [InlineData(-2)]
    [InlineData(3)]
    [InlineData(7)]
    public void SelectedIndex_OutOfRange_ShouldKeepSelectionWithoutSelectionChanged(int index)
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;
        int fireCount = 0;
        comboBox.SelectionChanged += (_, _) => fireCount++;

        comboBox.SelectedIndex = index;

        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.SelectedObject.ShouldBe("Banana");
        comboBox.Text.ShouldBe("Banana");
        fireCount.ShouldBe(0);
    }

    [Fact]
    public void SelectedIndex_DuplicateValues_ShouldSelectRequestedOccurrence()
    {
        ComboBox comboBox = CreateComboBox("A", "B", "A");

        comboBox.SelectedIndex = 2;

        comboBox.SelectedIndex.ShouldBe(2);
        comboBox.Text.ShouldBe("A");
    }

    [Fact]
    public void SelectedIndex_NonStringItems_ShouldShowToString()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.Items!.Add(new Fruit("Apple"));
        comboBox.Items.Add(new Fruit("Banana"));

        comboBox.SelectedIndex = 1;

        comboBox.Text.ShouldBe("Fruit:Banana");
        comboBox.SelectedObject.ShouldBeSameAs(comboBox.Items[1]);
    }

    [Fact]
    public void SelectedIndex_Set_ShouldRaiseSelectionChangedWithAddedItem()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        List<SelectionChangedEventArgs> raised = new();
        comboBox.SelectionChanged += (_, args) => raised.Add(args);

        comboBox.SelectedIndex = 1;

        raised.Count.ShouldBe(1);
        raised[0].AddedItems.Cast<object>().ShouldBe(new object[] { "Banana" });
        raised[0].RemovedItems.Count.ShouldBe(0);
    }

    [Fact]
    public void SelectedIndex_ChangedAfterSelection_ShouldRaiseSelectionChangedWithRemovedItem()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;
        List<SelectionChangedEventArgs> raised = new();
        comboBox.SelectionChanged += (_, args) => raised.Add(args);

        comboBox.SelectedIndex = 2;

        raised.Count.ShouldBe(1);
        raised[0].AddedItems.Cast<object>().ShouldBe(new object[] { "Cherry" });
        raised[0].RemovedItems.Cast<object>().ShouldBe(new object[] { "Banana" });
    }

    [Fact]
    public void RowSelected_ShouldUpdateTextSelectedObjectAndSelectedIndex()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");

        // This is what a mouse push on a drop-down row does.
        comboBox.ListBox.ListBoxItems[2].IsSelected = true;

        comboBox.SelectedIndex.ShouldBe(2);
        comboBox.SelectedObject.ShouldBe("Cherry");
        comboBox.Text.ShouldBe("Cherry");
    }

    [Fact]
    public void RowSelected_ShouldRaiseSelectionChangedOnce()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        List<SelectionChangedEventArgs> raised = new();
        comboBox.SelectionChanged += (_, args) => raised.Add(args);

        comboBox.ListBox.ListBoxItems[1].IsSelected = true;

        raised.Count.ShouldBe(1);
        raised[0].AddedItems.Cast<object>().ShouldBe(new object[] { "Banana" });
    }

    #endregion

    #region Items changes

    [Fact]
    public void Items_RemoveSelectedItem_ShouldClearTextAndSelection()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        comboBox.Items!.RemoveAt(1);

        comboBox.SelectedIndex.ShouldBe(-1);
        comboBox.SelectedObject.ShouldBeNull();
        string.IsNullOrEmpty(comboBox.Text).ShouldBeTrue(
            "because the item the text was showing no longer exists");
    }

    [Fact]
    public void Items_RemoveItemBeforeSelection_ShouldKeepSelectedObjectAndShiftIndex()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 2;

        comboBox.Items!.RemoveAt(0);

        comboBox.SelectedObject.ShouldBe("Cherry");
        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.Text.ShouldBe("Cherry");
    }

    [Fact]
    public void Items_InsertItemBeforeSelection_ShouldKeepSelectedObjectAndShiftIndex()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        comboBox.Items!.Insert(0, "Avocado");

        comboBox.SelectedObject.ShouldBe("Banana");
        comboBox.SelectedIndex.ShouldBe(2);
        comboBox.Text.ShouldBe("Banana");
        comboBox.ListBox.ListBoxItems.Count.ShouldBe(4);
    }

    [Fact]
    public void Items_AddItemAfterSelection_ShouldKeepSelection()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        comboBox.Items!.Add("Durian");

        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.Text.ShouldBe("Banana");
    }

    [Fact]
    public void Items_Clear_ShouldClearTextAndSelection()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        comboBox.Items!.Clear();

        comboBox.SelectedIndex.ShouldBe(-1);
        comboBox.SelectedObject.ShouldBeNull();
        string.IsNullOrEmpty(comboBox.Text).ShouldBeTrue(
            "because there is nothing left to show");
        comboBox.ListBox.ListBoxItems.Count.ShouldBe(0);
    }

    [Fact]
    public void Items_AssignNewCollection_ShouldClearTextAndSelection()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        comboBox.Items = new ObservableCollection<string> { "X", "Y" };

        comboBox.SelectedIndex.ShouldBe(-1);
        string.IsNullOrEmpty(comboBox.Text).ShouldBeTrue(
            "because the previously selected item is not in the new collection");
        comboBox.ListBox.ListBoxItems.Count.ShouldBe(2);
    }

    [Fact]
    public void Items_AssignNull_ShouldNotThrowAndShouldClearRows()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        Should.NotThrow(() => comboBox.Items = null);

        comboBox.ListBox.ListBoxItems.Count.ShouldBe(0);
        comboBox.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void Items_AssignedCollectionWithItemsBeforeSelection_ShouldSelectByIndex()
    {
        ObservableCollection<string> items = new() { "A", "B", "C" };
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.Items = items;

        comboBox.SelectedIndex = 2;

        comboBox.Text.ShouldBe("C");
        items.Add("D");
        comboBox.ListBox.ListBoxItems.Count.ShouldBe(4);
    }

    #endregion

    #region DisplayMemberPath

    [Fact]
    public void DisplayMemberPath_ShouldShowPropertyInText()
    {
        ComboBox comboBox = CreateFruitComboBox();
        comboBox.DisplayMemberPath = nameof(Fruit.Name);

        comboBox.SelectedIndex = 1;

        comboBox.Text.ShouldBe("Banana");
    }

    [Fact]
    public void DisplayMemberPath_SetAfterSelection_ShouldUpdateText()
    {
        ComboBox comboBox = CreateFruitComboBox();
        comboBox.SelectedIndex = 1;
        comboBox.Text.ShouldBe("Fruit:Banana");

        comboBox.DisplayMemberPath = nameof(Fruit.Name);

        comboBox.Text.ShouldBe("Banana");
    }

    [Fact]
    public void DisplayMemberPath_ShouldShowPropertyInDropDownRows()
    {
        ComboBox comboBox = CreateFruitComboBox();

        comboBox.DisplayMemberPath = nameof(Fruit.Name);

        GetRowText(comboBox.ListBox.ListBoxItems[0]).ShouldBe("Apple");
        GetRowText(comboBox.ListBox.ListBoxItems[1]).ShouldBe("Banana");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void DisplayMemberPath_ClearedAfterSet_ShouldRevertToToString(string? clearedValue)
    {
        ComboBox comboBox = CreateFruitComboBox();
        comboBox.DisplayMemberPath = nameof(Fruit.Name);
        comboBox.SelectedIndex = 1;

        comboBox.DisplayMemberPath = clearedValue;

        comboBox.Text.ShouldBe("Fruit:Banana");
        GetRowText(comboBox.ListBox.ListBoxItems[0]).ShouldBe("Fruit:Apple");
    }

    [Fact]
    public void DisplayMemberPath_SetBeforeItemsAdded_ShouldShowPropertyInRows()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.DisplayMemberPath = nameof(Fruit.Name);

        comboBox.Items!.Add(new Fruit("Apple"));

        GetRowText(comboBox.ListBox.ListBoxItems[0]).ShouldBe("Apple");
    }

    #endregion

    #region VisualTemplate (issue 6138)

    [Fact]
    public void VisualTemplate_SelectedIndex_ShouldUpdateTextAndSelectedObject()
    {
        ComboBox comboBox = CreateTemplatedComboBox("Apple", "Banana", "Cherry");

        comboBox.SelectedIndex = 1;

        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.SelectedObject.ShouldBe("Banana");
        comboBox.Text.ShouldBe("Banana");
    }

    [Fact]
    public void VisualTemplate_SelectedObject_ShouldUpdateTextAndSelectedIndex()
    {
        ComboBox comboBox = CreateTemplatedComboBox("Apple", "Banana", "Cherry");

        comboBox.SelectedObject = "Cherry";

        comboBox.SelectedIndex.ShouldBe(2);
        comboBox.Text.ShouldBe("Cherry");
    }

    [Fact]
    public void VisualTemplate_RowSelected_ShouldUpdateTextSelectedObjectAndSelectedIndex()
    {
        ComboBox comboBox = CreateTemplatedComboBox("Apple", "Banana", "Cherry");

        comboBox.ListBox.ListBoxItems[2].IsSelected = true;

        comboBox.SelectedIndex.ShouldBe(2);
        comboBox.SelectedObject.ShouldBe("Cherry");
        comboBox.Text.ShouldBe("Cherry");
    }

    [Fact]
    public void VisualTemplate_SelectedIndexChanged_ShouldRaiseSelectionChangedWithItems()
    {
        ComboBox comboBox = CreateTemplatedComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;
        List<SelectionChangedEventArgs> raised = new();
        comboBox.SelectionChanged += (_, args) => raised.Add(args);

        comboBox.SelectedIndex = 2;

        raised.Count.ShouldBe(1);
        raised[0].AddedItems.Cast<object?>().ShouldBe(new object?[] { "Cherry" });
        raised[0].RemovedItems.Cast<object?>().ShouldBe(new object?[] { "Banana" });
    }

    [Fact]
    public void VisualTemplate_AssignedAfterItemsAdded_ShouldSelectCorrectly()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");

        comboBox.VisualTemplate = new Gum.Forms.VisualTemplate(() => new ListBoxItem().Visual);
        comboBox.SelectedIndex = 1;

        comboBox.SelectedObject.ShouldBe("Banana");
        comboBox.Text.ShouldBe("Banana");
    }

    [Fact]
    public void VisualTemplate_AssignedAfterSelection_ShouldKeepSelectionConsistent()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        comboBox.VisualTemplate = new Gum.Forms.VisualTemplate(() => new ListBoxItem().Visual);

        // Rebuilding the rows must not leave SelectedIndex, SelectedObject and Text disagreeing
        // with each other, whichever way a rebuild resolves the selection.
        bool hasSelection = comboBox.SelectedIndex != -1;
        (comboBox.SelectedObject != null).ShouldBe(hasSelection);
        if (hasSelection)
        {
            comboBox.Items![comboBox.SelectedIndex].ShouldBe(comboBox.SelectedObject);
            comboBox.Text.ShouldBe(comboBox.SelectedObject!.ToString());
        }
        else
        {
            string.IsNullOrEmpty(comboBox.Text).ShouldBeTrue();
        }
    }

    [Fact]
    public void VisualTemplate_AssignedAfterSelection_ShouldBeAbleToSelectAgain()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        comboBox.VisualTemplate = new Gum.Forms.VisualTemplate(() => new ListBoxItem().Visual);
        comboBox.SelectedIndex = 2;

        comboBox.SelectedObject.ShouldBe("Cherry");
        comboBox.Text.ShouldBe("Cherry");
    }

    [Fact]
    public void VisualTemplate_RemoveSelectedItem_ShouldClearTextAndSelection()
    {
        ComboBox comboBox = CreateTemplatedComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        comboBox.Items!.RemoveAt(1);

        comboBox.SelectedIndex.ShouldBe(-1);
        comboBox.SelectedObject.ShouldBeNull();
        string.IsNullOrEmpty(comboBox.Text).ShouldBeTrue();
    }

    [Fact]
    public void VisualTemplate_RemoveItemBeforeSelection_ShouldKeepSelectedObject()
    {
        ComboBox comboBox = CreateTemplatedComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 2;

        comboBox.Items!.RemoveAt(0);

        comboBox.SelectedObject.ShouldBe("Cherry");
        comboBox.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void VisualTemplate_Clear_ShouldClearSelection()
    {
        ComboBox comboBox = CreateTemplatedComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        comboBox.Items!.Clear();

        comboBox.SelectedIndex.ShouldBe(-1);
        comboBox.SelectedObject.ShouldBeNull();
        string.IsNullOrEmpty(comboBox.Text).ShouldBeTrue();
    }

    [Fact]
    public void VisualTemplate_DuplicateValues_ShouldSelectRequestedOccurrence()
    {
        ComboBox comboBox = CreateTemplatedComboBox("A", "B", "A");

        comboBox.SelectedIndex = 2;

        comboBox.SelectedIndex.ShouldBe(2);
        comboBox.Text.ShouldBe("A");
    }

    [Fact]
    public void VisualTemplate_DisplayMemberPath_ShouldShowPropertyInTextAndRows()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.VisualTemplate = new Gum.Forms.VisualTemplate(() => new ListBoxItem().Visual);
        comboBox.DisplayMemberPath = nameof(Fruit.Name);
        comboBox.Items!.Add(new Fruit("Apple"));
        comboBox.Items.Add(new Fruit("Banana"));

        comboBox.SelectedIndex = 1;

        comboBox.Text.ShouldBe("Banana");
        GetRowText(comboBox.ListBox.ListBoxItems[0]).ShouldBe("Apple");
    }

    [Fact]
    public void VisualTemplate_NonStringItems_ShouldKeepSelectedObjectReference()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.VisualTemplate = new Gum.Forms.VisualTemplate(() => new ListBoxItem().Visual);
        Fruit apple = new("Apple");
        Fruit banana = new("Banana");
        comboBox.Items!.Add(apple);
        comboBox.Items.Add(banana);

        comboBox.SelectedIndex = 1;

        comboBox.SelectedObject.ShouldBeSameAs(banana);
        comboBox.Text.ShouldBe("Fruit:Banana");
    }

    [Fact]
    public void VisualTemplate_Bound_SelectedObjectShouldPushToViewModel()
    {
        ComboBox comboBox = CreateTemplatedComboBox("Apple", "Banana", "Cherry");
        SelectionViewModel viewModel = new();
        comboBox.BindingContext = viewModel;
        comboBox.SetBinding(nameof(comboBox.SelectedObject), nameof(viewModel.Selected));

        comboBox.SelectedIndex = 1;

        viewModel.Selected.ShouldBe("Banana");
    }

    #endregion

    #region FrameworkElementTemplate

    [Fact]
    public void FrameworkElementTemplate_SelectedIndex_ShouldUpdateTextAndSelectedObject()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.FrameworkElementTemplate = new Gum.Forms.FrameworkElementTemplate(() => new ListBoxItem());
        comboBox.Items!.Add("Apple");
        comboBox.Items.Add("Banana");
        comboBox.Items.Add("Cherry");

        comboBox.SelectedIndex = 1;

        comboBox.SelectedObject.ShouldBe("Banana");
        comboBox.Text.ShouldBe("Banana");
    }

    [Fact]
    public void FrameworkElementTemplate_RowSelected_ShouldUpdateText()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.FrameworkElementTemplate = new Gum.Forms.FrameworkElementTemplate(() => new ListBoxItem());
        comboBox.Items!.Add("Apple");
        comboBox.Items.Add("Banana");

        comboBox.ListBox.ListBoxItems[1].IsSelected = true;

        comboBox.SelectedObject.ShouldBe("Banana");
        comboBox.Text.ShouldBe("Banana");
    }

    #endregion

    #region Binding

    [Fact]
    public void Binding_SelectedObject_SelectingShouldPushToViewModel()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        SelectionViewModel viewModel = new();
        comboBox.BindingContext = viewModel;
        comboBox.SetBinding(nameof(comboBox.SelectedObject), nameof(viewModel.Selected));

        comboBox.SelectedIndex = 2;

        viewModel.Selected.ShouldBe("Cherry");
    }

    [Fact]
    public void Binding_SelectedObject_ViewModelChangeShouldUpdateText()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        SelectionViewModel viewModel = new();
        comboBox.BindingContext = viewModel;
        comboBox.SetBinding(nameof(comboBox.SelectedObject), nameof(viewModel.Selected));

        viewModel.Selected = "Banana";

        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.Text.ShouldBe("Banana");
    }

    [Fact]
    public void Binding_SelectedIndex_SelectingRowShouldPushToViewModel()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        SelectionViewModel viewModel = new();
        comboBox.BindingContext = viewModel;
        comboBox.SetBinding(nameof(comboBox.SelectedIndex), nameof(viewModel.SelectedIndex));

        comboBox.ListBox.ListBoxItems[1].IsSelected = true;

        viewModel.SelectedIndex.ShouldBe(1);
    }

    [Fact]
    public void Binding_Text_SelectionShouldNotOverwriteBoundText()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        SelectionViewModel viewModel = new() { Display = "From VM" };
        comboBox.BindingContext = viewModel;
        comboBox.SetBinding(nameof(comboBox.Text), nameof(viewModel.Display));

        comboBox.SelectedIndex = 1;

        comboBox.Text.ShouldBe("From VM");
    }

    #endregion

    #region Drop-down

    [Fact]
    public void IsDropDownOpen_True_ShouldPlaceListBoxInPopupRoot()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");

        comboBox.IsDropDownOpen = true;

        comboBox.IsDropDownOpen.ShouldBeTrue();
        comboBox.ListBox.Visual.Parent.ShouldBeSameAs(FrameworkElement.PopupRoot);
    }

    [Fact]
    public void IsDropDownOpen_False_ShouldRemoveListBoxFromPopupRoot()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;

        comboBox.IsDropDownOpen = false;

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.ListBox.Visual.Parent.ShouldBeNull();
        FrameworkElement.PopupRoot.Children.ShouldNotContain(comboBox.ListBox.Visual);
    }

    [Fact]
    public void IsDropDownOpen_OpenedClosedOpened_ShouldShowListBoxAgain()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");

        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;
        comboBox.IsDropDownOpen = true;

        comboBox.IsDropDownOpen.ShouldBeTrue();
        comboBox.ListBox.Visual.Parent.ShouldBeSameAs(FrameworkElement.PopupRoot);
        FrameworkElement.PopupRoot.Children.Count(x => x == comboBox.ListBox.Visual).ShouldBe(1);
    }

    [Fact]
    public void IsDropDownOpen_WithVisualTemplate_ShouldShowRowsForEveryItem()
    {
        ComboBox comboBox = CreateTemplatedComboBox("Apple", "Banana", "Cherry");

        comboBox.IsDropDownOpen = true;

        comboBox.ListBox.ListBoxItems.Count.ShouldBe(3);
        GetRowText(comboBox.ListBox.ListBoxItems[2]).ShouldBe("Cherry");
    }

    [Fact]
    public void IsDropDownOpen_SelectedIndexSetWhileOpen_ShouldUpdateText()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;

        comboBox.SelectedIndex = 2;

        comboBox.Text.ShouldBe("Cherry");
        comboBox.ListBox.ListBoxItems[2].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public void Push_OnDropDownRow_ShouldSelectItemAndCloseDropDown()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        GraphicalUiElement rowVisual = comboBox.ListBox.ListBoxItems[1].Visual;

        PushOn(rowVisual);

        comboBox.SelectedObject.ShouldBe("Banana");
        comboBox.Text.ShouldBe("Banana");
        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void VisualTemplate_Push_OnDropDownRow_ShouldSelectItemAndCloseDropDown()
    {
        ComboBox comboBox = CreateTemplatedComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        GraphicalUiElement rowVisual = comboBox.ListBox.ListBoxItems[2].Visual;

        PushOn(rowVisual);

        comboBox.SelectedObject.ShouldBe("Cherry");
        comboBox.Text.ShouldBe("Cherry");
        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void Push_OutsideOpenDropDown_ShouldCloseWithoutChangingSelection()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 0;
        comboBox.IsDropDownOpen = true;

        PushOn(visualOver: null, frames: 2);

        comboBox.IsDropDownOpen.ShouldBeFalse();
        comboBox.SelectedObject.ShouldBe("Apple");
        comboBox.Text.ShouldBe("Apple");
    }

    [Fact]
    public void Push_OnDisabledComboBox_ShouldNotOpenDropDown()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsEnabled = false;

        PushOn(comboBox.Visual);

        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void Gamepad_DownWhileOpen_ShouldSelectNextItemAndUpdateText()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 0;
        comboBox.IsDropDownOpen = true;

        PressDPad(down: true);
        comboBox.OnFocusUpdate();

        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.Text.ShouldBe("Banana");
    }

    [Fact]
    public void VisualTemplate_Gamepad_DownWhileOpen_ShouldSelectNextItemAndUpdateText()
    {
        ComboBox comboBox = CreateTemplatedComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 0;
        comboBox.IsDropDownOpen = true;

        PressDPad(down: true);
        comboBox.OnFocusUpdate();

        comboBox.SelectedIndex.ShouldBe(1);
        comboBox.Text.ShouldBe("Banana");
    }

    #endregion

    #region Replacing the ListBox

    [Fact]
    public void ListBox_Replaced_SelectingRowShouldUpdateText()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        ListBox replacement = new();
        comboBox.ListBox = replacement;
        comboBox.Items!.Add("Apple");
        comboBox.Items.Add("Banana");

        replacement.ListBoxItems[1].IsSelected = true;

        comboBox.Text.ShouldBe("Banana");
    }

    [Fact]
    public void ListBox_ReplacedAfterDisplayMemberPathSet_ShouldShowPropertyInRows()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.DisplayMemberPath = nameof(Fruit.Name);

        comboBox.ListBox = new ListBox();
        comboBox.Items!.Add(new Fruit("Apple"));

        GetRowText(comboBox.ListBox.ListBoxItems[0]).ShouldBe("Apple");
    }

    [Fact]
    public void ListBox_Replaced_SelectingRowInOldListBoxShouldNotUpdateText()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana");
        ListBox original = comboBox.ListBox;
        comboBox.ListBox = new ListBox();

        original.ListBoxItems[1].IsSelected = true;

        string.IsNullOrEmpty(comboBox.Text).ShouldBeTrue(
            "because the combo box no longer owns the original list box");
    }

    [Fact]
    public void ListBox_ReplacedWithListBoxThatHasSelection_ShouldShowSelectedText()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        ListBox replacement = new();
        replacement.Items!.Add("Apple");
        replacement.Items.Add("Banana");
        replacement.SelectedIndex = 1;

        comboBox.ListBox = replacement;

        comboBox.Text.ShouldBe("Banana");
    }

    [Fact]
    public void ListBox_Replaced_PushOnRowShouldCloseDropDown()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        ListBox replacement = new();
        comboBox.ListBox = replacement;
        comboBox.Items!.Add("Apple");
        comboBox.Items.Add("Banana");
        comboBox.IsDropDownOpen = true;

        PushOn(replacement.ListBoxItems[1].Visual);

        comboBox.Text.ShouldBe("Banana");
        comboBox.IsDropDownOpen.ShouldBeFalse();
    }

    #endregion

    #region Open drop-down follows the ComboBox

    [Fact]
    public void Activity_CanvasResized_OpenDropDownKeepsOffsetFromComboBox()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.Anchor(Anchor.Center);
        comboBox.IsDropDownOpen = true;
        GraphicalUiElement popup = comboBox.ListBox.Visual;
        float offsetX = popup.AbsoluteX - comboBox.Visual.AbsoluteX;
        float offsetY = popup.AbsoluteY - comboBox.Visual.AbsoluteY;
        float comboXAtOpen = comboBox.Visual.AbsoluteX;

        GraphicalUiElement.CanvasWidth = 1200;
        GraphicalUiElement.CanvasHeight = 900;
        Gum.GumService.Default.Root.UpdateLayout();
        comboBox.Visual.AbsoluteX.ShouldNotBe(comboXAtOpen);
        Tick();

        popup.AbsoluteX.ShouldBe(comboBox.Visual.AbsoluteX + offsetX, 0.5f);
        popup.AbsoluteY.ShouldBe(comboBox.Visual.AbsoluteY + offsetY, 0.5f);
        popup.Parent.ShouldBeSameAs(FrameworkElement.PopupRoot);
    }

    [Fact]
    public void Activity_AncestorMoved_OpenDropDownFollowsComboBox()
    {
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.Children!.Add(container);
        ComboBox comboBox = new();
        container.Children!.Add(comboBox.Visual);
        comboBox.Items!.Add("Apple");
        comboBox.Items.Add("Banana");
        comboBox.IsDropDownOpen = true;
        GraphicalUiElement popup = comboBox.ListBox.Visual;
        float offsetX = popup.AbsoluteX - comboBox.Visual.AbsoluteX;
        float offsetY = popup.AbsoluteY - comboBox.Visual.AbsoluteY;

        container.X += 120;
        container.Y += 40;
        container.UpdateLayout();
        Tick();

        popup.AbsoluteX.ShouldBe(comboBox.Visual.AbsoluteX + offsetX, 0.5f);
        popup.AbsoluteY.ShouldBe(comboBox.Visual.AbsoluteY + offsetY, 0.5f);
    }

    [Fact]
    public void Activity_ComboBoxMovesNearBottomEdge_OpenDropDownIsClampedToCanvas()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.Anchor(Anchor.BottomRight);
        comboBox.IsDropDownOpen = true;
        GraphicalUiElement popup = comboBox.ListBox.Visual;

        GraphicalUiElement.CanvasWidth = 700;
        GraphicalUiElement.CanvasHeight = 500;
        Gum.GumService.Default.Root.UpdateLayout();
        // GumService.Update sizes the popup root to the canvas each frame, before the roots animate.
        Gum.Forms.FormsUtilities.SetDimensionsToCanvas(FrameworkElement.PopupRoot);
        FrameworkElement.PopupRoot.UpdateLayout();
        Tick();

        float bottom = popup.AbsoluteY + popup.AbsoluteHeight;
        float right = popup.AbsoluteX + popup.AbsoluteWidth;
        bottom.ShouldBe(GraphicalUiElement.CanvasHeight, 0.5f);
        right.ShouldBeLessThanOrEqualTo(GraphicalUiElement.CanvasWidth + 0.5f);
    }

    [Fact]
    public void Activity_DropDownClosed_DoesNotTouchPopup()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.Anchor(Anchor.Center);
        GraphicalUiElement popup = comboBox.ListBox.Visual;
        float popupXBefore = popup.X;
        float popupYBefore = popup.Y;

        GraphicalUiElement.CanvasWidth = 1200;
        GraphicalUiElement.CanvasHeight = 900;
        Gum.GumService.Default.Root.UpdateLayout();
        Tick();

        comboBox.IsDropDownOpen.ShouldBeFalse();
        popup.Parent.ShouldBeNull();
        popup.X.ShouldBe(popupXBefore);
        popup.Y.ShouldBe(popupYBefore);
    }

    [Fact]
    public void Activity_ComboBoxUnchanged_DoesNotMovePopup()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.IsDropDownOpen = true;
        GraphicalUiElement popup = comboBox.ListBox.Visual;
        // Something other than the ComboBox moves the popup: re-placement must be driven by the
        // ComboBox's rectangle changing, not run every frame.
        popup.X += 30;
        float popupX = popup.X;

        Tick();

        popup.X.ShouldBe(popupX);
    }

    [Fact]
    public void Activity_ComboBoxMoved_OpenDropDownKeepsScrollAndSelection()
    {
        ComboBox comboBox = CreateComboBox();
        for (int i = 0; i < 40; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }
        comboBox.IsDropDownOpen = true;
        comboBox.SelectedIndex = 2;
        comboBox.ListBox.VerticalScrollBarValue = 60;
        double scrollBefore = comboBox.ListBox.VerticalScrollBarValue;
        scrollBefore.ShouldBeGreaterThan(0);
        GraphicalUiElement popup = comboBox.ListBox.Visual;
        float popupYBefore = popup.AbsoluteY;

        comboBox.Y += 50;
        comboBox.Visual.UpdateLayout();
        Tick();

        popup.AbsoluteY.ShouldBe(popupYBefore + 50, 0.5f);
        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(scrollBefore);
        comboBox.SelectedIndex.ShouldBe(2);
        comboBox.IsDropDownOpen.ShouldBeTrue();
    }

    // A ComboBox sized to its text (an in-memory font with 10-pixel A, B and C) in a left-to-right
    // stack with a 10-wide sibling after it, holding "A" and "ABCABC" with "ABCABC" selected.
    private static (ContainerRuntime stack, ComboBox comboBox, ContainerRuntime sibling) CreateTextSizedComboBoxInStack()
    {
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        stack.WidthUnits = DimensionUnitType.RelativeToChildren;
        stack.Width = 0;
        Gum.GumService.Default.Root.Children!.Add(stack);
        ComboBox comboBox = new();
        ComboBoxVisual visual = (ComboBoxVisual)comboBox.Visual;
        visual.TextInstance.XUnits = GeneralUnitType.PixelsFromSmall;
        visual.TextInstance.XOrigin = HorizontalAlignment.Left;
        visual.TextInstance.X = 0;
        visual.TextInstance.WidthUnits = DimensionUnitType.RelativeToChildren;
        visual.TextInstance.Width = 0;
        // No font loads headlessly, so a small in-memory one gives the text a measurable width.
        BitmapFont font = new BitmapFont((Texture2D)null!,
            "info face=\"Arial\" size=-18\n" +
            "common lineHeight=20 base=20 scaleW=256 scaleH=256 pages=1\n" +
            "page id=0 file=\"x.png\"\n" +
            "chars count=3\n" +
            "char id=65 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15\n" +
            "char id=66 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15\n" +
            "char id=67 x=0 y=0 width=10 height=13 xoffset=0 yoffset=4 xadvance=10 page=0 chnl=15\n");
        font.SetFontPattern(256, 256);
        visual.TextInstance.Typeface = font;
        visual.WidthUnits = DimensionUnitType.RelativeToChildren;
        visual.Width = 0;
        stack.Children!.Add(comboBox.Visual);
        ContainerRuntime sibling = new();
        sibling.Width = 10;
        stack.Children.Add(sibling);
        comboBox.Items!.Add("A");
        comboBox.Items.Add("ABCABC");
        comboBox.SelectedIndex = 1;
        return (stack, comboBox, sibling);
    }

    // #6736 keeps #6712's rule: layout a user suspended stays pending through a bulk change until
    // the user's ResumeLayout, for the suspended element and for what is under it.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BulkItemsChange_UnderUserSuspension_WaitsForResumeLayout(bool recursive)
    {
        (ContainerRuntime stack, ComboBox comboBox, ContainerRuntime sibling) = CreateTextSizedComboBoxInStack();
        stack.UpdateLayout();
        float comboWidthBefore = comboBox.Visual.AbsoluteWidth;
        float siblingXBefore = sibling.AbsoluteLeft;
        comboBox.Visual.SuspendLayout(recursive);

        comboBox.Items!.Clear();

        comboBox.Visual.AbsoluteWidth.ShouldBe(comboWidthBefore);
        sibling.AbsoluteLeft.ShouldBe(siblingXBefore);
        comboBox.Visual.ResumeLayout(recursive);
        float siblingXAfterResume = sibling.AbsoluteLeft;
        stack.UpdateLayout();
        siblingXAfterResume.ShouldBeLessThan(siblingXBefore);
        siblingXAfterResume.ShouldBe(sibling.AbsoluteLeft);
    }

    // #6736: a selection change that changes the ComboBox Text must re-lay out what depends on that
    // text (the ComboBox sized to it, the sibling stacked after it), matching a full relayout. The
    // bulk Items changes set the text while all layout is suspended, outside the ListBox's tree.
    [Theory]
    [InlineData("select")]
    [InlineData("deselect")]
    [InlineData("clear")]
    [InlineData("replace")]
    [InlineData("assign")]
    public void SelectionChange_ChangingText_LayoutMatchesFullRelayout(string change)
    {
        (ContainerRuntime stack, ComboBox comboBox, ContainerRuntime sibling) = CreateTextSizedComboBoxInStack();
        stack.UpdateLayout();
        float widthWithLongText = comboBox.Visual.AbsoluteWidth;

        switch (change)
        {
            case "select": comboBox.SelectedIndex = 0; break;
            case "deselect": comboBox.SelectedIndex = -1; break;
            case "clear": comboBox.Items.Clear(); break;
            case "replace": comboBox.Items[1] = "B"; break;
            case "assign": comboBox.Items = new ObservableCollection<object> { "A" }; break;
        }
        float comboWidth = comboBox.Visual.AbsoluteWidth;
        float siblingX = sibling.AbsoluteLeft;
        float stackWidth = stack.AbsoluteWidth;

        stack.UpdateLayout();

        comboBox.Visual.AbsoluteWidth.ShouldBeLessThan(widthWithLongText, "precondition: the width follows the text");
        comboWidth.ShouldBe(comboBox.Visual.AbsoluteWidth);
        siblingX.ShouldBe(sibling.AbsoluteLeft);
        stackWidth.ShouldBe(stack.AbsoluteWidth);
    }

    // ComboBox text is data-driven and deliberately untranslated (gum-localization), on selection
    // and on direct Text assignment.
    [Fact]
    public void Text_WithLocalizationService_StaysUntranslated()
    {
        Gum.Localization.LocalizationService localizationService = new();
        localizationService.AddDatabase(
            new Dictionary<string, string[]> { { "Greeting", new[] { "Hello" } } },
            new List<string> { "English" });
        localizationService.CurrentLanguage = 0;
        CustomSetPropertyOnRenderable.LocalizationService = localizationService;
        try
        {
            ComboBox comboBox = CreateComboBox("Greeting", "Other");

            comboBox.SelectedIndex = 0;
            comboBox.Text.ShouldBe("Greeting");

            comboBox.Text = "Greeting";
            comboBox.Text.ShouldBe("Greeting");
        }
        finally
        {
            CustomSetPropertyOnRenderable.LocalizationService = null;
        }
    }

    [Fact]
    public void Activity_ComboBoxClosed_DoesNotKeepAncestorsFromGoingIdle()
    {
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.Children!.Add(container);
        ComboBox comboBox = new();
        container.Children!.Add(comboBox.Visual);

        Tick();
        Tick();
        Tick();

        container.IsIdle.ShouldBeTrue();
    }

    [Fact]
    public void Activity_ComboBoxOpenedAfterGoingIdle_IsNotIdleUntilClosed()
    {
        ContainerRuntime container = new();
        Gum.GumService.Default.Root.Children!.Add(container);
        ComboBox comboBox = new();
        container.Children!.Add(comboBox.Visual);
        comboBox.Items!.Add("Apple");
        comboBox.Items.Add("Banana");
        Tick();
        Tick();
        Tick();
        container.IsIdle.ShouldBeTrue("precondition: a closed ComboBox lets the subtree go idle");
        GraphicalUiElement popup = comboBox.ListBox.Visual;

        comboBox.IsDropDownOpen = true;
        float popupYAtOpen = popup.AbsoluteY;
        comboBox.Y += 50;
        comboBox.Visual.UpdateLayout();
        Tick();
        Tick();

        container.IsIdle.ShouldBeFalse();
        popup.AbsoluteY.ShouldBe(popupYAtOpen + 50, 0.5f);

        comboBox.IsDropDownOpen = false;
        Tick();
        Tick();
        Tick();

        container.IsIdle.ShouldBeTrue();
    }

    [Fact]
    public void Activity_ComboBoxSizedToChildren_DoesNotPlaceAgainWhileNothingMoves()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        comboBox.Visual.Width = 0;
        comboBox.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        comboBox.Visual.Height = 0;
        comboBox.Visual.UpdateLayout();
        comboBox.IsDropDownOpen = true;
        GraphicalUiElement popup = comboBox.ListBox.Visual;
        // A placement would put the popup back below the ComboBox, undoing this nudge.
        popup.X += 30;
        float popupX = popup.X;

        Tick();
        Tick();
        Tick();

        popup.X.ShouldBe(popupX);
    }

    private static void Tick() => Gum.GumService.Default.Root.AnimateSelf(0.016);

    #endregion

    #region Runtime properties

    [Fact]
    public void SaveAndApplyRuntimeProperties_ShouldRestoreText()
    {
        ComboBox comboBox = CreateComboBox("Apple", "Banana", "Cherry");
        comboBox.SelectedIndex = 1;

        comboBox.SaveRuntimeProperties();
        comboBox.Text = "Changed";
        comboBox.ApplyRuntimeProperties();

        comboBox.Text.ShouldBe("Banana");
    }

    #endregion

    #region Helpers

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

    private static ComboBox CreateTemplatedComboBox(params string[] items)
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.VisualTemplate = new Gum.Forms.VisualTemplate(() => new ListBoxItem().Visual);
        foreach (string item in items)
        {
            comboBox.Items!.Add(item);
        }
        return comboBox;
    }

    private static ComboBox CreateFruitComboBox()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.Items!.Add(new Fruit("Apple"));
        comboBox.Items.Add(new Fruit("Banana"));
        comboBox.Items.Add(new Fruit("Cherry"));
        return comboBox;
    }

    private static string? GetRowText(ListBoxItem row)
    {
        GraphicalUiElement? text = row.Visual.GetGraphicalUiElementByName("TextInstance");
        return (text?.RenderableComponent as IText)?.RawText;
    }

    /// <summary>
    /// Simulates a primary-button push with the cursor over the center of the argument visual
    /// (or the bottom right corner of the canvas for null) and runs frames of UI activity.
    /// </summary>
    /// <param name="frames">
    /// How many frames to run. A "next push action" (such as the combo box closing itself when the
    /// user pushes elsewhere) is only armed on the frame after it was added.
    /// </param>
    private static void PushOn(InteractiveGue? visualOver, int frames = 1)
    {
        float x = GraphicalUiElement.CanvasWidth - 1;
        float y = GraphicalUiElement.CanvasHeight - 1;
        if (visualOver != null)
        {
            x = visualOver.GetAbsoluteX() + visualOver.AbsoluteWidth / 2;
            y = visualOver.GetAbsoluteY() + visualOver.AbsoluteHeight / 2;
        }

        Mock<ICursor> cursor = new();
        cursor.Setup(c => c.PrimaryPush).Returns(true);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.WindowPushed).Returns(visualOver);
        cursor.Setup(c => c.VisualOver).Returns(visualOver);
        cursor.Setup(c => c.X).Returns((int)x);
        cursor.Setup(c => c.Y).Returns((int)y);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(x);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(y);
        Gum.Forms.FormsUtilities.SetCursor(cursor.Object);

        for (int i = 0; i < frames; i++)
        {
            Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
        }
    }

    private static void PushOn(GraphicalUiElement visualOver, int frames = 1) =>
        PushOn(visualOver as InteractiveGue, frames);

    private static void PressDPad(bool down)
    {
        Gum.Input.GamePad gamepad = new();
        gamepad.Activity(
            new Microsoft.Xna.Framework.Input.GamePadState(
                new Microsoft.Xna.Framework.Input.GamePadThumbSticks(
                    new Microsoft.Xna.Framework.Vector2(0, 0),
                    new Microsoft.Xna.Framework.Vector2(0, 0)),
                new Microsoft.Xna.Framework.Input.GamePadTriggers(0, 0),
                new Microsoft.Xna.Framework.Input.GamePadButtons(0),
                new Microsoft.Xna.Framework.Input.GamePadDPad(
                    upValue: Microsoft.Xna.Framework.Input.ButtonState.Released,
                    downValue: down
                        ? Microsoft.Xna.Framework.Input.ButtonState.Pressed
                        : Microsoft.Xna.Framework.Input.ButtonState.Released,
                    leftValue: Microsoft.Xna.Framework.Input.ButtonState.Released,
                    rightValue: Microsoft.Xna.Framework.Input.ButtonState.Released)),
            0);
        FrameworkElement.GamePadsForUiControl.Add(gamepad);
    }

    private class Fruit
    {
        public string Name { get; }

        public Fruit(string name)
        {
            Name = name;
        }

        public override string ToString() => "Fruit:" + Name;
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

        public string? Display
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    #endregion

    public class CGComboBox : InteractiveGue
    {
        public Gum.Forms.DefaultVisuals.V3.ListBoxVisual? ListBoxInstance;
        public RectangleRuntime? FocusedIndicator { get; private set; }

        public CGComboBox(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                var background = new RectangleRuntime();
                background.Name = "Background";

                var TextInstance = new TextRuntime();
                TextInstance.Name = "TextInstance";

                ListBoxInstance = new Gum.Forms.DefaultVisuals.V3.ListBoxVisual(tryCreateFormsObject: false);
                ListBoxInstance.Name = "ListBoxInstance";


                background.Name = "Background";
                this.Children.Add(background);

                TextInstance.Text = "Selected Item";
                this.Children.Add(TextInstance);

                FocusedIndicator = new RectangleRuntime();
                FocusedIndicator.Name = "FocusedIndicator";
                this.Children.Add(FocusedIndicator);

                var rightSideText = new TextRuntime();
                rightSideText.Name = "DropdownIndicator";

                this.Children.Add(rightSideText);

                this.Children.Add(ListBoxInstance);
                ListBoxInstance.Visible = false;

                var comboBoxCategory = new StateSaveCategory();
                comboBoxCategory.Name = "ComboBoxCategory";
                this.AddCategory(comboBoxCategory);

                StateSave currentState;

                void AddState(string name)
                {
                    var state = new StateSave();
                    state.Name = name;
                    comboBoxCategory.States.Add(state);
                    currentState = state;
                }

                AddState(FrameworkElement.DisabledStateName);

                AddState(FrameworkElement.DisabledFocusedStateName);

                AddState(FrameworkElement.EnabledStateName);

                AddState(FrameworkElement.FocusedStateName);

                AddState(FrameworkElement.HighlightedStateName);

                AddState(FrameworkElement.HighlightedFocusedStateName);

                AddState(FrameworkElement.PushedStateName);

            }
            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new ComboBox(this);
            }
        }

        public ComboBox FormsControl => (ComboBox)FormsControlAsObject;
    }

    class TestViewModel : ViewModel
    {
        public ObservableCollection<string> Items { get; set; } = new ObservableCollection<string>();

        public TestViewModel()
        {
        }
    }

    [Fact]
    public void Visual_SetToNull_ShouldStopListeningToListBox()
    {
        ComboBox sut = new();
        ListBox listBox = sut.ListBox;
        listBox.Items!.Add("A");
        int selectionChangedCount = 0;
        sut.SelectionChanged += (_, _) => selectionChangedCount++;

        sut.Visual = null!;
        listBox.SelectedIndex = 0;

        selectionChangedCount.ShouldBe(0);
    }

    [Fact]
    public void ItemsReplace_OfSelectedItem_ClearsSelectionAndText()
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        ObservableCollection<object> items = new(new object[] { "A", "B", "C" });
        comboBox.Items = items;
        comboBox.SelectedObject = "B";
        comboBox.Text.ShouldBe("B");

        items[1] = "X";

        comboBox.SelectedObject.ShouldBeNull();
        comboBox.SelectedIndex.ShouldBe(-1);
        comboBox.Text.ShouldBeNullOrEmpty();
        comboBox.ListBox.ListBoxItems.Select(row => row.DataObject).ShouldBe(new object[] { "A", "X", "C" });
    }
}
