using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.ObjectModel;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// SetBinding bindings and the BindingContext on a Forms control keep following the view model
/// after the control's Visual is swapped (issue #6335).
/// </summary>
public class BindingAfterVisualSwapTests : BaseTestClass
{
    #region String / bool / numeric bindings keep following

    [Fact]
    public void Button_TextBindingSetBeforeSwap_FollowsViewModelAfterSwap()
    {
        TestViewModel vm = new() { Text = "one" };
        Button sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));

        sut.Visual = new ButtonVisual(tryCreateFormsObject: false);
        vm.Text = "two";

        sut.Text.ShouldBe("two");
    }

    [Fact]
    public void Button_BindingContextSetInCode_SurvivesSwap()
    {
        TestViewModel vm = new();
        Button sut = new() { BindingContext = vm };

        sut.Visual = new ButtonVisual(tryCreateFormsObject: false);

        sut.BindingContext.ShouldBe(vm);
    }

    [Fact]
    public void Button_TextBindingSetAfterSwap_FollowsViewModel()
    {
        TestViewModel vm = new() { Text = "one" };
        Button sut = new() { BindingContext = vm };
        sut.Visual = new ButtonVisual(tryCreateFormsObject: false);

        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        vm.Text = "two";

        sut.Text.ShouldBe("two");
    }

    [Fact]
    public void Button_IsEnabledBinding_FollowsViewModelAfterSwap()
    {
        TestViewModel vm = new() { IsChecked = true };
        Button sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Button.IsEnabled), nameof(TestViewModel.IsChecked));

        sut.Visual = new ButtonVisual(tryCreateFormsObject: false);
        vm.IsChecked = false;

        sut.IsEnabled.ShouldBeFalse();
        sut.Visual.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void CheckBox_IsCheckedTwoWayBinding_FollowsViewModelAfterSwap()
    {
        TestViewModel vm = new() { IsChecked = false };
        CheckBox sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(CheckBox.IsChecked), new Binding(nameof(TestViewModel.IsChecked)) { Mode = BindingMode.TwoWay });

        sut.Visual = new CheckBoxVisual(tryCreateFormsObject: false);
        vm.IsChecked = true;

        sut.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void CheckBox_IsCheckedTwoWayBinding_WritesBackToViewModelAfterSwap()
    {
        TestViewModel vm = new() { IsChecked = false };
        CheckBox sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(CheckBox.IsChecked), new Binding(nameof(TestViewModel.IsChecked)) { Mode = BindingMode.TwoWay });

        sut.Visual = new CheckBoxVisual(tryCreateFormsObject: false);
        sut.IsChecked = true;

        vm.IsChecked.ShouldBeTrue();
    }

    [Fact]
    public void Slider_ValueBinding_FollowsViewModelAfterSwap()
    {
        TestViewModel vm = new() { DoubleValue = 10 };
        Slider sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Slider.Value), nameof(TestViewModel.DoubleValue));

        sut.Visual = new SliderVisual(tryCreateFormsObject: false);
        vm.DoubleValue = 40;

        sut.Value.ShouldBe(40);
    }

    [Fact]
    public void ScrollBar_LargeChangeBinding_FollowsViewModelAfterSwap()
    {
        TestViewModel vm = new() { DoubleValue = 5 };
        ScrollBar sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(ScrollBar.LargeChange), nameof(TestViewModel.DoubleValue));

        sut.Visual = new ScrollBarVisual(fullInstantiation: true, tryCreateFormsObject: false);
        vm.DoubleValue = 25;

        sut.LargeChange.ShouldBe(25);
    }

    [Fact]
    public void TextBox_TextTwoWayBinding_FollowsViewModelAfterSwap()
    {
        TestViewModel vm = new() { Text = "one" };
        TextBox sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(TextBox.Text), new Binding(nameof(TestViewModel.Text)) { Mode = BindingMode.TwoWay });

        sut.Visual = new TextBoxVisual(tryCreateFormsObject: false);
        vm.Text = "two";

        sut.Text.ShouldBe("two");
    }

    [Fact]
    public void Label_NestedPathBinding_FollowsViewModelAfterSwap()
    {
        TestViewModel vm = new() { Child = new TestViewModel { Text = "one" } };
        Label sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Label.Text), new Binding("Child.Text"));

        sut.Visual = new LabelVisual(tryCreateFormsObject: false);
        vm.Child.Text = "two";

        sut.Text.ShouldBe("two");
    }

    #endregion

    #region BindingContext source

    [Fact]
    public void Button_BindingContextInheritedFromParent_FollowsWhenNewVisualIsReparented()
    {
        StackPanel parent = new();
        TestViewModel vm = new() { Text = "one" };
        parent.BindingContext = vm;
        Button sut = new();
        parent.AddChild(sut);
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        InteractiveGue oldVisual = sut.Visual;
        InteractiveGue newVisual = new ButtonVisual(tryCreateFormsObject: false);

        parent.Visual.Children.Remove(oldVisual);
        sut.Visual = newVisual;
        parent.Visual.Children.Add(newVisual);
        vm.Text = "two";

        sut.Text.ShouldBe("two");
    }

    [Fact]
    public void Button_BindingContextInheritedFromParent_FollowsWhenNewVisualParentedBeforeAssigned()
    {
        StackPanel parent = new();
        TestViewModel vm = new() { Text = "one" };
        parent.BindingContext = vm;
        Button sut = new();
        parent.AddChild(sut);
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        InteractiveGue newVisual = new ButtonVisual(tryCreateFormsObject: false);
        parent.Visual.Children.Add(newVisual);

        sut.Visual = newVisual;
        vm.Text = "two";

        sut.Text.ShouldBe("two");
    }

    [Fact]
    public void Button_BindingContextInheritedFromParent_IsNotPinnedToNewVisual()
    {
        StackPanel firstParent = new();
        TestViewModel firstVm = new() { Text = "first" };
        firstParent.BindingContext = firstVm;
        Button sut = new();
        firstParent.AddChild(sut);
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        InteractiveGue newVisual = new ButtonVisual(tryCreateFormsObject: false);
        sut.Visual = newVisual;
        StackPanel secondParent = new();
        TestViewModel secondVm = new() { Text = "second" };
        secondParent.BindingContext = secondVm;

        secondParent.Visual.Children.Add(newVisual);

        sut.BindingContext.ShouldBe(secondVm);
        sut.Text.ShouldBe("second");
    }

    [Fact]
    public void Button_ExplicitBindingContextDiffersFromParent_SurvivesSwapWithoutReparenting()
    {
        StackPanel parent = new();
        parent.BindingContext = new TestViewModel { Text = "parent" };
        TestViewModel ownVm = new() { Text = "own" };
        Button sut = new() { BindingContext = ownVm };
        parent.AddChild(sut);
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));

        sut.Visual = new ButtonVisual(tryCreateFormsObject: false);
        ownVm.Text = "own changed";

        sut.BindingContext.ShouldBe(ownVm);
        sut.Text.ShouldBe("own changed");
    }

    [Fact]
    public void Button_ContextChangedAfterSwap_RepointsBinding()
    {
        TestViewModel first = new() { Text = "first" };
        Button sut = new() { BindingContext = first };
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        sut.Visual = new ButtonVisual(tryCreateFormsObject: false);
        TestViewModel second = new() { Text = "second" };

        sut.BindingContext = second;
        first.Text = "ignored";

        sut.Text.ShouldBe("second");
    }

    [Fact]
    public void Button_SwappedAwayAndBack_UsesCurrentContextNotStaleOne()
    {
        TestViewModel first = new() { Text = "first" };
        Button sut = new() { BindingContext = first };
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        InteractiveGue originalVisual = sut.Visual;
        sut.Visual = new ButtonVisual(tryCreateFormsObject: false);
        TestViewModel second = new() { Text = "second" };
        sut.BindingContext = second;

        sut.Visual = originalVisual;
        second.Text = "second changed";

        sut.BindingContext.ShouldBe(second);
        sut.Text.ShouldBe("second changed");
    }

    [Fact]
    public void Button_BindingContextBoundToChild_FollowsAfterSwap()
    {
        TestViewModel root = new() { Child = new TestViewModel { Text = "one" } };
        StackPanel parent = new() { BindingContext = root };
        Button sut = new();
        parent.AddChild(sut);
        sut.SetBinding(nameof(Button.BindingContext), nameof(TestViewModel.Child));
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        InteractiveGue oldVisual = sut.Visual;
        InteractiveGue newVisual = new ButtonVisual(tryCreateFormsObject: false);

        parent.Visual.Children.Remove(oldVisual);
        sut.Visual = newVisual;
        parent.Visual.Children.Add(newVisual);
        root.Child = new TestViewModel { Text = "two" };

        sut.Text.ShouldBe("two");
    }

    #endregion

    #region ItemsControl

    [Fact]
    public void ListBox_ItemsBinding_FollowsViewModelAfterSwap()
    {
        TestViewModel vm = new()
        {
            Items = [new() { Text = "Item 1" }]
        };
        ListBox sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(ListBox.Items), nameof(TestViewModel.Items));

        sut.Visual = new ListBoxVisual(tryCreateFormsObject: false);
        vm.Items.Add(new TestViewModel { Text = "Item 2" });

        sut.ListBoxItems.Count.ShouldBe(2);
        sut.ListBoxItems[1].BindingContext.ShouldBe(vm.Items[1]);
    }

    [Fact]
    public void ListBoxItem_ItemBindingContext_SurvivesItemVisualSwap()
    {
        TestViewModel vm = new() { Items = [new() { Text = "Item 1" }] };
        ListBox listBox = new() { BindingContext = vm };
        listBox.SetBinding(nameof(ListBox.Items), nameof(TestViewModel.Items));
        ListBoxItem sut = listBox.ListBoxItems[0];

        sut.Visual = new ListBoxItemVisual(tryCreateFormsObject: false);

        sut.BindingContext.ShouldBe(vm.Items[0]);
    }

    #endregion

    #region Values authored on the new Visual still flow to the control

    [Fact]
    public void Button_WithBindingContextAndUnrelatedBinding_NewVisualAuthoredTextIsNotOverwritten()
    {
        TestViewModel vm = new() { IsChecked = true, Text = "from view model" };
        Button sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Button.IsEnabled), nameof(TestViewModel.IsChecked));
        ButtonVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored on new visual";

        sut.Visual = newVisual;

        sut.Text.ShouldBe("Authored on new visual");
    }

    [Fact]
    public void Button_WithBindingContext_NewVisualAuthoredDisabledStaysDisabled()
    {
        TestViewModel vm = new() { Text = "text" };
        Button sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        ButtonVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.IsEnabled = false;

        sut.Visual = newVisual;

        newVisual.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void ScrollViewer_WithBindingContext_NewVisualAuthoredLargeChangeIsKept()
    {
        TestViewModel vm = new();
        ScrollViewer sut = new() { BindingContext = vm };
        ScrollViewerVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);

        sut.Visual = newVisual;

        sut.LargeChange.ShouldBeGreaterThan(0);
        sut.VerticalScrollBar.ShouldNotBeNull();
    }

    #endregion

    #region Bound properties are refreshed from the view model

    [Fact]
    public void Button_TextBinding_ShowsViewModelValueNotNewVisualAuthoredTextAfterSwap()
    {
        TestViewModel vm = new() { Text = "from view model" };
        Button sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        ButtonVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored on new visual";

        sut.Visual = newVisual;

        sut.Text.ShouldBe("from view model");
        newVisual.TextInstance.Text.ShouldBe("from view model");
    }

    [Fact]
    public void Button_TextBindingWithTwoWayMode_ShowsViewModelValueAfterSwap()
    {
        TestViewModel vm = new() { Text = "from view model" };
        Button sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Button.Text), new Binding(nameof(TestViewModel.Text)) { Mode = BindingMode.TwoWay });
        ButtonVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored on new visual";

        sut.Visual = newVisual;

        sut.Text.ShouldBe("from view model");
        vm.Text.ShouldBe("from view model");
    }

    [Fact]
    public void TextBox_TwoWayTextBinding_ShowsViewModelValueAndDoesNotWriteAuthoredTextBackAfterSwap()
    {
        TestViewModel vm = new() { Text = "from view model" };
        TextBox sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(TextBox.Text), new Binding(nameof(TestViewModel.Text)) { Mode = BindingMode.TwoWay });
        int textChangeCount = 0;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(TestViewModel.Text))
            {
                textChangeCount++;
            }
        };
        TextBoxVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored on new visual";

        sut.Visual = newVisual;

        sut.Text.ShouldBe("from view model");
        vm.Text.ShouldBe("from view model");
        textChangeCount.ShouldBe(0);
    }

    [Fact]
    public void TextBox_TwoWayTextBinding_StillWritesUserEditsBackAfterRefresh()
    {
        TestViewModel vm = new() { Text = "from view model" };
        TextBox sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(TextBox.Text), new Binding(nameof(TestViewModel.Text)) { Mode = BindingMode.TwoWay });
        sut.Visual = new TextBoxVisual(fullInstantiation: true, tryCreateFormsObject: false);

        sut.Text = "typed";

        vm.Text.ShouldBe("typed");
    }

    [Fact]
    public void Label_NestedPathBinding_ShowsViewModelValueNotAuthoredTextAfterSwap()
    {
        TestViewModel vm = new() { Child = new TestViewModel { Text = "nested" } };
        Label sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Label.Text), new Binding("Child.Text"));
        LabelVisual newVisual = new(tryCreateFormsObject: false);
        newVisual.Text = "Authored on new visual";

        sut.Visual = newVisual;

        sut.Text.ShouldBe("nested");
    }

    [Fact]
    public void Label_TextBindingWithStringFormat_ShowsFormattedViewModelValueAfterSwap()
    {
        TestViewModel vm = new() { Text = "Alice" };
        Label sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Label.Text), new Binding(nameof(TestViewModel.Text)) { StringFormat = "First: {0}" });
        LabelVisual newVisual = new(tryCreateFormsObject: false);
        newVisual.Text = "Authored on new visual";

        sut.Visual = newVisual;

        sut.Text.ShouldBe("First: Alice");
    }

    [Fact]
    public void Label_TextBindingWithConverter_ShowsConvertedViewModelValueAfterSwap()
    {
        TestViewModel vm = new() { IsChecked = true };
        Label sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Label.Text), new Binding(nameof(TestViewModel.IsChecked)) { Converter = new YesNoConverter() });
        LabelVisual newVisual = new(tryCreateFormsObject: false);
        newVisual.Text = "Authored on new visual";

        sut.Visual = newVisual;

        sut.Text.ShouldBe("Yes");
    }

    [Fact]
    public void Button_SeveralBoundProperties_AllShowViewModelValuesAfterSwap()
    {
        TestViewModel vm = new() { Text = "from view model", IsChecked = false };
        Button sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        sut.SetBinding(nameof(Button.IsEnabled), nameof(TestViewModel.IsChecked));
        ButtonVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored on new visual";

        sut.Visual = newVisual;

        sut.Text.ShouldBe("from view model");
        sut.IsEnabled.ShouldBeFalse();
        newVisual.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void TextBox_BoundTextAndPlaceholder_BothShowViewModelValuesAfterSwap()
    {
        TestViewModel vm = new() { Text = "text", AuxVm = new AuxTestViewModel { Text = "placeholder" } };
        TextBox sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(TextBox.Text), nameof(TestViewModel.Text));
        sut.SetBinding(nameof(TextBox.Placeholder), new Binding("AuxVm.Text"));
        TextBoxVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored text";
        newVisual.PlaceholderTextInstance.Text = "Authored placeholder";

        sut.Visual = newVisual;

        sut.Text.ShouldBe("text");
        sut.Placeholder.ShouldBe("placeholder");
    }

    [Fact]
    public void Button_UnboundTextNextToBoundProperty_KeepsNewVisualAuthoredText()
    {
        TestViewModel vm = new() { IsChecked = true };
        Button sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Button.IsEnabled), nameof(TestViewModel.IsChecked));
        ButtonVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored on new visual";

        sut.Visual = newVisual;

        sut.Text.ShouldBe("Authored on new visual");
    }

    [Fact]
    public void Button_SwappedAwayAndBack_ShowsViewModelValueEachTime()
    {
        TestViewModel vm = new() { Text = "one" };
        Button sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        InteractiveGue originalVisual = sut.Visual;
        ButtonVisual other = new(fullInstantiation: true, tryCreateFormsObject: false);
        other.TextInstance.Text = "Authored on other";

        sut.Visual = other;
        sut.Text.ShouldBe("one");
        vm.Text = "two";
        sut.Visual = originalVisual;

        sut.Text.ShouldBe("two");
    }

    [Fact]
    public void Button_InheritedContextAndNewVisualParentedBeforeAssigned_ShowsViewModelValueAfterSwap()
    {
        StackPanel parent = new();
        TestViewModel vm = new() { Text = "from view model" };
        parent.BindingContext = vm;
        Button sut = new();
        parent.AddChild(sut);
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        ButtonVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored on new visual";
        parent.Visual.Children.Add(newVisual);

        sut.Visual = newVisual;

        sut.Text.ShouldBe("from view model");
    }

    [Fact]
    public void Button_ExplicitContextWithParentContext_ShowsOwnViewModelValueAfterSwap()
    {
        StackPanel parent = new() { BindingContext = new TestViewModel { Text = "parent" } };
        Button sut = new() { BindingContext = new TestViewModel { Text = "own" } };
        parent.AddChild(sut);
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        ButtonVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored on new visual";

        sut.Visual = newVisual;

        sut.Text.ShouldBe("own");
    }

    [Fact]
    public void Button_ViewModelValueNull_UsesBindingNullHandlingAfterSwap()
    {
        TestViewModel vm = new() { Text = null };
        Button sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(Button.Text), new Binding(nameof(TestViewModel.Text)) { FallbackValue = "fallback" });
        ButtonVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored on new visual";

        sut.Visual = newVisual;

        sut.Text.ShouldBe("fallback");
    }

    [Fact]
    public void Button_NoBindingContext_LeavesNewVisualAuthoredTextAfterSwap()
    {
        Button sut = new();
        sut.SetBinding(nameof(Button.Text), nameof(TestViewModel.Text));
        ButtonVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored on new visual";

        sut.Visual = newVisual;

        sut.Text.ShouldBe("Authored on new visual");
    }

    [Fact]
    public void ListBox_BoundItemsAndSelection_AreKeptAfterSwap()
    {
        TestViewModel vm = new()
        {
            Items = [new() { Text = "Item 1" }, new() { Text = "Item 2" }]
        };
        ListBox sut = new() { BindingContext = vm };
        sut.SetBinding(nameof(ListBox.Items), nameof(TestViewModel.Items));
        sut.SelectedIndex = 1;
        ListBoxVisual newVisual = new(tryCreateFormsObject: false);

        sut.Visual = newVisual;

        sut.ListBoxItems.Count.ShouldBe(2);
        newVisual.InnerPanelInstance.Children.Count.ShouldBe(2);
        sut.ListBoxItems[1].BindingContext.ShouldBe(vm.Items[1]);
        sut.SelectedIndex.ShouldBe(1);
    }

    private class YesNoConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter) =>
            value is true ? "Yes" : "No";

        public object? ConvertBack(object? value, Type sourceType, object? parameter) =>
            value is "Yes";
    }

    #endregion

    #region Visual-level (GraphicalUiElement) binding for contrast

    [Fact]
    public void NewVisualWithGueBinding_AssignedToControlWithContext_FollowsViewModel()
    {
        TestViewModel vm = new() { Text = "one" };
        Button sut = new() { BindingContext = vm };
        ButtonVisual newVisual = new(tryCreateFormsObject: false);
        newVisual.SetBinding(nameof(newVisual.X), nameof(TestViewModel.FloatValue));

        sut.Visual = newVisual;
        vm.FloatValue = 77;

        newVisual.X.ShouldBe(77);
    }

    #endregion
}
