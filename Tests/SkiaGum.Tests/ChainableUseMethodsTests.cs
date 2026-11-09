using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using RenderingLibrary;
using GumRuntime;
using Shouldly;

namespace SkiaGum.Tests;

/// <summary>
/// Every <c>Use...</c> call returns the concrete <c>GumService</c>, so the calls chain (#7765).
/// </summary>
public class ChainableUseMethodsTests : IDisposable
{
    private readonly Func<Gum.DataTypes.Variables.StateSave, string, string?, GraphicalUiElement?, object?>? _savedExpression =
        ElementSaveExtensions.CustomEvaluateExpression;
    private readonly Func<Gum.ShaderSource, SkiaSharp.SKRuntimeEffect?>? _savedResolver =
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver;

    public void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = _savedExpression;
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = _savedResolver;
        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.Clear();
    }

    [Fact]
    public void UseCalls_ChainOnTheConcreteService()
    {
        GumService service = new GumService();
        FormsUtilities.SetKeyboard(new Moq.Mock<IInputReceiverKeyboard>().Object);

        GumService result = service
            .UseKeyboardDefaults()
            .UseGamepadDefaults()
            .UseSingleThreadedAsync()
            .UseShadowDusk()
            .UseExpressions();

        result.ShouldBeSameAs(service);
        ElementSaveExtensions.CustomEvaluateExpression.ShouldNotBeNull();
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver.ShouldNotBeNull();
    }
}
