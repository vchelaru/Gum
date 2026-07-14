using System;
using System.IO;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Unit tests for <see cref="CustomEffectManager.CustomShaderFileExists"/>, the optional
/// custom-shader probe. The probe must resolve relative to the app base directory (where the
/// content actually ships) rather than the process working directory — launching an app via a
/// Windows file association sets the working directory to the opened file's folder, which
/// previously caused the probe to miss a shipped Content/Shader.xnb (#3694).
/// </summary>
public class CustomEffectManagerTests : BaseTestClass
{
    [Fact]
    public void CustomShaderFileExists_FindsShaderRelativeToBaseDirectory_WhenWorkingDirectoryDiffers()
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), "gum_3694_base_" + Guid.NewGuid().ToString("N"));
        string workingDirectory = Path.Combine(Path.GetTempPath(), "gum_3694_cwd_" + Guid.NewGuid().ToString("N"));
        string originalWorkingDirectory = Directory.GetCurrentDirectory();

        try
        {
            Directory.CreateDirectory(Path.Combine(baseDirectory, "Content"));
            File.WriteAllText(Path.Combine(baseDirectory, "Content", "Shader.xnb"), "dummy");

            // Simulate a file-association launch: the working directory is NOT where the app shipped.
            Directory.CreateDirectory(workingDirectory);
            Directory.SetCurrentDirectory(workingDirectory);

            CustomEffectManager.CustomShaderFileExists(baseDirectory).ShouldBeTrue();
        }
        finally
        {
            Directory.SetCurrentDirectory(originalWorkingDirectory);
            Directory.Delete(baseDirectory, recursive: true);
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Fact]
    public void CustomShaderFileExists_ReturnsFalse_WhenNoShaderShippedInBaseDirectory()
    {
        string baseDirectory = Path.Combine(Path.GetTempPath(), "gum_3694_noshader_" + Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(baseDirectory);

            CustomEffectManager.CustomShaderFileExists(baseDirectory).ShouldBeFalse();
        }
        finally
        {
            Directory.Delete(baseDirectory, recursive: true);
        }
    }
}
