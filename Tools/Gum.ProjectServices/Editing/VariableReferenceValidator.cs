using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RenderingLibrary.Graphics;
using ToolsUtilities;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Checks <c>VariableReferences</c> lines the way the tool does when they are typed: each line must parse as an
/// assignment to a variable the owner has, with a right side that evaluates to a compatible type.
/// Shared by the tool's Variables tab and <c>gumcli edit</c>.
/// </summary>
public interface IVariableReferenceValidator
{
    /// <summary>
    /// The lines of <paramref name="lines"/> that are invalid, each with why. Comments and empty lines are skipped.
    /// </summary>
    /// <param name="parentElement">The element whose state holds the lines.</param>
    /// <param name="leftSideInstance">The instance that owns the lines, or null for the element's own lines.</param>
    /// <param name="lines">The lines to check.</param>
    /// <param name="liveRoot">The element's displayed tree, so <c>Absolute*</c> reads resolve; null when nothing is displayed.</param>
    List<(string Line, GeneralResponse Response)> GetFailures(ElementSave parentElement, InstanceSave? leftSideInstance,
        IEnumerable<string> lines, GraphicalUiElement? liveRoot);

    /// <summary>
    /// Rewrites shorthand lines in place the way the Variables tab does: <c>Other.X</c> becomes <c>X = Other.X</c>,
    /// and a qualified left side keeps only its last part.
    /// </summary>
    void NormalizeLines(List<string> lines);
}

/// <inheritdoc/>
public class VariableReferenceValidator : IVariableReferenceValidator
{
    /// <summary>The assignment in <paramref name="item"/> (slash element paths allowed), or null when it has none.</summary>
    public static AssignmentExpressionSyntax? ParseAssignment(string item)
    {
        item = EvaluatedSyntax.ConvertToCSharpSyntax(item);

        var sourceNode = CSharpSyntaxTree.ParseText(item).GetCompilationUnitRoot();

        var assignment = GetAssignmentRoot(sourceNode);
        return assignment;
    }

    private static AssignmentExpressionSyntax? GetAssignmentRoot(SyntaxNode sourceNode)
    {

        if(sourceNode is AssignmentExpressionSyntax assignment)
        {
            return assignment;
        }
        else
        {
            var children = sourceNode.ChildNodes();

            foreach(var child in children)
            {
                var asAssignment = GetAssignmentRoot(child);
                if(asAssignment != null)
                {
                    return asAssignment;
                }
            }
        }
        return null;

    }

    #region Validation (failures)

    /// <inheritdoc/>
    public List<(string Line, GeneralResponse Response)> GetFailures(ElementSave parentElement, InstanceSave? leftSideInstance,
        IEnumerable<string> lines, GraphicalUiElement? liveRoot)
    {
        var failures = new List<(string, GeneralResponse)>();

        foreach (string line in lines)
        {
            AddFailureForLine(parentElement, leftSideInstance, failures, line, liveRoot);
        }

        return failures;
    }

    private void AddFailureForLine(ElementSave parentElement, InstanceSave? leftSideInstance, List<(string, GeneralResponse)> failures, string line, GraphicalUiElement? liveRoot)
    {
        if (line.StartsWith("//") || string.IsNullOrEmpty(line))
        {
            return;
        }

        ElementSave? channelOwner = leftSideInstance != null ? ObjectFinder.Self.GetElementSave(leftSideInstance) : parentElement;
        List<string> expandedLines = ElementSaveExtensions.ExpandCompositeReferenceLine(line, channelOwner).ToList();
        if (expandedLines.Count > 1)
        {
            // Composite reference (e.g. "Color = Other.Color") - "Color" is never a real
            // StateSave variable on either side, so validate each underlying channel line
            // independently instead. Report a single failure against the original collapsed
            // line (so CommentFailures can find and comment it out) if any channel is invalid.
            foreach (string expandedLine in expandedLines)
            {
                var channelFailures = new List<(string, GeneralResponse)>();
                AddFailureForLine(parentElement, leftSideInstance, channelFailures, expandedLine, liveRoot);
                if (channelFailures.Count > 0)
                {
                    failures.Add((line, channelFailures[0].Item2));
                    return;
                }
            }
            return;
        }

        // Failures are reported against the original line (so CommentFailures can find it), but the
        // expression is validated with "@" already resolved to the owning instance.
        var assignmentSyntax = ParseAssignment(
            ElementSaveExtensions.ResolveOwnerPrefix(line, leftSideInstance?.Name, ElementSaveExtensions.GetReferencableInstanceNames(parentElement)));

        if (assignmentSyntax == null)
        {
            failures.Add((line, GeneralResponse.UnsuccessfulWith("Could not parse line. This should be an assignment such as X=Y")));
            return;
        }

        var leftSide = assignmentSyntax.Left?.ToString();

        if(leftSide is "Name" or "BaseType" or "DefaultChildContainer")
        {
            failures.Add((line, GeneralResponse.UnsuccessfulWith($"{leftSide} cannot be assigned in variable references")));
            return;
        }

        var leftSideResponse =
            CheckLeftSideVariableExistence(parentElement, leftSideInstance, assignmentSyntax);

        // A successful response always carries the variable.
        if (leftSideResponse.Succeeded == false || leftSideResponse.Data is not { } leftSideVariable)
        {
            failures.Add((line, leftSideResponse));
            return;
        }

        if (EvaluatedSyntax.GetFunctionCallProblem(assignmentSyntax.Right) is { } functionProblem)
        {
            failures.Add((line, GeneralResponse.UnsuccessfulWith(functionProblem)));
            return;
        }

        EvaluatedSyntax? evaluatedSyntax = EvaluatedSyntax.FromSyntaxNode(assignmentSyntax.Right, ElementSaveExtensions.WithFormsPropertyInputs(parentElement.GetDefaultStateOrThrow(), assignmentSyntax.Right.ToString(), liveRoot), liveRoot: liveRoot);

        if (evaluatedSyntax == null)
        {
            // An unknown element ("Components/Nope.Width") fails before any type is known; name it instead of echoing the parsed syntax.
            failures.Add((line, GeneralResponse.UnsuccessfulWith(
                DescribeMissingReference(parentElement, leftSideInstance?.Name, line)?.EditMessage
                ?? $"Could not evaluate right-side expression {assignmentSyntax}")));
            return;
        }

        if (evaluatedSyntax.EvaluatedType == null)
        {
            failures.Add((line, GeneralResponse.UnsuccessfulWith(
                DescribeRightSideFailure(assignmentSyntax.Right, parentElement.GetDefaultStateOrThrow(), liveRoot, parentElement, leftSideInstance?.Name, line).EditMessage)));
            return;
        }

        if (IsCategoryStateLeftSide(leftSideVariable, parentElement, leftSideInstance, out _))
        {
            // Category-state LHS: cast to string so any string-producing RHS (literal,
            // ternary, expression) is accepted; CheckIfVariableTypesMatch enforces the
            // string-only rule and (for literals) the state-name existence check.
            if (!evaluatedSyntax.CastTo("string"))
            {
                failures.Add((line, GeneralResponse.UnsuccessfulWith(
                    $"Could not cast {evaluatedSyntax.EvaluatedType} to string for category-state assignment")));
                return;
            }
        }
        else if (!evaluatedSyntax.CastTo(leftSideVariable.Type))
        {
            failures.Add((line, GeneralResponse.UnsuccessfulWith(
                $"Could not cast {evaluatedSyntax.EvaluatedType} to {leftSideVariable.Type}")));
            return;
        }

        var typeMatchResponse = CheckIfVariableTypesMatch(leftSideVariable, parentElement, leftSideInstance, evaluatedSyntax);

        if (typeMatchResponse.Succeeded == false)
        {
            failures.Add((line, typeMatchResponse));
        }
    }

    /// <summary>Why a right side has no value: the edit-time dialog's message and a short reason for the Variables tab note.</summary>
    public sealed record RightSideFailure(string EditMessage, string NoteReason);

    /// <summary>
    /// Explains why <paramref name="right"/> evaluated to nothing. The one source of the wording shared by the
    /// edit-time dialog and the note on a row that stopped following its reference.
    /// </summary>
    public static RightSideFailure DescribeRightSideFailure(SyntaxNode right, StateSave state, GraphicalUiElement? liveRoot, ElementSave owner, string? ownerInstanceName, string line)
    {
        // The missing part comes first: it is the most specific cause, and the layout and divide-by-zero
        // messages below would otherwise hide a misspelled instance or variable.
        if (DescribeMissingReference(owner, ownerInstanceName, line) is { } missing)
        {
            return missing;
        }

        if (right.DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>()
            .Any(identifier => EvaluatedSyntax.IsLiveLayoutName(identifier.Identifier.Text)))
        {
            return new RightSideFailure(
                "The right side reads a value that is computed from the layout (such as Index or AbsoluteWidth). " +
                "Check that the instance exists in this element; Absolute* values also need the element to be displayed",
                "it reads a value computed from the layout, which is not available");
        }

        SyntaxNode? zeroDivisor = FindZeroDivisor(right, state, liveRoot);
        if (zeroDivisor != null)
        {
            string divisor = EvaluatedSyntax.ConvertToSlashSyntax(zeroDivisor.ToString());
            return new RightSideFailure(
                "The right side divides by zero. Check the divisor (the value after / or %), including variables that are currently 0",
                zeroDivisor is LiteralExpressionSyntax ? "divides by zero" : $"{divisor} is 0, so the reference can't divide by it");
        }

        return new RightSideFailure(
            "The right side cannot be evaluated, are you referencing a variable that doesn't exist or mixing variable types?",
            "it cannot be evaluated (a variable it reads is missing, or the types do not mix)");
    }

    /// <summary>
    /// Names the first path the right side of <paramref name="line"/> reads that the project does not have, using the
    /// same check as the GUM0009 error so the dialog, the row note and the error list agree. "First" means the first
    /// qualified element path (<c>Components/Name.Variable</c>) in reading order, else the first local path. With several
    /// missing names only that one is reported; the next appears once it is fixed. Null when every path exists.
    /// </summary>
    private static RightSideFailure? DescribeMissingReference(ElementSave owner, string? ownerInstanceName, string line)
    {
        string? problem = HeadlessErrorChecker.FindUnresolvableReference(owner, ownerInstanceName, line);
        return problem == null
            ? null
            : new RightSideFailure($"The right side reads {problem}.", $"it reads {problem}");
    }

    private static SyntaxNode? FindZeroDivisor(SyntaxNode rightSide, StateSave state, GraphicalUiElement? liveRoot)
    {
        foreach (BinaryExpressionSyntax binary in rightSide.DescendantNodesAndSelf().OfType<BinaryExpressionSyntax>())
        {
            if (!binary.OperatorToken.IsKind(SyntaxKind.SlashToken) && !binary.OperatorToken.IsKind(SyntaxKind.PercentToken))
            {
                continue;
            }

            // A division inside a ternary arm may be guarded, so its failure is not evidence of the cause.
            if (IsInsideConditionalArm(binary))
            {
                continue;
            }

            object? divisor = EvaluatedSyntax.FromSyntaxNode(binary.Right, ElementSaveExtensions.WithFormsPropertyInputs(state, binary.Right.ToString(), liveRoot), liveRoot: liveRoot)?.Value;
            if (divisor is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal
                && Convert.ToDouble(divisor) == 0)
            {
                return binary.Right;
            }
        }

        return null;
    }

    private static bool IsInsideConditionalArm(SyntaxNode node)
    {
        SyntaxNode child = node;
        while (child.Parent is { } parent)
        {
            if (parent is ConditionalExpressionSyntax conditional && child != conditional.Condition)
            {
                return true;
            }
            child = parent;
        }

        return false;
    }

    private GeneralResponse CheckIfVariableTypesMatch(VariableSave leftSideVariable, ElementSave parentElement, InstanceSave? leftSideInstance, EvaluatedSyntax assignment)
    {
        var leftSideType = leftSideVariable.Type;
        string? rightSideType = null;
        // get the right side type to compare:
        var ownerOfRightSideVariable = parentElement.DefaultState;

        rightSideType = assignment.EvaluatedType;

        // Category-state synthetic LHS: leftSideType is the category name (e.g. "ButtonCategory")
        // and we expect a string RHS naming a state in that category. If the RHS is a string
        // literal we additionally verify it names an existing state; if it is a non-literal
        // (ternary, expression) we accept and rely on runtime tolerance (ApplyState no-ops on
        // unknown state names).
        if (IsCategoryStateLeftSide(leftSideVariable, parentElement, leftSideInstance, out var category))
        {
            if (rightSideType != "string")
            {
                return GeneralResponse.UnsuccessfulWith(
                    $"Left side is a state of category [{category.Name}] but right side is of type [{rightSideType}]");
            }

            var isStringLiteral = assignment.SyntaxNode is LiteralExpressionSyntax literal
                && literal.IsKind(SyntaxKind.StringLiteralExpression);

            if (isStringLiteral && assignment.Value is string stateName)
            {
                bool stateExists = category.States.Any(s => s.Name == stateName);
                if (!stateExists)
                {
                    return GeneralResponse.UnsuccessfulWith(
                        $"Category [{category.Name}] has no state named [{stateName}]");
                }
            }

            return GeneralResponse.SuccessfulResponse;
        }

        var areEqual = rightSideType == leftSideType;

        if(!areEqual && rightSideType?.Contains(".") == true)
        {
            // right side is qualified. let's compare unqualified
            var afterLastDot = rightSideType.Substring(rightSideType.LastIndexOf(".") + 1);

            areEqual = afterLastDot == leftSideType;
        }

        if (!areEqual)
        {
            return GeneralResponse.UnsuccessfulWith($"Left side is of type [{leftSideType}] but right side is of type [{rightSideType}]");
        }

        // special case, some variable types like XUnits vs YUnits use the same 
        // type even though they shouldn't be mixed, so we need to compare the root
        // variable:
        var shouldTryMatchingRoots = leftSideType is
            nameof(HorizontalAlignment) or
            nameof(VerticalAlignment) or
            nameof(PositionUnitType) or
            nameof(DimensionUnitType);

        if(shouldTryMatchingRoots)
        {
            var leftSideQualified = leftSideInstance == null
                ? leftSideVariable.Name
                : leftSideInstance.Name + "." + leftSideVariable.Name;

            var leftSideRoot = ObjectFinder.Self.GetRootVariable(leftSideQualified, parentElement);
            VariableSave? rightSideRoot = null;
            // EvaluatedSyntax.FromSyntaxNode always sets SyntaxNode; it is nullable only because
            // the property is publicly settable.
            string rightSide = ElementSaveExtensions.DecodeOwnerName(assignment.SyntaxNode?.ToString() ?? string.Empty);

            if(rightSide.Contains("global::"))
            {
                EvaluatedSyntax.ConvertGlobalToElementNameWithSlashes(rightSide, out string? elementName, out string? elementType);

                var element = ObjectFinder.Self.GetElementSave(elementName);
                if(element != null)
                {
                    rightSide =  rightSide.Substring(($"global::{elementType}." + elementName).Length + 1);

                    rightSideRoot = ObjectFinder.Self.GetRootVariable(rightSide, element);
                }
            }

            else
            {
                // it's a variable in this element
                rightSideRoot = ObjectFinder.Self.GetRootVariable(rightSide, parentElement);
            }
            // see if this is a simple type:

            if(leftSideRoot != null && rightSideRoot != null && leftSideRoot.Name != rightSideRoot.Name)
            {
                // we found both, they don't match, don't allow it:
                return GeneralResponse.UnsuccessfulWith($"Left side is [{leftSideRoot.Name}] but right side is [{rightSideRoot.Name}]");
            }
        }

        return GeneralResponse.SuccessfulResponse;
    }

    private GeneralResponse<VariableSave> CheckLeftSideVariableExistence(ElementSave parentElement, InstanceSave? instance, AssignmentExpressionSyntax syntax)
    {
        var element = instance != null ? ObjectFinder.Self.GetElementSave(instance) : parentElement;

        var leftSide = syntax.Left.ToString();

        // An instance whose type is missing has no variables to assign.
        if (element == null)
        {
            return GeneralResponse<VariableSave>.UnsuccessfulWith($"Could not find variable [{leftSide}]");
        }

        var rootVar = ObjectFinder.Self.GetRootVariable(leftSide, element);

        if (rootVar == null && instance != null)
        {
            // A shader parameter is declared by no type; the container's saved entry defines it.
            rootVar = ObjectFinder.Self.GetShaderParameterDefinition(instance.Name + "." + leftSide, parentElement);
        }

        if (rootVar == null)
        {
            // Synthetic category-state LHS: "<CategoryName>State" assigns a state from the
            // matching StateSaveCategory. The variable does not exist on DefaultState but the
            // runtime routes the assignment through GraphicalUiElement.SetProperty.
            var category = FindCategoryForStateLeftSide(element, leftSide);
            if (category != null)
            {
                var syntheticVariable = new VariableSave
                {
                    Name = leftSide,
                    Type = category.Name,
                    SetsValue = true
                };
                var success = GeneralResponse<VariableSave>.SuccessfulResponse;
                success.Data = syntheticVariable;
                return success;
            }
        }

        if(rootVar == null)
        {
            if (EvaluatedSyntax.IsLiveLayoutName(leftSide))
            {
                return GeneralResponse<VariableSave>.UnsuccessfulWith(
                    $"{leftSide} is computed from the layout and cannot be assigned in variable references");
            }

            return GeneralResponse<VariableSave>.UnsuccessfulWith($"Could not find variable [{leftSide}]");
        }

        var toReturn = GeneralResponse<VariableSave>.SuccessfulResponse;
        toReturn.Data = rootVar;
        return toReturn;
    }

    /// <summary>
    /// Looks for a StateSaveCategory on <paramref name="element"/> (or its base-element
    /// chain) whose name + "State" matches the <paramref name="leftSide"/> token. Returns
    /// null when no match is found.
    /// </summary>
    private static StateSaveCategory? FindCategoryForStateLeftSide(ElementSave element, string leftSide)
    {
        if (string.IsNullOrEmpty(leftSide) || !leftSide.EndsWith("State"))
        {
            return null;
        }

        var categoryName = leftSide.Substring(0, leftSide.Length - "State".Length);
        if (categoryName.Length == 0)
        {
            return null;
        }

        // Walk this element + its inheritance chain looking for a matching category.
        var match = element.Categories?.FirstOrDefault(c => c.Name == categoryName);
        if (match != null)
        {
            return match;
        }

        var baseElements = ObjectFinder.Self.GetBaseElements(element);
        foreach (var baseElement in baseElements)
        {
            match = baseElement.Categories?.FirstOrDefault(c => c.Name == categoryName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns true if <paramref name="leftSideVariable"/> is the synthetic placeholder
    /// produced by <see cref="CheckLeftSideVariableExistence"/> for a "&lt;CategoryName&gt;State"
    /// assignment, and outputs the resolved category.
    /// </summary>
    private static bool IsCategoryStateLeftSide(VariableSave? leftSideVariable, ElementSave parentElement, InstanceSave? leftSideInstance, [NotNullWhen(true)] out StateSaveCategory? category)
    {
        category = null;
        if (leftSideVariable?.Name == null || !leftSideVariable.Name.EndsWith("State"))
        {
            return false;
        }

        var element = leftSideInstance != null ? ObjectFinder.Self.GetElementSave(leftSideInstance) : parentElement;
        if (element == null)
        {
            return false;
        }

        category = FindCategoryForStateLeftSide(element, leftSideVariable.Name);
        return category != null;
    }

    #endregion

    #region Line normalization

    private static readonly char[] equalsArray = new char[] { '=' };

    /// <inheritdoc/>
    public void NormalizeLines(List<string> lines)
    {
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var item = lines[i];

            if (item.TrimStart().StartsWith("//"))
            {
                continue;
            }

            var syntax = CSharpSyntaxTree.ParseText(item).GetCompilationUnitRoot();
            var assignment = syntax.DescendantNodes().FirstOrDefault(item => item is AssignmentExpressionSyntax) as AssignmentExpressionSyntax;

            string[] split;
            if (assignment == null)
            {
                split = new string[] { item.Trim() };
            }
            else
            {
                split = new string[]
                {
                    assignment.Left.ToString(),
                    assignment.Right.ToString()
                };
            }

            if (split.Length == 1)
            {
                split = AddImpliedLeftSide(lines, i, split);
            }

            if (split.Length > 1)
            {
                var leftSide = split[0];
                if (leftSide.Contains("."))
                {
                    var lastDot = leftSide.LastIndexOf('.');
                    var unqualifiedLeft = leftSide.Substring(lastDot + 1);
                    lines[i] = unqualifiedLeft + "=" + split[1];
                }
            }
        }
    }

    private static string[] AddImpliedLeftSide(List<string> asList, int i, string[] split)
    {
        // There is no left side, just a right side such as "Other.X", which assigns X.
        var rightSide = split[0];
        var afterDot = rightSide.Substring(rightSide.LastIndexOf('.') + 1);

        if (rightSide.Contains("."))
        {
            asList[i] = $"{afterDot} = {rightSide}";

            split = asList[i]
                .Split(equalsArray, StringSplitOptions.RemoveEmptyEntries)
                .Select(stringItem => stringItem.Trim()).ToArray();
        }
        return split;
    }

    #endregion
}
