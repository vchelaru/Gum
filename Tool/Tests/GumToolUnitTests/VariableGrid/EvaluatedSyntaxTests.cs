using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace GumToolUnitTests.VariableGrid;

public class EvaluatedSyntaxTests : BaseTestClass
{
    #region Helpers

    private static EvaluatedSyntax Evaluate(string expression, StateSave state)
    {
        string csharp = EvaluatedSyntax.ConvertToCSharpSyntax(expression);
        Microsoft.CodeAnalysis.SyntaxNode syntax = CSharpSyntaxTree.ParseText(csharp).GetCompilationUnitRoot();
        return EvaluatedSyntax.FromSyntaxNode(syntax, state);
    }

    private static StateSave BuildState(params (string name, object value, string type)[] variables)
    {
        StateSave state = new StateSave();
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        state.ParentContainer = screen;
        screen.States.Add(state);

        foreach ((string name, object value, string type) variable in variables)
        {
            state.Variables.Add(new VariableSave
            {
                Name = variable.name,
                Value = variable.value,
                Type = variable.type,
                SetsValue = true
            });
        }

        return state;
    }

    #endregion

    #region Arithmetic

    [Fact]
    public void FromSyntaxNode_AdditionOfFloats_ReturnsSumAsFloat()
    {
        StateSave state = BuildState(
            ("Instance.Width", 100f, "float"),
            ("Instance.Height", 50f, "float"));

        EvaluatedSyntax result = Evaluate("Instance.Width + Instance.Height", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(150f);
    }

    [Fact]
    public void FromSyntaxNode_AdditionOfInts_ReturnsSumAsInt()
    {
        StateSave state = BuildState(
            ("Instance.Red", 100, "int"),
            ("Instance.Green", 55, "int"));

        EvaluatedSyntax result = Evaluate("Instance.Red + Instance.Green", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(155);
    }

    [Fact]
    public void FromSyntaxNode_DivisionByConstant_ReturnsDividedValue()
    {
        StateSave state = BuildState(("Instance.Width", 200f, "float"));

        EvaluatedSyntax result = Evaluate("Instance.Width / 2", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(100f);
    }

    [Fact]
    public void FromSyntaxNode_DivisionOfTwoVariables_ReturnsDividedValue()
    {
        StateSave state = BuildState(
            ("Instance.Width", 200f, "float"),
            ("Instance.Height", 50f, "float"));

        EvaluatedSyntax result = Evaluate("Instance.Width / Instance.Height", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(4f);
    }

    [Fact]
    public void FromSyntaxNode_DivisionWithParentheses_ReturnsDividedValue()
    {
        StateSave state = BuildState(
            ("Instance.Width", 100f, "float"),
            ("Instance.Margin", 20f, "float"));

        EvaluatedSyntax result = Evaluate("(Instance.Width + Instance.Margin) / 2", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(60f);
    }

    [Fact]
    public void FromSyntaxNode_ConstantDividedByVariable_ReturnsDividedValue()
    {
        StateSave state = BuildState(("Instance.Width", 50f, "float"));

        EvaluatedSyntax result = Evaluate("100 / Instance.Width", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(2f);
    }

    [Fact]
    public void FromSyntaxNode_MultiplicationOfFloats_ReturnsProduct()
    {
        StateSave state = BuildState(("Instance.Width", 50f, "float"));

        EvaluatedSyntax result = Evaluate("Instance.Width * 3", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(150f);
    }

    [Fact]
    public void FromSyntaxNode_SubtractionOfFloats_ReturnsDifference()
    {
        StateSave state = BuildState(("Instance.Width", 100f, "float"));

        EvaluatedSyntax result = Evaluate("Instance.Width - 30", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(70f);
    }

    #endregion

    #region CastTo

    [Fact]
    public void CastTo_DoubleToFloat_CastsCorrectly()
    {
        StateSave state = BuildState(("Instance.Width", 3.14, "double"));

        EvaluatedSyntax result = Evaluate("Instance.Width", state);

        result.CastTo("float").ShouldBeTrue();
        result.Value.ShouldBeOfType<float>();
    }

    [Fact]
    public void CastTo_FloatToInt_Truncates()
    {
        StateSave state = BuildState(("Instance.Width", 3.7f, "float"));

        EvaluatedSyntax result = Evaluate("Instance.Width", state);

        result.CastTo("int").ShouldBeTrue();
        result.Value.ShouldBe(3);
    }

    [Fact]
    public void CastTo_IntToFloat_CastsCorrectly()
    {
        StateSave state = BuildState(("Instance.Width", 42, "int"));

        EvaluatedSyntax result = Evaluate("Instance.Width", state);

        result.CastTo("float").ShouldBeTrue();
        result.Value.ShouldBe(42f);
        result.Value.ShouldBeOfType<float>();
    }

    [Fact]
    public void CastTo_SameType_ReturnsTrue()
    {
        StateSave state = BuildState(("Instance.Width", 100f, "float"));

        EvaluatedSyntax result = Evaluate("Instance.Width", state);

        result.CastTo("float").ShouldBeTrue();
    }

    [Fact]
    public void CastTo_ToString_ConvertsAnyValue()
    {
        StateSave state = BuildState(("Instance.Width", 42, "int"));

        EvaluatedSyntax result = Evaluate("Instance.Width", state);

        result.CastTo("string").ShouldBeTrue();
        result.Value.ShouldBe("42");
    }

    #endregion

    #region ConvertSyntax

    [Fact]
    public void ConvertToCSharpSyntax_ComponentsSlashPath_ConvertsToGlobalQualified()
    {
        string result = EvaluatedSyntax.ConvertToCSharpSyntax("Components/Button.Width");

        result.ShouldBe("global::Components.Button.Width");
    }

    [Fact]
    public void ConvertToCSharpSyntax_NoSlashes_ReturnsUnchanged()
    {
        string result = EvaluatedSyntax.ConvertToCSharpSyntax("Instance.Width");

        result.ShouldBe("Instance.Width");
    }

    [Fact]
    public void ConvertToCSharpSyntax_ScreensSlashPath_ConvertsToGlobalQualified()
    {
        string result = EvaluatedSyntax.ConvertToCSharpSyntax("Screens/MainMenu.Instance.X");

        result.ShouldBe("global::Screens.MainMenu.Instance.X");
    }

    [Fact]
    public void ConvertToSlashSyntax_RoundtripsWithConvertToCSharpSyntax()
    {
        string original = "Components/Button.Width";
        string csharp = EvaluatedSyntax.ConvertToCSharpSyntax(original);
        string roundtripped = EvaluatedSyntax.ConvertToSlashSyntax(csharp);

        roundtripped.ShouldBe(original);
    }

    [Fact]
    public void ConvertToCSharpSyntax_DivisionByConstant_PreservesSlash()
    {
        string result = EvaluatedSyntax.ConvertToCSharpSyntax("Instance.Width / 2");

        result.ShouldBe("Instance.Width / 2");
    }

    [Fact]
    public void ConvertToCSharpSyntax_DivisionOfTwoVariables_PreservesSlash()
    {
        string result = EvaluatedSyntax.ConvertToCSharpSyntax("Instance.Width / Instance.Height");

        result.ShouldBe("Instance.Width / Instance.Height");
    }

    [Fact]
    public void ConvertToCSharpSyntax_DivisionWithParentheses_PreservesSlash()
    {
        string result = EvaluatedSyntax.ConvertToCSharpSyntax("(Instance.Width + 10) / 2");

        result.ShouldBe("(Instance.Width + 10) / 2");
    }

    [Fact]
    public void ConvertToCSharpSyntax_DivisionWithSpacesNoContext_PreservesSlash()
    {
        string result = EvaluatedSyntax.ConvertToCSharpSyntax("SomeVar / AnotherVar");

        result.ShouldBe("SomeVar / AnotherVar");
    }

    [Fact]
    public void ConvertToCSharpSyntax_MixedPathAndDivision_ConvertsBoth()
    {
        string result = EvaluatedSyntax.ConvertToCSharpSyntax("Components/Button.Width / 2");

        result.ShouldBe("global::Components.Button.Width / 2");
    }

    [Fact]
    public void ConvertToCSharpSyntax_SubfolderPathWithDivision_ConvertsBoth()
    {
        string result = EvaluatedSyntax.ConvertToCSharpSyntax("Components/Folder/Button.Width / 2");

        result.ShouldContain("global::Components.");
        result.ShouldEndWith(" / 2");
    }

    [Fact]
    public void ConvertToCSharpSyntax_SubfolderPath_ReplacesSlashes()
    {
        string result = EvaluatedSyntax.ConvertToCSharpSyntax("Components/Folder/SubFolder/Button.Width");

        result.ShouldStartWith("global::Components.");
        result.ShouldNotContain("/");
    }

    [Fact]
    public void ConvertToSlashSyntax_StandardsPath_RoundtripsCorrectly()
    {
        string original = "Standards/Text.FontSize";
        string csharp = EvaluatedSyntax.ConvertToCSharpSyntax(original);
        string roundtripped = EvaluatedSyntax.ConvertToSlashSyntax(csharp);

        roundtripped.ShouldBe(original);
    }

    #endregion

    #region CrossElementReferences

    [Fact]
    public void FromSyntaxNode_CrossElementComponentReference_ResolvesValue()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave button = new ComponentSave { Name = "Button" };
        StateSave buttonState = new StateSave { Name = "Default", ParentContainer = button };
        buttonState.Variables.Add(new VariableSave
        {
            Name = "Width",
            Value = 200f,
            Type = "float",
            SetsValue = true
        });
        button.States.Add(buttonState);
        project.Components.Add(button);

        StateSave localState = BuildState();

        EvaluatedSyntax result = Evaluate("Components/Button.Width", localState);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(200f);
    }

    #endregion

    #region Literals

    [Fact]
    public void FromSyntaxNode_FloatLiteral_ReturnsValue()
    {
        StateSave state = BuildState();

        EvaluatedSyntax result = Evaluate("3.14", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(3.14);
    }

    [Fact]
    public void FromSyntaxNode_IntegerLiteral_ReturnsValue()
    {
        StateSave state = BuildState();

        EvaluatedSyntax result = Evaluate("42", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(42);
    }

    #endregion

    #region Negation

    [Fact]
    public void FromSyntaxNode_NotOperatorOnBool_InvertsValue()
    {
        StateSave state = BuildState(("Instance.Visible", true, "bool"));

        EvaluatedSyntax result = Evaluate("!Instance.Visible", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(false);
    }

    [Fact]
    public void FromSyntaxNode_NotOperatorOnFalseBool_ReturnsTrue()
    {
        StateSave state = BuildState(("Instance.Visible", false, "bool"));

        EvaluatedSyntax result = Evaluate("!Instance.Visible", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(true);
    }

    #endregion

    #region NullAndMissing

    [Fact]
    public void FromSyntaxNode_MissingVariable_ReturnsNullValue()
    {
        StateSave state = BuildState();

        EvaluatedSyntax result = Evaluate("NonExistent.Width", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBeNull();
    }

    #endregion

    #region Parenthesized

    [Fact]
    public void FromSyntaxNode_ParenthesizedExpression_EvaluatesCorrectly()
    {
        StateSave state = BuildState(
            ("Instance.Width", 100f, "float"),
            ("Instance.Margin", 10f, "float"));

        EvaluatedSyntax result = Evaluate("(Instance.Width + Instance.Margin) * 2", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(220f);
    }

    #endregion

    #region SimpleVariableLookup

    [Fact]
    public void FromSyntaxNode_SimpleVariable_ResolvesValue()
    {
        StateSave state = BuildState(("Instance.X", 42f, "float"));

        EvaluatedSyntax result = Evaluate("Instance.X", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(42f);
    }

    #endregion

    #region TypeWidening

    [Fact]
    public void FromSyntaxNode_IntAndFloatAddition_WidensToFloat()
    {
        StateSave state = BuildState(
            ("Instance.IntVal", 10, "int"),
            ("Instance.FloatVal", 2.5f, "float"));

        EvaluatedSyntax result = Evaluate("Instance.IntVal + Instance.FloatVal", state);

        result.ShouldNotBeNull();
        result.Value.ShouldBe(12.5f);
    }

    #endregion
}
