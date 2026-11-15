using Gum;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using RenderingLibrary;
using Shouldly;
using SkiaGum;
using Xunit;

namespace SkiaHostTests;

/// <summary>
/// Every <c>Use...</c> call returns the host's concrete <c>GumService</c>, so the calls chain (#7765).
/// A host's test project derives from this class with its own <c>Gum.GumService</c> as
/// <typeparamref name="TService"/>: the chain below compiles to that exact type only when no call in it
/// widens the result to a base type.
/// </summary>
public abstract class ChainableUseMethodsTestsBase<TService> : IDisposable
    where TService : GumServiceSkiaBase
{
    private readonly IInputReceiverKeyboard? _originalKeyboard = FormsUtilities.Keyboard;
    private readonly Func<Gum.DataTypes.Variables.StateSave, string, string?, GraphicalUiElement?, object?>? _savedExpression =
        ElementSaveExtensions.CustomEvaluateExpression;
    private readonly Func<Gum.ShaderSource, SkiaSharp.SKRuntimeEffect?>? _savedResolver =
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver;

    private static TService Service => (TService)IGumService.Default!;

    public void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = _savedExpression;
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = _savedResolver;
        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.Clear();
        if (_originalKeyboard != null)
        {
            FormsUtilities.SetKeyboard(_originalKeyboard);
        }
    }

    [Fact]
    public void UseKeyboardDefaults_ReturnsTheSameService()
    {
        FormsUtilities.SetKeyboard(new Mock<IInputReceiverKeyboard>().Object);

        TService result = Service.UseKeyboardDefaults();

        result.ShouldBeSameAs(Service);
    }

    [Fact]
    public void UseGamepadDefaults_ReturnsTheSameService()
    {
        TService result = Service.UseGamepadDefaults();

        result.ShouldBeSameAs(Service);
    }

    [Fact]
    public void UseSingleThreadedAsync_ReturnsTheSameService()
    {
        TService result = Service.UseSingleThreadedAsync();

        result.ShouldBeSameAs(Service);
    }

    [Fact]
    public void UseShadowDusk_ReturnsTheSameServiceAndRegistersTheResolver()
    {
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = null;

        TService result = Service.UseShadowDusk();

        result.ShouldBeSameAs(Service);
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver.ShouldNotBeNull();
    }

    [Fact]
    public void UseExpressions_ReturnsTheSameServiceAndEnablesTheEvaluator()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;

        TService result = Service.UseExpressions();

        result.ShouldBeSameAs(Service);
        ElementSaveExtensions.CustomEvaluateExpression.ShouldNotBeNull();
    }

    [Fact]
    public void UseCalls_ChainOnTheHostsConcreteService()
    {
        FormsUtilities.SetKeyboard(new Mock<IInputReceiverKeyboard>().Object);

        TService result = Service
            .UseKeyboardDefaults()
            .UseGamepadDefaults()
            .UseSingleThreadedAsync()
            .UseShadowDusk()
            .UseExpressions();

        result.ShouldBeSameAs(Service);
    }
}
