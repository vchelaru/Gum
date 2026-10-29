using Gum.DataTypes;

// Gum runtime syntax version, kept in lock step with MonoGameGum/AssemblyAttributes.cs.
[assembly: GumSyntaxVersion(Version = 7)]

// Lets the tests cover the target detection.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("MonoGameGum.IntegrationTests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("GumShadowDusk.Tests.KniDesktopGL")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("GumShadowDusk.Tests.KniWindowsDX")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("GumShadowDusk.Tests.MonoGameWindowsDX")]
