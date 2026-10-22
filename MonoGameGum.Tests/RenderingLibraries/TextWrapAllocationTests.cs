using System;
using System.Collections.Generic;
using System.Text;
using MonoGameGum.TestsCommon;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

using Text = RenderingLibrary.Graphics.Text;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Issue #6420: changing the string of a wrapped <see cref="Text"/> every frame must not allocate
/// per word or per concatenation. The strings are made by the caller (a counter or log builds its own),
/// so each test hands the Text pre-made strings. What remains is the string of every wrapped line whose
/// content changed, because <see cref="Text.WrappedText"/> is a list of strings.
/// </summary>
public class TextWrapAllocationTests
{
    // The room a line string takes: object header and length (22 bytes), 2 bytes a char, rounded up to 8.
    private static long LineStringBytes(IEnumerable<string> lines)
    {
        long total = 0;
        foreach (string line in lines)
        {
            total += (22 + 2L * line.Length + 7) / 8 * 8;
        }
        return total;
    }

    [Theory]
    [InlineData(200, 300)]
    [InlineData(2400, 500)]
    public void RawText_ChangedEveryFrameToSameLengthString_AllocatesOnlyTheChangedLineStrings(int letters, int width)
    {
        string a = MakeWords(letters, seed: 1);
        string b = MakeWords(letters, seed: 2);
        a.Length.ShouldBe(b.Length);

        Text text = CreateText(width);
        text.RawText = a;
        long lineBytesA = LineStringBytes(text.WrappedText);
        text.WrappedText.Count.ShouldBeGreaterThan(5, "because the string must really wrap");
        text.RawText = b;
        long lineBytesB = LineStringBytes(text.WrappedText);

        bool flip = false;
        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                flip = !flip;
                text.RawText = flip ? a : b;
            },
            warmupIterations: 50,
            measuredIterations: 200);

        // Every line differs between the two strings, so the new line strings are the floor.
        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(
            System.Math.Max(lineBytesA, lineBytesB) + 64,
            $"measured {result.BytesPerIteration:N0} bytes/frame");
    }

    [Fact]
    public void RawText_ChangedEveryFrameToStringWithOnlyTheFirstWordChanged_AllocatesOnlyTheFirstLine()
    {
        string rest = MakeWords(2400, seed: 3);
        string a = "aaaa" + rest;
        string b = "bbbb" + rest;

        Text text = CreateText(500);
        text.RawText = a;
        text.WrappedText.Count.ShouldBeGreaterThan(20);
        long firstLineBytes = LineStringBytes(new[] { text.WrappedText[0] });

        bool flip = false;
        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                flip = !flip;
                text.RawText = flip ? b : a;
            },
            warmupIterations: 50,
            measuredIterations: 200);

        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(
            firstLineBytes + 64,
            $"measured {result.BytesPerIteration:N0} bytes/frame");
    }

    [Fact]
    public void RawText_ChangedEveryFrameToStringOfDifferentLength_AllocatesOnlyTheLineStrings()
    {
        string a = MakeWords(2400, seed: 4);
        string b = MakeWords(2000, seed: 5);

        Text text = CreateText(500);
        text.RawText = a;
        long lineBytesA = LineStringBytes(text.WrappedText);
        text.RawText = b;
        long lineBytesB = LineStringBytes(text.WrappedText);

        bool flip = false;
        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                flip = !flip;
                text.RawText = flip ? a : b;
            },
            warmupIterations: 50,
            measuredIterations: 200);

        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(
            System.Math.Max(lineBytesA, lineBytesB) + 64,
            $"measured {result.BytesPerIteration:N0} bytes/frame");
    }

    [Fact]
    public void RawText_ChangedEveryFrameToNewlineSeparatedLines_AllocatesOnlyTheLineStrings()
    {
        string a = MakeWords(1200, seed: 6).Replace("  ", " ").Replace(' ', '\n');
        string b = MakeWords(1200, seed: 7).Replace("  ", " ").Replace(' ', '\n');

        Text text = CreateText(500);
        text.RawText = a;
        long lineBytesA = LineStringBytes(text.WrappedText);
        text.RawText = b;
        long lineBytesB = LineStringBytes(text.WrappedText);

        bool flip = false;
        AllocationResult result = AllocationMeasurer.Measure(
            () =>
            {
                flip = !flip;
                text.RawText = flip ? a : b;
            },
            warmupIterations: 50,
            measuredIterations: 200);

        result.BytesPerIteration.ShouldBeLessThanOrEqualTo(
            System.Math.Max(lineBytesA, lineBytesB) + 64,
            $"measured {result.BytesPerIteration:N0} bytes/frame");
    }

    private static Text CreateText(float width)
    {
        Text text = new() { BitmapFont = TextWrapCharacterizationTests.CreateFont() };
        text.Width = width;
        return text;
    }

    // Deterministic lowercase words of 2 to 8 letters joined by single spaces, exactly `letters` long.
    private static string MakeWords(int letters, int seed)
    {
        Random random = new(seed);
        StringBuilder builder = new();
        while (builder.Length < letters)
        {
            if (builder.Length > 0)
            {
                builder.Append(' ');
            }
            int length = 2 + random.Next(7);
            for (int i = 0; i < length; i++)
            {
                builder.Append((char)('a' + random.Next(26)));
            }
        }
        return builder.ToString(0, letters);
    }
}
