using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gum.ProjectServices;

/// <summary>
/// One Gum runtime: the facts every list in this project used to keep by hand.
/// </summary>
/// <param name="AssemblyName">The runtime's dll name, which a consumer's own assembly must not share.</param>
/// <param name="PackageId">The NuGet package id, or null for a runtime that is not packed (Unity).</param>
/// <param name="ProjectPath">The runtime's csproj inside a Gum checkout, relative to its root.</param>
/// <param name="Platform">The scaffolded host that uses this runtime, or null when no host targets it directly.</param>
internal sealed record HostPlatformInfo(string AssemblyName, string? PackageId, string ProjectPath, HostPlatform? Platform = null)
{
    /// <summary>
    /// Every Gum runtime. The order is the order <c>SyntaxVersionDetectionService</c> tries packages in.
    /// A new runtime is added here and nowhere else; <c>HostPlatformInfoDriftTests</c> reads the csprojs to check.
    /// </summary>
    internal static IReadOnlyList<HostPlatformInfo> All { get; } = new[]
    {
        new HostPlatformInfo("MonoGameGum", "Gum.MonoGame", Path.Combine("MonoGameGum", "MonoGameGum.csproj"), HostPlatform.MonoGame),
        new HostPlatformInfo("KniGum", "Gum.KNI", Path.Combine("MonoGameGum", "KniGum", "KniGum.csproj"), HostPlatform.Kni),
        new HostPlatformInfo("FnaGum", "Gum.FNA", Path.Combine("MonoGameGum", "FnaGum", "FnaGum.csproj")),
        new HostPlatformInfo("SkiaGum", "Gum.SkiaSharp", Path.Combine("Runtimes", "SkiaGum", "SkiaGum.csproj")),
        new HostPlatformInfo("RaylibGum", "Gum.raylib", Path.Combine("Runtimes", "RaylibGum", "RaylibGum.csproj"), HostPlatform.Raylib),
        new HostPlatformInfo("SokolGum", "Gum.sokol", Path.Combine("Runtimes", "SokolGum", "SokolGum.csproj")),
        new HostPlatformInfo("SilkNetGum", "Gum.SilkNet", Path.Combine("Runtimes", "SilkNetGum", "SilkNetGum.csproj"), HostPlatform.SilkNet),
        new HostPlatformInfo("StrideGum", "Gum.Stride", Path.Combine("Runtimes", "StrideGum", "StrideGum.csproj"), HostPlatform.Stride),
        new HostPlatformInfo("BrutalGum.OGL", "Gum.Brutal.OGL", Path.Combine("Runtimes", "BrutalGum.OGL", "BrutalGum.OGL.csproj"), HostPlatform.Brutal),
        new HostPlatformInfo("BrutalGum.Vulkan", "Gum.Brutal.Vulkan", Path.Combine("Runtimes", "BrutalGum.Vulkan", "BrutalGum.Vulkan.csproj")),
        new HostPlatformInfo("KniSkiaGum", "Gum.KniSkia", Path.Combine("Runtimes", "KniSkiaGum", "KniSkiaGum.csproj"), HostPlatform.KniSkia),
        new HostPlatformInfo("SkiaGum.Standalone", "Gum.SkiaSharp.Standalone", Path.Combine("Runtimes", "SkiaGum.Standalone", "SkiaGum.Standalone.csproj")),
        new HostPlatformInfo("SkiaGum.Maui", "Gum.SkiaSharp.Maui", Path.Combine("Runtimes", "SkiaGum.Maui", "SkiaGum.Maui.csproj")),
        new HostPlatformInfo("SkiaGum.ShadowDusk", "Gum.SkiaSharp.ShadowDusk", Path.Combine("Runtimes", "SkiaGum.ShadowDusk", "SkiaGum.ShadowDusk.csproj")),
        new HostPlatformInfo("MonoGameGum.ShadowDusk", "Gum.MonoGame.ShadowDusk", Path.Combine("Runtimes", "GumShadowDusk", "MonoGameGum.ShadowDusk.csproj")),
        new HostPlatformInfo("KniGum.ShadowDusk", "Gum.KNI.ShadowDusk", Path.Combine("Runtimes", "GumShadowDusk", "KniGum.ShadowDusk.csproj")),
        new HostPlatformInfo("FnaGum.ShadowDusk", "Gum.FNA.ShadowDusk", Path.Combine("Runtimes", "GumShadowDusk", "FnaGum.ShadowDusk.csproj")),
        new HostPlatformInfo("RaylibGum.ShadowDusk", "Gum.raylib.ShadowDusk", Path.Combine("Runtimes", "RaylibGum.ShadowDusk", "RaylibGum.ShadowDusk.csproj")),
        // Copied into a Unity project rather than packed, but a same-named consumer assembly collides all the same.
        new HostPlatformInfo("UnityGum", null, Path.Combine("Runtimes", "UnityGum", "UnityGum.csproj")),
    };

    // Dlls that ship beside a runtime without being one: GumCommon, and the add-on packages (Gum.Expressions,
    // Gum.Shapes.*), which have no entry in All because syntax-version detection must not read their versions.
    private static readonly string[] OtherShippedAssemblyNames =
    {
        "GumCommon", "GumExpressions", "MonoGameGumShapes", "KniGumShapes"
    };

    /// <summary>Every assembly name a consumer project must not take: it would overwrite that dll in the shared output folder.</summary>
    internal static IReadOnlyList<string> ReservedAssemblyNames { get; } =
        All.Select(info => info.AssemblyName).Concat(OtherShippedAssemblyNames).ToArray();

    /// <summary>The package ids of every packed runtime, in <see cref="All"/> order.</summary>
    internal static IReadOnlyList<string> PackageIds { get; } =
        All.Where(info => info.PackageId != null).Select(info => info.PackageId!).ToArray();

    /// <summary>The runtime the scaffolded <paramref name="platform"/> host references.</summary>
    internal static HostPlatformInfo For(HostPlatform platform) =>
        All.FirstOrDefault(info => info.Platform == platform)
        ?? throw new ArgumentOutOfRangeException(nameof(platform), platform, null);
}
