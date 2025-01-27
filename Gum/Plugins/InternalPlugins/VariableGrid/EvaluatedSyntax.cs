using Gum.DataTypes;
using Gum.DataTypes.Variables;
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
    public string EvaluatedType { get; set; }
    public SyntaxNode SyntaxNode { get; set; }

    public object Value { get; set; }

    public override string ToString()
    {
        if(SyntaxNode == null)
        {
            return "<no syntax>";
        }
        else
        {
            return $"{SyntaxNode.ToString()} = {Value}";
        }
    }

    public static EvaluatedSyntax FromSyntaxNode(SyntaxNode syntaxNode, StateSave stateForUnqualifiedRightSide)
    {
        return Evaluate(syntaxNode, stateForUnqualifiedRightSide);
    }

    private static EvaluatedSyntax Evaluate(SyntaxNode syntaxNode, StateSave stateForUnqualifiedRightSide)
    {
        if(syntaxNode is BinaryExpressionSyntax binaryExpressionSytax)
        {
            var leftSyntax = binaryExpressionSytax.Left;
            var rightSyntax = binaryExpressionSytax.Right;

            var leftEvaluated = Evaluate(leftSyntax, stateForUnqualifiedRightSide);
            var rightEvaluated = Evaluate(rightSyntax, stateForUnqualifiedRightSide);

            var value = Combine(leftEvaluated, rightEvaluated, binaryExpressionSytax.OperatorToken);

            return FromSyntaxAndValue(syntaxNode, value);
        }
        else if(syntaxNode is ParenthesizedExpressionSyntax parenthesizedExpressionSyntax)
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
        if (syntaxNode is MemberAccessExpressionSyntax memberAccess)
        {
            // we just need to evaluate the right-side
            var rightSideToEvaluate = memberAccess.ToString();

            var rfv = new RecursiveVariableFinder(stateForUnqualifiedRightSide);

            var value = rfv.GetValue(rightSideToEvaluate);

            return FromSyntaxAndValue(syntaxNode, value);

        }
        else if(syntaxNode is LiteralExpressionSyntax literalExpression)
        {
            var value = literalExpression.Token.Value;

            return FromSyntaxAndValue(syntaxNode, value);
        }
        else if(syntaxNode is GlobalStatementSyntax globalStatementSyntax)
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
        else if(syntaxNode is CompilationUnitSyntax compilationUnitSyntax)
        {
            var childNodes = compilationUnitSyntax.ChildNodes();

            if (childNodes.Count() == 0) return null;

            foreach(var item in childNodes)
            {
                var evaluatedSyntax = Evaluate(item, stateForUnqualifiedRightSide);
                if(evaluatedSyntax != null)
                {
                    return evaluatedSyntax;
                }
            }
        }
        return null;
    }

    private static object Combine(EvaluatedSyntax leftEvaluated, EvaluatedSyntax rightEvaluated, SyntaxToken operatorToken)
    {
        dynamic dynamicValue1, dynamicValue2;
        GetDynamicValues(leftEvaluated.Value, rightEvaluated.Value, out dynamicValue1, out dynamicValue2);
        if(operatorToken.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PlusToken))
        {
            return dynamicValue1 + dynamicValue2;
        }
        else if(operatorToken.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.MinusToken))
        {
            return dynamicValue1 - dynamicValue2;
        }
        else if(operatorToken.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.AsteriskToken))
        {
            return dynamicValue1 * dynamicValue2;
        }
        else if(operatorToken.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SlashToken))
        {
            return dynamicValue1 / dynamicValue2;
        }

        return null;
    }



    static object AddNumbers(object obj1, object obj2)
    {
        // Get the types of the objects
        dynamic dynamicValue1, dynamicValue2;
        GetDynamicValues(obj1, obj2, out dynamicValue1, out dynamicValue2);
        return dynamicValue1 + dynamicValue2;
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














    private static EvaluatedSyntax FromSyntaxAndValue(SyntaxNode syntaxNode, object value)
    {
        var toReturn = new EvaluatedSyntax();
        toReturn.SyntaxNode = syntaxNode;
        toReturn.Value = value;

        toReturn.EvaluatedType = value is float ? "float"
            : value is string ? "string"
            : value is bool ? "bool"
            : value?.GetType().ToString();
        return toReturn;
    }
}
