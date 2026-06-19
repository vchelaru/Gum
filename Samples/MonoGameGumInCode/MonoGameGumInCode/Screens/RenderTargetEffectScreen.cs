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
/// Demonstrates <see cref="ContainerRuntime.RenderTargetEffect"/> (issue #816): a shader is
/// applied to a render-target container when its cached texture is blitted back to the screen,
/// acting as a post-process over the whole container's contents.
///
/// Two identical containers are shown side by side — the left renders normally, the right has a
/// grayscale shader assigned. The .fx is compiled at runtime with ShadowDusk (no content
/// pipeline / MGCB step required); the resulting bytes are handed to a MonoGame
/// <see cref="Effect"/> that Gum core neither compiles nor loads.
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

        var container = new ContainerRuntime();
        container.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        container.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        container.X = 4;
        container.Y = 4;
        container.Width = -8;
        container.Height = -8;
        container.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        container.StackSpacing = 8;
        this.AddChild(container);

        // Compile the shader once. On failure (e.g. the runtime can't load the compiled bytes),
        // the right container falls back to no effect. The status label below reports the
        // outcome at the very top so it's never ambiguous / cropped off-screen.
        Effect grayscale = TryCompileGrayscale(out string compileError);

        var status = new TextRuntime();
        status.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        status.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        status.Width = 0;
        status.Height = 0;
        if (grayscale != null)
        {
            status.Text = "RenderTargetEffect demo - shader compiled OK. The right container should render grayscale.";
            status.Color = Color.White;
        }
        else
        {
            status.Text = "RenderTargetEffect demo - SHADER COMPILE FAILED:\n" + compileError;
            status.Color = Color.OrangeRed;
        }
        container.AddChild(status);

        var row = AddRow(container);
        row.AddChild(BuildDemoContainer("Normal", effect: null, errorMessage: null));
        row.AddChild(BuildDemoContainer("Grayscale", effect: grayscale, errorMessage: compileError));
    }

    private static ContainerRuntime BuildDemoContainer(string caption, Effect effect, string errorMessage)
    {
        var cell = new ContainerRuntime();
        cell.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        cell.StackSpacing = 4;
        cell.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        cell.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;

        AddLabel(cell, caption);

        // The render target holds colorful content so the grayscale post-process is obvious.
        var renderTarget = new ContainerRuntime();
        renderTarget.IsRenderTarget = true;
        renderTarget.Width = 220;
        renderTarget.Height = 180;
        renderTarget.ChildrenLayout = Gum.Managers.ChildrenLayout.TopToBottomStack;
        renderTarget.StackSpacing = 6;

        var bearRow = new ContainerRuntime();
        bearRow.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        bearRow.StackSpacing = 6;
        bearRow.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        bearRow.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        foreach (var tint in new[] { Color.Red, Color.LightGreen, Color.CornflowerBlue })
        {
            var bear = new SpriteRuntime();
            bear.SourceFileName = "BearTexture.png";
            bear.Width = 56;
            bear.Height = 56;
            bear.Color = tint;
            bearRow.AddChild(bear);
        }
        renderTarget.AddChild(bearRow);

        var colorText = new TextRuntime();
        colorText.Text = "Colorful content";
        colorText.Color = Color.Gold;
        renderTarget.AddChild(colorText);

        if (effect != null)
        {
            renderTarget.RenderTargetEffect = effect;
        }

        cell.AddChild(renderTarget);

        if (!string.IsNullOrEmpty(errorMessage))
        {
            var error = new TextRuntime();
            error.Text = "Shader compile failed:\n" + errorMessage;
            error.Color = Color.OrangeRed;
            error.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            error.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
            cell.AddChild(error);
        }

        return cell;
    }

    private static Effect TryCompileGrayscale(out string errorMessage)
    {
        errorMessage = null;

        var graphicsDevice = SystemManagers.Default.Renderer.GraphicsDevice;
        if (graphicsDevice == null)
        {
            errorMessage = "GraphicsDevice not available.";
            return null;
        }

        try
        {
            var compiler = new EffectCompiler();
            Result<CompiledShader, ShaderError[]> result =
                compiler.Compile(GrayscaleFx, new CompilerOptions { Target = PlatformTarget.OpenGL });

            if (result.IsSuccess)
            {
                return new Effect(graphicsDevice, result.Value.Data);
            }

            errorMessage = string.Join("\n", result.Error.Select(e => e.Message));
            return null;
        }
        catch (System.Exception e)
        {
            // A thrown error here is typically a missing/unloadable native compiler asset rather
            // than a shader-syntax problem; surface it instead of taking down the screen.
            errorMessage = e.GetType().Name + ": " + e.Message;
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

    private static ContainerRuntime AddRow(ContainerRuntime container)
    {
        var row = new ContainerRuntime();
        row.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 24;
        row.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        container.AddChild(row);
        return row;
    }
}
