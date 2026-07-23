using System.IO;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using SkiaSharp;

namespace SilkNetGum.Screens;

// Skia mirror of Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/RenderTargetEffectScreen.cs and
// Samples/raylib/Screens/RenderTargetShaderScreen.cs (issue #3998). Demonstrates a post-process
// shader applied to a render-target container: the shader is bound when the container's baked
// texture composites back to the screen, so it recolors the whole container's contents. Two wiring
// styles, matching the other two screens cell-for-cell:
//   - Middle cell: compile an SkiaSharp.SKRuntimeEffect in code and assign it to
//                  ContainerRuntime.RenderTargetEffect directly.
//   - Right cell:  reference the .sksl by path via ContainerRuntime.SourceShaderFile; Gum resolves it
//                  through the app-registered SkiaGum.CustomSetPropertyOnRenderable
//                  .RenderTargetEffectResolver (wired in Program.Main).
// Both use the same resources/Grayscale.sksl. Left is the unmodified bear for comparison. Unlike the
// MonoGame side (which compiles HLSL at runtime via ShadowDusk) and like raylib, Skia's SkSL is
// hand-authored text with no runtime compiler dependency — SKRuntimeEffect.CreateShader does the
// only "compiling" needed.
internal class RenderTargetShaderScreen : FrameworkElement
{
    // Relative path (resolved against the working directory the resolver is given) to the grayscale
    // SkSL shader.
    private const string ShaderFileName = "resources/Grayscale.sksl";

    public RenderTargetShaderScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        ContainerRuntime root = new();
        root.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        root.WidthUnits = DimensionUnitType.RelativeToChildren;
        root.HeightUnits = DimensionUnitType.RelativeToChildren;
        root.Width = 0;
        root.Height = 0;
        root.StackSpacing = 16;
        root.X = 12;
        root.Y = 12;
        this.AddChild(root);

        // Compile the shader in code for the middle cell. Null (rather than throwing) if the file is
        // missing or fails to compile, so the middle cell degrades to a pass-through instead of
        // crashing the screen.
        SKRuntimeEffect? inCodeEffect = CompileGrayscaleEffect(out string status);

        TextRuntime statusLabel = new();
        statusLabel.Text = status;
        statusLabel.Red = inCodeEffect != null ? (byte)220 : (byte)255;
        statusLabel.Green = inCodeEffect != null ? (byte)220 : (byte)120;
        statusLabel.Blue = inCodeEffect != null ? (byte)220 : (byte)120;
        statusLabel.WidthUnits = DimensionUnitType.RelativeToChildren;
        statusLabel.HeightUnits = DimensionUnitType.RelativeToChildren;
        statusLabel.Width = 0;
        statusLabel.Height = 0;
        root.AddChild(statusLabel);

        ContainerRuntime row = new();
        row.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 48;
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        root.AddChild(row);

        // Left: the unmodified bear (no render target, no shader).
        row.AddChild(BuildCell("Original", effect: null, sourceShaderFile: null));

        // Middle: the grayscale effect compiled in code and assigned directly.
        row.AddChild(BuildCell("RenderTargetEffect (loaded in code)", effect: inCodeEffect, sourceShaderFile: null));

        // Right: the same shader referenced by file path; Gum's registered resolver compiles it.
        // Gated on the middle cell's compile succeeding — with no resolver registered, or a
        // resolver that throws, SourceShaderFile assignment is a graceful no-op (#3998), so this
        // keeps both cells degrading the same way when the shader can't be found/compiled.
        row.AddChild(BuildCell(
            "SourceShaderFile (.sksl reference)",
            effect: null,
            sourceShaderFile: inCodeEffect != null ? ShaderFileName : null));
    }

    /// <summary>
    /// Compiles the SkSL text at <see cref="ShaderFileName"/> into an <see cref="SKRuntimeEffect"/>,
    /// or returns null if the file is missing or fails to compile. This is also the body of the
    /// <see cref="SkiaGum.CustomSetPropertyOnRenderable.RenderTargetEffectResolver"/> the app
    /// registers in <c>Program.Main</c>, so a container's <c>SourceShaderFile</c> resolves through
    /// exactly this code.
    /// </summary>
    public static SKRuntimeEffect? CompileEffectFromFile(string path) => CompileEffectFromFile(path, out _);

    private static SKRuntimeEffect? CompileEffectFromFile(string path, out string status)
    {
        if (!File.Exists(path))
        {
            status = "SHADER FILE NOT FOUND: " + path;
            return null;
        }

        SKRuntimeEffect? effect = SKRuntimeEffect.CreateShader(File.ReadAllText(path), out string errors);
        if (effect == null)
        {
            status = "SHADER COMPILE FAILED:\n" + errors;
            return null;
        }

        status = "SkSL shader compiled. Middle and right bears render grayscale.";
        return effect;
    }

    private static SKRuntimeEffect? CompileGrayscaleEffect(out string status) =>
        CompileEffectFromFile(ShaderFileName, out status);

    private static ContainerRuntime BuildCell(string caption, SKRuntimeEffect? effect, string? sourceShaderFile)
    {
        ContainerRuntime cell = new();
        cell.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        cell.StackSpacing = 6;
        cell.WidthUnits = DimensionUnitType.RelativeToChildren;
        cell.HeightUnits = DimensionUnitType.RelativeToChildren;
        cell.Width = 0;
        cell.Height = 0;

        AddLabel(cell, caption);

        ContainerRuntime holder = new();
        holder.Width = 128;
        holder.Height = 128;

        SpriteRuntime bear = new();
        bear.SourceFileName = "BearTexture.png";
        bear.WidthUnits = DimensionUnitType.Absolute;
        bear.HeightUnits = DimensionUnitType.Absolute;
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

    private static void AddLabel(ContainerRuntime container, string text)
    {
        TextRuntime label = new();
        label.Text = text;
        label.Red = 200;
        label.Green = 200;
        label.Blue = 200;
        label.WidthUnits = DimensionUnitType.RelativeToChildren;
        label.HeightUnits = DimensionUnitType.RelativeToChildren;
        label.Width = 0;
        label.Height = 0;
        container.AddChild(label);
    }
}
