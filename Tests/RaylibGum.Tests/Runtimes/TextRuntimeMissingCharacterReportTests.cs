using System;
using System.Collections.Generic;
using System.IO;
using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using RaylibGum.Renderables;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

// Issue #6818, Raylib side: a character outside the font's ranges is reported once per font.
// Reports are deduplicated per font and size across the process, so the test uses a size no other
// test uses.
public class TextRuntimeMissingCharacterReportTests : BaseTestClass
{
    private static string FixtureFontPath =>
        Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Orbitron-Black.ttf");

    [Fact]
    public void Text_WhenFontLacksCharacter_ReportsItOnce()
    {
        bool savedGrowth = TextRuntime.UseAutomaticFontGrowth;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        List<string> messages = new();
        Action<string> handler = messages.Add;
        try
        {
            TextRuntime.UseAutomaticFontGrowth = false;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithRaylibFontCreator();
            CustomSetPropertyOnRenderable.PropertyAssignmentError += handler;

            TextRuntime textRuntime = new();
            textRuntime.Text = "A";
            textRuntime.Font = FixtureFontPath;
            textRuntime.FontSize = 37;
            textRuntime.Name = "PriceLabel";
            messages.Clear();

            // U+20AC is in Orbitron-Black.ttf but outside the default ranges (32-126,160-255).
            textRuntime.Text = "A€";
            textRuntime.Text = "€A";

            messages.ShouldHaveSingleItem();
            messages[0].ShouldContain("PriceLabel");
            messages[0].ShouldContain("U+20AC");
            messages[0].ShouldContain("8364");
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= handler;
            TextRuntime.UseAutomaticFontGrowth = savedGrowth;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }
}
