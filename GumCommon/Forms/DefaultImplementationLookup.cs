// Compiled only into GumCommon (not shared to FRB through GumCoreShared.projitems), like
// ElementSaveFormsExtensions.cs.
#if !FRB
using System.Linq;
using Gum.DataTypes;

namespace Gum.Forms;

/// <summary>
/// Picks the component the framework builds when it creates a Forms control on its own in a from-file
/// project (a Tooltip, a MenuItem's submenu popup): the behavior's DefaultImplementation, the same rule
/// codegen uses, falling back to the project's first component that has the behavior.
/// </summary>
internal static class DefaultImplementationLookup
{
    internal static ComponentSave? Find(GumProjectSave? project, string behaviorName)
    {
        if (project == null)
        {
            return null;
        }
        string? defaultImplementation = project.Behaviors
            .FirstOrDefault(behavior => behavior.Name == behaviorName)?.DefaultImplementation;
        ComponentSave? firstWithBehavior = null;
        foreach (ComponentSave component in project.Components)
        {
            if (!component.Behaviors.Any(behavior => behavior.BehaviorName == behaviorName))
            {
                continue;
            }
            if (component.Name == defaultImplementation)
            {
                return component;
            }
            firstWithBehavior ??= component;
        }
        return firstWithBehavior;
    }
}
#endif
