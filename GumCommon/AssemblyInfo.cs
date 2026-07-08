using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MonoGameGum.Tests")]
[assembly: InternalsVisibleTo("MonoGameGum.Tests.V2")]
[assembly: InternalsVisibleTo("RaylibGum.Tests")]

// MonoGameGum, KniGum, and FnaGum each link in CustomSetPropertyOnRenderable.cs,
// which writes to internal members on GraphicalUiElement (e.g., IsFontDirty.set).
// These three assemblies are logically part of the same Gum runtime and are
// expected to participate in the deferred-font-load contract.
[assembly: InternalsVisibleTo("MonoGameGum")]
[assembly: InternalsVisibleTo("KniGum")]
[assembly: InternalsVisibleTo("FnaGum")]

// RaylibGum/SokolGum source-link MonoGameGum's Forms control files (Forms/Controls/**/*.cs)
// and need internal access to FrameworkElement.PropertyRegistry from those file's
// SetBinding/IsDataBound code paths.
[assembly: InternalsVisibleTo("RaylibGum")]
[assembly: InternalsVisibleTo("SokolGum")]
