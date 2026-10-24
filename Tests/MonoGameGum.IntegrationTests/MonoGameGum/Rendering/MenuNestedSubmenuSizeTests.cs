using System;
using System.Collections.Generic;
using Gum.Forms;
using Gum.Forms.Controls;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;
using V3Styling = Gum.Forms.DefaultVisuals.V3.Styling;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// The size of each nested submenu popup (File, File > Recent, File > Recent > More) for the V3 visuals
/// and every code-only theme. Pinned to the sizes these popups had before themes moved their popup to
/// MenuItem.DefaultSubmenuPopupTemplate (#6741).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class MenuNestedSubmenuSizeTests : BaseTestClass
{
    public static IEnumerable<object?[]> Themes()
    {
        yield return new object?[] { "standard", null };
        yield return new object?[] { "bubblegum", (Action)Gum.Themes.Bubblegum.BubblegumTheme.Apply };
        yield return new object?[] { "darkpro", (Action)Gum.Themes.DarkPro.DarkProTheme.Apply };
        yield return new object?[] { "editor", (Action)Gum.Themes.Editor.EditorTheme.Apply };
        yield return new object?[] { "forestglade", (Action)Gum.Themes.ForestGlade.ForestGladeTheme.Apply };
        yield return new object?[] { "hazard", (Action)Gum.Themes.Hazard.HazardTheme.Apply };
        yield return new object?[] { "meadow", (Action)Gum.Themes.Meadow.MeadowTheme.Apply };
        yield return new object?[] { "neon", (Action)Gum.Themes.Neon.NeonTheme.Apply };
        yield return new object?[] { "retro95", (Action)Gum.Themes.Retro95.Retro95Theme.Apply };
        yield return new object?[] { "template", (Action)Gum.Themes.Template.TemplateTheme.Apply };
    }

    // "width x height" of the File, Recent and More popups.
    private static readonly Dictionary<string, string> Expected = new()
    {
        ["standard"] = "85x44, 69x44, 165x65",
        ["bubblegum"] = "107x78, 96x78, 168x116",
        ["darkpro"] = "111x64, 96x64, 192x95",
        ["editor"] = "76x36, 62x36, 136x53",
        ["forestglade"] = "101x72, 90x72, 162x110",
        ["hazard"] = "99x74, 91x74, 146x110",
        ["meadow"] = "114x84, 99x84, 185x125",
        ["neon"] = "105x69, 91x69, 173x101",
        ["retro95"] = "99x50, 88x50, 152x74",
        ["template"] = "107x66, 96x66, 168x98",
    };

    [Theory]
    [MemberData(nameof(Themes))]
    public void OpenThreeLevels_PopupSizes_MatchPinned(string themeName, Action? applyTheme)
    {
        string sizes = MeasureNestedPopups(applyTheme);

        sizes.ShouldBe(Expected.GetValueOrDefault(themeName), themeName);
    }

    private string MeasureNestedPopups(Action? applyTheme)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        V3Styling previousStyle = V3Styling.ActiveStyle;
        var previousFontCreator = global::Gum.Wireframe.CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            using Lease lease = new();
            applyTheme?.Invoke();

            Menu menu = new Menu();
            MenuItem file = new MenuItem { Header = "File" };
            MenuItem recent = new MenuItem { Header = "Recent" };
            MenuItem more = new MenuItem { Header = "More" };
            more.Items!.Add(new MenuItem { Header = "Alpha" });
            more.Items.Add(new MenuItem { Header = "A much longer entry" });
            more.Items.Add(new MenuItem { Header = "Gamma" });
            recent.Items!.Add(new MenuItem { Header = "First" });
            recent.Items.Add(more);
            file.Items!.Add(new MenuItem { Header = "New" });
            file.Items.Add(recent);
            menu.Items!.Add(file);
            menu.AddToRoot();
            menu.Visual.UpdateLayout();

            file.TryShowPopup();
            recent.TryShowPopup();
            more.TryShowPopup();

            return string.Join(", ", Size(file), Size(recent), Size(more));
        }
        finally
        {
            MenuItem.DefaultSubmenuPopupTemplate = null;
            FrameworkElement.DefaultFormsTemplates.Clear();
            foreach (KeyValuePair<Type, VisualTemplate> pair in previousTemplates)
            {
                FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value;
            }
            V3Styling.ActiveStyle = previousStyle;
            global::Gum.Wireframe.CustomSetPropertyOnRenderable.InMemoryFontCreator = previousFontCreator;
        }
    }

    private static string Size(MenuItem owner)
    {
        ScrollViewer popup = owner.itemsPopup.ShouldNotBeNull(owner.Header?.ToString());
        return $"{popup.Visual.AbsoluteWidth:0.#}x{popup.Visual.AbsoluteHeight:0.#}";
    }

    private sealed class Lease : SharedGameLease
    {
        public Lease()
        {
            global::Gum.GumService.Default.Initialize(Game, global::Gum.Forms.DefaultVisualsVersion.V3);
            global::Gum.Renderables.ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
        }

        protected override void OnDispose()
        {
            if (global::Gum.GumService.Default.IsInitialized)
            {
                global::Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
