using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Mvvm;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Item controls write an item's display text into their TextInstance. A text that is data bound
/// (on the text visual or on the control's own text property) keeps its bound value instead.
/// </summary>
public class ItemTextBindingTests : BaseTestClass
{
    private class ItemViewModel : ViewModel
    {
        public string? Name { get => Get<string?>(); set => Set(value); }
        public string? Label { get => Get<string?>(); set => Set(value); }
    }

    private static TextRuntime TextInstanceOf(FrameworkElement element) =>
        (TextRuntime)element.Visual.GetGraphicalUiElementByName("TextInstance")!;

    [Fact]
    public void ListBox_StringItem_RowShowsString()
    {
        ListBox listBox = new();

        listBox.Items!.Add("Sword");

        TextInstanceOf(listBox.ListBoxItems[0]).Text.ShouldBe("Sword");
    }

    [Fact]
    public void ListBox_VisualTemplateWithoutTextBinding_RowShowsToString()
    {
        ListBox listBox = new();
        listBox.VisualTemplate = new VisualTemplate(() => new ListBoxItemVisual());
        ItemViewModel item = new() { Name = "Sword" };

        listBox.Items!.Add(item);

        TextInstanceOf(listBox.ListBoxItems[0]).Text.ShouldBe(item.ToString());
    }

    [Fact]
    public void ListBox_VisualTemplateWithBoundTextInstance_RowShowsBoundValue()
    {
        ListBox listBox = new();
        listBox.VisualTemplate = new VisualTemplate(() =>
        {
            ListBoxItemVisual visual = new();
            visual.TextInstance.SetBinding(nameof(TextRuntime.Text), nameof(ItemViewModel.Name));
            return visual;
        });

        listBox.Items!.Add(new ItemViewModel { Name = "Sword" });

        TextInstanceOf(listBox.ListBoxItems[0]).Text.ShouldBe("Sword");
    }

    [Fact]
    public void ListBox_FrameworkElementTemplateWithBoundTextInstance_RowShowsBoundValue()
    {
        ListBox listBox = new();
        listBox.FrameworkElementTemplate = new FrameworkElementTemplate(() =>
        {
            ListBoxItem listBoxItem = new();
            TextInstanceOf(listBoxItem).SetBinding(nameof(TextRuntime.Text), nameof(ItemViewModel.Name));
            return listBoxItem;
        });

        listBox.Items!.Add(new ItemViewModel { Name = "Sword" });

        TextInstanceOf(listBox.ListBoxItems[0]).Text.ShouldBe("Sword");
    }

    [Fact]
    public void ListBox_DisplayMemberPathSetWithBoundTextInstance_RowKeepsBoundValue()
    {
        ListBox listBox = new();
        listBox.VisualTemplate = new VisualTemplate(() =>
        {
            ListBoxItemVisual visual = new();
            visual.TextInstance.SetBinding(nameof(TextRuntime.Text), nameof(ItemViewModel.Name));
            return visual;
        });
        listBox.Items!.Add(new ItemViewModel { Name = "Sword", Label = "Label" });

        listBox.DisplayMemberPath = nameof(ItemViewModel.Label);

        TextInstanceOf(listBox.ListBoxItems[0]).Text.ShouldBe("Sword");
    }

    [Fact]
    public void ComboBox_BoundTextInstance_SelectionKeepsBoundValue()
    {
        ComboBox comboBox = new();
        TextInstanceOf(comboBox).SetBinding(nameof(TextRuntime.Text), nameof(ItemViewModel.Name));
        comboBox.BindingContext = new ItemViewModel { Name = "Bound" };
        comboBox.Items!.Add("Sword");

        comboBox.SelectedIndex = 0;

        TextInstanceOf(comboBox).Text.ShouldBe("Bound");
    }

    [Fact]
    public void ComboBox_BoundTextDisplayMemberPathSet_TextKeepsBoundValue()
    {
        ComboBox comboBox = new();
        comboBox.SetBinding(nameof(ComboBox.Text), nameof(ItemViewModel.Name));
        comboBox.BindingContext = new ItemViewModel { Name = "Bound" };

        comboBox.DisplayMemberPath = nameof(ItemViewModel.Label);

        comboBox.Text.ShouldBe("Bound");
    }

    [Fact]
    public void MenuItem_BoundTextInstance_UpdateToObjectKeepsBoundValue()
    {
        MenuItem menuItem = new();
        TextInstanceOf(menuItem).SetBinding(nameof(TextRuntime.Text), nameof(ItemViewModel.Name));
        ItemViewModel item = new() { Name = "Sword" };
        menuItem.BindingContext = item;

        menuItem.UpdateToObject(item);

        TextInstanceOf(menuItem).Text.ShouldBe("Sword");
    }

    [Fact]
    public void MenuItem_BoundHeader_UpdateToObjectKeepsBoundValue()
    {
        MenuItem menuItem = new();
        menuItem.SetBinding(nameof(MenuItem.Header), nameof(ItemViewModel.Name));
        ItemViewModel item = new() { Name = "Sword" };
        menuItem.BindingContext = item;

        menuItem.UpdateToObject(item);

        menuItem.Header.ShouldBe("Sword");
    }

    [Fact]
    public void MenuItem_UnboundText_UpdateToObjectShowsToString()
    {
        MenuItem menuItem = new();

        menuItem.UpdateToObject("Sword");

        menuItem.Header.ShouldBe("Sword");
    }
}
