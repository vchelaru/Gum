using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CSharp.RuntimeBinder;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Expressions;

/// <summary>
/// Roslyn-based expression evaluator for Gum variable references.
/// Parses and evaluates right-side expressions such as "OtherInstance.Width + 10".
/// </summary>
public class EvaluatedSyntax
{
    #region Fields/Properties

    public string EvaluatedType { get; set; }
    public SyntaxNode SyntaxNode { get; set; }

    public object Value { get; set; }

    #endregion

    public override string ToString()
    {
        if (SyntaxNode == null)
        {
            return "<no syntax>";
        }
        else
        {
            return $"{SyntaxNode.ToString()} = {Value}";
        }
    }

    #region Parse

    public static EvaluatedSyntax FromSyntaxNode(SyntaxNode syntaxNode, StateSave stateForUnqualifiedRightSide)
    {
        return Evaluate(syntaxNode, stateForUnqualifiedRightSide);
    }

    private static EvaluatedSyntax Evaluate(SyntaxNode syntaxNode, StateSave stateForUnqualifiedRightSide)
    {
        if (syntaxNode is BinaryExpressionSyntax binaryExpressionSytax)
        {
            var leftSyntax = binaryExpressionSytax.Left;
            var rightSyntax = binaryExpressionSytax.Right;

            var leftEvaluated = Evaluate(leftSyntax, stateForUnqualifiedRightSide);
            var rightEvaluated = Evaluate(rightSyntax, stateForUnqualifiedRightSide);

            var value = Combine(leftEvaluated, rightEvaluated, binaryExpressionSytax.OperatorToken);

            return FromSyntaxAndValue(syntaxNode, value);
        }
        else if (syntaxNode is ParenthesizedExpressionSyntax parenthesizedExpressionSyntax)
        {
            var childNodes = parenthesizedExpressionSyntax.ChildNodes();

            if (childNodes == null || childNodes.Count() == 0)
            {
                return null;
            }

            foreach (var item in childNodes)
            {
                var evaluatedSyntax = Evaluate(item, stateForUnqualifiedRightSide);
                if (evaluatedSyntax != null)
                {
                    return evaluatedSyntax;
                }
            }

        }
        else if (syntaxNode is IdentifierNameSyntax or VariableDeclarationSyntax)
        {
            var rfv = new RecursiveVariableFinder(stateForUnqualifiedRightSide);

            var value = rfv.GetValue(syntaxNode.ToString());

            return FromSyntaxAndValue(syntaxNode, value);
        }
        else if (syntaxNode is MemberAccessExpressionSyntax memberAccess)
        {
            // we just need to evaluate the right-side
            var rightSideToEvaluate = memberAccess.ToString();


            RecursiveVariableFinder rfv = null;

            var stateForRfv = stateForUnqualifiedRightSide;

            if (rightSideToEvaluate.StartsWith("global::"))
            {
                string elementName, elementType;
                ConvertGlobalToElementNameWithSlashes(rightSideToEvaluate, out elementName, out elementType);

                if (elementName != null)
                {
                    var element = ObjectFinder.Self.GetElementSave(elementName);
                    stateForRfv = element?.DefaultState;
                    rightSideToEvaluate = rightSideToEvaluate.Substring(($"global::{elementType}." + elementName).Length + 1);
                }
            }

            if (stateForRfv == null)
            {
                return null;
            }
            else
            {
                rfv = new RecursiveVariableFinder(stateForRfv);

                var value = rfv.GetValue(rightSideToEvaluate);

                return FromSyntaxAndValue(syntaxNode, value);
            }

        }
        else if (syntaxNode is PrefixUnaryExpressionSyntax prefixUnary)
        {
            if (prefixUnary.OperatorToken.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ExclamationToken))
            {
                var operand = Evaluate(prefixUnary.Operand, stateForUnqualifiedRightSide);
                if (operand?.Value is bool b)
                {
                    return FromSyntaxAndValue(syntaxNode, !b);
                }
            }
            return null;
        }
        else if (syntaxNode is LiteralExpressionSyntax literalExpression)
        {
            var value = literalExpression.Token.Value;

            return FromSyntaxAndValue(syntaxNode, value);
        }
        else if (syntaxNode is GlobalStatementSyntax globalStatementSyntax)
        {
            var statement = globalStatementSyntax.Statement;

            var childNodes = statement?.ChildNodes();

            if (childNodes == null || childNodes.Count() == 0)
            {
                return null;
            }

            foreach (var item in childNodes)
            {
                var evaluatedSyntax = Evaluate(item, stateForUnqualifiedRightSide);
                if (evaluatedSyntax != null)
                {
                    return evaluatedSyntax;
                }
            }
        }
        else if (syntaxNode is CompilationUnitSyntax compilationUnitSyntax)
        {
            var childNodes = compilationUnitSyntax.ChildNodes();

            if (childNodes?.Count() > 0 != true)
            {
                return null;
            }

            foreach (var item in childNodes)
            {
                var evaluatedSyntax = Evaluate(item, stateForUnqualifiedRightSide);
                if (evaluatedSyntax != null)
                {
                    return evaluatedSyntax;
                }
            }
        }
        return null;
    }

    private static object Combine(EvaluatedSyntax leftEvaluated, EvaluatedSyntax rightEvaluated, SyntaxToken operatorToken)
    {
        if (leftEvaluated?.Value == null || rightEvaluated?.Value == null)
        {
            return null;
        }

        dynamic dynamicValue1, dynamicValue2;
        GetDynamicValues(leftEvaluated.Value, rightEvaluated.Value, out dynamicValue1, out dynamicValue2);

        try
        {
            if (operatorToken.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PlusToken))
            {
                return dynamicValue1 + dynamicValue2;
            }
            else if (operatorToken.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.MinusToken))
            {
                return dynamicValue1 - dynamicValue2;
            }
            else if (operatorToken.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.AsteriskToken))
            {
                return dynamicValue1 * dynamicValue2;
            }
            else if (operatorToken.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SlashToken))
            {
                object result = dynamicValue1 / dynamicValue2;
                // Float/double division by zero returns Infinity/NaN instead of throwing
                if (result is float f && (float.IsInfinity(f) || float.IsNaN(f)))
                {
                    return null;
                }
                if (result is double d && (double.IsInfinity(d) || double.IsNaN(d)))
                {
                    return null;
                }
                return result;
            }
            else
            {
                System.Diagnostics.Debugger.Break();
            }
        }
        catch(RuntimeBinderException)
        {
            // This can happen if someone does something like tries to subtract strings ("A" - "B")
        }
        catch(DivideByZeroException)
        {
            // Division by zero returns null rather than crashing
        }

        return null;
    }

    private static void GetDynamicValues(object obj1, object obj2, out dynamic dynamicValue1, out dynamic dynamicValue2)
    {
        dynamicValue1 = null;
        dynamicValue2 = null;

        var isFirstNumeric = TryGetNumericType(obj1, out Type type1);
        var isSecondNumeric = TryGetNumericType(obj2, out Type type2);

        if(isFirstNumeric && isSecondNumeric)
        {
            // Find the larger (wider) type to ensure proper addition
            var targetType = GetWiderNumericType(type1, type2);

            // Convert both numbers to the wider type
            var value1 = Convert.ChangeType(obj1, targetType);
            var value2 = Convert.ChangeType(obj2, targetType);

            // Perform the addition
            dynamicValue1 = value1;
            dynamicValue2 = value2;
        }
        else
        {
            var isFirstNumericOrString = isFirstNumeric || obj1 is string;
            var isSecondNumericOrString = isSecondNumeric || obj2 is string;

            if(isFirstNumericOrString && isSecondNumericOrString)
            {
                dynamicValue1 = obj1;
                dynamicValue2 = obj2;
            }
        }
    }
    private static EvaluatedSyntax FromSyntaxAndValue(SyntaxNode syntaxNode, object value)
    {
        var toReturn = new EvaluatedSyntax();
        toReturn.SyntaxNode = syntaxNode;
        toReturn.Value = value;
        string type = GetSimpleTypeNameForValue(value);

        toReturn.EvaluatedType = type;

        return toReturn;
    }


    #endregion

    #region Convert
    private static string GetSimpleTypeNameForValue(object value)
    {
        return value is float ? "float"
            : value is string ? "string"
            : value is bool ? "bool"
            : value is int ? "int"
            : value is decimal ? "decimal"
            : value is double ? "double"
            : value is long ? "long"
            : value?.GetType().ToString();
    }

    static bool TryGetNumericType(object obj, out Type type)
    {
        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        type = obj.GetType();

        if (!IsNumericType(type))
        {
            return false;
        }
        else
        {
            return true;
        }

    }

    static bool IsNumericType(Type type)
    {
        return type == typeof(byte) || type == typeof(sbyte) ||
               type == typeof(short) || type == typeof(ushort) ||
               type == typeof(int) || type == typeof(uint) ||
               type == typeof(long) || type == typeof(ulong) ||
               type == typeof(float) || type == typeof(double) ||
               type == typeof(decimal);
    }

    static Type GetWiderNumericType(Type type1, Type type2)
    {
        // Define a precedence list for numeric types
        var typeOrder = new[]
        {
            typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
            typeof(int), typeof(uint), typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        // Find the type with the higher precedence
        int index1 = Array.IndexOf(typeOrder, type1);
        int index2 = Array.IndexOf(typeOrder, type2);

        return typeOrder[Math.Max(index1, index2)];
    }


    public static string ConvertToCSharpSyntax(string lineOfText)
    {
        if (lineOfText.Contains("Components/"))
        {
            lineOfText = lineOfText.Replace("Components/", "global::Components.");
        }
        if (lineOfText.Contains("Screens/"))
        {
            lineOfText = lineOfText.Replace("Screens/", "global::Screens.");
        }
        if (lineOfText.Contains("Standards/"))
        {
            lineOfText = lineOfText.Replace("Standards/", "global::Standards.");
        }

        // Remaining slashes are either subfolder path separators (e.g. Folder/SubFolder/Button)
        // or the division operator. We replace only path separators with a Unicode placeholder.
        // Rules to identify division:
        // 1. Either side is a numeric constant
        // 2. Left side contains a dot (meaning it's a qualified property, not a path segment)
        // 3. Either side is wrapped in parentheses
        // 4. Spaces surround the slash (fallback for ambiguous cases)
        lineOfText = ReplacePathSlashes(lineOfText);

        return lineOfText;
    }

    private static bool IsDivisionSlash(string text, int slashIndex)
    {
        // Find the token to the left and right of the slash
        string leftToken = GetTokenBefore(text, slashIndex);
        string rightToken = GetTokenAfter(text, slashIndex);

        if (leftToken.Length == 0 || rightToken.Length == 0)
        {
            return false;
        }

        // Rule 1: Either side is a numeric constant
        if (IsNumericConstant(leftToken) || IsNumericConstant(rightToken))
        {
            return true;
        }

        // Rule 2: Left side contains a dot (it's a qualified property like Instance.Width),
        // but not if it's a global:: qualified path (those dots are from prefix replacement)
        if (leftToken.Contains('.') && !leftToken.StartsWith("global::"))
        {
            return true;
        }

        // Rule 3: Either side is wrapped in parentheses
        if (leftToken.EndsWith(")") || rightToken.StartsWith("("))
        {
            return true;
        }

        // Rule 4: Spaces surround the slash (fallback)
        if (slashIndex > 0 && slashIndex < text.Length - 1 &&
            text[slashIndex - 1] == ' ' && text[slashIndex + 1] == ' ')
        {
            return true;
        }

        return false;
    }

    private static string GetTokenBefore(string text, int index)
    {
        int end = index - 1;
        // Skip whitespace
        while (end >= 0 && text[end] == ' ')
        {
            end--;
        }

        if (end < 0)
        {
            return "";
        }

        int start = end;
        // Walk back through word characters, dots, colons (for global::), and closing parens
        while (start > 0 && (char.IsLetterOrDigit(text[start - 1]) || text[start - 1] == '.' || text[start - 1] == '_' || text[start - 1] == ')' || text[start - 1] == ':'))
        {
            start--;
        }

        return text.Substring(start, end - start + 1);
    }

    private static string GetTokenAfter(string text, int index)
    {
        int start = index + 1;
        // Skip whitespace
        while (start < text.Length && text[start] == ' ')
        {
            start++;
        }

        if (start >= text.Length)
        {
            return "";
        }

        int end = start;
        // Walk forward through word characters, dots, and opening parens
        while (end + 1 < text.Length && (char.IsLetterOrDigit(text[end + 1]) || text[end + 1] == '.' || text[end + 1] == '_' || text[end + 1] == '('))
        {
            end++;
        }

        return text.Substring(start, end - start + 1);
    }

    private static bool IsNumericConstant(string token)
    {
        if (token.Length == 0)
        {
            return false;
        }

        // Handle tokens that might have trailing/leading parens
        string trimmed = token.TrimStart('(').TrimEnd(')');
        if (trimmed.Length == 0)
        {
            return false;
        }

        return char.IsDigit(trimmed[0]) || (trimmed[0] == '.' && trimmed.Length > 1 && char.IsDigit(trimmed[1]));
    }

    private static string ReplacePathSlashes(string text)
    {
        var result = new System.Text.StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '/')
            {
                if (IsDivisionSlash(text, i))
                {
                    result.Append('/');
                }
                else
                {
                    result.Append('\u1234');
                }
            }
            else
            {
                result.Append(text[i]);
            }
        }

        return result.ToString();
    }

    public static string ConvertToSlashSyntax(string cSharp)
    {
        var convertedText = cSharp.Replace('\u1234', '/')
            .Replace("global::Components.", "Components/")
            .Replace("global::Screens.", "Screens/")
            .Replace("global::Standards.", "Standards/");

        return convertedText;
    }

    public static void ConvertGlobalToElementNameWithSlashes(string rightSideToEvaluate, out string elementName, out string elementType)
    {
        elementName = null;
        elementType = null;
        if (rightSideToEvaluate.StartsWith("global::Components."))
        {
            elementType = "Components";
        }
        else if (rightSideToEvaluate.StartsWith("global::Screens."))
        {
            elementType = "Screens";
        }
        else if (rightSideToEvaluate.StartsWith("global::Standards."))
        {
            elementType = "Standards";
        }
        if (elementType != null)
        {
            elementName = rightSideToEvaluate.Substring($"global::{elementType}.".Length);
            var nextDot = elementName.IndexOf('.');
            if (nextDot != -1)
            {
                elementName = elementName.Substring(0, nextDot);
                elementName = elementName.Replace('\u1234', '/');
            }
        }
    }


    #endregion

    public bool CastTo(string desiredType)
    {
        if(desiredType == this.EvaluatedType)
        {
            return true;
        }

        if(this.EvaluatedType?.Contains(".") == true &&
            this.EvaluatedType.EndsWith("." + desiredType))
        {
            // Assume this is qualified and we want
            // to un-qualify it.
            this.EvaluatedType = desiredType;
            return true;
        }

        switch (desiredType)
        {
            case "int":
                if (this.EvaluatedType == "float")
                {
                    this.Value = (int)(float)this.Value;
                    this.EvaluatedType = desiredType;
                    return true;
                }
                else if(this.EvaluatedType == "double")
                {
                    this.Value = (int)(double)this.Value;
                    this.EvaluatedType = desiredType;
                    return true;
                }
                break;
            case "float":
                if(this.EvaluatedType == "int")
                {
                    this.Value = (float)(int)this.Value;
                    this.EvaluatedType = desiredType;
                    return true;
                }
                if (this.EvaluatedType == "double")
                {
                    this.Value = (float)(double)this.Value;
                    this.EvaluatedType = desiredType;
                    return true;
                }
                break;
            case "double":
                if (this.EvaluatedType == "int")
                {
                    this.Value = (double)(int)this.Value;
                    this.EvaluatedType = desiredType;
                    return true;
                }
                if (this.EvaluatedType == "float")
                {
                    this.Value = (double)(float)this.Value;
                    this.EvaluatedType = desiredType;
                    return true;
                }
                break;
            case "string":
                this.Value = this.Value?.ToString();
                this.EvaluatedType = desiredType;
                return true;
        }

        return false;
    }
}
