using System;
using System.IO;
using Shouldly;
using SkiaGum.Content;
using SkiaSharp;

namespace SkiaGum.Tests.Content;

public class SkSlRuntimeEffectLoaderTests
{
    private static string WriteTempFile(string extension, string text)
    {
        string path = Path.Combine(Path.GetTempPath(), "GumSkSlLoaderTest_" + Guid.NewGuid().ToString("N") + extension);
        File.WriteAllText(path, text);
        return path;
    }

    [Fact]
    public void Load_SkslFile_ReturnsCompiledEffect()
    {
        string path = WriteTempFile(".sksl", "half4 main(float2 coord) { return half4(1, 0, 0, 1); }");
        try
        {
            using SKRuntimeEffect effect = SkSlRuntimeEffectLoader.Load(path);

            effect.ShouldNotBeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_UppercaseExtension_IsAccepted()
    {
        string path = WriteTempFile(".SKSL", "half4 main(float2 coord) { return half4(1, 0, 0, 1); }");
        try
        {
            using SKRuntimeEffect effect = SkSlRuntimeEffectLoader.Load(path);

            effect.ShouldNotBeNull();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_InvalidSksl_ThrowsWithTheCompilerErrors()
    {
        string path = WriteTempFile(".sksl", "half4 main(float2 coord) { return notDeclared; }");
        try
        {
            InvalidOperationException exception =
                Should.Throw<InvalidOperationException>(() => SkSlRuntimeEffectLoader.Load(path));

            exception.Message.ShouldContain("notDeclared");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData(".fx")]
    [InlineData(".slang")]
    public void Load_NonSkslExtension_ThrowsNamingTheShadowDuskPackage(string extension)
    {
        string path = WriteTempFile(extension, "irrelevant");
        try
        {
            NotSupportedException exception =
                Should.Throw<NotSupportedException>(() => SkSlRuntimeEffectLoader.Load(path));

            exception.Message.ShouldContain(".sksl");
            exception.Message.ShouldContain("Gum.SkiaSharp.ShadowDusk");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
