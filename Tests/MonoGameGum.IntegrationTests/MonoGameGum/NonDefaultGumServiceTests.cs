using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Gum.Renderables;
using RenderingLibrary;
using RenderingLibrary.Content;
using Shouldly;
using System;
using System.Reflection;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Code that must work on a GumService created with <c>new GumService()</c> while
/// <c>GumService.Default</c> is never initialized (#6066).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class NonDefaultGumServiceTests : BaseTestClass
{
    private static readonly FieldInfo DefaultField =
        typeof(Gum.GumService).GetField("_default", BindingFlags.Static | BindingFlags.NonPublic)!;

    public override void Dispose()
    {
        FrameworkElement.GamePadsForUiControl.Clear();
        ShapeRenderer.Self.Uninitialize();
        base.Dispose();
    }

    [Fact]
    public void UseGamepadDefaults_OnNonDefaultService_ShouldNotCreateDefaultService()
    {
        using NonDefaultServiceGame game = new NonDefaultServiceGame();
        game.RunOneFrame();
        DefaultField.SetValue(null, null);
        // Constructing any GumService reassigns this static, so it shows whether a Default was
        // constructed behind the call, independent of the private field the check above reads.
        Action<GraphicalUiElement> sentinel = _ => { };
        GraphicalUiElement.RefreshLocalizationOnElementAction = sentinel;

        game.GumService.UseGamepadDefaults();

        DefaultField.GetValue(null).ShouldBeNull();
        GraphicalUiElement.RefreshLocalizationOnElementAction.ShouldBeSameAs(sentinel);
        IGumService.Default.ShouldBeSameAs(game.GumService);
        FrameworkElement.GamePadsForUiControl.Count.ShouldBe(game.GumService.Gamepads.Length);
        for (int i = 0; i < game.GumService.Gamepads.Length; i++)
        {
            FrameworkElement.GamePadsForUiControl[i].ShouldBeSameAs(game.GumService.Gamepads[i]);
        }
    }

    [Fact]
    public void ShapeRendererInitialize_OnNonDefaultService_ShouldInitialize()
    {
        using NonDefaultServiceGame game = new NonDefaultServiceGame();
        game.RunOneFrame();
        DefaultField.SetValue(null, null);
        ShapeRenderer.Self.Uninitialize();
        Action<GraphicalUiElement> sentinel = _ => { };
        GraphicalUiElement.RefreshLocalizationOnElementAction = sentinel;

        ShapeRenderer.Self.Initialize();

        ShapeRenderer.Self.IsInitialized.ShouldBeTrue();
        DefaultField.GetValue(null).ShouldBeNull();
        GraphicalUiElement.RefreshLocalizationOnElementAction.ShouldBeSameAs(sentinel);
    }

    private sealed class NonDefaultServiceGame : SharedGameLease
    {
        public Gum.GumService GumService { get; } = new Gum.GumService();

        protected override void OnInitialize() => GumService.Initialize(Game, DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }
    }
}
