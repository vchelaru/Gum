using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.ObjectModel;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #6066 follow-up: every site that sets the thread-static GraphicalUiElement.IsAllLayoutSuspended
// around a body must restore the PREVIOUS value when the body throws, and let the exception through.
// Each test makes the guarded body throw, then checks the flag equals what it was before the call
// (both false and true) and that the exception propagates.
//
// Not covered here: SkiaGumCanvasView.ForceGumLayout (Runtimes/SkiaGum.Maui, net9.0 MAUI) and
// GumSKElement.ForceGumLayout (Runtimes/SkiaGum.Wpf, net8.0-windows). Neither is referenced by any
// test project, so those two try/finally blocks are verified by reading only.
public class LayoutSuspensionExceptionSafetyTests : BaseTestClass
{
    private const string ThrowMessage = "Injected failure inside the suspended body.";

    private class ThrowingRuntime : GraphicalUiElement
    {
        public ThrowingRuntime() : base(new InvisibleRenderable(), null)
        {
            throw new InvalidOperationException(ThrowMessage);
        }
    }

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        base.Dispose();
    }

    private static void ShouldThrowInjected(Action action)
    {
        Should.Throw<InvalidOperationException>(action).Message.ShouldBe(ThrowMessage);
    }

    // The previous == false case is ToGraphicalUiElementLayoutSuspensionTests.
    [Fact]
    public void ToGraphicalUiElement_ShouldLeaveFlagSet_WhenBuildThrowsInsideAnOuterSuspension()
    {
        ComponentSave thrower = new() { Name = "Thrower" };
        thrower.States.Add(new StateSave { Name = "Default", ParentContainer = thrower });
        ElementSaveExtensions.RegisterGueInstantiation("Thrower", () => new ThrowingRuntime());

        GumProjectSave gumProject = new();
        StandardElementSave container = new() { Name = "Container" };
        container.States.Add(new StateSave { Name = "Default", ParentContainer = container });
        gumProject.StandardElements.Add(container);
        gumProject.Components.Add(thrower);
        ScreenSave screen = new() { Name = "BrokenScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Bad", BaseType = "Thrower", ParentContainer = screen });
        gumProject.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = gumProject;
        GraphicalUiElement.IsAllLayoutSuspended = true;

        ShouldThrowInjected(() => screen.ToGraphicalUiElement());

        GraphicalUiElement.IsAllLayoutSuspended.ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefreshStyles_ShouldRestorePreviousFlag_WhenBodyThrows(bool previous)
    {
        Action<object>? originalSave = GraphicalUiElement.SaveFormsRuntimePropertiesAction;
        Button button = new();
        GraphicalUiElement.SaveFormsRuntimePropertiesAction = _ => throw new InvalidOperationException(ThrowMessage);
        GraphicalUiElement.IsAllLayoutSuspended = previous;
        try
        {
            ShouldThrowInjected(() => button.Visual.RefreshStyles());

            GraphicalUiElement.IsAllLayoutSuspended.ShouldBe(previous);
        }
        finally
        {
            GraphicalUiElement.SaveFormsRuntimePropertiesAction = originalSave;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ComboBoxOpen_ShouldRestorePreviousFlag_WhenShowingListBoxThrows(bool previous)
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.ListBox.Visual.VisibleChanged += (_, _) => throw new InvalidOperationException(ThrowMessage);
        GraphicalUiElement.IsAllLayoutSuspended = previous;

        ShouldThrowInjected(() => comboBox.IsDropDownOpen = true);

        GraphicalUiElement.IsAllLayoutSuspended.ShouldBe(previous);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemoveFromRoot_ShouldRestorePreviousFlag_WhenBodyThrows(bool previous)
    {
        Button button = new();
        button.AddToRoot();
        button.Visual.ParentChanged += (_, _) => throw new InvalidOperationException(ThrowMessage);
        GraphicalUiElement.IsAllLayoutSuspended = previous;

        ShouldThrowInjected(() => button.RemoveFromRoot());

        GraphicalUiElement.IsAllLayoutSuspended.ShouldBe(previous);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ItemsSetter_ShouldRestorePreviousFlag_WhenCreatingItemVisualThrows(bool previous)
    {
        ListBox listBox = new();
        listBox.VisualTemplate = new VisualTemplate(() => throw new InvalidOperationException(ThrowMessage));
        ObservableCollection<string> items = new() { "A" };
        GraphicalUiElement.IsAllLayoutSuspended = previous;

        ShouldThrowInjected(() => listBox.Items = items);

        GraphicalUiElement.IsAllLayoutSuspended.ShouldBe(previous);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FrameworkElementTemplateSetter_ShouldRestorePreviousFlag_WhenCreatingItemThrows(bool previous)
    {
        ItemsControl itemsControl = new();
        itemsControl.Items = new ObservableCollection<string> { "A" };
        GraphicalUiElement.IsAllLayoutSuspended = previous;

        ShouldThrowInjected(() =>
            itemsControl.FrameworkElementTemplate = new FrameworkElementTemplate(
                () => throw new InvalidOperationException(ThrowMessage)));

        GraphicalUiElement.IsAllLayoutSuspended.ShouldBe(previous);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ItemsCollectionAdd_ShouldRestorePreviousFlag_WhenCreatingItemVisualThrows(bool previous)
    {
        ListBox listBox = new();
        ObservableCollection<string> items = new();
        listBox.Items = items;
        listBox.VisualTemplate = new VisualTemplate(() => throw new InvalidOperationException(ThrowMessage));
        GraphicalUiElement.IsAllLayoutSuspended = previous;

        ShouldThrowInjected(() => items.Add("A"));

        GraphicalUiElement.IsAllLayoutSuspended.ShouldBe(previous);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MenuItemShowPopup_ShouldRestorePreviousFlag_WhenSizingPopupThrows(bool previous)
    {
        // The guarded block only runs when no ScrollViewerVisualTemplate produced a popup visual.
        // Hiding the scroll bars re-applies the "NoScrollBar" state, which is where the throw
        // is injected (armed once the popup has been parented, so construction is unaffected).
        VisualTemplate originalTemplate = FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)];
        bool injectionEnabled = true;
        FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)] = new VisualTemplate(() =>
        {
            GraphicalUiElement popupVisual = originalTemplate.CreateContent(null);
            bool armed = false;
            popupVisual.ParentChanged += (_, _) => armed = true;
            StateSave noScrollBar = popupVisual.Categories[ScrollViewer.ScrollBarVisibilityCategoryName]
                .States.Find(item => item.Name == "NoScrollBar")!;
            Action? originalApply = noScrollBar.Apply;
            noScrollBar.Apply = () =>
            {
                if (armed && injectionEnabled)
                {
                    throw new InvalidOperationException(ThrowMessage);
                }
                originalApply?.Invoke();
            };
            return popupVisual;
        });
        try
        {
            Menu menu = new();
            menu.AddToRoot();
            MenuItem fileItem = new();
            fileItem.ScrollViewerVisualTemplate = null;
            menu.Items!.Add(fileItem);
            fileItem.Items!.Add("SubItem");
            GraphicalUiElement.IsAllLayoutSuspended = previous;

            ShouldThrowInjected(() => fileItem.IsSelected = true);

            GraphicalUiElement.IsAllLayoutSuspended.ShouldBe(previous);
        }
        finally
        {
            // The popup stays in the root, and Dispose re-lays it out.
            injectionEnabled = false;
            FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)] = originalTemplate;
        }
    }
}
