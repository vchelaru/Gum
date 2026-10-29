using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MonoGameGum.Tests")]
[assembly: InternalsVisibleTo("MonoGameGum.Tests.V3")]
[assembly: InternalsVisibleTo("MonoGameGum.IntegrationTests")]
// Svg loading in the optional Gum.Shapes packages reports a bad file through the same PropertyAssignmentError choke point as a Sprite.
[assembly: InternalsVisibleTo("MonoGameGumShapes")]
[assembly: InternalsVisibleTo("KniGumShapes")]
