using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class CodeGenerationNameVerifierTests
{
    private readonly CodeGenerationNameVerifier _verifier;

    public CodeGenerationNameVerifierTests()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();

        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out It.Ref<string>.IsAny, out It.Ref<CommonValidationError>.IsAny))
            .Returns((string name, out string whyNot, out CommonValidationError error) =>
            {
                if (name.Length > 0 && name[0] != '_' && !char.IsLetter(name[0]))
                {
                    whyNot = $"Name may not begin with character {name[0]}";
                    error = CommonValidationError.InvalidStartingCharacterForCSharp;
                    return false;
                }

                string[] keywords = { "abstract", "as", "base", "bool", "break", "byte", "case", "catch",
                    "char", "checked", "class", "const", "continue", "decimal", "default", "delegate",
                    "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
                    "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int",
                    "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object",
                    "operator", "out", "override", "params", "private", "protected", "public", "readonly",
                    "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static",
                    "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint",
                    "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while" };

                if (Array.IndexOf(keywords, name) >= 0)
                {
                    whyNot = "Name is a C# reserved keyword";
                    error = CommonValidationError.ReservedCSharpKeyword;
                    return false;
                }

                whyNot = null!;
                error = CommonValidationError.None;
                return true;
            });

        _verifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
    }

    [Fact]
    public void ToCSharpName_ReservedKeyword_PrefixesWithAt()
    {
        string result = _verifier.ToCSharpName("class", out bool isPrefixed);

        result.ShouldBe("@class");
        isPrefixed.ShouldBeTrue();
    }

    [Fact]
    public void ToCSharpName_SpacesInName_ReplacedWithUnderscores()
    {
        string result = _verifier.ToCSharpName("My Button");

        result.ShouldBe("My_Button");
    }

    [Fact]
    public void ToCSharpName_StartsWithDigit_PrefixesWithUnderscore()
    {
        string result = _verifier.ToCSharpName("1stButton", out bool isPrefixed);

        result.ShouldBe("_1stButton");
        isPrefixed.ShouldBeTrue();
    }

    [Fact]
    public void ToCSharpName_ValidName_ReturnsUnchanged()
    {
        string result = _verifier.ToCSharpName("MyComponent", out bool isPrefixed);

        result.ShouldBe("MyComponent");
        isPrefixed.ShouldBeFalse();
    }

    [Fact]
    public void ToCSharpName_ValidNameStartingWithUnderscore_ReturnsUnchanged()
    {
        string result = _verifier.ToCSharpName("_privateField", out bool isPrefixed);

        result.ShouldBe("_privateField");
        isPrefixed.ShouldBeFalse();
    }
}
