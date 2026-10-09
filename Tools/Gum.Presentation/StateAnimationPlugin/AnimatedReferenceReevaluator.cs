using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumRuntime;
using Gum.Wireframe;
using System;
using System.Collections.Generic;

namespace StateAnimationPlugin;

/// <summary>
/// Re-evaluates the variable-reference rows an animation preview tick affects, so a variable that
/// references an animated variable follows it in the wireframe (issue #5662).
/// </summary>
/// <remarks>
/// Rows are evaluated against the tick's throwaway state, which is where the animated values live,
/// so the evaluator sees them without any extra plumbing. Only rows that read an animated variable
/// (directly or through another affected row) are evaluated: each evaluation parses an expression
/// with Roslyn, so evaluating every row on every tick would make cost scale with the screen's
/// total reference count instead of with what the animation touches.
/// </remarks>
internal static class AnimatedReferenceReevaluator
{
    private const string ReferencesName = "VariableReferences";

    /// <summary>
    /// The names of every variable the given states set, such as <c>Width</c> or <c>Instance.Width</c>.
    /// </summary>
    public static HashSet<string> GetVariableNames(IEnumerable<StateSave> states)
    {
        HashSet<string> names = new HashSet<string>();
        foreach (StateSave state in states)
        {
            foreach (VariableSave variable in state.Variables)
            {
                names.Add(variable.Name);
            }
        }
        return names;
    }

    /// <summary>
    /// Replaces the reference rows on <paramref name="state"/> with only those affected by
    /// <paramref name="animatedNames"/>, then applies them to <paramref name="state"/>. The state is
    /// modified, so it must be a throwaway such as the one a preview tick produces.
    /// </summary>
    /// <param name="animatesUnknownVariables">True when the animation can set variables that are not in
    /// <paramref name="animatedNames"/> (sub-animations), so every row is treated as affected.</param>
    public static void Apply(ElementSave element, StateSave state, HashSet<string> animatedNames,
        bool animatesUnknownVariables, GraphicalUiElement? liveRoot)
    {
        VariableReferenceGraph graph = VariableReferenceGraph.GetForState(element, state);

        //////////////////////// EARLY OUT
        if (!graph.HasRows)
        {
            return;
        }
        ////////////////////// END EARLY OUT

        List<ReferenceRow> affected = new List<ReferenceRow>();
        if (animatesUnknownVariables)
        {
            graph.FindAllRows(affected);
        }
        else
        {
            graph.FindAffectedRows(animatedNames, affected);
        }

        state.VariableLists.RemoveAll(list => list.GetRootName() == ReferencesName);
        if (affected.Count == 0)
        {
            return;
        }

        // One list per row keeps the dependency order across instances.
        foreach (ReferenceRow row in affected)
        {
            string listName = string.IsNullOrEmpty(row.SourceObject)
                ? ReferencesName
                : row.SourceObject + "." + ReferencesName;
            VariableListSave<string> list = new VariableListSave<string> { Name = listName, Type = "string" };
            list.Value.Add(row.Line);
            state.VariableLists.Add(list);
        }
        element.ApplyVariableReferences(state, liveRoot, isFullCommit: false, notifyChanges: false);
    }
}
