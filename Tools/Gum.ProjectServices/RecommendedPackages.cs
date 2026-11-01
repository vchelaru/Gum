using System;
using System.Collections.Generic;

namespace Gum.ProjectServices;

/// <summary>
/// The NuGet add-ons the setup docs (<c>docs/code/getting-started/setup/adding-initializing-gum</c>) recommend per platform,
/// which New Project adds to the code project when asked. Only packages that exist on nuget.org are listed; a source-linked project uses <see cref="ForSourceLinked"/> instead.
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
        // Gum.Shapes.MonoGame is left out until a stable release carries Gum.Renderables.ShapeRenderer: stable 2026.10.1.1 lacks it, so the host init line would not build. Re-add as the first entry after the November stable (with the ShadowDusk rows).
        HostPlatform.MonoGame => new[] { "KernSmith.MonoGameGum", Expressions },
        // monogame-kni-fna/README.md, KNI tab.
        // Gum.Shapes.KNI: left out for the same reason as Gum.Shapes.MonoGame.
        HostPlatform.Kni => new[] { "KernSmith.KniGum", Expressions },
        // raylib-raylib-cs.md.
        HostPlatform.Raylib => new[] { "KernSmith.RaylibGum", Expressions },
        // silk.net.md: Skia-based, so no Gum.Shapes. Gum.SkiaSharp.ShadowDusk (also fits Stride) is left out until it has a stable version: nuget.org has only previews, and the floating Version="*" does not resolve to a prerelease (NU1103). Add a ShadowDusk row here after its stable publish. The Silk.NET packages are already in the host template.
        HostPlatform.SilkNet => new[] { Expressions },
        // stride.md: no Shapes (Skia-based). ShadowDusk is left out for now, as on silknet.
        HostPlatform.Stride => new[] { Expressions },
        // No setup page; the platform is always source-linked, where NuGet add-ons would pull in a second Gum.
        HostPlatform.KniSkia => Array.Empty<string>(),
        _ => Array.Empty<string>()
    };

    /// <summary>
    /// The add-ons a source-linked project links for <paramref name="platform"/> instead of adding packages, as package ids so the
    /// host's init lines stay one table. Unlike <see cref="For"/> it includes Gum.Shapes.MonoGame / Gum.Shapes.KNI: the checkout
    /// always carries the shapes project, so the stable-release gap that keeps shapes out of the NuGet list does not apply.
    /// Over-linking is deliberate: source-linking is an advanced path, and an unwanted reference is easy to remove.
    /// </summary>
    public static IReadOnlyList<string> ForSourceLinked(HostPlatform platform) => platform switch
    {
        HostPlatform.MonoGame => new[] { "Gum.Shapes.MonoGame", "KernSmith.MonoGameGum", Expressions },
        HostPlatform.Kni => new[] { "Gum.Shapes.KNI", "KernSmith.KniGum", Expressions },
        HostPlatform.Raylib => new[] { "KernSmith.RaylibGum", Expressions },
        HostPlatform.SilkNet => new[] { Expressions },
        HostPlatform.Stride => new[] { Expressions },
        // Always source-linked and has no add-ons to match (see For).
        _ => Array.Empty<string>()
    };

    /// <summary>
    /// The project of <paramref name="packageId"/> inside a Gum checkout, relative to the checkout's root (the
    /// <see cref="System.IO.Path.DirectorySeparatorChar"/> separates segments).
    /// </summary>
    public static string GetSourceProject(string packageId) => packageId switch
    {
        "Gum.Shapes.MonoGame" => Combine("Runtimes", "GumShapes", "MonoGameGumShapes.csproj"),
        "Gum.Shapes.KNI" => Combine("Runtimes", "GumShapes", "KniGumShapes.csproj"),
        "KernSmith.MonoGameGum" => Combine("Integrations", "KernSmith", "KernSmith.MonoGameGum", "KernSmith.MonoGameGum.csproj"),
        "KernSmith.KniGum" => Combine("Integrations", "KernSmith", "KernSmith.KniGum", "KernSmith.KniGum.csproj"),
        "KernSmith.RaylibGum" => Combine("Integrations", "KernSmith", "KernSmith.RaylibGum", "KernSmith.RaylibGum.csproj"),
        Expressions => Combine("Runtimes", "GumExpressions", "GumExpressions.csproj"),
        _ => throw new ArgumentOutOfRangeException(nameof(packageId), packageId, "No source project is known for this package.")
    };

    private static string Combine(params string[] segments) => System.IO.Path.Combine(segments);
}
