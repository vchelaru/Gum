using System;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Expressions;
using Gum.Managers;
using GumRuntime;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Re-materializes every VariableReferences scalar in a FormsThemes/&lt;Name&gt; theme's
/// Components AND Screens against its current Styles.gucx values. NOT a real test - a reusable
/// manual migration step for porting a new theme (see the gum-forms-theme-import skill's
/// "recolor" step).
/// <para>
/// Why this exists: <c>gumcli check-references --fix</c> only fills scalars that are entirely
/// MISSING; it does not detect or correct a stale literal that no longer matches what its
/// VariableReferences string currently resolves to (confirmed empirically while porting Hazard,
/// #4135 - it made zero changes to a project full of stale-but-present values). After hand-editing
/// a swatch's value in Styles.gucx, or renaming which swatch a control's VariableReferences
/// string points at, every existing hardcoded literal alongside those references is stale until
/// something re-runs ApplyVariableReferences and re-saves. This is that something.
/// </para>
/// <para>
/// Usage for a new theme port: after Styles.gucx is finalized and every control's
/// VariableReferences strings point at the correct (possibly renamed) swatch names, remove the
/// Skip below, set <c>ThemeName</c>, run this test once via
/// <c>dotnet test --filter FullyQualifiedName~ThemeRecolorHelper</c>, then re-add Skip. Follow
/// with <c>gumcli check</c> / <c>check-references</c> / <c>diff-standards</c> to verify.
/// </para>
/// </summary>
public class ThemeRecolorHelper
{
    // Change this before running.
    private const string ThemeName = "Meadow";

    [Fact(Skip = "Manual migration step. Set Skip = null and ThemeName, then run.")]
    public void ReapplyAndSave()
    {
        StandardElementsManager.Self.Initialize();
        GumExpressionService.Initialize();

        string themeDir = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", ThemeName);
        string gumxPath = Path.Combine(themeDir, "GumProject.gumx");

        ProjectLoadResult result = new ProjectLoader().Load(gumxPath);
        result.Success.ShouldBeTrue();

        GumProjectSave project = result.Project!;
        ObjectFinder.Self.GumProjectSave = project;

        project.ApplyAllVariableReferences();

        // useCompactFormat: true is load-bearing - the default (false) serializes
        // VariableSave/InstanceSave as child elements (the legacy v1 shape) instead of the
        // attribute-based v2 shape every theme file on disk actually uses. Both round-trip
        // through ProjectLoader fine, so this silently produces a valid-but-inconsistent file
        // if you drop the argument.
        foreach (ComponentSave component in project.Components)
        {
            string path = Path.Combine(themeDir, "Components", component.Name + ".gucx");
            component.Save(path, useCompactFormat: true);
        }

        // Screens need the same re-materialization as Components - a demo screen's own
        // instances (e.g. DemoScreenGum's title text) can carry VariableReferences to Styles
        // just like a control does, and go just as stale.
        foreach (ScreenSave screen in project.Screens)
        {
            string path = Path.Combine(themeDir, "Screens", screen.Name + ".gusx");
            screen.Save(path, useCompactFormat: true);
        }
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string templatesDir = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates");
            if (Directory.Exists(templatesDir))
            {
                return current;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate repo root from " + AppContext.BaseDirectory);
    }
}
