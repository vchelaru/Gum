using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using KernSmith;
using KernSmith.Font;
using KernSmith.Gum;
using KernSmith.Output;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Renderables;

/// <summary>
/// Issue #8044: default Text asks for "Arial", which Linux and the browser do not have, so an
/// oversampled build failed there. When a system-font build of the default family fails, the generator
/// registers the bundled Liberation Sans under that name once and retries. The family name is a seam so
/// these tests do not depend on which fonts the machine has: they use a name no machine has.
/// KernSmith's font registry is process-wide, so each test restores it.
/// </summary>
public class GumFontGeneratorBundledArialTests : IDisposable
{
    private const string MissingFamily = "GumBundledArialTestFamily";

    private readonly string _previousFamily = BundledArialFont.FamilyName;
    private readonly Action<string, byte[], string?> _previousRegister = BundledArialFont.RegisterFont;

    public GumFontGeneratorBundledArialTests()
    {
        BundledArialFont.FamilyName = MissingFamily;
        BundledArialFont.Reset();
        BmFont.UnregisterFont(MissingFamily);
    }

    public void Dispose()
    {
        BundledArialFont.FamilyName = _previousFamily;
        BundledArialFont.RegisterFont = _previousRegister;
        BundledArialFont.Reset();
        BmFont.UnregisterFont(MissingFamily);
    }

    private static BmfcSave Make(string name) =>
        new BmfcSave { FontName = name, FontSize = 40, UseSmoothing = true, Ranges = "65" };

    [Fact]
    public void Generate_WhenTheDefaultFamilyIsNotInstalledOrRegistered_BuildsFromTheBundledFont()
    {
        BmFontResult result = GumFontGenerator.Generate(Make(MissingFamily));

        result.Pages.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Generate_WhenTheUserRegisteredTheFamily_DoesNotRegisterTheBundledFont()
    {
        int registrations = 0;
        BundledArialFont.RegisterFont = (_, _, _) => registrations++;
        BmFont.RegisterFont(MissingFamily, File.ReadAllBytes(FixtureFontPath));

        GumFontGenerator.Generate(Make(MissingFamily));

        registrations.ShouldBe(0);
    }

    [Fact]
    public void Generate_WhenTheBuildKeepsFailing_RegistersTheBundledFontOnlyOnce()
    {
        List<string> registered = new();
        // A register that does nothing keeps every build failing, so a second attempt would register again.
        BundledArialFont.RegisterFont = (family, _, _) => registered.Add(family);

        Should.Throw<Exception>(() => GumFontGenerator.Generate(Make(MissingFamily)));
        Should.Throw<Exception>(() => GumFontGenerator.Generate(Make(MissingFamily)));

        registered.ShouldBe(new[] { MissingFamily });
    }

    [Fact]
    public void Generate_WhenAnotherMissingFamilyFails_DoesNotRegisterTheBundledFont()
    {
        int registrations = 0;
        BundledArialFont.RegisterFont = (_, _, _) => registrations++;

        Should.Throw<Exception>(() => GumFontGenerator.Generate(Make("GumSomeOtherMissingFamily")));

        registrations.ShouldBe(0);
    }

    [Fact]
    public void Generate_WhenTheBuildFailsForAnotherReason_DoesNotRegisterTheBundledFont()
    {
        int registrations = 0;
        BundledArialFont.RegisterFont = (_, _, _) => registrations++;
        Func<string, FontGeneratorOptions, BmFontResult> previous = GumFontGenerator.GenerateFromSystem;
        GumFontGenerator.GenerateFromSystem = (_, _) => throw new InvalidOperationException("rasterizer failed");
        try
        {
            Should.Throw<InvalidOperationException>(() => GumFontGenerator.Generate(Make(MissingFamily)));
        }
        finally
        {
            GumFontGenerator.GenerateFromSystem = previous;
        }

        registrations.ShouldBe(0, "because an installed Arial must not be shadowed by a failure that is not 'not installed'");
    }

    [Fact]
    public void TryRegisterFor_WhenAnotherThreadIsRegistering_WaitsAndReportsTheRetryIsWorthIt()
    {
        using ManualResetEventSlim entered = new();
        using ManualResetEventSlim release = new();
        BundledArialFont.RegisterFont = (_, _, _) =>
        {
            entered.Set();
            release.Wait();
        };

        Task<bool> first = Task.Run(() => BundledArialFont.TryRegisterFor(MissingFamily));
        entered.Wait();
        Task<bool> second = Task.Run(() => BundledArialFont.TryRegisterFor(MissingFamily));

        second.Wait(200).ShouldBeFalse("because the second caller must wait for the registration in progress");
        release.Set();

        first.Result.ShouldBeTrue();
        second.Result.ShouldBeTrue();
    }

    private static string FixtureFontPath =>
        Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf");
}
