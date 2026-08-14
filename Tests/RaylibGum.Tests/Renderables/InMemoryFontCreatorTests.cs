using Gum.GueDeriving;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System;
using Xunit;

namespace RaylibGum.Tests.Renderables;

public class InMemoryFontCreatorTests : BaseTestClass
{
    // Records what it was asked for and returns null, so UpdateToFontValues falls through to the
    // existing disk / system-font path (no bogus Raylib_cs.Font is ever assigned to the renderable).
    private sealed class RecordingFontCreator : IRaylibFontCreator
    {
        public int CallCount;
        public BmfcSave? LastReceived;

        public Raylib_cs.Font? TryCreateFont(BmfcSave bmfcSave)
        {
            CallCount++;
            LastReceived = bmfcSave;
            return null;
        }
    }

    // Always throws, simulating a rasterizer failure (e.g. KernSmith failing under a WASM host) so
    // UpdateToFontValues's catch block around InMemoryFontCreator.TryCreateFont is exercised.
    private sealed class ThrowingFontCreator : IRaylibFontCreator
    {
        public Raylib_cs.Font? TryCreateFont(BmfcSave bmfcSave)
            => throw new InvalidOperationException("Simulated font rasterization failure.");
    }

    [Fact]
    public void UpdateToFontValues_WhenInMemoryFontCreatorSet_ConsultsItWithCurrentFontProperties()
    {
        // A GUID-suffixed font name, not a fixed one like "Arial": LoaderManager's font cache is a
        // process-wide static that other tests (e.g. TextMarkupTests) populate with a real
        // Arial+Bold+20 font from a working font creator. If this test also requests Arial+Bold+20, a
        // cache-hit early-return in UpdateToFontValues can return before ever consulting THIS test's
        // creator, leaving LastReceived stale from an earlier property assignment - order-dependent,
        // so it passed locally but failed in CI (whichever test happens to run first differs by
        // environment). A name unique to this test can never collide.
        string uniqueFontName = "GumInMemoryFontCreatorTest_" + Guid.NewGuid().ToString("N");
        RecordingFontCreator creator = new RecordingFontCreator();
        IRaylibFontCreator? saved = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new TextRuntime();
            textRuntime.Font = uniqueFontName;
            textRuntime.IsBold = true;
            textRuntime.FontSize = 20;

            creator.CallCount.ShouldBeGreaterThan(0);
            creator.LastReceived.ShouldNotBeNull();
            creator.LastReceived!.FontName.ShouldBe(uniqueFontName);
            creator.LastReceived.FontSize.ShouldBe(20);
            creator.LastReceived.IsBold.ShouldBeTrue();
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = saved;
        }
    }

    // The bug this pins: an InMemoryFontCreator that throws (e.g. KernSmith failing under a WASM
    // host) was previously swallowed by a bare catch with no way for the game/developer to find out -
    // text silently fell back to the default font with zero diagnostics anywhere.
    [Fact]
    public void UpdateToFontValues_WhenInMemoryFontCreatorThrows_ShouldInvokePropertyAssignmentError()
    {
        string uniqueFontName = "GumThrowingFontCreatorTest_" + Guid.NewGuid().ToString("N");
        ThrowingFontCreator creator = new ThrowingFontCreator();
        IRaylibFontCreator? saved = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        string? capturedMessage = null;
        void Handler(string message) => capturedMessage = message;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Handler;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new TextRuntime();
            textRuntime.Font = uniqueFontName;
            textRuntime.FontSize = 20;

            capturedMessage.ShouldNotBeNull();
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = saved;
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= Handler;
        }
    }
}
