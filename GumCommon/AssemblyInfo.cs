using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MonoGameGum.Tests")]

// MonoGameGum, KniGum, and FnaGum each link in CustomSetPropertyOnRenderable.cs,
// which writes to internal members on GraphicalUiElement (e.g., IsFontDirty.set).
// These three assemblies are logically part of the same Gum runtime and are
// expected to participate in the deferred-font-load contract.
[assembly: InternalsVisibleTo("MonoGameGum")]
[assembly: InternalsVisibleTo("KniGum")]
[assembly: InternalsVisibleTo("FnaGum")]
