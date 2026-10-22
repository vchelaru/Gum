using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries.Graphics;

public class StyledSubstringSplitterTests
{
    [Fact]
    public void GetStyledSubstrings_ShouldReturnEmptyList_IfLineIsEmpty()
    {
        StyledSubstringSplitter sut = new StyledSubstringSplitter();

        List<StyledSubstring> substrings = sut.GetStyledSubstrings(0, "", new List<InlineVariable>());

        substrings.Count.ShouldBe(0);
    }

    [Fact]
    public void GetStyledSubstrings_ShouldReturnSingleUnstyledEntry_IfNoInlineVariablesProvided()
    {
        StyledSubstringSplitter sut = new StyledSubstringSplitter();

        List<StyledSubstring> substrings = sut.GetStyledSubstrings(0, "Hello", new List<InlineVariable>());

        substrings.Count.ShouldBe(1);
        substrings[0].Substring.ShouldBe("Hello");
        substrings[0].Variables.Count.ShouldBe(0);
    }

    [Fact]
    public void GetStyledSubstrings_ShouldSplitIntoTwoEntries_IfVariableCoversFirstCharacters()
    {
        StyledSubstringSplitter sut = new StyledSubstringSplitter();
        List<InlineVariable> inlineVariables = new List<InlineVariable>
        {
            new InlineVariable
            {
                VariableName = "IsBold",
                Value = true,
                StartIndex = 0,
                CharacterCount = 2
            }
        };

        List<StyledSubstring> substrings = sut.GetStyledSubstrings(0, "Hello", inlineVariables);

        substrings.Count.ShouldBe(2);
        substrings[0].Substring.ShouldBe("He");
        substrings[0].Variables.Count.ShouldBe(1);
        substrings[0].Variables[0].VariableName.ShouldBe("IsBold");

        substrings[1].Substring.ShouldBe("llo");
        substrings[1].Variables.Count.ShouldBe(0);
    }

    private static InlineVariable Color(int start, int count, string value = "Red") =>
        new InlineVariable { VariableName = "Color", Value = value, StartIndex = start, CharacterCount = count };

    private static string Describe(List<StyledSubstring> substrings) =>
        string.Join("|", substrings.ConvertAll(x => x.Substring + ":" + x.Variables.Count));

    [Fact]
    public void GetStyledSubstringsCached_ReturnsTheSameRuns_WhenNothingChanged()
    {
        StyledSubstringSplitter sut = new StyledSubstringSplitter();
        List<InlineVariable> variables = new List<InlineVariable> { Color(2, 4) };

        List<StyledSubstring> first = sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);
        List<StyledSubstring> second = sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        second.ShouldBeSameAs(first);
        Describe(first).ShouldBe(Describe(sut.GetStyledSubstrings(0, "abcdefghij", variables)));
    }

    [Fact]
    public void GetStyledSubstringsCached_KeepsLinesApart()
    {
        StyledSubstringSplitter sut = new StyledSubstringSplitter();
        List<InlineVariable> variables = new List<InlineVariable> { Color(2, 4) };

        List<StyledSubstring> line0 = sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);
        List<StyledSubstring> line1 = sut.GetStyledSubstrings(1, 10, "klmnopqrst", variables);

        line1.ShouldNotBeSameAs(line0);
        sut.GetStyledSubstrings(0, 0, "abcdefghij", variables).ShouldBeSameAs(line0);
        sut.GetStyledSubstrings(1, 10, "klmnopqrst", variables).ShouldBeSameAs(line1);
    }

    [Fact]
    public void GetStyledSubstringsCached_Rebuilds_WhenTheLineTextChanges()
    {
        StyledSubstringSplitter sut = new StyledSubstringSplitter();
        List<InlineVariable> variables = new List<InlineVariable> { Color(2, 4) };
        sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        List<StyledSubstring> result = sut.GetStyledSubstrings(0, 0, "ABCDEFGH", variables);

        result[1].Substring.ShouldBe("CDEF");
        Describe(result).ShouldBe(Describe(sut.GetStyledSubstrings(0, "ABCDEFGH", variables)));
    }

    [Fact]
    public void GetStyledSubstringsCached_Rebuilds_WhenTheStartOfTheLineChanges()
    {
        StyledSubstringSplitter sut = new StyledSubstringSplitter();
        List<InlineVariable> variables = new List<InlineVariable> { Color(2, 4) };
        sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        List<StyledSubstring> result = sut.GetStyledSubstrings(0, 1, "abcdefghij", variables);

        Describe(result).ShouldBe(Describe(sut.GetStyledSubstrings(1, "abcdefghij", variables)));
        Describe(result).ShouldNotBe(Describe(sut.GetStyledSubstrings(0, "abcdefghij", variables)));
    }

    [Fact]
    public void GetStyledSubstringsCached_Rebuilds_WhenAVariableIsAdded()
    {
        StyledSubstringSplitter sut = new StyledSubstringSplitter();
        List<InlineVariable> variables = new List<InlineVariable> { Color(2, 4) };
        sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        variables.Add(Color(7, 2, "Blue"));
        List<StyledSubstring> result = sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        Describe(result).ShouldBe(Describe(sut.GetStyledSubstrings(0, "abcdefghij", variables)));
        result.Count.ShouldBe(5);
    }

    [Fact]
    public void GetStyledSubstringsCached_Rebuilds_WhenAVariableIsRemoved()
    {
        StyledSubstringSplitter sut = new StyledSubstringSplitter();
        List<InlineVariable> variables = new List<InlineVariable> { Color(2, 4) };
        sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        variables.Clear();
        List<StyledSubstring> result = sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        result.Count.ShouldBe(1);
        result[0].Variables.Count.ShouldBe(0);
    }

    [Fact]
    public void GetStyledSubstringsCached_Rebuilds_WhenAVariableIsReplacedByAnotherInstance()
    {
        StyledSubstringSplitter sut = new StyledSubstringSplitter();
        InlineVariable original = Color(2, 4);
        List<InlineVariable> variables = new List<InlineVariable> { original };
        sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        InlineVariable replacement = Color(2, 4, "Blue");
        variables[0] = replacement;
        List<StyledSubstring> result = sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        result[1].Variables[0].ShouldBeSameAs(replacement);
    }

    [Fact]
    public void GetStyledSubstringsCached_Rebuilds_WhenAVariableRangeIsEditedInPlace()
    {
        StyledSubstringSplitter sut = new StyledSubstringSplitter();
        InlineVariable variable = Color(2, 4);
        List<InlineVariable> variables = new List<InlineVariable> { variable };
        sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        variable.StartIndex = 5;
        variable.CharacterCount = 3;
        List<StyledSubstring> result = sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        result[1].Substring.ShouldBe("fgh");
    }

    [Fact]
    public void GetStyledSubstringsCached_Rebuilds_WhenAVariableNameIsEditedInPlace()
    {
        StyledSubstringSplitter sut = new StyledSubstringSplitter();
        InlineVariable variable = Color(2, 4);
        List<InlineVariable> variables = new List<InlineVariable> { variable };
        sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        variable.VariableName = "FontScale";
        List<StyledSubstring> result = sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        result[1].Variables[0].VariableName.ShouldBe("FontScale");
    }

    [Fact]
    public void GetStyledSubstringsCached_RunsSeeAVariableValueChangedInPlace()
    {
        StyledSubstringSplitter sut = new StyledSubstringSplitter();
        InlineVariable variable = Color(2, 4);
        List<InlineVariable> variables = new List<InlineVariable> { variable };
        List<StyledSubstring> first = sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        variable.Value = "Blue";
        List<StyledSubstring> second = sut.GetStyledSubstrings(0, 0, "abcdefghij", variables);

        second.ShouldBeSameAs(first);
        second[1].Variables[0].Value.ShouldBe("Blue");
    }
}
