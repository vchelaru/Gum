using System;
using System.Collections.Generic;

namespace Gum.ProjectServices;

/// <summary>
/// The NuGet add-ons the setup docs (<c>docs/code/getting-started/setup/adding-initializing-gum</c>) recommend per platform,
/// which New Project adds to the code project when asked. Only packages that exist on nuget.org are listed.
/// </summary>
public static class RecommendedPackages
{
    private const string Expressions = "Gum.Expressions";

    /// <summary>
    /// The packages for <paramref name="platform"/>, in the order the docs list them. Empty for KNI with Skia, which is always
    /// source-linked and has no setup page. Never lists <c>KernSmith.Rasterizers.StbTrueType</c> (web only), nor, on
    /// the docs' side, the ShadowDusk packages of MonoGame, KNI and raylib (<c>Gum.MonoGame.ShadowDusk</c>, <c>Gum.KNI.ShadowDusk</c>,
    /// <c>Gum.raylib.ShadowDusk</c>), which are not published to nuget.org yet.
    /// </summary>
    public static IReadOnlyList<string> For(HostPlatform platform) => platform switch
    {
        // monogame-kni-fna/README.md, MonoGame tab.
        HostPlatform.MonoGame => new[] { "Gum.Shapes.MonoGame", "KernSmith.MonoGameGum", Expressions },
        // monogame-kni-fna/README.md, KNI tab.
        HostPlatform.Kni => new[] { "Gum.Shapes.KNI", "KernSmith.KniGum", Expressions },
        // raylib-raylib-cs.md.
        HostPlatform.Raylib => new[] { "KernSmith.RaylibGum", Expressions },
        // silk.net.md: Skia-based, so no Gum.Shapes. The Silk.NET packages are already in the host template.
        HostPlatform.SilkNet => new[] { "Gum.SkiaSharp.ShadowDusk", Expressions },
        // stride.md: the renderer ships inside Gum.Stride.
        HostPlatform.Stride => new[] { Expressions },
        // No setup page; the platform is always source-linked, where NuGet add-ons would pull in a second Gum.
        HostPlatform.KniSkia => Array.Empty<string>(),
        _ => Array.Empty<string>()
    };
}
