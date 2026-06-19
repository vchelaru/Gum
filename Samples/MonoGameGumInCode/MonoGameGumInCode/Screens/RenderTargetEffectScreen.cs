using System.IO;
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
/// Demonstrates two ways to apply a post-process shader to a render-target container (the shader
/// runs when the container's cached texture is blitted back to the screen, acting as a post-process
/// over the whole container):
/// <list type="bullet">
/// <item>Middle cell — compile an <see cref="Effect"/> in code and assign it to
/// <see cref="ContainerRuntime.RenderTargetEffect"/> directly (issue #816).</item>
/// <item>Right cell — reference the .fx by path via <see cref="ContainerRuntime.SourceShaderFile"/>
/// (issue #3206). Gum resolves the path through the app-registered
/// <see cref="Gum.Wireframe.CustomSetPropertyOnRenderable.RenderTargetEffectResolver"/> (wired in
/// <c>Game1.Initialize</c>); Gum core itself neither compiles nor loads the shader.</item>
/// </list>
/// Both cells use the same <c>Content/Grayscale.fx</c>, compiled at runtime with ShadowDusk (no
/// content pipeline). Left is the unmodified bear for comparison.
/// </summary>
internal class RenderTargetEffectScreen : FrameworkElement
{
    // Relative to FileManager.RelativeDirectory (set to "Content/" by GumService.Initialize).
    private const string ShaderFileName = "Grayscale.fx";

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

        // Compile once up front: this drives the middle cell (RenderTargetEffect set in code) and
        // the status label. The right cell only wires SourceShaderFile when this succeeds — runtime
        // MissingFileBehavior is ThrowException, so resolving a shader that can't compile would
        // throw; gating on a known-good compile keeps the demo degrading the same way both cells do.
        Effect inCodeEffect =
            CompileEffectFromFile(ToolsUtilities.FileManager.RelativeDirectory + ShaderFileName, out string status);

        var statusLabel = new TextRuntime();
        statusLabel.Text = status;
        statusLabel.Color = inCodeEffect != null ? Color.White : Color.OrangeRed;
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
        row.AddChild(BuildCell("Original", effect: null, sourceShaderFile: null));

        // Middle: the grayscale Effect compiled in code and assigned directly.
        row.AddChild(BuildCell("RenderTargetEffect (set in code)", effect: inCodeEffect, sourceShaderFile: null));

        // Right: the same shader referenced by file path; Gum's registered resolver loads it.
        row.AddChild(BuildCell("SourceShaderFile (.fx reference)",
            effect: null,
            sourceShaderFile: inCodeEffect != null ? ShaderFileName : null));
    }

    private static ContainerRuntime BuildCell(string caption, Effect effect, string sourceShaderFile)
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
        else if (!string.IsNullOrEmpty(sourceShaderFile))
        {
            holder.IsRenderTarget = true;
            holder.SourceShaderFile = sourceShaderFile;
        }

        cell.AddChild(holder);
        return cell;
    }

    /// <summary>
    /// Compiles the .fx text at <paramref name="path"/> into a MonoGame <see cref="Effect"/> via
    /// ShadowDusk (no content pipeline), or returns null on failure. This is also the body of the
    /// <see cref="Gum.Wireframe.CustomSetPropertyOnRenderable.RenderTargetEffectResolver"/> the app
    /// registers in <c>Game1.Initialize</c>, so a Container's <c>SourceShaderFile</c> resolves
    /// through exactly this code.
    /// </summary>
    public static Effect CompileEffectFromFile(string path) => CompileEffectFromFile(path, out _);

    private static Effect CompileEffectFromFile(string path, out string status)
    {
        var graphicsDevice = SystemManagers.Default.Renderer.GraphicsDevice;
        if (graphicsDevice == null)
        {
            status = "SHADER COMPILE FAILED: GraphicsDevice not available.";
            return null;
        }

        if (!File.Exists(path))
        {
            status = "SHADER FILE NOT FOUND: " + path;
            return null;
        }

        try
        {
            var compiler = new EffectCompiler();
            Result<CompiledShader, ShaderError[]> result =
                compiler.Compile(File.ReadAllText(path), new CompilerOptions { Target = PlatformTarget.OpenGL });

            if (result.IsSuccess)
            {
                status = "Shader compiled at runtime (ShadowDusk). Middle and right bears render grayscale.";
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
