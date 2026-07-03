using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Raylib_cs;
using RenderingLibrary.Graphics;
using static Raylib_cs.Raylib;

namespace Examples.Shapes;

// Raylib mirror of Samples/MonoGameGumInCode/MonoGameGumInCode/Screens/RenderTargetEffectScreen.cs
// (issue #3465). Demonstrates a post-process shader applied to a render-target container: the shader
// is bound when the container's baked texture composites back to the screen, so it recolors the whole
// container's contents. Two wiring styles, matching the MonoGame screen cell-for-cell:
//   - Middle cell: load a Raylib_cs.Shader in code and assign it to ContainerRuntime.RenderTargetEffect.
//   - Right cell:  reference the .fs by path via ContainerRuntime.SourceShaderFile; Gum resolves it
//                  through the app-registered RenderTargetEffectResolver (wired in Program.Main).
// Both use the same resources/Grayscale.fs. Left is the unmodified bear for comparison. Unlike the
// MonoGame side (which compiles HLSL at runtime via ShadowDusk), raylib loads GLSL directly, so no
// shader-compiler dependency is needed.
internal class RenderTargetShaderScreen : FrameworkElement
{
    // Relative path (resolved against FileManager.RelativeDirectory) to the grayscale GLSL shader.
    private const string ShaderFileName = "resources/Grayscale.fs";

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

        // Load the shader in code for the middle cell. LoadShader returns a default (id 0) shader if
        // the file is missing; the grayscale still degrades to a pass-through rather than crashing.
        Shader inCodeShader = LoadShader(null, ShaderFileName);

        TextRuntime statusLabel = new();
        statusLabel.Text = "Grayscale GLSL post-process. Middle and right bears render grayscale; left is the original.";
        statusLabel.Red = 220;
        statusLabel.Green = 220;
        statusLabel.Blue = 220;
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
        row.AddChild(BuildCell("Original", shader: null, sourceShaderFile: null));

        // Middle: the grayscale shader loaded in code and assigned directly.
        row.AddChild(BuildCell("RenderTargetEffect (loaded in code)", shader: inCodeShader, sourceShaderFile: null));

        // Right: the same shader referenced by file path; Gum's registered resolver loads it.
        row.AddChild(BuildCell("SourceShaderFile (.fs reference)", shader: null, sourceShaderFile: ShaderFileName));
    }

    private static ContainerRuntime BuildCell(string caption, Shader? shader, string? sourceShaderFile)
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
        bear.SourceFileName = "resources\\BearTexture.png";
        bear.WidthUnits = DimensionUnitType.Absolute;
        bear.HeightUnits = DimensionUnitType.Absolute;
        bear.Width = 128;
        bear.Height = 128;
        holder.AddChild(bear);

        if (shader != null)
        {
            holder.IsRenderTarget = true;
            holder.RenderTargetEffect = shader;
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
