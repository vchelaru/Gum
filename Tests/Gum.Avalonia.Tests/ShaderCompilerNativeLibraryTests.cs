using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The head compiles render-target shaders for OpenGL only, which never needs DXC's DXIL signing
/// library, so the build leaves the Windows dxil.dll out (issue #5427). libdxil.so stays: on Linux,
/// Vortice.Dxc only loads libdxcompiler.so after loading libdxil.so. This test project imports the
/// same exclusion as the head, so RenderTargetShaderResolverTests prove the OpenGL compile works.
/// </summary>
public class ShaderCompilerNativeLibraryTests
{
    [Fact]
    public void Output_HasTheDxcCompiler_AndOnlyTheLinuxDxilSigningLibrary()
    {
        string[] fileNames = Directory
            .EnumerateFiles(AppContext.BaseDirectory, "*", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Select(name => name!.ToLowerInvariant())
            .ToArray();

        fileNames.ShouldContain(name => name.Contains("dxcompiler"));
        fileNames.ShouldContain("libdxil.so");
        fileNames.ShouldNotContain("dxil.dll");
    }
}
