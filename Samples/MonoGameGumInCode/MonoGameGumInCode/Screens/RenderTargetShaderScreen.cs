#nullable enable
using System.IO;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
#if RAYLIB
using System.Linq;
using Raylib_cs;
using ShadowDusk.Compiler.Raylib;
using ShadowDusk.Core;
using static Raylib_cs.Raylib;
using Color = Raylib_cs.Color;
using EffectType = Raylib_cs.Shader;
#elif SKIA
using SkiaSharp;
using Color = SkiaSharp.SKColor;
using EffectType = SkiaSharp.SKRuntimeEffect;
#else
using System.Linq;
using RenderingLibrary;
using ShadowDusk.Compiler;
using ShadowDusk.Compiler.Slang;
using ShadowDusk.Core;
using Color = Microsoft.Xna.Framework.Color;
using EffectType = Microsoft.Xna.Framework.Graphics.Effect;
#endif

#if RAYLIB
namespace Examples.Shapes;
#elif SKIA
namespace SilkNetGum.Screens;
#else
namespace MonoGameGumInCode.Screens;
#endif

/// <summary>
/// Demonstrates a post-process shader applied to a render-target container (issues #816/#3206 on
/// MonoGame, #3465 on raylib, #3998 on Skia), shared by the MonoGame, raylib, and SilkNetGum/Skia
/// samples like <see cref="RenderTargetScreen"/>. The shader binds when the container's baked
/// texture composites back to the screen, so it recolors the whole container's contents. Two wiring
/// styles, matching cell-for-cell across all three backends:
/// <list type="bullet">
/// <item>Middle cell — load/compile an effect in code and assign it directly to
/// <see cref="ContainerRuntime.RenderTargetEffect"/>.</item>
/// <item>Right cell — reference the shader file by path via
/// <see cref="ContainerRuntime.SourceShaderFile"/>. Gum resolves the path through the
/// app-registered <c>RenderTargetEffectResolver</c> (wired in each sample's entry point); Gum core
/// itself neither compiles nor loads the shader.</item>
/// </list>
/// All cells share the same grayscale logic, authored per-platform in the shading language each
/// backend needs: MonoGame compiles HLSL at runtime via ShadowDusk (no content pipeline,
/// <c>Content/Grayscale.fx</c>); raylib loads GLSL directly (no compiler dependency,
/// <c>resources/Grayscale.fs</c>); Skia compiles hand-authored SkSL via
/// <c>SKRuntimeEffect.CreateShader</c> (<c>resources/Grayscale.sksl</c>). Left is the unmodified
/// bear for comparison. A fourth, MonoGame-only cell (<c>Content/Grayscale.slang</c>) repeats the
/// SourceShaderFile wiring style for Slang input instead of raw HLSL (issue #4677) — ShadowDusk's
/// Slang frontend converts it to <c>.fx</c> text before compiling. A second, MonoGame-only row
/// (issue #4679) adds two more single-pass Slang examples the same way: <c>Content/Blur.slang</c>
/// (fixed radius — Gum's render-target-effect blit sets no shader parameters, so the blur amount
/// is a compile-time constant, not host-adjustable) and <c>Content/Sepia.slang</c>.
/// <para>The second row, on all three backends, runs one Slang file (<c>Outline.slang</c>) that
/// outlines whatever the container draws. ShadowDusk converts it for each backend and
/// <see cref="ContainerRuntime.SetShaderParameter(string, float)"/> sets its thickness and color.</para>
/// </summary>
internal class RenderTargetShaderScreen : FrameworkElement
{
#if RAYLIB
    // Relative path (resolved against FileManager.RelativeDirectory) to the grayscale GLSL shader.
    private const string ShaderFileName = "resources/Grayscale.fs";
#elif SKIA
    // Relative path (resolved against the working directory the resolver is given) to the grayscale
    // SkSL shader.
    private const string ShaderFileName = "resources/Grayscale.sksl";
#else
    // Relative to FileManager.RelativeDirectory (set to "Content/" by GumService.Initialize).
    private const string ShaderFileName = "Grayscale.fx";
    // Same effect authored in Slang instead of raw HLSL (issue #4677) — no per-shader-model
    // boilerplate, no technique/pass block. ShadowDusk's Slang frontend converts it to .fx text
    // before compiling; see RenderTargetShaderResolver.ResolveFxSource for the tool-side twin.
    private const string SlangShaderFileName = "Grayscale.slang";
    // Two more single-pass, parameterless Slang examples (issue #4679). Blur's radius is a
    // constant baked into the shader itself, sized for this demo's 128x128 cells — Gum's
    // render-target-effect blit sets no shader parameters and runs a single pass, so an
    // adjustable blur isn't possible without new engine work.
    private const string BlurSlangShaderFileName = "Blur.slang";
    private const string SepiaSlangShaderFileName = "Sepia.slang";
#endif

    // The same Outline.slang on every backend; only the folder it is copied to differs.
#if RAYLIB || SKIA
    private const string OutlineShaderFileName = "resources/Outline.slang";
#else
    private const string OutlineShaderFileName = "Outline.slang";
#endif

    public RenderTargetShaderScreen() : base(new ContainerRuntime())
    {
        Dock(Gum.Wireframe.Dock.Fill);

        var root = new ContainerRuntime();
        root.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        root.WidthUnits = DimensionUnitType.RelativeToChildren;
        root.HeightUnits = DimensionUnitType.RelativeToChildren;
        root.Width = 0;
        root.Height = 0;
        root.StackSpacing = 16;
        root.X = 12;
        root.Y = 12;
        this.AddChild(root);

        // Compile/load once up front: this drives the middle cell (effect set in code) and the
        // status label. The right cell only wires SourceShaderFile when this succeeds — runtime
        // MissingFileBehavior is ThrowException, so resolving a shader that can't compile would
        // throw; gating on a known-good compile keeps the demo degrading the same way both cells do.
#if RAYLIB || SKIA
        string effectPath = ShaderFileName;
#else
        string effectPath = ToolsUtilities.FileManager.RelativeDirectory + ShaderFileName;
#endif
        EffectType? inCodeEffect = CompileEffectFromFile(effectPath, out string status);

        var statusLabel = new TextRuntime();
        statusLabel.Text = status;
        statusLabel.Color = inCodeEffect != null ? Rgba(220, 220, 220, 255) : Rgba(255, 120, 120, 255);
        statusLabel.WidthUnits = DimensionUnitType.RelativeToChildren;
        statusLabel.HeightUnits = DimensionUnitType.RelativeToChildren;
        statusLabel.Width = 0;
        statusLabel.Height = 0;
        root.AddChild(statusLabel);

        var row = new ContainerRuntime();
        row.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        row.StackSpacing = 48;
        row.WidthUnits = DimensionUnitType.RelativeToChildren;
        row.HeightUnits = DimensionUnitType.RelativeToChildren;
        row.Width = 0;
        row.Height = 0;
        root.AddChild(row);

        // Left: the unmodified bear (no render target, no shader).
        row.AddChild(BuildCell("Original", effect: null, sourceShaderFile: null));

        // Middle: the grayscale effect loaded/compiled in code and assigned directly.
        row.AddChild(BuildCell("RenderTargetEffect (set in code)", effect: inCodeEffect, sourceShaderFile: null));

        // Right: the same shader referenced by file path; Gum's registered resolver loads it.
        row.AddChild(BuildCell("SourceShaderFile (shader file reference)",
            effect: null,
            sourceShaderFile: inCodeEffect != null ? ShaderFileName : null));

        // Second row, every backend: one Slang outline shader, with its thickness and color set from
        // code through SetShaderParameter. Both cells share the shader and differ only in values.
        var outlineRow = new ContainerRuntime();
        outlineRow.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        outlineRow.StackSpacing = 48;
        outlineRow.WidthUnits = DimensionUnitType.RelativeToChildren;
        outlineRow.HeightUnits = DimensionUnitType.RelativeToChildren;
        outlineRow.Width = 0;
        outlineRow.Height = 0;
        root.AddChild(outlineRow);

        outlineRow.AddChild(BuildOutlineCell("Outline.slang (2 px, yellow)", thickness: 2, red: 1f, green: 0.85f, blue: 0.1f));
        outlineRow.AddChild(BuildOutlineCell("Outline.slang (6 px, cyan)", thickness: 6, red: 0.1f, green: 0.9f, blue: 1f));
        outlineRow.AddChild(BuildOutlineCell("Bear + circle (follows cursor), slider sets thickness", thickness: 3, red: 1f, green: 1f, blue: 1f, interactive: true));

#if !RAYLIB && !SKIA
        // Fourth cell, XNA-like only: the same effect authored in Slang (Content/Grayscale.slang)
        // instead of raw HLSL, referenced the same way via SourceShaderFile — proves the Slang
        // frontend wiring (issue #4677) end to end, not just that .fx still works.
        row.AddChild(BuildSlangSourceShaderFileCell("SourceShaderFile (.slang)", SlangShaderFileName));

        // Second row, XNA-like only: two more example shaders (issue #4679), same wiring style.
        var effectsRow = new ContainerRuntime();
        effectsRow.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        effectsRow.StackSpacing = 48;
        effectsRow.WidthUnits = DimensionUnitType.RelativeToChildren;
        effectsRow.HeightUnits = DimensionUnitType.RelativeToChildren;
        effectsRow.Width = 0;
        effectsRow.Height = 0;
        root.AddChild(effectsRow);

        effectsRow.AddChild(BuildSlangSourceShaderFileCell("Blur (.slang, fixed radius)", BlurSlangShaderFileName));
        effectsRow.AddChild(BuildSlangSourceShaderFileCell("Sepia (.slang)", SepiaSlangShaderFileName));
#endif
    }

#if !RAYLIB && !SKIA
    // Compiles a .slang shader (relative to FileManager.RelativeDirectory) and wires it up via
    // SourceShaderFile, gating on a known-good compile the same way the .fx cell above does.
    private static ContainerRuntime BuildSlangSourceShaderFileCell(string caption, string slangFileName)
    {
        string effectPath = ToolsUtilities.FileManager.RelativeDirectory + slangFileName;
        EffectType? effect = CompileEffectFromFile(effectPath, out _);
        return BuildCell(caption, effect: null, sourceShaderFile: effect != null ? slangFileName : null);
    }
#endif

    // A bear in a render target container, outlined by Outline.slang. TexelSize is one pixel of the
    // container; Thickness is in those pixels. The interactive cell also draws a circle overlapping
    // the bear, so the outline wraps the combined shape, and a slider sets the thickness. A shader
    // that fails to convert or compile throws from the SourceShaderFile assignment on some backends,
    // so the failure is shown in the cell.
    private static ContainerRuntime BuildOutlineCell(
        string caption, float thickness, float red, float green, float blue, bool interactive = false)
    {
        // Transparent margin around the content, so the outline has room to draw (8 px at most).
        float cellSize = interactive ? 176 : 144;
        float margin = interactive ? 16 : 8;

        var cell = new ContainerRuntime();
        cell.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        cell.StackSpacing = 6;
        cell.WidthUnits = DimensionUnitType.RelativeToChildren;
        cell.HeightUnits = DimensionUnitType.RelativeToChildren;
        cell.Width = 0;
        cell.Height = 0;

        AddLabel(cell, caption);

        var holder = new ContainerRuntime();
        holder.Width = cellSize;
        holder.Height = cellSize;
        holder.IsRenderTarget = true;

        var bear = new SpriteRuntime();
#if RAYLIB
        bear.SourceFileName = "resources\\BearTexture.png";
#else
        bear.SourceFileName = "BearTexture.png";
#endif
        bear.WidthUnits = DimensionUnitType.Absolute;
        bear.HeightUnits = DimensionUnitType.Absolute;
        bear.X = margin;
        bear.Y = margin;
        bear.Width = cellSize - 2 * margin;
        bear.Height = cellSize - 2 * margin;
        holder.AddChild(bear);

        if (interactive)
        {
            // Overlaps the bear's lower right and sticks out past it, so the outline has to follow
            // both shapes as one. It has no stroke of its own, so the only outline is the shader's.
            const float circleSize = 72;
            const float edgeRoom = 8;
            var circle = new CircleRuntime();
            circle.IsFilled = true;
            circle.StrokeWidth = 0;
            circle.FillColor = Rgba(255, 120, 60, 255);
            circle.X = cellSize - margin - circleSize;
            circle.Y = cellSize - margin - circleSize;
            circle.Width = circleSize;
            circle.Height = circleSize;
            holder.AddChild(circle);

            // The circle follows the cursor while it is over the container, kept 8 px from the edge
            // so the largest outline still fits.
            holder.HasEvents = true;
            holder.RollOver += (_, _) =>
            {
                float cursorX = FrameworkElement.MainCursor.XRespectingGumZoomAndBounds() - holder.AbsoluteLeft;
                float cursorY = FrameworkElement.MainCursor.YRespectingGumZoomAndBounds() - holder.AbsoluteTop;
                float farthest = cellSize - circleSize - edgeRoom;
                circle.X = System.Math.Max(edgeRoom, System.Math.Min(farthest, cursorX - circleSize / 2));
                circle.Y = System.Math.Max(edgeRoom, System.Math.Min(farthest, cursorY - circleSize / 2));
            };
        }

        holder.SetShaderParameter("TexelSize", 1f / cellSize, 1f / cellSize);
        holder.SetShaderParameter("Thickness", thickness);
        holder.SetShaderParameter("OutlineColor", red, green, blue, 1f);

        try
        {
            holder.SourceShaderFile = OutlineShaderFileName;
        }
        catch (System.Exception e)
        {
            AddLabel(cell, "OUTLINE SHADER FAILED: " + e.Message);
        }

        cell.AddChild(holder);

        if (interactive)
        {
            TextRuntime thicknessLabel = AddLabel(cell, "Thickness: " + thickness.ToString("0") + " px");

            var slider = new Slider();
            slider.Minimum = 0;
            slider.Maximum = 8;
            slider.TicksFrequency = 1;
            slider.IsSnapToTickEnabled = true;
            slider.Value = thickness;
            slider.Width = cellSize;
            slider.ValueChanged += (_, _) =>
            {
                holder.SetShaderParameter("Thickness", (float)slider.Value);
                thicknessLabel.Text = "Thickness: " + slider.Value.ToString("0") + " px";
            };
            cell.Children.Add(slider.Visual);
        }

        return cell;
    }

    // Portable color construction across the three backends' Color aliases (XNA / Raylib_cs /
    // SKColor all expose a (byte,byte,byte,byte) form), matching RenderTargetScreen's Rgba helper.
    private static Color Rgba(byte r, byte g, byte b, byte a) => new Color(r, g, b, a);

    private static ContainerRuntime BuildCell(string caption, EffectType? effect, string? sourceShaderFile)
    {
        var cell = new ContainerRuntime();
        cell.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        cell.StackSpacing = 6;
        cell.WidthUnits = DimensionUnitType.RelativeToChildren;
        cell.HeightUnits = DimensionUnitType.RelativeToChildren;
        cell.Width = 0;
        cell.Height = 0;

        AddLabel(cell, caption);

        var holder = new ContainerRuntime();
        holder.Width = 128;
        holder.Height = 128;

        var bear = new SpriteRuntime();
#if RAYLIB
        bear.SourceFileName = "resources\\BearTexture.png";
#else
        bear.SourceFileName = "BearTexture.png";
#endif
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

    /// <summary>
    /// Loads/compiles the shader at <paramref name="path"/> into a platform effect, or returns null
    /// on failure. This is also the body of the app-registered <c>RenderTargetEffectResolver</c>
    /// (wired in each sample's entry point), so a container's
    /// <see cref="ContainerRuntime.SourceShaderFile"/> resolves through exactly this code.
    /// </summary>
    public static EffectType? CompileEffectFromFile(string path) => CompileEffectFromFile(path, out _);

    private static EffectType? CompileEffectFromFile(string path, out string status)
    {
#if RAYLIB
        // LoadShader returns a default (id 0) shader if the file is missing; the grayscale still
        // degrades to a pass-through rather than crashing. raylib loads GLSL directly with no
        // compiler dependency, so unlike MonoGame/Skia (which compile shader source at runtime),
        // there's no compile-error diagnostic to surface here.
        if (string.Equals(Path.GetExtension(path), ".slang", System.StringComparison.OrdinalIgnoreCase))
        {
            // A .slang file is converted to GLSL by ShadowDusk, then loaded like any raylib shader.
            Result<RaylibShader, ShaderError[]> conversion =
                RaylibConverter.ConvertSlang(File.ReadAllText(path), new RaylibConvertOptions());
            if (conversion.IsFailure)
            {
                status = "SHADER CONVERT FAILED:\n" + string.Join("\n", conversion.Error.Select(e => e.Message));
                return null;
            }

            status = "Slang converted to GLSL by ShadowDusk.";
            return LoadShaderFromMemory(null, conversion.Value.FragmentShader);
        }

        EffectType shader = LoadShader(null, path);
        status = "Grayscale GLSL post-process. Middle and right bears render grayscale; left is the original.";
        return shader;
#elif SKIA
        if (!File.Exists(path))
        {
            status = "SHADER FILE NOT FOUND: " + path;
            return null;
        }

        EffectType? effect = SKRuntimeEffect.CreateShader(File.ReadAllText(path), out string errors);
        if (effect == null)
        {
            status = "SHADER COMPILE FAILED:\n" + errors;
            return null;
        }

        status = "SkSL shader compiled. Middle and right bears render grayscale.";
        return effect;
#else
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
            string fxSource = ResolveFxSource(path);

            var compiler = new EffectCompiler();
            Result<CompiledShader, ShaderError[]> result =
                compiler.Compile(fxSource, new CompilerOptions { Target = PlatformTarget.OpenGL });

            if (result.IsSuccess)
            {
                status = "Shader compiled at runtime (ShadowDusk). Middle and right bears render grayscale.";
                return new EffectType(graphicsDevice, result.Value.Data);
            }

            status = "SHADER COMPILE FAILED:\n" + string.Join("\n", result.Error.Select(e => e.Message));
            return null;
        }
        catch (System.Exception e)
        {
            // A thrown error here is typically a missing/unloadable native compiler asset, or a
            // Slang→.fx conversion failure (see ResolveFxSource), rather than a shader-syntax
            // problem; surface it instead of taking down the screen.
            status = "SHADER COMPILE THREW: " + e.GetType().Name + ": " + e.Message;
            return null;
        }
#endif
    }

#if !RAYLIB && !SKIA
    /// <summary>
    /// Returns ordinary <c>.fx</c> effect text for <paramref name="path"/>. A <c>.slang</c> file is
    /// converted first via ShadowDusk's Slang frontend (Slang has no technique/pass concept, so it
    /// can't go through <see cref="EffectCompiler.Compile"/> directly); every other extension is
    /// assumed to already be <c>.fx</c> source and passed through unchanged.
    /// </summary>
    private static string ResolveFxSource(string path)
    {
        string sourceText = File.ReadAllText(path);
        if (!string.Equals(System.IO.Path.GetExtension(path), SlangFrontend.Extension, System.StringComparison.OrdinalIgnoreCase))
        {
            return sourceText;
        }

        Result<SlangFxConversion, ShaderError[]> conversion = SlangFrontend.ConvertToFx(
            sourceText,
            new SlangConvertOptions { SourceName = path });

        if (conversion.IsFailure)
        {
            throw new System.InvalidOperationException(
                "ShadowDusk could not convert the Slang render-target shader '" + path + "':\n" +
                string.Join("\n", conversion.Error.Select(e => e.Message)));
        }

        return conversion.Value.FxText;
    }
#endif

    private static TextRuntime AddLabel(ContainerRuntime container, string text)
    {
        var label = new TextRuntime();
        label.Text = text;
        label.Color = Rgba(200, 200, 200, 255);
        label.WidthUnits = DimensionUnitType.RelativeToChildren;
        label.HeightUnits = DimensionUnitType.RelativeToChildren;
        label.Width = 0;
        label.Height = 0;
        container.AddChild(label);
        return label;
    }
}
