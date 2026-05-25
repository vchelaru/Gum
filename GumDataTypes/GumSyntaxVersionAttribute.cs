using System;

namespace Gum.DataTypes;

/// <summary>
/// Declares the syntax version supported by a Gum runtime assembly. The Gum tool's
/// code generator reads this attribute to determine which namespaces and conventions
/// to use when emitting C# code for a target project.
/// </summary>
/// <remarks>
/// <para>
/// When the attribute is absent from a referenced assembly, the code generator assumes
/// pre-unification conventions. Each increment of <see cref="Version"/> corresponds to
/// a set of breaking changes documented in docs/gum-tool/upgrading/syntax-versions.md.
/// </para>
/// <para>
/// This attribute should be applied at the assembly level in each Gum runtime project
/// (GumCommon, MonoGameGum, RaylibGum, SkiaGum).
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class GumSyntaxVersionAttribute : Attribute
{
    /// <summary>
    /// The syntax version number. Higher values indicate newer conventions.
    /// Version 0 is the baseline (no breaking changes from the pre-attribute era).
    /// Version 1 introduced the unified <c>Gum.GueDeriving</c> namespace for runtime classes
    /// (non-breaking; old namespaces remain via <c>[Obsolete]</c> shims).
    /// Version 2 introduced the fill/stroke two-slot model on shape runtimes: the role
    /// interface <c>ICircleRenderable</c> was removed and replaced by
    /// <c>IFilledCircleRenderable</c> / <c>IStrokedCircleRenderable</c> (and matching
    /// rectangle role interfaces), with shape runtimes now holding two renderables drawn
    /// on the same frame. See <c>.claude/designs/runtime-unification/FillStrokeTwoSlotModel.md</c>
    /// and PR #2769 / issue #2768.
    /// </summary>
    public int Version;
}
