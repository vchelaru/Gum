using Gum;
using Gum.Expressions;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Gum.DataTypes.Variables;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Every <c>Use...</c> call returns the host's concrete <c>GumService</c>, so the calls chain (#7765):
/// <c>GumUI.UseKeyboardDefaults().UseGamepadDefaults().UseExpressions()</c>.
/// </summary>
public class ChainableUseMethodsTests : BaseTestClass
{
    private readonly Func<StateSave, string, string?, GraphicalUiElement?, object?>? _savedExpression =
        ElementSaveExtensions.CustomEvaluateExpression;

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = _savedExpression;
        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.Clear();
        base.Dispose();
    }

    private static Gum.GumService Service => Gum.GumService.Default;

    [Fact]
    public void UseKeyboardDefaults_ReturnsTheSameService()
    {
        FormsUtilities.SetKeyboard(new Keyboard());

        Gum.GumService result = Service.UseKeyboardDefaults();

        result.ShouldBeSameAs(Service);
    }

    [Fact]
    public void UseGamepadDefaults_ReturnsTheSameService()
    {
        Gum.GumService result = Service.UseGamepadDefaults();

        result.ShouldBeSameAs(Service);
    }

    [Fact]
    public void UseSingleThreadedAsync_ReturnsTheSameService()
    {
        Gum.GumService result = Service.UseSingleThreadedAsync();

        result.ShouldBeSameAs(Service);
    }

    [Fact]
    public void UseExpressions_EnablesTheEvaluator()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;

        Service.UseExpressions();

        ElementSaveExtensions.CustomEvaluateExpression.ShouldNotBeNull();
    }

    [Fact]
    public void UseExpressions_ReturnsTheSameServiceType()
    {
        Gum.GumService result = Service.UseExpressions();

        result.ShouldBeSameAs(Service);
    }

    [Fact]
    public void UseCalls_ChainOnTheConcreteService()
    {
        FormsUtilities.SetKeyboard(new Keyboard());

        Gum.GumService result = Service
            .UseKeyboardDefaults()
            .UseGamepadDefaults()
            .UseSingleThreadedAsync()
            .UseExpressions();

        result.ShouldBeSameAs(Service);
    }
}
