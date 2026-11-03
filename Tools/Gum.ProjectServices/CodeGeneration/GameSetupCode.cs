using System;
using System.Collections.Generic;
using System.Text;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// The game-setup code the Code tab shows (issue #7261): what a game needs to load the project and
/// enable everything the Gum Preview enables by default. It is built from <see cref="PreviewDefaults.All"/>,
/// the same list the Preview applies, so the code cannot drift from what the Preview does.
/// </summary>
public static class GameSetupCode
{
    /// <summary>
    /// Returns the setup text for a project's output library, or null when the section should be hidden
    /// (the legacy libraries). A null <paramref name="outputLibrary"/> (code generation not set up)
    /// shows the MonoGame/KNI/FNA setup.
    /// </summary>
    /// <param name="outputLibrary">The project's output library, or null when none is chosen yet.</param>
    /// <param name="projectFileName">The .gumx/.gumj file name the game loads.</param>
    /// <param name="screenName">The selected Screen, whose generated class the setup loads, or null.</param>
    public static string? Generate(OutputLibrary? outputLibrary, string projectFileName, string? screenName)
    {
        switch (outputLibrary)
        {
            case null:
            case OutputLibrary.MonoGame:
            case OutputLibrary.GumForms:
                return GenerateXnaFamily(projectFileName, screenName);
            case OutputLibrary.Raylib:
            case OutputLibrary.Skia:
            case OutputLibrary.Silk:
                return GenerateNote(outputLibrary.Value);
            default:
                return null;
        }
    }

    private static string GenerateXnaFamily(string projectFileName, string? screenName)
    {
        StringBuilder text = new StringBuilder();
        text.AppendLine("// Everything the Gum Preview turns on, so your game behaves the same.");
        text.AppendLine("// If your game already does this, you do not need to change it.");

        AppendPhase(text, PreviewDefaultPhase.Constructor, "public Game1()", () => { });
        text.AppendLine();
        AppendPhase(text, PreviewDefaultPhase.AfterGumInitialize, "protected override void Initialize()", () =>
        {
            text.AppendLine($"    GumUI.Initialize(this, \"{projectFileName}\");");
            text.AppendLine();
        }, hasLoad: true, screenName: screenName);
        return text.ToString().TrimEnd();
    }

    private static void AppendPhase(StringBuilder text, PreviewDefaultPhase phase, string signature, Action before,
        bool hasLoad = false, string? screenName = null)
    {
        text.AppendLine(signature);
        text.AppendLine("{");
        before();
        foreach (PreviewDefault item in PreviewDefaults.All)
        {
            if (item.Phase != phase)
            {
                continue;
            }
            text.AppendLine($"    // {item.Explanation}");
            foreach (string line in item.CodeLines)
            {
                text.AppendLine($"    {line}");
            }
        }
        if (hasLoad)
        {
            text.AppendLine();
            if (screenName != null)
            {
                int separator = screenName.LastIndexOf('/');
                string className = separator >= 0 ? screenName.Substring(separator + 1) : screenName;
                text.AppendLine("    // Show the selected Screen (your generated class).");
                text.AppendLine($"    var screen = new {className}();");
                text.AppendLine("    screen.AddToRoot();");
            }
            else
            {
                text.AppendLine("    // Select a Screen to see the lines that show it.");
            }
            text.AppendLine();
            text.AppendLine("    base.Initialize();");
        }
        text.AppendLine("}");
    }

    private static string GenerateNote(OutputLibrary outputLibrary)
    {
        return "// The Gum Preview runs on MonoGame, so what it enables by default (Apos.Shapes, tab, keyboard and gamepad\n" +
            "// navigation) are MonoGame/KNI/FNA steps. There is no equivalent setup to show for " + outputLibrary.GetName() + ".";
    }
}
