using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The head compiles render-target shaders for OpenGL only, which never needs DXC's DXIL signing
/// library, so the build leaves dxil.dll / libdxil.so out (issue #5427). This test project imports
/// the same exclusion as the head, so RenderTargetShaderResolverTests prove the OpenGL compile works
/// without it.
/// </summary>
public class ShaderCompilerNativeLibraryTests
{
    [Fact]
    public void Output_HasTheDxcCompiler_ButNotTheDxilSigningLibrary()
    {
        string[] fileNames = Directory
            .EnumerateFiles(AppContext.BaseDirectory, "*", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Select(name => name!.ToLowerInvariant())
            .ToArray();

        fileNames.ShouldContain(name => name.Contains("dxcompiler"));
        fileNames.ShouldNotContain(name => name == "dxil.dll" || name == "libdxil.so");
    }
}
