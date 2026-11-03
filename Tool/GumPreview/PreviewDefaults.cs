using System;
using System.Collections.Generic;

namespace GumPreview;

/// <summary>When in a game's startup a default goes.</summary>
public enum PreviewDefaultPhase
{
    /// <summary>In the Game constructor, before the graphics device exists.</summary>
    Constructor,

    /// <summary>In Initialize, after the Gum service is initialized with the project.</summary>
    AfterGumInitialize,
}

/// <summary>One thing the Preview turns on for the user that a game has to turn on itself (issue #7261).</summary>
/// <param name="Id">Stable key the Preview's startup dispatches on.</param>
/// <param name="Explanation">One line saying what it enables.</param>
/// <param name="Phase">Where in startup the code goes.</param>
/// <param name="CodeLines">The MonoGame/KNI/FNA lines a game needs.</param>
public sealed record PreviewDefault(string Id, string Explanation, PreviewDefaultPhase Phase, IReadOnlyList<string> CodeLines);

/// <summary>What the Preview calls to apply each default; Game1 implements it.</summary>
public interface IPreviewDefaultsTarget
{
    void UseHiDefProfile();
    void InitializeShapes();
    void UseKeyboardDefaults();
    void UseGamepadDefaults();
}

/// <summary>
/// The single list of defaults the Preview enables. The Preview's startup applies it with
/// <see cref="Apply"/>, and the Code tab's game-setup code is generated from the same list, so the
/// code shown cannot drift from what the Preview does.
/// </summary>
public static class PreviewDefaults
{
    public static IReadOnlyList<PreviewDefault> All { get; } = new PreviewDefault[]
    {
        new("HiDefProfile",
            "Apos.Shapes uses a Shader Model 4 effect, which needs the HiDef graphics profile; shapes silently do not draw otherwise.",
            PreviewDefaultPhase.Constructor,
            new[] { "_graphics.GraphicsProfile = GraphicsProfile.HiDef;" }),
        new("Shapes",
            "Enables the shape renderer (fills, gradients, rounded corners, shadows) behind Rectangle, Circle, Arc and Polygon.",
            PreviewDefaultPhase.AfterGumInitialize,
            new[] { "Gum.Renderables.ShapeRenderer.Self.Initialize();" }),
        new("KeyboardDefaults",
            "Tab and Shift+Tab move focus; arrow keys, Enter and Space operate the focused control.",
            PreviewDefaultPhase.AfterGumInitialize,
            new[] { "GumUI.UseKeyboardDefaults();" }),
        new("GamepadDefaults",
            "D-pad and A move focus and activate the focused control.",
            PreviewDefaultPhase.AfterGumInitialize,
            new[] { "GumUI.UseGamepadDefaults();" }),
    };

    /// <summary>Applies the defaults of one phase, in list order.</summary>
    public static void Apply(IPreviewDefaultsTarget target, PreviewDefaultPhase phase)
    {
        foreach (PreviewDefault item in All)
        {
            if (item.Phase != phase)
            {
                continue;
            }
            switch (item.Id)
            {
                case "HiDefProfile": target.UseHiDefProfile(); break;
                case "Shapes": target.InitializeShapes(); break;
                case "KeyboardDefaults": target.UseKeyboardDefaults(); break;
                case "GamepadDefaults": target.UseGamepadDefaults(); break;
                default: throw new InvalidOperationException($"No Preview default applies '{item.Id}'.");
            }
        }
    }
}
