using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Gum.ProjectServices;

/// <summary>
/// What a scaffolded host game (<c>Game1.cs</c> or <c>Program.cs</c>) does after it initializes Gum: initialize the
/// packages that were added, then show the project's demo screen (or explain how to add a screen).
/// </summary>
internal static class PlatformHostContent
{
    // The add-on call each package needs after Gum's own Initialize (the setup docs list the same). The calls are extension
    // methods on the service, so none needs the platform's types, and GetInitLines emits them in AddOnCallOrder.
    private static readonly Dictionary<string, string> CallByPackage = new Dictionary<string, string>
    {
        ["Gum.Shapes.MonoGame"] = "UseShapes",
        ["Gum.Shapes.KNI"] = "UseShapes",
        ["Gum.MonoGame.ShadowDusk"] = "UseShadowDusk",
        ["Gum.KNI.ShadowDusk"] = "UseShadowDusk",
        ["Gum.raylib.ShadowDusk"] = "UseShadowDusk",
        ["Gum.SkiaSharp.ShadowDusk"] = "UseShadowDusk",
        ["KernSmith.MonoGameGum"] = "UseKernSmith",
        ["KernSmith.KniGum"] = "UseKernSmith",
        ["KernSmith.RaylibGum"] = "UseKernSmith",
        ["Gum.Expressions"] = "UseExpressions",
    };

    // The one order every platform's host (and the samples) call the add-ons in, after UseKeyboardDefaults and UseGamepadDefaults.
    private static readonly string[] AddOnCallOrder = { "UseShadowDusk", "UseShapes", "UseKernSmith", "UseExpressions" };

    /// <summary>How the scaffolded host refers to Gum: the <c>GumUI</c> alias every template declares.</summary>
    internal static string GetGumReceiver(HostPlatform platform) => "GumUI";

    /// <summary>The folder the Game1 template loads the Gum project from. Skia reads it from disk instead of through the content pipeline, so the path starts at Content.</summary>
    internal static string GetGumProjectFolder(HostPlatform platform) =>
        platform == HostPlatform.KniSkia ? "Content/GumProject/" : "GumProject/";

    /// <summary>Extra Game1 constructor lines. The Skia host resizes its render target with the window, so it opts in to a resizable one; the Shapes package needs the HiDef graphics profile.</summary>
    internal static IReadOnlyList<string> GetWindowLines(HostPlatform platform, IReadOnlyList<string> addedPackages)
    {
        List<string> lines = new List<string>();
        if (platform == HostPlatform.KniSkia)
        {
            lines.Add("Window.AllowUserResizing = true;");
        }

        if (AddsShapes(addedPackages))
        {
            lines.Add("_graphics.GraphicsProfile = GraphicsProfile.HiDef;");
        }

        return lines;
    }

    internal static IReadOnlyList<string> GetInitLines(IReadOnlyList<string> addedPackages, string gum = "GumService.Default")
    {
        HashSet<string> calls = new HashSet<string>(addedPackages.Where(CallByPackage.ContainsKey).Select(package => CallByPackage[package]));
        return AddOnCallOrder.Where(calls.Contains).Select(call => $"{gum}.{call}();").ToList();
    }

    internal static IReadOnlyList<string> GetUsings(DemoScreenInfo? screen, bool generated, IReadOnlyList<string> addedPackages)
    {
        List<string> usings = new List<string>();
        if (screen != null)
        {
            usings.AddRange(generated
                ? new[] { $"using {screen.Namespace};" }
                : new[] { "using Gum.Managers;", "using GumRuntime;" });
        }

        if (AddsShapes(addedPackages))
        {
            usings.Add("using Microsoft.Xna.Framework.Graphics;");
        }

        return usings;
    }

    // Apos.Shapes uses a Shader Model 4 effect, which needs the HiDef profile (PreviewDefaults has the same line); KNI's UseShapes throws without it (#7812).
    private static bool AddsShapes(IReadOnlyList<string> addedPackages) =>
        addedPackages.Any(package => CallByPackage.TryGetValue(package, out string? call) && call == "UseShapes");

    internal static IReadOnlyList<string> GetScreenLines(DemoScreenInfo? screen, bool generated, string gum = "GumService.Default")
    {
        if (screen == null)
        {
            return new[]
            {
                "// Create screens and components in the Gum tool, generate code, then add them here with:",
                "// new MyScreen().AddToRoot();"
            };
        }

        if (generated)
        {
            return new[]
            {
                "// The demo screen's code was generated into the project. Create your own screens in the Gum tool,",
                "// generate code, then add them here the same way.",
                $"new {screen.ClassName}().AddToRoot();"
            };
        }

        return new[]
        {
            "// Shows the project's demo screen, loaded from the Gum project at runtime. To create screens as classes instead,",
            "// generate code for them in the Gum tool, then add them here with: new MyScreen().AddToRoot();",
            $"ObjectFinder.Self.GetElementSave(\"{screen.Name}\")!.ToGraphicalUiElement({gum}.SystemManagers, addToManagers: false).AddToRoot();"
        };
    }

    /// <summary>Replaces a template line holding only <paramref name="placeholder"/> with <paramref name="lines"/> at its indent, or removes it when there are none.</summary>
    internal static string Expand(string template, string placeholder, IReadOnlyList<string> lines)
    {
        Regex line = new Regex("^([ \\t]*)" + Regex.Escape(placeholder) + "\\r?\\n", RegexOptions.Multiline);
        return line.Replace(template, match =>
            string.Concat(lines.Select(text => match.Groups[1].Value + text + Environment.NewLine)));
    }
}
