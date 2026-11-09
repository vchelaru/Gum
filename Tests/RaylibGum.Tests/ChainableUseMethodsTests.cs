using Gum;
using Gum.Forms.Controls;
using Gum.Forms;
using Gum.Input;
using KernSmith.Gum;
using RaylibGum.Renderables;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests;

/// <summary>
/// Every <c>Use...</c> call returns the concrete raylib <c>GumService</c>, so the calls chain (#7765).
/// </summary>
public class ChainableUseMethodsTests : BaseTestClass
{
    private readonly IRaylibFontCreator? _savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
    private readonly Func<Gum.ShaderSource, object?>? _savedResolver =
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver;
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.InMemoryFontCreator = _savedCreator;
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = _savedResolver;
        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.Clear();
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
        base.Dispose();
    }

    private static Gum.GumService Service => Gum.GumService.Default;

    [Fact]
    public void UseKernSmith_SetsTheRaylibKernSmithFontCreator()
    {
        CustomSetPropertyOnRenderable.InMemoryFontCreator = null;

        Service.UseKernSmith();

        CustomSetPropertyOnRenderable.InMemoryFontCreator.ShouldBeOfType<KernSmithRaylibFontCreator>();
    }

    [Fact]
    public void UseCalls_ChainOnTheConcreteService()
    {
        FormsUtilities.SetKeyboard(new Mock<IInputReceiverKeyboard>().Object);

        Gum.GumService result = Service
            .UseKeyboardDefaults()
            .UseGamepadDefaults()
            .UseShadowDusk()
            .UseKernSmith()
            .UseExpressions()
            .UseSingleThreadedAsync();

        result.ShouldBeSameAs(Service);
    }

}
