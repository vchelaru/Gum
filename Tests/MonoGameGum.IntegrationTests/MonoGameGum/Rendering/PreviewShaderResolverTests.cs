using System.IO;
using Gum.GueDeriving;
using Gum.Wireframe;
using GumPreview;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// The Preview window (Tool/GumPreview) must register a SourceShaderFile resolver, or a project's
/// shaded container renders unshaded there while the editor canvas shows the effect.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class PreviewShaderResolverTests : BaseTestClass
{
    [Fact]
    public void SourceShaderFile_ResolvesToEffect_AfterPreviewRegistersItsResolver()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        try
        {
            PreviewShaderResolver.Register(game.GraphicsDevice);

            ContainerRuntime container = new();
            container.Width = 64;
            container.Height = 64;
            container.IsRenderTarget = true;
            container.AddToManagers(SystemManagers.Default, null);

            container.SourceShaderFile = Path.Combine(AppContext.BaseDirectory, "Outline.slang");

            Effect? effect = container.RenderTargetEffect;
            effect.ShouldNotBeNull();
            effect.Parameters["Thickness"].ShouldNotBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
        }
    }

    private sealed class MinimalGame : SharedGameLease
    {
        public GumService GumService { get; } = new GumService();

        protected override void OnInitialize() => GumService.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }
    }
}
