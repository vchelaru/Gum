using Gum.DataTypes;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for AnimationContainerViewModel, relocated out of Gum.csproj
/// into the headless Gum.Presentation assembly (ADR-0005, #3754) as a clean leaf VM with zero
/// injected interfaces, even though most of its StateAnimationPlugin.ViewModels siblings remain
/// blocked in Gum.csproj by direct System.Windows.Media.Imaging.BitmapImage usage.
/// </summary>
public class AnimationContainerViewModelTests
{
    [Fact]
    public void Name_ReturnsElementName_WhenNoInstance()
    {
        ElementSave element = new ComponentSave { Name = "MyComponent" };
        AnimationContainerViewModel viewModel = new(element, null);

        viewModel.Name.ShouldBe("MyComponent (container)");
    }

    [Fact]
    public void Name_ReturnsInstanceNameAndBaseType_WhenInstanceProvided()
    {
        ElementSave element = new ComponentSave { Name = "MyComponent" };
        InstanceSave instance = new InstanceSave { Name = "MyButton", BaseType = "Button" };
        AnimationContainerViewModel viewModel = new(element, instance);

        viewModel.Name.ShouldBe("MyButton (Button)");
    }
}
