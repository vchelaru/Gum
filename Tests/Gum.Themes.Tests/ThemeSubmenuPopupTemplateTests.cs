using System;
using System.Collections.Generic;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace Gum.Themes.Tests;

/// <summary>
/// A code-only theme supplies its submenu popup through MenuItem.DefaultSubmenuPopupTemplate rather than
/// a per-item ScrollViewerVisualTemplate on its MenuItem visual, so a per-item template stays a true
/// override and switching themes replaces the popup (#6741).
/// </summary>
public class ThemeSubmenuPopupTemplateTests : IDisposable
{
    private readonly Dictionary<Type, VisualTemplate> _previousTemplates = new(FrameworkElement.DefaultFormsTemplates);

    public void Dispose()
    {
        MenuItem.DefaultSubmenuPopupTemplate = null;
        FrameworkElement.DefaultFormsTemplates.Clear();
        foreach (KeyValuePair<Type, VisualTemplate> pair in _previousTemplates)
        {
            FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value;
        }
    }

    public static IEnumerable<object[]> Themes() => MenuFocusTests.Themes();

    [Theory]
    [MemberData(nameof(Themes))]
    public void RegisterVisuals_MenuItem_HasNoPerItemScrollViewerTemplate(string themeName, Action configureStyling, Action registerVisuals)
    {
        configureStyling();
        registerVisuals();

        new MenuItem().ScrollViewerVisualTemplate.ShouldBeNull(themeName);
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void RegisterVisuals_SetsDefaultSubmenuPopupTemplate(string themeName, Action configureStyling, Action registerVisuals)
    {
        configureStyling();
        registerVisuals();

        MenuItem.DefaultSubmenuPopupTemplate.ShouldNotBeNull(themeName)
            .CreateContent(null).ShouldBeAssignableTo<InteractiveGue>(themeName);
    }

    [Fact]
    public void RegisterVisuals_Retro95_DefaultSubmenuPopupTemplateIsRaisedMenuPopup()
    {
        Retro95.Retro95Theme.ConfigureStyling();
        Retro95.Retro95Theme.RegisterVisuals();

        MenuItem.DefaultSubmenuPopupTemplate.ShouldNotBeNull().CreateContent(null).ShouldBeOfType<Retro95.Retro95MenuPopupVisual>();
    }

    // Switching from Retro95 to another theme replaces Retro95's popup.
    [Theory]
    [MemberData(nameof(Themes))]
    public void RegisterVisuals_AfterRetro95_ReplacesRetro95Popup(string themeName, Action configureStyling, Action registerVisuals)
    {
        if (themeName == "Retro95")
        {
            return;
        }
        Retro95.Retro95Theme.ConfigureStyling();
        Retro95.Retro95Theme.RegisterVisuals();

        configureStyling();
        registerVisuals();

        MenuItem.DefaultSubmenuPopupTemplate.ShouldNotBeNull(themeName)
            .CreateContent(null).ShouldNotBeOfType<Retro95.Retro95MenuPopupVisual>(themeName);
    }
}
