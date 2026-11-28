using System;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.Data;
using Gum.GueDeriving;
using Gum.Mvvm;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Bindings that drive a generated-code-style category state property (Forms and visual), visual-side
/// bindings whose source and target types differ, and visual-side context inheritance through
/// re-parenting.
/// </summary>
public class BindingStateCategoryAndVisualTypeTests : BaseTestClass
{
    public enum AffordCategory
    {
        CanAfford,
        CannotAfford
    }

    private class ShopViewModel : ViewModel
    {
        public int Gold { get => Get<int>(); set => Set(value); }

        [DependsOn(nameof(Gold))]
        public bool CanAfford => Gold >= 10;

        [DependsOn(nameof(Gold))]
        public AffordCategory AffordState => CanAfford ? AffordCategory.CanAfford : AffordCategory.CannotAfford;

        public string? Label { get => Get<string?>(); set => Set(value); }
    }

    private static StateSaveCategory CreateAffordCategory()
    {
        StateSaveCategory category = new() { Name = nameof(AffordCategory) };
        StateSave canAfford = new() { Name = nameof(AffordCategory.CanAfford) };
        canAfford.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 200f, SetsValue = true });
        StateSave cannotAfford = new() { Name = nameof(AffordCategory.CannotAfford) };
        cannotAfford.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 50f, SetsValue = true });
        category.States.Add(canAfford);
        category.States.Add(cannotAfford);
        return category;
    }

    /// <summary>Mirrors the shape the Gum tool's code generator emits for a Forms component category.</summary>
    private class ShopItemButton : Button
    {
        private AffordCategory? _affordCategoryState;

        public ShopItemButton()
        {
            Visual.AddCategory(CreateAffordCategory());
        }

        public AffordCategory? AffordCategoryState
        {
            get => _affordCategoryState;
            set
            {
                _affordCategoryState = value;
                if (value != null)
                {
                    StateSaveCategory category = Visual.Categories[nameof(AffordCategory)];
                    Visual.ApplyState(category.States.Find(item => item.Name == value.ToString())!);
                }
            }
        }
    }

    /// <summary>Mirrors the shape the code generator emits for a visual-only (runtime) component category.</summary>
    private class ShopItemRuntime : ContainerRuntime
    {
        private AffordCategory? _affordCategoryState;

        public ShopItemRuntime()
        {
            AddCategory(CreateAffordCategory());
        }

        public AffordCategory? AffordCategoryState
        {
            get => _affordCategoryState;
            set
            {
                _affordCategoryState = value;
                if (value != null)
                {
                    ApplyState(Categories[nameof(AffordCategory)].States.Find(item => item.Name == value.ToString())!);
                }
            }
        }
    }

    private class BoolToAffordConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter) =>
            value is true ? AffordCategory.CanAfford : AffordCategory.CannotAfford;

        public object? ConvertBack(object? value, Type sourceType, object? parameter) => Binding.DoNothing;
    }

    [Fact]
    public void FormsCategoryState_BoundToEnumViewModelProperty_AppliesStateOnChange()
    {
        ShopViewModel viewModel = new() { Gold = 0 };
        ShopItemButton button = new() { BindingContext = viewModel };
        button.SetBinding(nameof(ShopItemButton.AffordCategoryState), nameof(ShopViewModel.AffordState));

        button.Visual.Width.ShouldBe(50f);
        viewModel.Gold = 10;
        button.Visual.Width.ShouldBe(200f);
        button.AffordCategoryState.ShouldBe(AffordCategory.CanAfford);
    }

    [Fact]
    public void FormsCategoryState_BoundToBoolThroughConverter_AppliesStateOnChange()
    {
        ShopViewModel viewModel = new() { Gold = 0 };
        ShopItemButton button = new() { BindingContext = viewModel };
        button.SetBinding(nameof(ShopItemButton.AffordCategoryState), new Binding(nameof(ShopViewModel.CanAfford))
        {
            Mode = BindingMode.OneWay,
            Converter = new BoolToAffordConverter()
        });

        button.Visual.Width.ShouldBe(50f);
        viewModel.Gold = 15;
        button.Visual.Width.ShouldBe(200f);
    }

    [Fact]
    public void FormsCategoryStateAndIsEnabled_BoundToSameViewModel_BothFollow()
    {
        ShopViewModel viewModel = new() { Gold = 0 };
        ShopItemButton button = new() { BindingContext = viewModel };
        button.SetBinding(nameof(ShopItemButton.AffordCategoryState), nameof(ShopViewModel.AffordState));
        button.SetBinding(nameof(Button.IsEnabled), nameof(ShopViewModel.CanAfford));

        viewModel.Gold = 10;

        button.IsEnabled.ShouldBeTrue();
        button.Visual.Width.ShouldBe(200f);
        viewModel.Gold = 1;
        button.IsEnabled.ShouldBeFalse();
        button.Visual.Width.ShouldBe(50f);
    }

    [Fact]
    public void VisualCategoryState_BoundToEnumViewModelProperty_AppliesStateOnChange()
    {
        ShopViewModel viewModel = new() { Gold = 0 };
        ShopItemRuntime runtime = new() { BindingContext = viewModel };
        runtime.SetBinding(nameof(ShopItemRuntime.AffordCategoryState), nameof(ShopViewModel.AffordState));

        runtime.Width.ShouldBe(50f);
        viewModel.Gold = 10;
        runtime.Width.ShouldBe(200f);
    }

    [Fact]
    public void VisualBinding_IntSourceToFloatTarget_Converts()
    {
        ShopViewModel viewModel = new() { Gold = 12 };
        ContainerRuntime container = new() { BindingContext = viewModel };

        container.SetBinding(nameof(ContainerRuntime.Width), nameof(ShopViewModel.Gold));

        container.Width.ShouldBe(12f);
    }

    [Fact]
    public void VisualBinding_IntSourceToStringTarget_UsesFormat()
    {
        ShopViewModel viewModel = new() { Gold = 1234 };
        TextRuntime text = new() { BindingContext = viewModel };

        text.SetBinding(nameof(TextRuntime.Text), nameof(ShopViewModel.Gold), "N0");

        text.Text.ShouldBe(1234.ToString("N0"));
    }

    [Fact]
    public void VisualBinding_BoolSourceToVisible_Follows()
    {
        ShopViewModel viewModel = new() { Gold = 0 };
        ContainerRuntime container = new() { BindingContext = viewModel };
        container.SetBinding(nameof(ContainerRuntime.Visible), nameof(ShopViewModel.CanAfford));

        container.Visible.ShouldBeFalse();
        viewModel.Gold = 10;
        container.Visible.ShouldBeTrue();
    }

    // A visual binds on the Forms engine, which sets the target's default when the source value cannot convert
    // (VisualBindingEnginePinTests.Forms_StringToBool_SetsDefault).
    [Fact]
    public void VisualBinding_SourceTypeCannotConvert_SetsTargetDefault()
    {
        ShopViewModel viewModel = new() { Label = "yes" };
        ContainerRuntime container = new() { Visible = true, BindingContext = viewModel };

        container.SetBinding(nameof(ContainerRuntime.Visible), nameof(ShopViewModel.Label));

        container.Visible.ShouldBeFalse();
    }

    [Fact]
    public void VisualBinding_InheritedContext_ChildReparentedToOtherContext_FollowsNewContext()
    {
        ShopViewModel first = new() { Label = "first" };
        ShopViewModel second = new() { Label = "second" };
        ContainerRuntime parentA = new() { BindingContext = first };
        ContainerRuntime parentB = new() { BindingContext = second };
        TextRuntime text = new();
        text.SetBinding(nameof(TextRuntime.Text), nameof(ShopViewModel.Label));
        parentA.AddChild(text);

        text.Parent = parentB;
        first.Label = "first changed";

        text.Text.ShouldBe("second");
        second.Label = "second changed";
        text.Text.ShouldBe("second changed");
    }

    [Fact]
    public void VisualBinding_InheritedContext_ChildUnparented_StopsFollowing()
    {
        ShopViewModel viewModel = new() { Label = "start" };
        ContainerRuntime parent = new() { BindingContext = viewModel };
        TextRuntime text = new();
        text.SetBinding(nameof(TextRuntime.Text), nameof(ShopViewModel.Label));
        parent.AddChild(text);

        text.Parent = null;
        viewModel.Label = "after";

        text.Text.ShouldBe("start");
    }

    [Fact]
    public void VisualBinding_ParentContextReplaced_ChildFollowsNewContext()
    {
        ShopViewModel first = new() { Label = "first" };
        ShopViewModel second = new() { Label = "second" };
        ContainerRuntime parent = new() { BindingContext = first };
        TextRuntime text = new();
        text.SetBinding(nameof(TextRuntime.Text), nameof(ShopViewModel.Label));
        parent.AddChild(text);

        parent.BindingContext = second;
        first.Label = "stale";

        text.Text.ShouldBe("second");
    }

    [Fact]
    public void VisualBinding_ChildExplicitContext_OverridesParentReplacement()
    {
        ShopViewModel parentVm = new() { Label = "parent" };
        ShopViewModel childVm = new() { Label = "child" };
        ContainerRuntime parent = new() { BindingContext = parentVm };
        TextRuntime text = new() { BindingContext = childVm };
        text.SetBinding(nameof(TextRuntime.Text), nameof(ShopViewModel.Label));
        parent.AddChild(text);

        parent.BindingContext = new ShopViewModel { Label = "other" };

        text.Text.ShouldBe("child");
    }
}
