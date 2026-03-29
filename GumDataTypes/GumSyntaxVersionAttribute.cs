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
/// (MonoGameGum, RaylibGum, SkiaGum).
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class GumSyntaxVersionAttribute : Attribute
{
    /// <summary>
    /// The syntax version number. Higher values indicate newer conventions.
    /// Version 0 is the baseline (no breaking changes from the pre-attribute era).
    /// </summary>
    public int Version;
}
