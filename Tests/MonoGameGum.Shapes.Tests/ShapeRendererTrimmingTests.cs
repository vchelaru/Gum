using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Gum.Renderables;
using Shouldly;

namespace MonoGameGum.Shapes.Tests;

// Issue #6434 — Apos.Shapes picks its embedded shader by reading MonoGame's internal
// Shader.Profile through reflection. A trimmed (Release iOS) build removes that property because
// nothing names it, and Apos then falls back to the DirectX shader, which iOS refuses. The
// shapes package keeps the property itself with a DynamicDependency on the method that creates
// the ShapeBatch. The trimmed result is proven by hand (see the PR); these tests keep the
// declaration from rotting.
public class ShapeRendererTrimmingTests
{
    const string ShaderTypeName = "Microsoft.Xna.Framework.Graphics.Shader";
    const string MonoGameAssemblyName = "MonoGame.Framework";

    [Fact]
    public void InitializeInternal_KeepsMonoGameShaderProfile_ForTheTrimmer()
    {
        MethodInfo? initialize = typeof(ShapeRenderer).GetMethod(
            "InitializeInternal", BindingFlags.Instance | BindingFlags.NonPublic);
        initialize.ShouldNotBeNull();

        IEnumerable<DynamicDependencyAttribute> dependencies =
            initialize!.GetCustomAttributes<DynamicDependencyAttribute>();

        dependencies.ShouldContain(d =>
            d.TypeName == ShaderTypeName &&
            d.AssemblyName == MonoGameAssemblyName &&
            d.MemberSignature == "Profile");
    }

    [Fact]
    public void MonoGameShader_StillHasTheProfileMemberTheDeclarationNames()
    {
        // If MonoGame renames Shader.Profile, the declaration above silently stops protecting
        // anything and Apos.Shapes would not find it either.
        Type? shader = typeof(Microsoft.Xna.Framework.Game).Assembly.GetType(ShaderTypeName);
        shader.ShouldNotBeNull();

        PropertyInfo? profile = shader!.GetProperty(
            "Profile",
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        profile.ShouldNotBeNull();
        typeof(Microsoft.Xna.Framework.Game).Assembly.GetName().Name.ShouldBe(MonoGameAssemblyName);
    }
}
