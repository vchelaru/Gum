using Gum.DataTypes;
using System.Runtime.CompilerServices;

// Exposes internal members (e.g. the NineSlice section-layout math, ComputeDrawSections /
// NineSliceDrawSection) to the unit test project.
[assembly: InternalsVisibleTo("RaylibGum.Tests")]

// Gum runtime syntax version — an assembly-level integer the Gum tool's code generator reads
// to decide which runtime conventions/namespaces to emit code against. This is NOT the .gumx
// project file format version.
//
// Only bump when a runtime change forces codegen to emit different code (renamed/removed role
// interfaces, new runtime types, namespace moves). Pure renderable/Forms/sample changes do NOT
// bump it. When bumping, change all four runtime assemblies (GumCommon, MonoGameGum, RaylibGum,
// SkiaGum) in lock step and add a row to the version table.
//
// The version history is maintained in one place — see:
// https://docs.flatredball.com/gum/gum-tool/upgrading/syntax-versions
[assembly: GumSyntaxVersion(Version = 2)]
