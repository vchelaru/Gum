using System.Collections.Generic;
using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.ProjectServices;

/// <summary>
/// Detects and propagates <c>VariableReferences</c> rows whose left-hand-side
/// scalars are not materialized into the owning state's <see cref="StateSave.Variables"/>.
/// This catches files authored by something other than the Gum tool's normal
/// reference-edit path (AI agent, hand edit, programmatic creation) where the
/// references row exists but the scalars are missing.
/// </summary>
public interface IReferencePropagationService
{
    /// <summary>
    /// Walks every element in the project and returns the states with unpropagated references.
    /// </summary>
    DetectUnpropagatedReferencesResult Detect(GumProjectSave project);

    /// <summary>
    /// Walks every element in the project, runs the same propagation the tool does
    /// when a reference is authored interactively, and returns the elements that
    /// were modified. The project is mutated in place; the caller is responsible
    /// for persisting changed elements (e.g. via <c>ElementSave.Save</c>).
    /// </summary>
    IReadOnlyList<ElementSave> PropagateReferences(GumProjectSave project);
}

public class DetectUnpropagatedReferencesResult
{
    public List<ElementWithUnpropagatedReferences> Elements { get; } = new();

    public bool HasUnpropagatedReferences => Elements.Count > 0;
}

public class ElementWithUnpropagatedReferences
{
    public ElementSave Element { get; set; } = null!;
    public List<StateSave> States { get; set; } = new();
}
