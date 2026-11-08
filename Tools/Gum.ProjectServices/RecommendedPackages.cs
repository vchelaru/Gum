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
    /// source-linked and has no setup page. Never lists <c>KernSmith.Rasterizers.StbTrueType</c> (web only) or
    /// <c>Gum.FNA.ShadowDusk</c> (FNA is not a New Project platform).
    /// </summary>
    public static IReadOnlyList<string> For(HostPlatform platform) => For(platform, NewProjectPackageChannel.Stable);

    /// <summary>
    /// The packages for <paramref name="platform"/> on <paramref name="channel"/>. Stable and Preview share one list: every listed
    /// package has a stable version, which the Preview csproj's <c>*-*</c> float also resolves. Local source links
    /// projects instead (<see cref="ForSourceLinked"/>), so it gets the stable list here.
    /// </summary>
    public static IReadOnlyList<string> For(HostPlatform platform, NewProjectPackageChannel channel) => ForStable(platform);

    private static IReadOnlyList<string> ForStable(HostPlatform platform) => platform switch
    {
        // monogame-kni-fna/README.md, MonoGame tab.
        HostPlatform.MonoGame => new[] { "Gum.Shapes.MonoGame", "KernSmith.MonoGameGum", "Gum.MonoGame.ShadowDusk", Expressions },
        // monogame-kni-fna/README.md, KNI tab.
        HostPlatform.Kni => new[] { "Gum.Shapes.KNI", "KernSmith.KniGum", "Gum.KNI.ShadowDusk", Expressions },
        // raylib-raylib-cs.md.
        HostPlatform.Raylib => new[] { "KernSmith.RaylibGum", "Gum.raylib.ShadowDusk", Expressions },
        // silk.net.md: Skia-based, so no Gum.Shapes. The Silk.NET packages are already in the host template.
        HostPlatform.SilkNet => new[] { "Gum.SkiaSharp.ShadowDusk", Expressions },
        // stride.md: no Shapes (Skia-based).
        HostPlatform.Stride => new[] { "Gum.SkiaSharp.ShadowDusk", Expressions },
        // brutal.md: no Shapes (Skia-based). The BRUTAL packages are already in the host template.
        HostPlatform.Brutal => new[] { "Gum.SkiaSharp.ShadowDusk", Expressions },
        // No setup page; the platform is always source-linked, where NuGet add-ons would pull in a second Gum.
        HostPlatform.KniSkia => Array.Empty<string>(),
        _ => Array.Empty<string>()
    };

    /// <summary>
    /// The add-ons a source-linked project links for <paramref name="platform"/> instead of adding packages, as package ids so the
    /// host's init lines stay one table. It matches <see cref="For"/>: each NuGet add-on is the project of the same name in the checkout.
    /// Over-linking is deliberate: source-linking is an advanced path, and an unwanted reference is easy to remove.
    /// </summary>
    public static IReadOnlyList<string> ForSourceLinked(HostPlatform platform) => ForStable(platform);

    /// <summary>
    /// The project of <paramref name="packageId"/> inside a Gum checkout, relative to the checkout's root (the
    /// <see cref="System.IO.Path.DirectorySeparatorChar"/> separates segments).
    /// </summary>
    public static string GetSourceProject(string packageId) => packageId switch
    {
        "Gum.Shapes.MonoGame" => Combine("Runtimes", "GumShapes", "MonoGameGumShapes.csproj"),
        "Gum.Shapes.KNI" => Combine("Runtimes", "GumShapes", "KniGumShapes.csproj"),
        "Gum.MonoGame.ShadowDusk" => Combine("Runtimes", "GumShadowDusk", "MonoGameGum.ShadowDusk.csproj"),
        "Gum.KNI.ShadowDusk" => Combine("Runtimes", "GumShadowDusk", "KniGum.ShadowDusk.csproj"),
        "Gum.raylib.ShadowDusk" => Combine("Runtimes", "RaylibGum.ShadowDusk", "RaylibGum.ShadowDusk.csproj"),
        "Gum.SkiaSharp.ShadowDusk" => Combine("Runtimes", "SkiaGum.ShadowDusk", "SkiaGum.ShadowDusk.csproj"),
        "KernSmith.MonoGameGum" => Combine("Integrations", "KernSmith", "KernSmith.MonoGameGum", "KernSmith.MonoGameGum.csproj"),
        "KernSmith.KniGum" => Combine("Integrations", "KernSmith", "KernSmith.KniGum", "KernSmith.KniGum.csproj"),
        "KernSmith.RaylibGum" => Combine("Integrations", "KernSmith", "KernSmith.RaylibGum", "KernSmith.RaylibGum.csproj"),
        Expressions => Combine("Runtimes", "GumExpressions", "GumExpressions.csproj"),
        _ => throw new ArgumentOutOfRangeException(nameof(packageId), packageId, "No source project is known for this package.")
    };

    private static string Combine(params string[] segments) => System.IO.Path.Combine(segments);
}
