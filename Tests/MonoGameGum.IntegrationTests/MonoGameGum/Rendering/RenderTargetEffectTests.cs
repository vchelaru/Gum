using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using ShadowDusk.Compiler;
using ShadowDusk.Compiler.Slang;
using ShadowDusk.Core;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Proves that a render-target container's <see cref="ContainerRuntime.RenderTargetEffect"/> is
/// bound to the SpriteBatch when the container's cached texture is blitted back to the screen
/// (issue #816). A real <see cref="BasicEffect"/> stands in for a user post-process shader so the
/// binding can be verified without shipping a compiled .fx in the test project.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class RenderTargetEffectTests : BaseTestClass
{
    [Fact]
    public void RenderTargetEffect_ShouldBeBound_WhenRenderTargetContainerIsDrawn()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime container = new();
        container.Width = 100;
        container.Height = 100;
        container.IsRenderTarget = true;

        TextRuntime text = new();
        text.Text = "Hello";
        container.AddChild(text);

        BasicEffect effect = new BasicEffect(game.GraphicsDevice);
        container.RenderTargetEffect = effect;

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        try
        {
            // First Draw does one-time render-target setup; draw twice so the effect-bound blit
            // is captured in LastFrameDrawStates on a steady-state frame.
            renderer.Draw(managers);
            renderer.Draw(managers);

            bool effectWasBound = renderer.SpriteRenderer.LastFrameDrawStates
                .Any(state => ReferenceEquals(state.Effect, effect));

            effectWasBound.ShouldBeTrue();
        }
        finally
        {
            effect.Dispose();
        }
    }

    [Fact]
    public void RenderTargetEffect_ShouldNotBeBound_WhenContainerHasNoEffect()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        BasicEffect effect = new BasicEffect(game.GraphicsDevice);

        ContainerRuntime container = new();
        container.Width = 100;
        container.Height = 100;
        container.IsRenderTarget = true;

        TextRuntime text = new();
        text.Text = "Hello";
        container.AddChild(text);

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        try
        {
            renderer.Draw(managers);
            renderer.Draw(managers);

            bool effectWasBound = renderer.SpriteRenderer.LastFrameDrawStates
                .Any(state => ReferenceEquals(state.Effect, effect));

            effectWasBound.ShouldBeFalse();
        }
        finally
        {
            effect.Dispose();
        }
    }

    // Same grayscale shader the RenderTargetEffectScreen sample uses, compiled at runtime via
    // ShadowDusk. Kept here (duplicated) so this test fully reproduces the sample's pipeline.
    private const string GrayscaleFx = @"
#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
    return float4(gray, gray, gray, color.a);
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
";

    [Fact]
    public void RenderTargetEffect_GrayscaleShader_ActuallyGraysThePixels()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        // 1) Compile the sample's grayscale .fx with ShadowDusk (no content pipeline).
        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> compileResult =
            compiler.Compile(GrayscaleFx, new CompilerOptions { Target = PlatformTarget.OpenGL }, TestContext.Current.CancellationToken);

        compileResult.IsSuccess.ShouldBeTrue(
            compileResult.IsFailure
                ? "ShadowDusk failed to compile the grayscale shader:\n" +
                    string.Join("\n", compileResult.Error.Select(e => e.Message))
                : "");

        using Effect grayscale = new(gd, compileResult.Value.Data);

        // 2) A solid-red render-target container.
        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 64;
        container.Height = 64;
        container.IsRenderTarget = true;

#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill without the shape dependency.
        ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
        red.Width = 64;
        red.Height = 64;
        red.Color = Color.Red;
        container.AddChild(red);

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        // 3) Render with the grayscale effect, then again without it as a control.
        container.RenderTargetEffect = grayscale;
        Color withEffect = RenderToCaptureAndSample(gd, renderer, managers);

        container.RenderTargetEffect = null;
        Color withoutEffect = RenderToCaptureAndSample(gd, renderer, managers);

        // Control: the unmodified blit is clearly red (red channel dominates).
        withoutEffect.R.ShouldBeGreaterThan((byte)150);
        ((int)withoutEffect.R - withoutEffect.G).ShouldBeGreaterThan(80);

        // With grayscale: the channels collapse to roughly equal (R ≈ G ≈ B).
        Math.Abs(withEffect.R - withEffect.G).ShouldBeLessThan(25);
        Math.Abs(withEffect.R - withEffect.B).ShouldBeLessThan(25);
    }

    // Writes the Strength parameter into the red channel, so the sampled red shows which value the
    // blit applied.
    private const string StrengthFx = @"
#if OPENGL
    #define PS_SHADERMODEL ps_3_0
#else
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

float Strength;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    return float4(Strength * color.a, 0, 0, color.a);
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
";

    // Two containers share one Effect instance (as SourceShaderFile does through the cache) and set
    // different values for the same parameter, so each blit must apply its own container's value.
    [Fact]
    public void ShaderParameter_AppliesEachContainersOwnValue_WhenEffectIsShared()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> compileResult =
            compiler.Compile(StrengthFx, new CompilerOptions { Target = PlatformTarget.OpenGL }, TestContext.Current.CancellationToken);
        compileResult.IsSuccess.ShouldBeTrue(
            compileResult.IsFailure ? string.Join("\n", compileResult.Error.Select(e => e.Message)) : "");
        using Effect shared = new(gd, compileResult.Value.Data);

        ContainerRuntime dim = BuildRedRenderTarget(0f);
        dim.RenderTargetEffect = shared;
        dim.SetShaderParameter("Strength", 0.25f);
        ContainerRuntime bright = BuildRedRenderTarget(64f);
        bright.RenderTargetEffect = shared;
        bright.SetShaderParameter("Strength", 0.75f);

        dim.AddToManagers(managers, null);
        bright.AddToManagers(managers, null);
        dim.UpdateLayout();
        bright.UpdateLayout();

        Color[] data = RenderToCapture(gd, renderer, managers);

        Math.Abs(data[(20 * CaptureSize) + 20].R - 64).ShouldBeLessThan(12);
        Math.Abs(data[(20 * CaptureSize) + 84].R - 191).ShouldBeLessThan(12);
    }

    // Runs the Outline.slang file the samples ship through the Slang frontend and the OpenGL
    // compiler, so a break in the shader or the conversion shows up here and not only by eye.
    [Fact]
    public void Outline_SampleSlangShader_DrawsTheColorJustOutsideTheContent_AndStopsAtThickness()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        Result<SlangFxConversion, ShaderError[]> conversion = SlangFrontend.ConvertToFx(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Outline.slang")), new SlangConvertOptions { SourceName = "Outline.slang" });
        conversion.IsSuccess.ShouldBeTrue(
            conversion.IsFailure ? string.Join("\n", conversion.Error.Select(e => e.Message)) : "");

        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> compileResult =
            compiler.Compile(conversion.Value.FxText, new CompilerOptions { Target = PlatformTarget.OpenGL }, TestContext.Current.CancellationToken);
        compileResult.IsSuccess.ShouldBeTrue(
            compileResult.IsFailure ? string.Join("\n", compileResult.Error.Select(e => e.Message)) : "");
        using Effect outline = new(gd, compileResult.Value.Data);

        // A 24x24 red square in the middle of a 48x48 render target, outlined in blue.
        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 48;
        container.Height = 48;
        container.IsRenderTarget = true;

#pragma warning disable CS0618
        ColoredRectangleRuntime square = new();
#pragma warning restore CS0618
        square.X = 12;
        square.Y = 12;
        square.Width = 24;
        square.Height = 24;
        square.Color = Color.Red;
        container.AddChild(square);

        container.RenderTargetEffect = outline;
        container.SetShaderParameter("Thickness", 2f);
        container.SetShaderParameter("OutlineColor", 0f, 0f, 1f, 1f);

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        Color[] data = RenderToCapture(gd, renderer, managers);
        Color Pixel(int x, int y) => data[(y * CaptureSize) + x];

        // Inside the square is still red.
        Pixel(24, 24).R.ShouldBeGreaterThan((byte)200);
        Pixel(24, 24).B.ShouldBeLessThan((byte)60);

        // One and two pixels outside the left edge (the square starts at x = 12) are the blue
        // outline, three pixels out is not.
        Pixel(11, 24).B.ShouldBeGreaterThan((byte)200);
        Pixel(11, 24).R.ShouldBeLessThan((byte)60);
        Pixel(10, 24).B.ShouldBeGreaterThan((byte)200);
        Pixel(9, 24).B.ShouldBeLessThan((byte)30);
    }

    // The tool lists a shader's parameters from the compiled effect, so the names, kinds and sizes
    // must come out as the shader declared them (Outline.slang declares TexelSize, Thickness and
    // OutlineColor besides the texture it samples).
    [Fact]
    public void EffectShaderParameters_Read_ListsTheParametersTheShaderDeclares()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        Result<SlangFxConversion, ShaderError[]> conversion = SlangFrontend.ConvertToFx(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Outline.slang")), new SlangConvertOptions { SourceName = "Outline.slang" });
        conversion.IsSuccess.ShouldBeTrue();
        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> compileResult =
            compiler.Compile(conversion.Value.FxText, new CompilerOptions { Target = PlatformTarget.OpenGL }, TestContext.Current.CancellationToken);
        compileResult.IsSuccess.ShouldBeTrue();
        using Effect outline = new(game.GraphicsDevice, compileResult.Value.Data);

        System.Collections.Generic.IReadOnlyList<ShaderParameterInfo> parameters = EffectShaderParameters.Read(outline);

        parameters.Select(parameter => parameter.Name).OrderBy(name => name)
            .ShouldBe(new[] { "OutlineColor", "TexelSize", "Thickness" });
        ShaderParameterInfo thickness = parameters.Single(parameter => parameter.Name == "Thickness");
        thickness.Kind.ShouldBe(ShaderParameterKind.Float);
        thickness.Count.ShouldBe(1);
        parameters.Single(parameter => parameter.Name == "TexelSize").Count.ShouldBe(2);
        parameters.Single(parameter => parameter.Name == "OutlineColor").Count.ShouldBe(4);
    }

    private static ContainerRuntime BuildRedRenderTarget(float x)
    {
        ContainerRuntime container = new();
        container.X = x;
        container.Y = 0;
        container.Width = 40;
        container.Height = 40;
        container.IsRenderTarget = true;

#pragma warning disable CS0618
        ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
        red.Width = 40;
        red.Height = 40;
        red.Color = Color.Red;
        container.AddChild(red);
        return container;
    }

    [Fact]
    public void RenderTargetEffect_GrayscaleShader_GraysThePixels_UnderCameraZoom()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> compileResult =
            compiler.Compile(GrayscaleFx, new CompilerOptions { Target = PlatformTarget.OpenGL }, TestContext.Current.CancellationToken);
        compileResult.IsSuccess.ShouldBeTrue(
            compileResult.IsFailure ? string.Join("\n", compileResult.Error.Select(e => e.Message)) : "");
        using Effect grayscale = new(gd, compileResult.Value.Data);

        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 100;
        container.Height = 100;
        container.IsRenderTarget = true;

#pragma warning disable CS0618
        ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
        red.Width = 100;
        red.Height = 100;
        red.Color = Color.Red;
        container.AddChild(red);

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        // Reproduce the sample's GumService.EnableZoomToWindow() — it just sets a camera zoom.
        renderer.Camera.Zoom = 2f;

        container.RenderTargetEffect = grayscale;
        Color withEffect = RenderToCaptureAndSample(gd, renderer, managers);

        Math.Abs(withEffect.R - withEffect.G).ShouldBeLessThan(25);
        Math.Abs(withEffect.R - withEffect.B).ShouldBeLessThan(25);
    }

    [Fact]
    public void RenderTargetEffect_GrayscaleShader_GraysThePixels_WhenDeeplyNested()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> compileResult =
            compiler.Compile(GrayscaleFx, new CompilerOptions { Target = PlatformTarget.OpenGL }, TestContext.Current.CancellationToken);
        compileResult.IsSuccess.ShouldBeTrue(
            compileResult.IsFailure ? string.Join("\n", compileResult.Error.Select(e => e.Message)) : "");
        using Effect grayscale = new(gd, compileResult.Value.Data);

        // Mirror the sample's nesting: root -> row -> cell -> holder(render target) -> red.
        ContainerRuntime root = new();
        root.X = 0;
        root.Y = 0;
        ContainerRuntime row = new();
        root.AddChild(row);
        ContainerRuntime cell = new();
        row.AddChild(cell);

        ContainerRuntime holder = new();
        holder.X = 0;
        holder.Y = 0;
        holder.Width = 100;
        holder.Height = 100;
        holder.IsRenderTarget = true;

#pragma warning disable CS0618
        ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
        red.Width = 100;
        red.Height = 100;
        red.Color = Color.Red;
        // Set the effect before AddToManagers, exactly as the sample's screen does.
        holder.RenderTargetEffect = grayscale;
        holder.AddChild(red);
        cell.AddChild(holder);

        root.AddToManagers(managers, null);
        root.UpdateLayout();

        Color withEffect = RenderToCaptureAndSample(gd, renderer, managers);

        Math.Abs(withEffect.R - withEffect.G).ShouldBeLessThan(25);
        Math.Abs(withEffect.R - withEffect.B).ShouldBeLessThan(25);
    }

    // ---- SourceShaderFile (issue #3206): the .fx file-reference path that resolves into
    // RenderTargetEffect via a pluggable, consumer-registered resolver. ----

    // Diagnostic for #3210: the Gum tool renders through KNI's DirectX 11 backend, so its resolver
    // (RenderTargetShaderResolver) compiles for PlatformTarget.DirectX — a path none of the
    // OpenGL-targeted tests in this file exercise. Verify ShadowDusk can actually produce DXBC for
    // the grayscale shader (no GraphicsDevice needed for the compile itself).
    [Fact]
    public void Compile_GrayscaleShader_ForDirectXTarget_Succeeds()
    {
        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> compileResult =
            compiler.Compile(GrayscaleFx, new CompilerOptions { Target = PlatformTarget.DirectX }, TestContext.Current.CancellationToken);

        compileResult.IsSuccess.ShouldBeTrue(
            compileResult.IsFailure
                ? "ShadowDusk failed to compile the grayscale shader for DirectX:\n" +
                    string.Join("\n", compileResult.Error.Select(e => e.Message))
                : "");
        compileResult.Value.Data.Length.ShouldBeGreaterThan(0);
    }

    // Issue #4677: Gum's two SourceShaderFile resolvers (the tool's RenderTargetShaderResolver and
    // Samples/MonoGameGumInCode's RenderTargetShaderScreen) both convert a .slang file to .fx text
    // via ShadowDusk's Slang frontend before compiling, since Slang has no technique/pass concept
    // and can't go through EffectCompiler.Compile directly. Same grayscale effect as GrayscaleFx
    // above, authored in Slang's own idiom (a [shader("fragment")] attribute, no technique block).
    private const string GrayscaleSlang = @"
Texture2D SpriteTexture;
SamplerState SpriteTextureSampler;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

[shader(""fragment"")]
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 color = SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
    return float4(gray, gray, gray, color.a);
}
";

    [Fact]
    public void SlangGrayscaleShader_ConvertsAndCompiles_ForBothResolverTargets()
    {
        Result<SlangFxConversion, ShaderError[]> conversion = SlangFrontend.ConvertToFx(
            GrayscaleSlang, new SlangConvertOptions { SourceName = "Grayscale.slang" });

        conversion.IsSuccess.ShouldBeTrue(
            conversion.IsFailure
                ? "ShadowDusk's Slang frontend could not convert the grayscale shader:\n" +
                    string.Join("\n", conversion.Error.Select(e => e.Message))
                : "");

        EffectCompiler compiler = new();

        // OpenGL — the target Samples/MonoGameGumInCode's game-side resolver compiles for.
        Result<CompiledShader, ShaderError[]> openGlResult = compiler.Compile(
            conversion.Value.FxText, new CompilerOptions { Target = PlatformTarget.OpenGL }, TestContext.Current.CancellationToken);
        openGlResult.IsSuccess.ShouldBeTrue(
            openGlResult.IsFailure ? string.Join("\n", openGlResult.Error.Select(e => e.Message)) : "");

        // DirectX — the target the Gum tool's RenderTargetShaderResolver compiles for (issue #3210).
        Result<CompiledShader, ShaderError[]> directXResult = compiler.Compile(
            conversion.Value.FxText, new CompilerOptions { Target = PlatformTarget.DirectX }, TestContext.Current.CancellationToken);
        directXResult.IsSuccess.ShouldBeTrue(
            directXResult.IsFailure ? string.Join("\n", directXResult.Error.Select(e => e.Message)) : "");
    }

    [Fact]
    public void SourceShaderFile_CompilesOnce_WhenReferencedByMultipleContainers()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;

        string fxPath = WriteTempShader(GrayscaleFx);
        int invocationCount = 0;
        try
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path =>
            {
                invocationCount++;
                return CompileShader(gd, path);
            };

            ContainerRuntime first = new();
            first.IsRenderTarget = true;
            first.SourceShaderFile = fxPath;

            ContainerRuntime second = new();
            second.IsRenderTarget = true;
            second.SourceShaderFile = fxPath;

            // The second container referencing the same .fx must hit the LoaderManager cache.
            invocationCount.ShouldBe(1);
        }
        finally
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            File.Delete(fxPath);
        }
    }

    [Fact]
    public void SourceShaderFile_GraysThePixels_WhenDeeplyNested()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        string fxPath = WriteTempShader(GrayscaleFx);
        try
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path => CompileShader(gd, path);

            // Mirror the sample's nesting: root -> row -> cell -> holder(render target) -> red.
            ContainerRuntime root = new();
            root.X = 0;
            root.Y = 0;
            ContainerRuntime row = new();
            root.AddChild(row);
            ContainerRuntime cell = new();
            row.AddChild(cell);

            ContainerRuntime holder = new();
            holder.X = 0;
            holder.Y = 0;
            holder.Width = 100;
            holder.Height = 100;
            holder.IsRenderTarget = true;

#pragma warning disable CS0618
            ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
            red.Width = 100;
            red.Height = 100;
            red.Color = Color.Red;
            holder.SourceShaderFile = fxPath;
            holder.AddChild(red);
            cell.AddChild(holder);

            root.AddToManagers(managers, null);
            root.UpdateLayout();

            Color withEffect = RenderToCaptureAndSample(gd, renderer, managers);

            Math.Abs(withEffect.R - withEffect.G).ShouldBeLessThan(25);
            Math.Abs(withEffect.R - withEffect.B).ShouldBeLessThan(25);
        }
        finally
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            File.Delete(fxPath);
        }
    }

    [Fact]
    public void SourceShaderFile_GraysThePixels_WhenResolverRegistered()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        string fxPath = WriteTempShader(GrayscaleFx);
        try
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path => CompileShader(gd, path);

            ContainerRuntime container = new();
            container.X = 0;
            container.Y = 0;
            container.Width = 64;
            container.Height = 64;
            container.IsRenderTarget = true;

#pragma warning disable CS0618
            ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
            red.Width = 64;
            red.Height = 64;
            red.Color = Color.Red;
            container.AddChild(red);

            container.AddToManagers(managers, null);
            container.UpdateLayout();

            // The .fx reference resolves through the string path (SetProperty) into RenderTargetEffect.
            container.SourceShaderFile = fxPath;
            Color withEffect = RenderToCaptureAndSample(gd, renderer, managers);

            Math.Abs(withEffect.R - withEffect.G).ShouldBeLessThan(25);
            Math.Abs(withEffect.R - withEffect.B).ShouldBeLessThan(25);
        }
        finally
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            File.Delete(fxPath);
        }
    }

    // #3210: the Gum editor backs a Container with a LineRectangle (the outline visual), not the
    // runtime's InvisibleRenderable, so the render-target effect must be carried via the shared
    // IRenderTargetRenderable interface that both implement. Mirror the editor's setup here: a GUE
    // whose contained renderable is a LineRectangle, made a render target with a SourceShaderFile.
    [Fact]
    public void SourceShaderFile_GraysThePixels_WhenContainerRenderableIsLineRectangle()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        string fxPath = WriteTempShader(GrayscaleFx);
        try
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path => CompileShader(gd, path);

            GraphicalUiElement container = new(new RenderingLibrary.Math.Geometry.LineRectangle(managers));
            container.X = 0;
            container.Y = 0;
            container.Width = 64;
            container.Height = 64;

#pragma warning disable CS0618
            ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
            red.Width = 64;
            red.Height = 64;
            red.Color = Color.Red;
            container.AddChild(red);

            container.AddToManagers(managers, null);
            container.UpdateLayout();

            // Route through the string property path exactly as the editor's variable grid does.
            container.SetProperty("IsRenderTarget", true);
            container.SetProperty("SourceShaderFile", fxPath);

            Color withEffect = RenderToCaptureAndSample(gd, renderer, managers);

            Math.Abs(withEffect.R - withEffect.G).ShouldBeLessThan(25);
            Math.Abs(withEffect.R - withEffect.B).ShouldBeLessThan(25);
        }
        finally
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            File.Delete(fxPath);
        }
    }

    [Fact]
    public void SourceShaderFile_IsNoOp_WhenNoResolverRegistered()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        // No resolver registered (clear any leakage from a prior test).
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;

        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 64;
        container.Height = 64;
        container.IsRenderTarget = true;

#pragma warning disable CS0618
        ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
        red.Width = 64;
        red.Height = 64;
        red.Color = Color.Red;
        container.AddChild(red);

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        // With no resolver the assignment is a graceful no-op (no crash); the container renders
        // unshaded, so the pixel stays red.
        container.SourceShaderFile = "Shaders/DoesNotMatter.fx";
        Color result = RenderToCaptureAndSample(gd, renderer, managers);

        result.R.ShouldBeGreaterThan((byte)150);
        ((int)result.R - result.G).ShouldBeGreaterThan(80);
    }

    [Fact]
    public void SourceShaderFile_RaisesPropertyAssignmentError_WhenResolverFailsAndConsumingSilently()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        MissingFileBehavior previousBehavior = GraphicalUiElement.MissingFileBehavior;
        string? reportedError = null;
        Action<string> handler = message => reportedError = message;
        try
        {
            GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ConsumeSilently;
            CustomSetPropertyOnRenderable.PropertyAssignmentError += handler;
            // Resolver registered but unable to produce an effect (missing .fx / compile failure).
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => null;

            ContainerRuntime container = new();
            container.IsRenderTarget = true;

            // ConsumeSilently must not throw; it reports through PropertyAssignmentError instead.
            container.SourceShaderFile = "Shaders/Missing.fx";

            reportedError.ShouldNotBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= handler;
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            GraphicalUiElement.MissingFileBehavior = previousBehavior;
        }
    }

    [Fact]
    public void SourceShaderFile_Throws_WhenResolverFailsAndMissingFileBehaviorIsThrow()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        MissingFileBehavior previousBehavior = GraphicalUiElement.MissingFileBehavior;
        try
        {
            GraphicalUiElement.MissingFileBehavior = MissingFileBehavior.ThrowException;
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = _ => null;

            ContainerRuntime container = new();
            container.IsRenderTarget = true;

            Should.Throw<FileNotFoundException>(() => container.SourceShaderFile = "Shaders/Missing.fx");
        }
        finally
        {
            CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            GraphicalUiElement.MissingFileBehavior = previousBehavior;
        }
    }

    /// <summary>
    /// Writes the given shader source to a unique temp .fx file and returns its absolute path.
    /// </summary>
    private static string WriteTempShader(string source)
    {
        string path = Path.Combine(Path.GetTempPath(), "GumSourceShaderTest_" + Guid.NewGuid().ToString("N") + ".fx");
        File.WriteAllText(path, source);
        return path;
    }

    /// <summary>
    /// Stand-in for a consumer's resolver: reads the .fx at <paramref name="path"/>, compiles it
    /// with ShadowDusk, and returns a MonoGame <see cref="Effect"/> (or null on compile failure).
    /// </summary>
    private static Effect? CompileShader(GraphicsDevice gd, string path)
    {
        string source = File.ReadAllText(path);
        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> result =
            compiler.Compile(source, new CompilerOptions { Target = PlatformTarget.OpenGL }, TestContext.Current.CancellationToken);
        if (result.IsFailure)
        {
            return null;
        }
        return new Effect(gd, result.Value.Data);
    }

    /// <summary>
    /// Renders the current managers tree into an off-screen capture target (PreserveContents so
    /// Gum's own mid-frame render-target switches don't wipe it), drawing twice so the first
    /// frame's one-time render-target setup doesn't skew the captured pixels, then returns a
    /// pixel sampled from inside the 64x64 container at the top-left.
    /// </summary>
    private const int CaptureSize = 128;

    private static Color RenderToCaptureAndSample(GraphicsDevice gd, Renderer renderer, SystemManagers managers)
    {
        // Container sits at (0,0) sized 64x64; sample well inside it.
        return RenderToCapture(gd, renderer, managers)[(20 * CaptureSize) + 20];
    }

    private static Color[] RenderToCapture(GraphicsDevice gd, Renderer renderer, SystemManagers managers)
    {
        const int w = CaptureSize;
        const int h = CaptureSize;
        using RenderTarget2D capture = new(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(Color.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        Color[] data = new Color[w * h];
        capture.GetData(data);
        return data;
    }

    /// <summary>
    /// Minimal Game host that initializes a fresh <see cref="GumService"/> per test so
    /// <see cref="Renderer.Draw(SystemManagers)"/> can be invoked against a live device.
    /// </summary>
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
