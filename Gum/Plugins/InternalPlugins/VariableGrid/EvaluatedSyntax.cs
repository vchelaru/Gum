using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Gum.Plugins.InternalPlugins.VariableGrid;
internal class EvaluatedSyntax
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

            if (childNodes == null || childNodes.Count() == 0) return null;

            foreach (var item in childNodes)
            {
                var evaluatedSyntax = Evaluate(item, stateForUnqualifiedRightSide);
                if (evaluatedSyntax != null)
                {
                    return evaluatedSyntax;
                }
            }

        }
        else if (syntaxNode is IdentifierNameSyntax identifierNameSyntax)
        {
                            var rfv = new RecursiveVariableFinder(stateForUnqualifiedRightSide);

            var value = rfv.GetValue(identifierNameSyntax.ToString());

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
        else if (syntaxNode is LiteralExpressionSyntax literalExpression)
        {
            var value = literalExpression.Token.Value;

            return FromSyntaxAndValue(syntaxNode, value);
        }
        else if (syntaxNode is GlobalStatementSyntax globalStatementSyntax)
        {
            var statement = globalStatementSyntax.Statement;

            var childNodes = statement?.ChildNodes();

            if (childNodes == null || childNodes.Count() == 0) return null;

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

            if (childNodes.Count() == 0) return null;

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
        if (leftEvaluated?.Value == null || rightEvaluated?.Value == null) return null;

        dynamic dynamicValue1, dynamicValue2;
        GetDynamicValues(leftEvaluated.Value, rightEvaluated.Value, out dynamicValue1, out dynamicValue2);
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
            return dynamicValue1 / dynamicValue2;
        }
        else
        {
            System.Diagnostics.Debugger.Break();
        }

        return null;
    }

    private static void GetDynamicValues(object obj1, object obj2, out dynamic dynamicValue1, out dynamic dynamicValue2)
    {
        var type1 = GetNumericType(obj1);
        var type2 = GetNumericType(obj2);

        // Find the larger (wider) type to ensure proper addition
        var targetType = GetWiderNumericType(type1, type2);

        // Convert both numbers to the wider type
        var value1 = Convert.ChangeType(obj1, targetType);
        var value2 = Convert.ChangeType(obj2, targetType);

        // Perform the addition
        dynamicValue1 = value1;
        dynamicValue2 = value2;
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

    static Type GetNumericType(object obj)
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj));

        var type = obj.GetType();

        if (!IsNumericType(type))
            throw new ArgumentException($"Object of type {type} is not a numeric type.");

        return type;
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

        // any additional slashes are part of the name, so we want to replace those with something
        // that still evaluates to a variable in C#, but which can be differentiated from a period, or
        // anything else the user might type
        lineOfText = lineOfText.Replace('/', '\u1234');


        return lineOfText;
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
        }

        return false;
    }
}
