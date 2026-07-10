using Gum.DataTypes;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SilkNetGum.Tests")]

// Gum runtime syntax version — an assembly-level integer the Gum tool's code generator reads to
// decide which runtime conventions/namespaces to emit code against. Kept in lock step with the
// other runtime assemblies (GumCommon, MonoGameGum, RaylibGum, SkiaGum, SokolGum). See:
// https://docs.flatredball.com/gum/gum-tool/upgrading/syntax-versions
[assembly: GumSyntaxVersion(Version = 3)]
