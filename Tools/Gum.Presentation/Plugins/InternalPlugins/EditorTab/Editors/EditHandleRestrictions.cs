using Gum.DataTypes;
using Gum.Managers;

namespace Gum.Wireframe.Editors;

/// <summary>
/// Turns off the canvas editing handles for variables the loaded project's rules hide (#7720).
/// </summary>
public class EditHandleRestrictions
{
    private readonly IProjectRestrictions _projectRestrictions;

    public EditHandleRestrictions(IProjectRestrictions projectRestrictions)
    {
        _projectRestrictions = projectRestrictions;
    }

    /// <summary>
    /// Disables the move, resize and rotate flags on <paramref name="context"/> for each of X, Y, Width,
    /// Height and Rotation the rules hide for the selected object. Never re-enables a flag.
    /// </summary>
    /// <param name="selectedTag">The selected instance or element.</param>
    public void Apply(EditorContext context, object? selectedTag)
    {
        if (selectedTag is not InstanceSave && selectedTag is not ElementSave)
        {
            return;
        }

        if (IsHidden(selectedTag, "X"))
        {
            context.IsXMovementEnabled = false;
        }
        if (IsHidden(selectedTag, "Y"))
        {
            context.IsYMovementEnabled = false;
        }
        if (IsHidden(selectedTag, "Width"))
        {
            context.IsWidthChangeEnabled = false;
        }
        if (IsHidden(selectedTag, "Height"))
        {
            context.IsHeightChangeEnabled = false;
        }
        if (IsHidden(selectedTag, "Rotation"))
        {
            context.IsRotationEnabled = false;
        }
    }

    /// <summary>
    /// False when the rules hide both XOrigin and YOrigin for the selected object, so the origin marker has nothing to show.
    /// </summary>
    public bool GetShouldShowOrigin(object? selectedTag) =>
        !(IsHidden(selectedTag, "XOrigin") && IsHidden(selectedTag, "YOrigin"));

    /// <summary>
    /// False when the rules hide X for the selected object. The red offset line and its dotted connector follow X only,
    /// independent of the origin variables and of whether X is locked.
    /// </summary>
    public bool GetShouldShowXOffsetLine(object? selectedTag) => !IsHidden(selectedTag, "X");

    /// <summary>
    /// False when the rules hide Y for the selected object. The green offset line and its dotted connector follow Y only.
    /// </summary>
    public bool GetShouldShowYOffsetLine(object? selectedTag) => !IsHidden(selectedTag, "Y");

    private bool IsHidden(object? selectedTag, string variableRootName) => selectedTag switch
    {
        InstanceSave instance => _projectRestrictions.IsVariableHidden(instance, variableRootName),
        ElementSave element => _projectRestrictions.IsVariableHidden(element, variableRootName),
        _ => false,
    };
}
