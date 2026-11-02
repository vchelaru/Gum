using Gum.ProjectServices.CodeGeneration;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// A string value with a line break is emitted as an escaped regular literal: a raw line break inside a
/// verbatim literal would change the string whenever the generated file's line endings are converted.
/// </summary>
public class CodeGeneratorStringLiteralTests
{
    [Theory]
    [InlineData("A\nB", "\"A\\nB\"")]
    [InlineData("A\r\nB", "\"A\\r\\nB\"")]
    [InlineData("A\rB", "\"A\\rB\"")]
    [InlineData("A\u2028B", "\"A\\u2028B\"")]
    [InlineData("say \"hi\"\nC:\\dir", "\"say \\\"hi\\\"\\nC:\\\\dir\"")]
    [InlineData("plain", "@\"plain\"")]
    [InlineData("say \"hi\"", "@\"say \"\"hi\"\"\"")]
    [InlineData("", "@\"\"")]
    public void ToStringLiteral_EmitsALiteralWithNoRawLineBreak(string text, string expectedLiteral)
    {
        string literal = CodeGenerator.ToStringLiteral(text);

        literal.ShouldBe(expectedLiteral);
        literal.IndexOfAny(new[] { '\r', '\n', '\u0085', '\u2028', '\u2029' }).ShouldBe(-1);
    }
}
