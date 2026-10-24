using System;
using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The visual a MenuItem's submenu popup is built from, by precedence: the item's own
/// ScrollViewerVisualTemplate, then MenuItem.DefaultSubmenuPopupTemplate, then the registered
/// ScrollViewer template, then the code default (#6741). Generated code sets DefaultSubmenuPopupTemplate
/// to the theme's MenuItemSubmenuPopup component.
/// </summary>
public class MenuItemDefaultSubmenuPopupTemplateTests : BaseTestClass
{
    private readonly Dictionary<Type, VisualTemplate> _previousTemplates = new(FrameworkElement.DefaultFormsTemplates);

    public override void Dispose()
    {
        MenuItem.DefaultSubmenuPopupTemplate = null;
        FrameworkElement.DefaultFormsTemplates.Clear();
        foreach (KeyValuePair<Type, VisualTemplate> pair in _previousTemplates)
        {
            FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value;
        }
        base.Dispose();
    }

    // A bare visual with the ScrollViewer structure and no Forms control, named so a test can tell it apart.
    private static VisualTemplate NamedScrollViewerTemplate(string name) =>
        new VisualTemplate((_, createForms) => new ScrollViewerVisual(tryCreateFormsObject: createForms) { Name = name });

    // File (New, Open), with File's submenu open. The V3 MenuItem visual gives each item its own
    // ScrollViewerVisualTemplate; clearing it makes File behave like an item from a project component.
    private static MenuItem OpenFileMenu(bool keepV3ItemTemplate = false)
    {
        ContainerRuntime container = new ContainerRuntime();
        container.Width = 400;
        container.WidthUnits = DimensionUnitType.Absolute;
        container.Height = 40;
        container.HeightUnits = DimensionUnitType.Absolute;
        Gum.GumService.Default.Root.Children!.Add(container);

        Menu menu = new Menu();
        container.Children!.Add(menu.Visual);
        MenuItem file = new MenuItem { Header = "File" };
        menu.Items!.Add(file);
        file.Items!.Add(new MenuItem { Header = "New" });
        file.Items.Add(new MenuItem { Header = "Open" });
        if (!keepV3ItemTemplate)
        {
            file.ScrollViewerVisualTemplate = null;
        }
        menu.Visual.UpdateLayout();

        file.TryShowPopup();
        file.itemsPopup.ShouldNotBeNull("File's submenu opened");
        return file;
    }

    // Teardown clears it with DefaultFormsTemplates, so it does not survive into the next Initialize.
    [Fact]
    public void ReleaseFormsAndProject_DefaultSubmenuPopupTemplateSet_ClearsIt()
    {
        MenuItem.DefaultSubmenuPopupTemplate = NamedScrollViewerTemplate("SubmenuPopup");
        // Teardown skips null roots; detach them so the rest of the test class keeps its environment.
        var popupRoot = FrameworkElement.PopupRoot;
        var modalRoot = FrameworkElement.ModalRoot;
        var cursor = FrameworkElement.MainCursor;
        var keyboard = FrameworkElement.MainKeyboard;
        var keyboards = System.Linq.Enumerable.ToList(FrameworkElement.KeyboardsForUiControl);
        var gamePads = System.Linq.Enumerable.ToList(FrameworkElement.GamePadsForUiControl);
        var project = Gum.Managers.ObjectFinder.Self.GumProjectSave;
        FrameworkElement.PopupRoot = null!;
        FrameworkElement.ModalRoot = null!;
        try
        {
            Gum.GumServiceTeardown.ReleaseFormsAndProject(isLastService: false);

            MenuItem.DefaultSubmenuPopupTemplate.ShouldBeNull();
        }
        finally
        {
            FrameworkElement.PopupRoot = popupRoot;
            FrameworkElement.ModalRoot = modalRoot;
            FrameworkElement.MainCursor = cursor;
            FrameworkElement.MainKeyboard = keyboard;
            FrameworkElement.KeyboardsForUiControl.AddRange(keyboards);
            FrameworkElement.GamePadsForUiControl.AddRange(gamePads);
            Gum.Managers.ObjectFinder.Self.GumProjectSave = project;
        }
    }

    [Fact]
    public void DefaultSubmenuPopupTemplate_NotSet_IsNull()
    {
        MenuItem.DefaultSubmenuPopupTemplate.ShouldBeNull();
    }

    // A code-only project that sets nothing keeps the V3 MenuItem visual's own popup.
    [Fact]
    public void OpenSubmenu_CodeOnlyNothingSet_UsesV3ItemTemplate()
    {
        MenuItem file = OpenFileMenu(keepV3ItemTemplate: true);

        file.itemsPopup!.Visual.ShouldBeOfType<ScrollViewerVisualSizedToChildren>();
    }

    // The V3 MenuItem visual's template is a per-item ScrollViewerVisualTemplate, so it wins.
    [Fact]
    public void OpenSubmenu_CodeOnlyDefaultSubmenuPopupTemplateSet_V3ItemTemplateWins()
    {
        MenuItem.DefaultSubmenuPopupTemplate = NamedScrollViewerTemplate("SubmenuPopup");

        MenuItem file = OpenFileMenu(keepV3ItemTemplate: true);

        file.itemsPopup!.Visual.ShouldBeOfType<ScrollViewerVisualSizedToChildren>();
    }

    // An item with no template of its own and nothing registered gets the code default ScrollViewer.
    [Fact]
    public void OpenSubmenu_NoItemTemplateNothingSet_UsesCodeDefaultScrollViewerVisual()
    {
        Type expectedVisualType = new ScrollViewer().Visual.GetType();

        MenuItem file = OpenFileMenu();

        file.itemsPopup!.Visual.GetType().ShouldBe(expectedVisualType);
    }

    [Fact]
    public void OpenSubmenu_RegisteredScrollViewerTemplate_UsesIt()
    {
        FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)] = NamedScrollViewerTemplate("Registered");

        MenuItem file = OpenFileMenu();

        file.itemsPopup!.Visual.Name.ShouldBe("Registered");
    }

    [Fact]
    public void OpenSubmenu_DefaultSubmenuPopupTemplateSet_UsesIt()
    {
        MenuItem.DefaultSubmenuPopupTemplate = NamedScrollViewerTemplate("SubmenuPopup");

        MenuItem file = OpenFileMenu();

        file.itemsPopup!.Visual.Name.ShouldBe("SubmenuPopup");
    }

    [Fact]
    public void OpenSubmenu_DefaultSubmenuPopupTemplateAndRegisteredScrollViewerTemplate_DefaultSubmenuPopupTemplateWins()
    {
        FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)] = NamedScrollViewerTemplate("Registered");
        MenuItem.DefaultSubmenuPopupTemplate = NamedScrollViewerTemplate("SubmenuPopup");

        MenuItem file = OpenFileMenu();

        file.itemsPopup!.Visual.Name.ShouldBe("SubmenuPopup");
    }

    [Fact]
    public void OpenSubmenu_ItemScrollViewerVisualTemplateAndDefaultSubmenuPopupTemplate_ItemTemplateWins()
    {
        MenuItem.DefaultSubmenuPopupTemplate = NamedScrollViewerTemplate("SubmenuPopup");
        ContainerRuntime container = new ContainerRuntime();
        Gum.GumService.Default.Root.Children!.Add(container);
        Menu menu = new Menu();
        container.Children!.Add(menu.Visual);
        MenuItem file = new MenuItem { Header = "File", ScrollViewerVisualTemplate = NamedScrollViewerTemplate("PerItem") };
        menu.Items!.Add(file);
        file.Items!.Add(new MenuItem { Header = "New" });

        file.TryShowPopup();

        file.itemsPopup.ShouldNotBeNull().Visual.Name.ShouldBe("PerItem");
    }

    // Generated GumForms code builds the visual with its derived ScrollViewer as the Forms control; the
    // popup is that control.
    [Fact]
    public void OpenSubmenu_DefaultSubmenuPopupTemplateCreatesScrollViewer_PopupIsThatControl()
    {
        ScrollViewer? created = null;
        MenuItem.DefaultSubmenuPopupTemplate = new VisualTemplate((_, createForms) =>
        {
            ScrollViewerVisual visual = new ScrollViewerVisual(tryCreateFormsObject: false);
            if (createForms)
            {
                created = new ScrollViewer(visual);
            }
            return visual;
        });

        MenuItem file = OpenFileMenu();

        created.ShouldNotBeNull();
        file.itemsPopup.ShouldBeSameAs(created);
    }

    // Like the project's submenu-popup component, the popup sizes itself to its rows.
    [Fact]
    public void OpenSubmenu_DefaultSubmenuPopupTemplate_GrowsByOneRowWhenARowIsAdded()
    {
        MenuItem.DefaultSubmenuPopupTemplate = NamedScrollViewerTemplate("SubmenuPopup");
        MenuItem file = OpenFileMenu();
        ScrollViewer popup = file.itemsPopup!;
        float heightBefore = popup.Visual.AbsoluteHeight;

        MenuItem added = new MenuItem { Header = "Close" };
        file.Items!.Add(added);

        (popup.Visual.AbsoluteHeight - heightBefore).ShouldBe(added.Visual.AbsoluteHeight, 1f);
    }
}
