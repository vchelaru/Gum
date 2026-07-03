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
}
