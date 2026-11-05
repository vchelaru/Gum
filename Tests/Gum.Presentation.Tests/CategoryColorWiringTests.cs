using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.StatePlugin.ViewModels;
using Gum.Services.Dialogs;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Presentation.Tests;

/// <summary>
/// The glue that keeps category colors current after a change (#7591): the States tree re-reading them,
/// the animation view models re-notifying, cloning, the plugin event, and the History tab's wording.
/// </summary>
public class CategoryColorWiringTests : BaseTestClass
{
    [Fact]
    public void StateTree_RefreshTo_RaisesColorNameOnItsCategories_SoTheDotUpdates()
    {
        Mock<ISelectedState> selectedState = new();
        StateTreeViewModel viewModel = new(Mock.Of<IStateTreeViewRightClickService>(), selectedState.Object, Mock.Of<IDialogService>());
        ComponentSave component = new() { Name = "Button" };
        component.States.Add(new StateSave { Name = "Default" });
        StateSaveCategory category = new() { Name = "Size" };
        component.Categories.Add(category);
        selectedState.Setup(x => x.SelectedStateContainer).Returns(component);
        selectedState.Setup(x => x.SelectedElement).Returns(component);
        viewModel.RefreshTo(component, selectedState.Object, new ObjectFinder());
        category.ColorName = "Teal";
        List<string?> raised = new();
        viewModel.Categories.Single().PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        viewModel.RefreshTo(component, selectedState.Object, new ObjectFinder());

        raised.ShouldContain(nameof(CategoryViewModel.ColorName));
        viewModel.Categories.Single().ColorName.ShouldBe("Teal");
    }

    [Fact]
    public void Keyframe_Clone_KeepsTheCategoryColor()
    {
        AnimatedKeyframeViewModel keyframe = new() { StateName = "Size/Small", CategoryColorName = "Indigo" };

        keyframe.Clone().CategoryColorName.ShouldBe("Indigo");
    }

    private static (ComponentSave Element, AnimationViewModel Animation) AnimationOver(params (string State, float Time)[] frames)
    {
        ComponentSave element = new() { Name = "Button" };
        element.States.Add(new StateSave { Name = "Default" });
        element.Categories.Add(new StateSaveCategory { Name = "Size", ColorName = "Indigo", States = { new StateSave { Name = "Small" } } });
        element.Categories.Add(new StateSaveCategory { Name = "Misc", States = { new StateSave { Name = "A" } } });
        AnimationSave save = new() { Name = "Grow" };
        foreach ((string state, float time) in frames)
        {
            save.States.Add(new AnimatedStateSave { StateName = state, Time = time });
        }
        Mock<ISelectedState> selectedState = new();
        selectedState.Setup(s => s.SelectedElement).Returns(element);
        AnimationViewModel animation = AnimationViewModel.FromSave(save, element, Mock.Of<StateAnimationPlugin.Managers.IAnimationSaveRepository>(),
            selectedState.Object, Mock.Of<Gum.Wireframe.IWireframeObjectManager>());
        return (element, animation);
    }

    [Fact]
    public void Animation_RefreshErrors_RaisesCategoryColorNameChanged()
    {
        (ComponentSave element, AnimationViewModel animation) = AnimationOver(("Size/Small", 0f));
        List<string?> raised = new();
        animation.PropertyChanged += (_, e) => raised.Add(e.PropertyName);
        element.Categories[0].ColorName = "Teal";

        animation.RefreshErrors(element);

        raised.ShouldContain(nameof(AnimationViewModel.CategoryColorName));
    }

    [Fact]
    public void Animation_AddingAnEarlierKeyframe_RaisesCategoryColorNameChanged()
    {
        (_, AnimationViewModel animation) = AnimationOver(("Misc/A", 0.5f));
        List<string?> raised = new();
        animation.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        animation.AddKeyframe(new AnimatedKeyframeViewModel { StateName = "Size/Small", Time = 0f, CategoryColorName = "Indigo" });

        raised.ShouldContain(nameof(AnimationViewModel.CategoryColorName));
        animation.CategoryColorName.ShouldBe("Indigo");
    }

    [Fact]
    public void Animation_TakesTheColorOfTheEarliestStateKeyframe_NotTheFirstInTheList()
    {
        AnimationViewModel animation = new(Mock.Of<ISelectedState>(), Mock.Of<Gum.Wireframe.IWireframeObjectManager>());
        animation.Keyframes.Add(new AnimatedKeyframeViewModel { StateName = "Misc/A", Time = 0.5f, CategoryColorName = "Rose" });
        animation.Keyframes.Add(new AnimatedKeyframeViewModel { StateName = "Size/Small", Time = 0f, CategoryColorName = "Indigo" });

        animation.CategoryColorName.ShouldBe("Indigo");
    }

    [Fact]
    public void PluginManager_CategoryColorChange_ReachesPluginsThatSubscribe()
    {
        PluginManager manager = new(Mock.Of<IPluginEnablementStore>(), Mock.Of<IPluginHostConfiguration>());
        ColorPlugin plugin = new();
        manager.Plugins = new List<PluginBase> { plugin };
        PluginManager.StartupPlugin(plugin, manager);
        StateSaveCategory category = new() { Name = "Size" };

        manager.CategoryColorChange(category);

        plugin.Received.ShouldBe(new[] { category });
    }

    private sealed class ColorPlugin : PluginBase
    {
        public List<StateSaveCategory> Received { get; } = new();
        public override string FriendlyName => "ColorPlugin";
        public override void StartUp() => CategoryColorChange += Received.Add;
        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }

    private static ComponentSave ComponentWithColor(string? color)
    {
        ComponentSave component = new() { Name = "B" };
        component.States.Add(new StateSave { Name = "Default" });
        component.Categories.Add(new StateSaveCategory { Name = "Size", ColorName = color });
        return component;
    }

    [Fact]
    public void Undo_Description_NamesAColorOnlyChange()
    {
        string description = UndoSnapshot.CompareAgainst(ComponentWithColor(null), ComponentWithColor("Teal")).ToString();

        description.ShouldBe("Change color of category: Size");
    }

    [Fact]
    public void Undo_Description_IsEmptyWhenTheColorIsTheSame_AndTreatsEmptyAsUnset()
    {
        UndoSnapshot.CompareAgainst(ComponentWithColor("Teal"), ComponentWithColor("Teal")).ToString().ShouldBeEmpty();
        UndoSnapshot.CompareAgainst(ComponentWithColor(null), ComponentWithColor("")).ToString().ShouldBeEmpty();
    }
}
