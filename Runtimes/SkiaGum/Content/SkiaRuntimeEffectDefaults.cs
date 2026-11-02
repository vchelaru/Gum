using System.Runtime.CompilerServices;
using RenderingLibrary.Graphics;
using SkiaSharp;

namespace SkiaGum.Content;

/// <summary>
/// The defaults a shader declares (<c>float Thickness = 1.0;</c>) for an <see cref="SKRuntimeEffect"/>.
/// SkSL declares every uniform without a value, so Skia cannot report them; whatever compiled the
/// effect (the Gum.SkiaSharp.ShadowDusk loader does) registers them here, and a container that does
/// not set a parameter then draws with the declared default instead of zero.
/// </summary>
public static class SkiaRuntimeEffectDefaults
{
    private static readonly ConditionalWeakTable<SKRuntimeEffect, ShaderUniformDefaults> _defaults =
        new ConditionalWeakTable<SKRuntimeEffect, ShaderUniformDefaults>();

    /// <summary>Records the declared defaults of <paramref name="effect"/>, replacing any earlier ones.</summary>
    public static void Set(SKRuntimeEffect effect, ShaderUniformDefaults defaults)
    {
        _defaults.Remove(effect);
        _defaults.Add(effect, defaults);
    }

    /// <summary>The defaults registered for <paramref name="effect"/>, or null when there are none.</summary>
    public static ShaderUniformDefaults? TryGet(SKRuntimeEffect effect) =>
        _defaults.TryGetValue(effect, out ShaderUniformDefaults? defaults) ? defaults : null;
}
