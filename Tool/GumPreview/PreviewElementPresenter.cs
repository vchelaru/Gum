using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Wireframe;

namespace GumPreview;

/// <summary>
/// Puts the element the tool selected on Preview's root, built from the project in
/// <see cref="ObjectFinder"/>. Used for the first show, a new selection, and the rebuild after a hot
/// reload that could not match a restart, so all three give the same tree.
/// </summary>
internal static class PreviewElementPresenter
{
    /// <summary>
    /// Replaces the root's children with the selected element in its selected state. False, with the
    /// root left empty, when the project has no such screen or component.
    /// </summary>
    public static bool Show(PreviewSelectionMessage selection)
    {
        ElementSave? element = ObjectFinder.Self.GetElementSave(selection.ElementName);
        if (element == null)
        {
            GumService.Default.Root.Children.Clear();
            return false;
        }

        // Built before the root is cleared, so an element that throws while building leaves the last
        // good frame showing.
        GraphicalUiElement built = element.ToGraphicalUiElement();
        GumService.Default.Root.Children.Clear();
        built.AddToRoot();
        ApplySelectedState(selection);
        return true;
    }

    // Puts the shown element in the state selected in the tool, on top of its default state - the
    // same thing the tool's own canvas shows for a selected categorized state (issue #4856).
    private static void ApplySelectedState(PreviewSelectionMessage selection)
    {
        if (selection.StateName == null)
        {
            return;
        }

        foreach (GraphicalUiElement root in GumService.Default.Root.Children)
        {
            if (root.ElementSave != null && root.ElementSave.Name == selection.ElementName)
            {
                StateSave? state = selection.FindState(root.ElementSave);
                if (state != null)
                {
                    root.ApplyState(state);
                }
            }
        }
    }
}
