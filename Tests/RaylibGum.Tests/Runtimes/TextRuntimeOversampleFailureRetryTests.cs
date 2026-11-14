using System;
using Gum.GueDeriving;
using RaylibGum.Renderables;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using Xunit;

namespace RaylibGum.Tests.Runtimes;

// Issue #8035, Raylib branch: a font the creator cannot build at the oversampled size is not retried
// (or its error re-reported) every frame.
public class TextRuntimeOversampleFailureRetryTests : BaseTestClass
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateAutomaticFontOversampling_CreatorFailsAtUnchangedZoom_IsNotRetriedEveryFrame(bool creatorThrows)
    {
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        IRaylibFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        DateTime now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        TextRuntime.OversampleFailureClock = () => now;
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = null;
            TextRuntime.UseFontOversampling = true;
            TextRuntime textRuntime = new();
            textRuntime.Font = "FailFont";
            textRuntime.FontSize = 18;
            FailingCreator creator = new() { Throw = creatorThrows };
            CustomSetPropertyOnRenderable.InMemoryFontCreator = creator;
            int errors = 0;
            Action<string> onError = _ => errors++;
            CustomSetPropertyOnRenderable.PropertyAssignmentError += onError;
            try
            {
                for (int frame = 0; frame < 5; frame++)
                {
                    textRuntime.UpdateAutomaticFontOversampling(2f);
                }
            }
            finally
            {
                CustomSetPropertyOnRenderable.PropertyAssignmentError -= onError;
            }

            creator.Calls.ShouldBe(1);
            errors.ShouldBe(creatorThrows ? 1 : 0);

            now = now.AddSeconds(60);
            textRuntime.UpdateAutomaticFontOversampling(2f);

            creator.Calls.ShouldBe(2);
        }
        finally
        {
            TextRuntime.OversampleFailureClock = () => DateTime.UtcNow;
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    private sealed class FailingCreator : IRaylibFontCreator
    {
        public bool Throw;
        public int Calls;

        public Raylib_cs.Font? TryCreateFont(BmfcSave bmfcSave)
        {
            Calls++;
            if (Throw)
            {
                throw new InvalidOperationException($"System font '{bmfcSave.FontName}' not found");
            }
            return null;
        }
    }
}
