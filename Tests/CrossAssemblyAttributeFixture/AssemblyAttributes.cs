using Gum.DataTypes;

// Test fixture only: reproduces the real-world shape where a Gum runtime assembly's
// [assembly: GumSyntaxVersion] attribute type is defined in a DIFFERENT referenced
// assembly (GumCommon), the same way MonoGameGum/RaylibGum/SkiaGum reference it.
[assembly: GumSyntaxVersion(Version = 5)]
