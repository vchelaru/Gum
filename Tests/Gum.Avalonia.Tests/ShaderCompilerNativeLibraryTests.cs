using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// ShadowDusk.HLSL brings in Vortice.Dxc, which ships DXC's compiler and its DXIL signing library
/// for Windows and Linux. Both ship with the tool and THIRD-PARTY-NOTICES credits both (issue #5427).
/// On Linux, Vortice.Dxc only loads libdxcompiler.so after loading libdxil.so, so dropping either
/// breaks the shader compile there.
/// </summary>
public class ShaderCompilerNativeLibraryTests
{
    [Fact]
    public void Output_HasTheDxcCompiler_AndTheDxilSigningLibrary()
    {
        string[] fileNames = Directory
            .EnumerateFiles(AppContext.BaseDirectory, "*", SearchOption.AllDirectories)
            .Select(Path.GetFileName)
            .Select(name => name!.ToLowerInvariant())
            .ToArray();

        fileNames.ShouldContain(name => name.Contains("dxcompiler"));
        fileNames.ShouldContain("dxil.dll");
        fileNames.ShouldContain("libdxil.so");
    }
}
