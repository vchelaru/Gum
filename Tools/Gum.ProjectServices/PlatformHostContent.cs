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
    // Each line is the one the setup docs (docs/code/getting-started/setup/adding-initializing-gum) give for the package,
    // after Gum's own Initialize. GraphicsDevice is the Game's property in the MonoGame and KNI Game1.
    private static readonly Dictionary<string, string> InitLineByPackage = new Dictionary<string, string>
    {
        // Kept for the November stable; no table row adds it until then.
        ["Gum.Shapes.MonoGame"] = "Gum.Renderables.ShapeRenderer.Self.Initialize();",
        ["Gum.Shapes.KNI"] = "Gum.Renderables.ShapeRenderer.Self.Initialize();",
        ["KernSmith.MonoGameGum"] =
            "Gum.Wireframe.CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmith.Gum.KernSmithFontCreator(GraphicsDevice);",
        ["KernSmith.KniGum"] =
            "Gum.Wireframe.CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmith.Gum.KernSmithFontCreator(GraphicsDevice);",
        ["KernSmith.RaylibGum"] =
            "RaylibGum.Renderables.CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmith.Gum.KernSmithRaylibFontCreator();",
        ["Gum.Expressions"] = "Gum.Expressions.GumExpressionService.Initialize();",
    };

    internal static IReadOnlyList<string> GetInitLines(IReadOnlyList<string> addedPackages) =>
        addedPackages.Where(InitLineByPackage.ContainsKey).Select(package => InitLineByPackage[package]).Distinct().ToList();

    internal static IReadOnlyList<string> GetUsings(DemoScreenInfo? screen, bool generated)
    {
        if (screen == null)
        {
            return Array.Empty<string>();
        }

        return generated
            ? new[] { "using Gum.Forms.Controls;", $"using {screen.Namespace};" }
            : new[] { "using Gum.Managers;", "using GumRuntime;" };
    }

    internal static IReadOnlyList<string> GetScreenLines(DemoScreenInfo? screen, bool generated)
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
            $"ObjectFinder.Self.GetElementSave(\"{screen.Name}\")!.ToGraphicalUiElement(GumService.Default.SystemManagers, addToManagers: false).AddToRoot();"
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
