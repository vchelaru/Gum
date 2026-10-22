using System;
using KernSmith;
using KernSmith.Gum;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// KernSmith finds rasterizer backends by reflection, which a trimmed or AOT publish (every iOS
/// release build) removes, so the StbTrueType backend's static constructor must be forced to run
/// before it is used. That has to happen whenever the resolved backend is StbTrueType, whether the
/// caller passed it or the mobile build defaulted to it.
/// </summary>
public class GumFontGeneratorStbTrueTypeRegistrationTests : IDisposable
{
    private readonly Func<Type?> _previousResolveType = GumFontGenerator.ResolveStbTrueTypeRasterizerType;
    private readonly Action<Type> _previousForce = GumFontGenerator.ForceStaticConstructor;
    private readonly List<Type> _forced = new();

    public GumFontGeneratorStbTrueTypeRegistrationTests()
    {
        GumFontGenerator.ResolveStbTrueTypeRasterizerType = () => typeof(GumFontGeneratorStbTrueTypeRegistrationTests);
        GumFontGenerator.ForceStaticConstructor = _forced.Add;
    }

    public void Dispose()
    {
        GumFontGenerator.ResolveStbTrueTypeRasterizerType = _previousResolveType;
        GumFontGenerator.ForceStaticConstructor = _previousForce;
    }

    [Fact]
    public void Generate_WithExplicitStbTrueType_ForcesTheBackendToRegister()
    {
        // Generation itself fails here, since the backend is not really registered in this process;
        // only the registration step before it is under test.
        Record.Exception(() => GumFontGenerator.Generate(NewFont(), RasterizerBackend.StbTrueType));

        _forced.ShouldBe(new[] { typeof(GumFontGeneratorStbTrueTypeRegistrationTests) });
    }

    [Fact]
    public void BeginIncremental_WithExplicitStbTrueType_ForcesTheBackendToRegister()
    {
        BmfcSave font = NewFont();
        font.FontFile = "missing.ttf";

        Record.Exception(() => GumFontGenerator.BeginIncremental(font, backend: RasterizerBackend.StbTrueType));

        _forced.Count.ShouldBe(1);
    }

    [Fact]
    public void Generate_WithExplicitFreeType_DoesNotForceStbTrueType()
    {
        Record.Exception(() => GumFontGenerator.Generate(NewFont(), RasterizerBackend.FreeType));

        _forced.ShouldBeEmpty();
    }

    [Fact]
    public void Generate_WithNoBackendOnDesktop_DoesNotForceStbTrueType()
    {
        Record.Exception(() => GumFontGenerator.Generate(NewFont()));

        _forced.ShouldBeEmpty();
    }

    [Fact]
    public void Generate_WithExplicitStbTrueTypeAndNoPackage_DoesNotThrowFromTheRegistrationStep()
    {
        GumFontGenerator.ResolveStbTrueTypeRasterizerType = () => null;

        Exception? exception = Record.Exception(
            () => GumFontGenerator.Generate(NewFont(), RasterizerBackend.StbTrueType));

        _forced.ShouldBeEmpty();
        (exception is NullReferenceException).ShouldBeFalse();
    }

    private static BmfcSave NewFont() => new BmfcSave { FontName = "Arial", FontSize = 18, Ranges = "65" };
}
