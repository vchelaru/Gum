using Gum.DataTypes;

// Gum runtime syntax version — an assembly-level integer the Gum tool's code generator (and, for
// these FRB-facing GumCore.* assemblies specifically, FlatRedBall's own Glue codegen) reads to
// decide which runtime conventions/namespaces/types to emit code against. This is NOT the .gumx
// project file format version.
//
// Shared across every GumCore.* project under GumCoreXnaPc (DesktopGlNet6, FNA, Kni.DesktopGL,
// Kni.Web, Android, iOS) via an individual <Compile Include> in each .csproj - these projects pull
// their source from GumCoreShared.projitems/GumCoreShared.FlatRedBall.projitems rather than a
// single shared project file, so this one file is included the same way rather than introducing a
// new shared-project mechanism just for one attribute.
//
// Keep this value in lock-step with GumCommon/MonoGameGum/RaylibGum/SkiaGum/SilkNetGum's own
// AssemblyAttributes.cs - see GumDataTypes/GumSyntaxVersionAttribute.cs for the version history.
[assembly: GumSyntaxVersion(Version = 4)]
