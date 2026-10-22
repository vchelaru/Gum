using MonoGameGum.TestsCommon;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using Xunit;

using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Issue #6419: a cloned Text must not share the original's styled-run cache, or two Texts drawing with
/// different inline variables would clear each other's cache every frame and allocate again.
/// </summary>
public class TextCloneSplitterCacheTests
{
    private const string Line = "abcdefghij";

    private static Text CreateOriginalAndClone(out Text clone)
    {
        Text original = new Text();
        original.InlineVariables.Add(new InlineVariable { VariableName = "Color", Value = "Red", StartIndex = 2, CharacterCount = 4 });
        clone = original.Clone();
        clone.InlineVariables.Add(new InlineVariable { VariableName = "Color", Value = "Blue", StartIndex = 7, CharacterCount = 2 });
        return original;
    }

    [Fact]
    public void Clone_GetsItsOwnStyledRuns()
    {
        Text original = CreateOriginalAndClone(out Text clone);

        List<StyledSubstring> originalRuns = original.GetCachedStyledSubstrings(0, 0, Line);
        List<StyledSubstring> cloneRuns = clone.GetCachedStyledSubstrings(0, 0, Line);

        originalRuns.Count.ShouldBe(3);
        cloneRuns.Count.ShouldBe(5);
        original.GetCachedStyledSubstrings(0, 0, Line).ShouldBeSameAs(originalRuns);
    }

    [Fact]
    public void GetCachedStyledSubstrings_AlternatingBetweenAnOriginalAndItsClone_AllocatesNothing()
    {
        Text original = CreateOriginalAndClone(out Text clone);

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                original.GetCachedStyledSubstrings(0, 0, Line);
                clone.GetCachedStyledSubstrings(0, 0, Line);
            },
            attempts: 3,
            warmupIterations: 10,
            measuredIterations: 20);

        result.TotalBytes.ShouldBe(0, $"measured {result.BytesPerIteration:N0} bytes/frame");
    }
}
