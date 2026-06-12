using Gum.DataTypes;
using System.Collections.Generic;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Decides when and how code generation targets the collapsed two-slot shape runtimes
/// (<c>CircleRuntime</c> / <c>RectangleRuntime</c>) in place of the legacy
/// <c>ColoredCircle</c> / <c>ColoredRectangle</c> / <c>RoundedRectangle</c> standard elements,
/// and how their single-color variables translate to the fill/stroke channel properties.
/// Issue #2775; gated on runtime syntax version 2 (#2774).
/// </summary>
public interface ICollapsedShapeCodeGenLogic
{
    /// <summary>
    /// Returns whether generated code for the given standard element should target the collapsed
    /// runtime type. True only when the element is a legacy shape, the resolved syntax version is
    /// at or above the collapse version, the output library is MonoGame or MonoGameForms, and
    /// instantiation is fully-in-code.
    /// </summary>
    bool ShouldCollapse(string? standardElementName, int resolvedSyntaxVersion, CodeOutputProjectSettings? projectSettings);

    /// <summary>
    /// Returns the collapsed runtime class name for a legacy shape standard element name
    /// (for example <c>CircleRuntime</c> for <c>ColoredCircle</c>), or null when the element is
    /// not a legacy shape.
    /// </summary>
    string? TryGetCollapsedRuntimeName(string? standardElementName);

    /// <summary>
    /// Resolves the effective <c>IsFilled</c> for a legacy shape: the value explicitly set in the
    /// container's default state if present, else the shape standard element's default, else true.
    /// This drives whether the legacy single-color variables route to the fill or stroke channels.
    /// </summary>
    bool GetEffectiveIsFilled(ElementSave? container, InstanceSave? instance, ElementSave shapeElement);

    /// <summary>
    /// Translates a legacy shape variable root name to its collapsed-runtime property name
    /// (for example <c>Red</c> becomes <c>FillRed</c> or <c>StrokeRed</c>). Returns the name
    /// unchanged when no translation applies.
    /// </summary>
    string TranslateVariableRootName(string rootName, bool effectiveIsFilled);

    /// <summary>
    /// Returns whether a legacy shape variable has no target on the collapsed runtime and must be
    /// skipped entirely (gradient start color always; stroke variables when the shape is filled).
    /// </summary>
    bool ShouldDropVariable(string rootName, bool effectiveIsFilled);

    /// <summary>
    /// Returns the property assignments (for example <c>IsFilled = true;</c>) that re-establish
    /// the legacy shape's default visual on a freshly-constructed collapsed runtime.
    /// </summary>
    IEnumerable<string> GetBaselineAssignments(string standardElementName, bool effectiveIsFilled);
}
