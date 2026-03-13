using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using MonoGameGum.Input;
using MonoGameGum.GueDeriving;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;

namespace MonoGameGum.Tests.V2;

/// <summary>
/// Tests covering subsystems touched by <c>GumService.Uninitialize()</c> that
/// are accessible from MonoGameGum.Tests.V2.
/// Tests that mutate shared static state capture and restore it in a try/finally
/// so subsequent tests in the suite are not affected.
/// </summary>
public class GumServiceUninitializeTests
{
    // -------------------------------------------------------------------------
    // Root / PopupRoot / ModalRoot children clearing
    // -------------------------------------------------------------------------

    [Fact]
    public void ModalRoot_AfterChildrenClear_HasNoChildren()
    {
        ContainerRuntime child = new ContainerRuntime();
        GumService.Default.ModalRoot.Children.Add(child);
        GumService.Default.ModalRoot.Children.ShouldContain(child);

        try
        {
            GumService.Default.ModalRoot.Children.Clear();

            GumService.Default.ModalRoot.Children.Count.ShouldBe(0);
        }
        finally
        {
            GumService.Default.ModalRoot.Children.Clear();
        }
    }

    [Fact]
    public void PopupRoot_AfterChildrenClear_HasNoChildren()
    {
        ContainerRuntime child = new ContainerRuntime();
        GumService.Default.PopupRoot.Children.Add(child);
        GumService.Default.PopupRoot.Children.ShouldContain(child);

        try
        {
            GumService.Default.PopupRoot.Children.Clear();

            GumService.Default.PopupRoot.Children.Count.ShouldBe(0);
        }
        finally
        {
            GumService.Default.PopupRoot.Children.Clear();
        }
    }

    [Fact]
    public void Root_AfterChildrenClear_HasNoChildren()
    {
        ContainerRuntime child = new ContainerRuntime();
        GumService.Default.Root.Children.Add(child);
        GumService.Default.Root.Children.ShouldContain(child);

        try
        {
            GumService.Default.Root.Children.Clear();

            GumService.Default.Root.Children.Count.ShouldBe(0);
        }
        finally
        {
            GumService.Default.Root.Children.Clear();
        }
    }

    // -------------------------------------------------------------------------
    // FrameworkElement.DefaultFormsComponents
    // -------------------------------------------------------------------------

    [Fact]
    public void DefaultFormsComponents_AfterClear_DoesNotContainPreviousEntry()
    {
#pragma warning disable CS0618 // DefaultFormsComponents is Obsolete — tested intentionally
        Dictionary<Type, Type> original =
            new Dictionary<Type, Type>(FrameworkElement.DefaultFormsComponents);

        try
        {
            Type sentinelType = typeof(GumServiceUninitializeTests);
            FrameworkElement.DefaultFormsComponents[sentinelType] = sentinelType;

            FrameworkElement.DefaultFormsComponents.Clear();

            FrameworkElement.DefaultFormsComponents.ContainsKey(sentinelType).ShouldBeFalse();
        }
        finally
        {
            FrameworkElement.DefaultFormsComponents.Clear();
            foreach (KeyValuePair<Type, Type> entry in original)
            {
                FrameworkElement.DefaultFormsComponents[entry.Key] = entry.Value;
            }
        }
#pragma warning restore CS0618
    }

    [Fact]
    public void DefaultFormsComponents_Clear_RemovesAllEntries()
    {
#pragma warning disable CS0618
        Dictionary<Type, Type> original =
            new Dictionary<Type, Type>(FrameworkElement.DefaultFormsComponents);

        try
        {
            FrameworkElement.DefaultFormsComponents[typeof(GumServiceUninitializeTests)] =
                typeof(GumServiceUninitializeTests);

            FrameworkElement.DefaultFormsComponents.Clear();

            FrameworkElement.DefaultFormsComponents.Count.ShouldBe(0);
        }
        finally
        {
            FrameworkElement.DefaultFormsComponents.Clear();
            foreach (KeyValuePair<Type, Type> entry in original)
            {
                FrameworkElement.DefaultFormsComponents[entry.Key] = entry.Value;
            }
        }
#pragma warning restore CS0618
    }

    // -------------------------------------------------------------------------
    // FrameworkElement.DefaultFormsTemplates
    // -------------------------------------------------------------------------

    [Fact]
    public void DefaultFormsTemplates_AfterClear_DoesNotContainPreviousEntry()
    {
        Dictionary<Type, VisualTemplate> original =
            new Dictionary<Type, VisualTemplate>(FrameworkElement.DefaultFormsTemplates);

        try
        {
            Type sentinelType = typeof(GumServiceUninitializeTests);
            FrameworkElement.DefaultFormsTemplates[sentinelType] =
                new VisualTemplate(() => new GraphicalUiElement());

            FrameworkElement.DefaultFormsTemplates.Clear();

            FrameworkElement.DefaultFormsTemplates.ContainsKey(sentinelType).ShouldBeFalse();
        }
        finally
        {
            FrameworkElement.DefaultFormsTemplates.Clear();
            foreach (KeyValuePair<Type, VisualTemplate> entry in original)
            {
                FrameworkElement.DefaultFormsTemplates[entry.Key] = entry.Value;
            }
        }
    }

    [Fact]
    public void DefaultFormsTemplates_Clear_RemovesAllEntries()
    {
        Dictionary<Type, VisualTemplate> original =
            new Dictionary<Type, VisualTemplate>(FrameworkElement.DefaultFormsTemplates);

        try
        {
            FrameworkElement.DefaultFormsTemplates[typeof(GumServiceUninitializeTests)] =
                new VisualTemplate(() => new GraphicalUiElement());

            FrameworkElement.DefaultFormsTemplates.Clear();

            FrameworkElement.DefaultFormsTemplates.Count.ShouldBe(0);
        }
        finally
        {
            FrameworkElement.DefaultFormsTemplates.Clear();
            foreach (KeyValuePair<Type, VisualTemplate> entry in original)
            {
                FrameworkElement.DefaultFormsTemplates[entry.Key] = entry.Value;
            }
        }
    }

    // -------------------------------------------------------------------------
    // FormsUtilities.Uninitialize — internal, visible via InternalsVisibleTo
    // -------------------------------------------------------------------------

    [Fact]
    public void FormsUtilities_Uninitialize_SetsCursorToNull()
    {
        // Save state so we can restore it in teardown.
        Cursor? savedCursor = FormsUtilities.Cursor;
        InteractiveGue? savedPopupRoot = FrameworkElement.PopupRoot;
        InteractiveGue? savedModalRoot = FrameworkElement.ModalRoot;

        try
        {
            // Precondition: the test framework sets up a cursor in TestAssemblyInitialize.
            FormsUtilities.Cursor.ShouldNotBeNull();

            FormsUtilities.Uninitialize();

            FormsUtilities.Cursor.ShouldBeNull();
        }
        finally
        {
            RestoreFormsUtilitiesState(savedCursor, savedPopupRoot, savedModalRoot);
        }
    }

    [Fact]
    public void FormsUtilities_Uninitialize_SetsKeyboardToNull()
    {
        Cursor? savedCursor = FormsUtilities.Cursor;
        InteractiveGue? savedPopupRoot = FrameworkElement.PopupRoot;
        InteractiveGue? savedModalRoot = FrameworkElement.ModalRoot;

        try
        {
            // Precondition: the test framework sets up a keyboard in TestAssemblyInitialize.
            FormsUtilities.Keyboard.ShouldNotBeNull();

            FormsUtilities.Uninitialize();

            FormsUtilities.Keyboard.ShouldBeNull();
        }
        finally
        {
            RestoreFormsUtilitiesState(savedCursor, savedPopupRoot, savedModalRoot);
        }
    }

    [Fact]
    public void FormsUtilities_Uninitialize_ResetsGamepadsToNewArray()
    {
        Cursor? savedCursor = FormsUtilities.Cursor;
        InteractiveGue? savedPopupRoot = FrameworkElement.PopupRoot;
        InteractiveGue? savedModalRoot = FrameworkElement.ModalRoot;

        try
        {
            GamePad[] originalGamepads = FormsUtilities.Gamepads;

            FormsUtilities.Uninitialize();

            // After Uninitialize, Gamepads is a different array instance of the same length.
            FormsUtilities.Gamepads.ShouldNotBeSameAs(originalGamepads);
            FormsUtilities.Gamepads.Length.ShouldBe(4);
        }
        finally
        {
            RestoreFormsUtilitiesState(savedCursor, savedPopupRoot, savedModalRoot);
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Restores FormsUtilities and FrameworkElement state after a test that calls
    /// <c>FormsUtilities.Uninitialize()</c> so subsequent tests continue working.
    /// </summary>
    /// <remarks>
    /// Not testable: the private <c>keyboard</c> field and the <c>Gamepads</c>
    /// property (private setter) cannot be restored individually.  Calling
    /// <c>InitializeDefaults</c> is the only public way to reinstate them.
    /// </remarks>
    private static void RestoreFormsUtilitiesState(
        Cursor? cursor,
        InteractiveGue? popupRoot,
        InteractiveGue? modalRoot)
    {
        // Calling InitializeDefaults re-creates cursor, keyboard, gamepads,
        // PopupRoot, and ModalRoot. We then put the suite-wide roots back.
        FormsUtilities.InitializeDefaults(
            systemManagers: SystemManagers.Default,
            defaultVisualsVersion: DefaultVisualsVersion.V2);

        // Restore PopupRoot / ModalRoot to the values that existed before the test
        // so that containers newly allocated by InitializeDefaults are discarded.
        FrameworkElement.PopupRoot = popupRoot;
        FrameworkElement.ModalRoot = modalRoot;

        // Restore the cursor to the one that was active before the test
        // (InitializeDefaults overwrites it with a new Cursor()).
        if (cursor != null)
        {
            FormsUtilities.SetCursor(cursor);
        }
    }
}
