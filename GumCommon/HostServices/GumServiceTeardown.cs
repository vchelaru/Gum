using Gum.Forms.Controls;
using Gum.Managers;
using RenderingLibrary.Content;

// Companion to Gum.GumService / GumServiceSkiaBase (issue #5230): the backend-neutral part of
// Uninitialize both host families share. Backend-specific teardown (FormsUtilities, renderer,
// registrations, the per-backend SystemManagers.Default) stays with each service.
namespace Gum;

public static class GumServiceTeardown
{
    /// <summary>
    /// Removes and clears <see cref="FrameworkElement.PopupRoot"/> and
    /// <see cref="FrameworkElement.ModalRoot"/>, clears the Forms input registrations and default
    /// templates, unloads the Gum project, and disposes all cached content.
    /// </summary>
    public static void ReleaseFormsAndContent()
    {
        // The statics reset with null! below are declared non-null but start as null! until
        // Initialize sets them; teardown returns them to that pre-Initialize state.
        if (FrameworkElement.PopupRoot != null)
        {
            FrameworkElement.PopupRoot.Children.Clear();
            FrameworkElement.PopupRoot.RemoveFromManagers();
            FrameworkElement.PopupRoot = null!;
        }

        if (FrameworkElement.ModalRoot != null)
        {
            FrameworkElement.ModalRoot.Children.Clear();
            FrameworkElement.ModalRoot.RemoveFromManagers();
            FrameworkElement.ModalRoot = null!;
        }

        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.Clear();
        FrameworkElement.MainCursor = null!;
        FrameworkElement.MainKeyboard = null!;

        FrameworkElement.DefaultFormsTemplates.Clear();
#pragma warning disable CS0618 // obsolete types and members still load from older projects
        FrameworkElement.DefaultFormsComponents.Clear();
#pragma warning restore CS0618

        ObjectFinder.Self.GumProjectSave = null;

        LoaderManager.Self.DisposeAndClear();
    }
}
