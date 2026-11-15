using Gum;
using Gum.Forms;
using Gum.Renderables;
using KernSmith.Gum;
using Gum.Wireframe;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// Each optional extra is an explicit, chainable <c>Use...</c> call on the service (#7765).
/// <c>Initialize</c> never enables one by itself.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ChainableUseAddOnTests : BaseTestClass
{
    private readonly IInMemoryFontCreator? _savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
    private readonly Func<Gum.ShaderSource, object?>? _savedShaderResolver =
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver;

    public override void Dispose()
    {
        CustomSetPropertyOnRenderable.InMemoryFontCreator = _savedCreator;
        CustomSetPropertyOnRenderable.RenderTargetEffectSourceResolver = _savedShaderResolver;
        ShapeRenderer.Self.Uninitialize();
        base.Dispose();
    }

    [Fact]
    public void UseShapes_InitializesTheShapeRenderer()
    {
        using UseGame game = new UseGame();
        game.RunOneFrame();
        ShapeRenderer.Self.Uninitialize();

        game.GumService.UseShapes();

        ShapeRenderer.Self.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void UseShapes_CalledTwice_DoesNotThrow()
    {
        using UseGame game = new UseGame();
        game.RunOneFrame();
        ShapeRenderer.Self.Uninitialize();

        game.GumService.UseShapes().UseShapes();

        ShapeRenderer.Self.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void UseKernSmith_SetsTheKernSmithFontCreator()
    {
        using UseGame game = new UseGame();
        game.RunOneFrame();
        CustomSetPropertyOnRenderable.InMemoryFontCreator = null;

        game.GumService.UseKernSmith();

        CustomSetPropertyOnRenderable.InMemoryFontCreator.ShouldBeOfType<KernSmithFontCreator>();
    }

    [Fact]
    public void UseKernSmith_BeforeInitialize_Throws()
    {
        Gum.GumService service = new Gum.GumService();

        Should.Throw<InvalidOperationException>(() => service.UseKernSmith());
    }

    [Fact]
    public void UseCalls_ChainOnTheConcreteService()
    {
        using UseGame game = new UseGame();
        game.RunOneFrame();
        ShapeRenderer.Self.Uninitialize();

        Gum.GumService result = game.GumService
            .UseShadowDusk()
            .UseKernSmith()
            .UseShapes()
            .UseExpressions()
            .UseKeyboardDefaults()
            .UseGamepadDefaults();

        result.ShouldBeSameAs(game.GumService);
    }

    private sealed class UseGame : SharedGameLease
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
