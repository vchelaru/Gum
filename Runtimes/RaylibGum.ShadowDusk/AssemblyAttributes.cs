using System.Runtime.CompilerServices;
using Gum.DataTypes;

// Gum runtime syntax version, kept in lock step with Runtimes/RaylibGum/AssemblyAttributes.cs.
[assembly: GumSyntaxVersion(Version = 7)]

// Lets the tests count the shaders the loader keeps loaded.
[assembly: InternalsVisibleTo("RaylibGum.Tests")]
