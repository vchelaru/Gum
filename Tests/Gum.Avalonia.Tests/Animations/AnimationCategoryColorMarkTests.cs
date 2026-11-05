using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Gum.Avalonia.Plugins.StateAnimation;
using Gum.DataTypes;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests.Animations;

/// <summary>An animation's category color (#7591) is drawn as a dot, the same mark the keyframes and the States tree use.</summary>
public class AnimationCategoryColorMarkTests
{
    private static Border RowMark(string? colorName)
    {
        AnimationViewModel animation = new AnimationViewModel(Mock.Of<ISelectedState>(), Mock.Of<IWireframeObjectManager>());
        animation.AddKeyframe(new AnimatedKeyframeViewModel { StateName = "Size/Small", CategoryColorName = colorName });
        Control row = AnimationsView.CreateAnimationRow();
        Window window = new Window { Content = row, DataContext = animation };
        window.Show();
        window.UpdateLayout();
        return (Border)((Panel)row).Children[0];
    }

    [AvaloniaFact]
    public void AnimationRow_StartsWithARoundDotNotABar_ReservedEvenWhenUncolored()
    {
        Border uncolored = RowMark(null);

        uncolored.Bounds.Width.ShouldBeGreaterThan(0);
        uncolored.Bounds.Width.ShouldBe(uncolored.Bounds.Height);
        uncolored.CornerRadius.TopLeft.ShouldBeGreaterThanOrEqualTo(uncolored.Bounds.Width / 2);
        ((ISolidColorBrush)uncolored.Background!).Color.A.ShouldBe((byte)0);
    }

    [AvaloniaFact]
    public void AnimationRow_Dot_UsesTheCategoryColorFromTheTheme()
    {
        Border colored = RowMark("Indigo");

        colored.Background.ShouldBeAssignableTo<ISolidColorBrush>();
        ((ISolidColorBrush)colored.Background!).Color.A.ShouldBe((byte)255);
        colored.Bounds.Width.ShouldBe(colored.Bounds.Height);
    }

    [AvaloniaFact]
    public void ChangingACategoryColor_UpdatesTheAnimationAndItsKeyframes_ThroughThePluginEvent()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", "Looks", "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk");
        editor.AddStateKeyframe("Looks/Pressed");
        editor.ViewModel.Animations.Single().CategoryColorName.ShouldBeNull();

        button.Categories[0].ColorName = "Teal";
        editor.PluginManager.CategoryColorChange(button.Categories[0]);

        editor.ViewModel.Animations.Single().CategoryColorName.ShouldBe("Teal");
        editor.ViewModel.Animations.Single().Keyframes.Single().CategoryColorName.ShouldBe("Teal");
    }
}
