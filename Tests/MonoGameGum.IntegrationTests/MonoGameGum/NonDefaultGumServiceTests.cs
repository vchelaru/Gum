using Gum.Forms;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using MonoGameAndGum.Renderables;
using RenderingLibrary.Content;
using Shouldly;
using System.Reflection;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Code that must work on a GumService created with <c>new GumService()</c> while
/// <c>GumService.Default</c> is never initialized (#6066).
/// </summary>
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

        game.GumService.UseGamepadDefaults();

        DefaultField.GetValue(null).ShouldBeNull();
        FrameworkElement.GamePadsForUiControl.ShouldBe(game.GumService.Gamepads);
    }

    [Fact]
    public void ShapeRendererInitialize_OnNonDefaultService_ShouldInitialize()
    {
        using NonDefaultServiceGame game = new NonDefaultServiceGame();
        game.RunOneFrame();
        DefaultField.SetValue(null, null);
        ShapeRenderer.Self.Uninitialize();

        ShapeRenderer.Self.Initialize();

        ShapeRenderer.Self.IsInitialized.ShouldBeTrue();
        DefaultField.GetValue(null).ShouldBeNull();
    }

    private sealed class NonDefaultServiceGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        public Gum.GumService GumService { get; } = new Gum.GumService();

        public NonDefaultServiceGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumService.Initialize(this, DefaultVisualsVersion.V3);
        }

        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
            base.Dispose(disposing);
        }
    }
}
