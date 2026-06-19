using System.Linq;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using ShadowDusk.Compiler;
using ShadowDusk.Core;

namespace MonoGameGumInCode.Screens;

/// <summary>
/// Demonstrates <see cref="ContainerRuntime.RenderTargetEffect"/> (issue #816): a shader applied
/// to a render-target container when its cached texture is blitted back to the screen, acting as
/// a post-process over the whole container.
///
/// Left is the unmodified bear. Right is the same bear inside a render-target container with a
/// grayscale shader assigned. The .fx is compiled at runtime with ShadowDusk (no content
/// pipeline); the resulting bytes are handed to a MonoGame <see cref="Effect"/> that Gum core
/// neither compiles nor loads.
/// </summary>
internal class RenderTargetEffectScreen : FrameworkElement
{
    // Canonical MonoGame 2D post-process effect: the technique declares only a pixel shader, so
    // SpriteBatch supplies the vertex transform. Compiled for the OpenGL (DesktopGL) target.
    const string GrayscaleFx = @"
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

    public RenderTargetEffectScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        var root = new ContainerRuntime();
        root.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        root.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        root.X = 4;
        root.Y = 4;
        root.Width = -8;
        root.Height = -8;
        root.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        root.StackSpacing = 16;
        this.AddChild(root);

        Effect grayscale = TryCompileGrayscale(out string status);

        var statusLabel = new TextRuntime();
        statusLabel.Text = status;
        statusLabel.Color = grayscale != null ? Color.White : Color.OrangeRed;
        statusLabel.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        statusLabel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        statusLabel.Width = 0;
        statusLabel.Height = 0;
        root.AddChild(statusLabel);

        var row = new ContainerRuntime();
        row.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 48;
        row.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        root.AddChild(row);

        // Left: the unmodified bear (no render target, no shader).
        row.AddChild(BuildCell("Original", effect: null));

        // Right: the same bear inside a render-target container with the grayscale shader.
        row.AddChild(BuildCell("Render target + grayscale shader", effect: grayscale));
    }

    private static ContainerRuntime BuildCell(string caption, Effect effect)
    {
        var cell = new ContainerRuntime();
        cell.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        cell.StackSpacing = 6;
        cell.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        cell.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        AddLabel(cell, caption);

        var holder = new ContainerRuntime();
        holder.Width = 128;
        holder.Height = 128;

        var bear = new SpriteRuntime();
        bear.SourceFileName = "BearTexture.png";
        bear.Width = 128;
        bear.Height = 128;
        holder.AddChild(bear);

        if (effect != null)
        {
            holder.IsRenderTarget = true;
            holder.RenderTargetEffect = effect;
        }

        cell.AddChild(holder);
        return cell;
    }

    private static Effect TryCompileGrayscale(out string status)
    {
        var graphicsDevice = SystemManagers.Default.Renderer.GraphicsDevice;
        if (graphicsDevice == null)
        {
            status = "SHADER COMPILE FAILED: GraphicsDevice not available.";
            return null;
        }

        try
        {
            var compiler = new EffectCompiler();
            Result<CompiledShader, ShaderError[]> result =
                compiler.Compile(GrayscaleFx, new CompilerOptions { Target = PlatformTarget.OpenGL });

            if (result.IsSuccess)
            {
                status = "Shader compiled at runtime (ShadowDusk). The right bear renders grayscale.";
                return new Effect(graphicsDevice, result.Value.Data);
            }

            status = "SHADER COMPILE FAILED:\n" + string.Join("\n", result.Error.Select(e => e.Message));
            return null;
        }
        catch (System.Exception e)
        {
            // A thrown error here is typically a missing/unloadable native compiler asset rather
            // than a shader-syntax problem; surface it instead of taking down the screen.
            status = "SHADER COMPILE THREW: " + e.GetType().Name + ": " + e.Message;
            return null;
        }
    }

    private static void AddLabel(ContainerRuntime container, string text)
    {
        var label = new TextRuntime();
        label.Text = text;
        label.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        label.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        label.Width = 0;
        label.Height = 0;
        container.AddChild(label);
    }
}
