using Gum.GueDeriving;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
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

    [Fact]
    public void UpdateToFontValues_WhenInMemoryFontCreatorSet_ConsultsItWithCurrentFontProperties()
    {
        RecordingFontCreator creator = new RecordingFontCreator();
        IRaylibFontCreator? saved = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;

            TextRuntime textRuntime = new TextRuntime();
            textRuntime.Font = "Arial";
            textRuntime.IsBold = true;
            textRuntime.FontSize = 20;

            creator.CallCount.ShouldBeGreaterThan(0);
            creator.LastReceived.ShouldNotBeNull();
            creator.LastReceived!.FontName.ShouldBe("Arial");
            creator.LastReceived.FontSize.ShouldBe(20);
            creator.LastReceived.IsBold.ShouldBeTrue();
        }
        finally
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = saved;
        }
    }
}
