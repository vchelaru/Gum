using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumRuntime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RenderingLibrary.Graphics;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Management.Instrumentation;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid;
public class VariableReferenceLogic
{
    #region Fields/Properties

    private readonly GuiCommands _guiCommands;
    private readonly WireframeCommands _wireframeCommands;
    private readonly IDialogService _dialogService;
    private readonly FileCommands _fileCommands;

    #endregion

    public VariableReferenceLogic(GuiCommands guiCommands, 
        WireframeCommands wireframeCommands, 
        IDialogService dialogService,
        FileCommands fileCommands)
    {
        _guiCommands = guiCommands;
        _wireframeCommands = wireframeCommands;
        _dialogService =  dialogService;
        _fileCommands = fileCommands;
    }

    public AssignmentExpressionSyntax GetAssignmentSyntax(string item)
    {
        item = EvaluatedSyntax.ConvertToCSharpSyntax(item);

        var sourceNode = CSharpSyntaxTree.ParseText(item).GetCompilationUnitRoot();

        var assignment = GetAssignmentRoot(sourceNode);
        return assignment;
    }

    private static AssignmentExpressionSyntax GetAssignmentRoot(SyntaxNode sourceNode)
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

    private List<(string, GeneralResponse)> GetIndividualFailures(ElementSave parentElement, InstanceSave leftSideInstance, VariableListSave newDirectValue)
    {
        var values = newDirectValue.ValueAsIList;

        var failures = new List<(string, GeneralResponse)>();

        foreach (string line in values)
        {
            AddFailureForLine(parentElement, leftSideInstance, failures, line);
        }

        return failures;
    }

    private void AddFailureForLine(ElementSave parentElement, InstanceSave leftSideInstance, List<(string, GeneralResponse)> failures, string line)
    {
        if (line.StartsWith("//") || string.IsNullOrEmpty(line))
        {
            return;
        }

        var assignmentSyntax = GetAssignmentSyntax(line);

        GeneralResponse response = GeneralResponse.SuccessfulResponse;

        if (assignmentSyntax == null)
        {
            response = GeneralResponse.UnsuccessfulWith("Could not parse line. This should be an assignment such as X=Y");
        }

        VariableSave leftSideVariable = null;

        if(response.Succeeded)
        {
            var leftSide = assignmentSyntax.Left?.ToString();

            if(leftSide is "Name" or "BaseType" or "DefaultChildContainer")
            {
                response = GeneralResponse.UnsuccessfulWith($"{leftSide} cannot be assigned in variable references");
            }
        }

        if (response.Succeeded)
        {
            var leftSideResponse =
                CheckLeftSideVariableExistence(parentElement, leftSideInstance, assignmentSyntax);

            if (leftSideResponse.Succeeded == false)
            {
                response = leftSideResponse;
            }
            else
            {
                leftSideVariable = leftSideResponse.Data;
            }
        }

        EvaluatedSyntax evaluatedSyntax = null;

        if (response.Succeeded)
        {
            evaluatedSyntax = EvaluatedSyntax.FromSyntaxNode(assignmentSyntax.Right, parentElement.DefaultState);

            if (evaluatedSyntax == null)
            {
                response = GeneralResponse.UnsuccessfulWith($"Could not evaluate right-side expression {assignmentSyntax}");
            }
        }

        if (response.Succeeded && evaluatedSyntax.EvaluatedType == null)
        {
            response = GeneralResponse.UnsuccessfulWith(
                $"The right side cannot be evaluated, are you referencing a variable that doesn't exist or mixing variable types?");
        }

        if (response.Succeeded && !evaluatedSyntax.CastTo(leftSideVariable.Type))
        {
            response = GeneralResponse.UnsuccessfulWith(
                $"Could not cast {evaluatedSyntax.EvaluatedType} to {leftSideVariable.Type}");
        }

        if (response.Succeeded)
        {
            var typeMatchResponse = CheckIfVariableTypesMatch(leftSideVariable, parentElement, leftSideInstance, evaluatedSyntax);

            if (typeMatchResponse.Succeeded == false)
            {
                response = typeMatchResponse;
            }
        }

        if (response.Succeeded == false)
        {
            failures.Add((line, response));
        }
    }

    private static void CommentFailures(VariableListSave newDirectValue, List<(string, GeneralResponse)> failures)
    {
        foreach (var failure in failures)
        {

            var count = newDirectValue.ValueAsIList.Count;

            // find the failure, comment it out:
            for (int i = 0; i < count; i++)
            {
                if (newDirectValue.ValueAsIList[i] as string == failure.Item1)
                {
                    newDirectValue.ValueAsIList[i] = "//" + newDirectValue.ValueAsIList[i];
                }
            }
        }
    }

    private void ShowFailureMessage(List<(string, GeneralResponse)> failures)
    {
        if (failures.Count > 0)
        {
            string message = "";

            foreach (var failure in failures)
            {
                var generalResponseMessage = failure.Item2.Message;
                if (string.IsNullOrEmpty(generalResponseMessage))
                {
                    message += $"[{failure.Item1}]\n";
                }
                else
                {
                    message += $"[{failure.Item1}] {failure.Item2.Message} \n";
                }
            }

            message += "\n\nInvalid lines will be commented out";

            _dialogService.ShowMessage(message, "Invalid Variable Reference");
        }
    }

    private GeneralResponse CheckIfVariableTypesMatch(VariableSave leftSideVariable, ElementSave parentElement, InstanceSave leftSideInstance, EvaluatedSyntax assignment)
    {
        var leftSideType = leftSideVariable.Type;
        string rightSideType = null;
        // get the right side type to compare:
        var ownerOfRightSideVariable = parentElement.DefaultState;

        rightSideType = assignment.EvaluatedType;

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
            VariableSave rightSideRoot = null;
            var rightSide = assignment.SyntaxNode.ToString();

            if(rightSide?.Contains("global::") == true)
            {
                EvaluatedSyntax.ConvertGlobalToElementNameWithSlashes(rightSide, out string elementName, out string elementType);

                var element = ObjectFinder.Self.GetElementSave(elementName);
                if(element != null)
                {
                    rightSide =  rightSide.Substring(($"global::{elementType}." + elementName).Length + 1);

                    rightSideRoot = ObjectFinder.Self.GetRootVariable(rightSide, element);
                }
            }

            else 
            {
                var unevaluated = 
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

    private GeneralResponse<VariableSave> CheckLeftSideVariableExistence(ElementSave parentElement, InstanceSave instance, AssignmentExpressionSyntax syntax)
    {
        var element = instance != null ? ObjectFinder.Self.GetElementSave(instance) : parentElement;

        var leftSide = syntax.Left.ToString();

        var rootVar = ObjectFinder.Self.GetRootVariable(leftSide, element);

        if(rootVar == null)
        {
            return GeneralResponse<VariableSave>.UnsuccessfulWith($"Could not find variable [{leftSide}]");
        }

        var toReturn = GeneralResponse<VariableSave>.SuccessfulResponse;
        toReturn.Data = rootVar;
        return toReturn;
    }

    #endregion

    #region Assignment Reactions


    public void DoVariableReferenceReaction(ElementSave parentElement, InstanceSave leftSideInstance, string unqualifiedMember,
        StateSave stateSave, string qualifiedName, bool trySave)
    {
        if (unqualifiedMember == "VariableReferences")
        {
            var newDirectValue = stateSave.GetVariableListSave(qualifiedName);

            if (newDirectValue != null)
            {
                var failures = GetIndividualFailures(parentElement, leftSideInstance, newDirectValue);

                if (failures.Count > 0)
                {
                    ShowFailureMessage(failures);

                    CommentFailures(newDirectValue, failures);
                }
            }
        }

        // apply references on this element first, then apply the values to the other references:
        ElementSaveExtensions.ApplyVariableReferences(parentElement, stateSave);

        if (unqualifiedMember == "VariableReferences")
        {
            _guiCommands.RefreshVariableValues();
        }

        // Oct 13, 2022
        // This should set 
        // values on all contained objects for this particular state
        // Maybe this could be slow? not sure, but this covers all cases so if
        // there are performance issues, will investigate later.
        var references = ObjectFinder.Self.GetElementReferencesToThis(parentElement);
        var filteredReferences = references
            .Where(item => item.ReferenceType == ReferenceType.VariableReference);

        HashSet<StateSave> statesAlreadyApplied = new HashSet<StateSave>();
        HashSet<ElementSave> elementsToSave = new HashSet<ElementSave>();
        foreach (var reference in filteredReferences)
        {
            if (statesAlreadyApplied.Contains(reference.StateSave) == false)
            {
                ElementSaveExtensions.ApplyVariableReferences(reference.OwnerOfReferencingObject, reference.StateSave);
                statesAlreadyApplied.Add(reference.StateSave);
                elementsToSave.Add(reference.OwnerOfReferencingObject);
            }
        }

        if (trySave)
        {
            foreach (var elementToSave in elementsToSave)
            {
                _fileCommands.TryAutoSaveElement(elementToSave);
            }
        }

        var newValue = stateSave.GetValueRecursive(qualifiedName);

        // this could be a tunneled variable. If so, we may need to propagate the value to other instances one level deeper
        var didSetDeepReference = DoVariableReferenceReactionOnInstanceVariableSet(parentElement, leftSideInstance, stateSave, unqualifiedMember, newValue);

        // now force save it if it's a variable reference:
        if (unqualifiedMember == "VariableReferences" && trySave)
        {
            _fileCommands.TryAutoSaveElement(parentElement);
        }

        // The MainEditorTabPlugin handles refreshing in most cases, but if there was a "deep" set due to variable references,
        // then we forcefully change the values here.
        if (didSetDeepReference)
        {
            _wireframeCommands.Refresh(forceLayout: true);
        }
    }

    private static bool DoVariableReferenceReactionOnInstanceVariableSet(ElementSave container, InstanceSave instance, StateSave stateSave, string unqualifiedVariableName, object newValue)
    {
        var didAssignDeepReference = false;
        ElementSave instanceElement = null;
        if (instance != null)
        {
            instanceElement = ObjectFinder.Self.GetElementSave(instance.BaseType);
        }
        if (instanceElement != null)
        {
            var variableOnInstance = instanceElement.GetVariableFromThisOrBase(unqualifiedVariableName);

            List<TypedElementReference> references = null;

            references = ObjectFinder.Self.GetElementReferencesToThis(instanceElement);
            var filteredReferences = references
                .Where(item => item.ReferenceType == ReferenceType.VariableReference)
                .ToArray();

            if (references != null && variableOnInstance != null)
            {
                foreach (var reference in filteredReferences)
                {
                    var variableListSave = reference.ReferencingObject as VariableListSave;

                    var stringList = variableListSave?.ValueAsIList as List<string>;

                    if (stringList != null)
                    {
                        foreach (var assignment in stringList)
                        {
                            if (assignment.Contains("="))
                            {
                                var leftSide = assignment.Substring(0, assignment.IndexOf("=")).Trim();
                                var rightSide = assignment.Substring(assignment.IndexOf("=") + 1).Trim();

                                var simplifiedRightSide = rightSide;

                                var instanceElementQualified = instanceElement.Name;
                                if (instanceElement is ComponentSave)
                                {
                                    instanceElementQualified = "Components/" + instanceElementQualified;
                                }
                                else if (instanceElement is ScreenSave)
                                {
                                    instanceElementQualified = "Screens/" + instanceElementQualified;
                                }
                                else if (instanceElement is StandardElementSave)
                                {
                                    instanceElementQualified = "StandardElements/" + instanceElementQualified;
                                }

                                if (rightSide.StartsWith(instanceElementQualified))
                                {
                                    simplifiedRightSide = rightSide.Substring(instanceElementQualified.Length
                                        // +1 to take off the period before the variable name
                                        + 1);
                                }

                                var didAssignReferencedVariable = simplifiedRightSide == variableOnInstance.Name ||
                                    simplifiedRightSide == variableOnInstance.ExposedAsName;

                                if (didAssignReferencedVariable)
                                {
                                    var ownerOfVariableReferenceName = variableListSave.SourceObject;

                                    var ownerOfVariableReferenceInstanceSave = instanceElement.GetInstance(ownerOfVariableReferenceName);

                                    if (ownerOfVariableReferenceInstanceSave != null)
                                    {
                                        // Setting this variable results in ownerOfVariableReferenceInstanceSave.leftSide also being assigned
                                        // but ownerOfVariableReferenceInstanceSave is inside of instanceElement, so we need to find all instances
                                        // of instanceElement in container, and assign those.
                                        var qualifiedNameInInstanceElement = ownerOfVariableReferenceInstanceSave.Name + "." + leftSide;
                                        //didAssignDeepReference = AssignInstanceValues(container, instanceElement, stateSave, qualifiedNameInInstanceElement, newValue) ||
                                        //    didAssignDeepReference;

                                        var deepQualifiedName = instance.Name + "." + qualifiedNameInInstanceElement;

                                        stateSave.SetValue(deepQualifiedName, newValue);
                                        didAssignDeepReference = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return didAssignDeepReference;
    }


    public void ReactIfChangedMemberIsVariableReference(ElementSave parentElement, InstanceSave instance, StateSave stateSave, string changedMember, object oldValue)
    {
        ///////////////////// Early Out/////////////////////////////////////
        if (changedMember != "VariableReferences") return;

        var changedMemberWithPrefix = changedMember;
        if (instance != null)
        {
            changedMemberWithPrefix = instance.Name + "." + changedMember;
        }

        var newValueAsList = stateSave.GetVariableListSave(changedMemberWithPrefix)?.ValueAsIList as List<string>;

        ///////////////////End Early Out/////////////////////////////////////

        bool didChange = ModifyLines(oldValue, newValueAsList, instance);


        if (didChange)
        {
            _guiCommands.RefreshVariables(force: true);
        }
    }

    #endregion

    #region Line Assignment Expansion / Modifications

    static char[] equalsArray = new char[] { '=' };
    bool ModifyLines(object oldValue, List<string> newValueAsList, InstanceSave selectedInstance)
    {
        var oldValueAsList = oldValue as List<string>;


        if (newValueAsList != null)
        {
            for (int i = newValueAsList.Count - 1; i >= 0; i--)
            {
                var item = newValueAsList[i];

                var syntax = CSharpSyntaxTree.ParseText(item).GetCompilationUnitRoot();
                var assignment = syntax.DescendantNodes().FirstOrDefault(item => item is AssignmentExpressionSyntax) as AssignmentExpressionSyntax;


                string[] split = new string[0];
                if(assignment == null)
                {
                    split = new string[] { item?.Trim() };
                }
                else
                {
                    split = new string[]
                    {
                        assignment.Left.ToString(),
                        assignment.Right.ToString()
                    };
                }

                if (split.Length == 0)
                {
                    continue;
                }

                if (split.Length == 1)
                {
                    split = AddImpliedLeftSide(newValueAsList, i, split);
                }

                if(split.Length > 1 && selectedInstance != null )
                {
                    // need to loop through each item and adjust its text...
                    QualifyInstanceVariables(newValueAsList, selectedInstance, i, split);
                }

                if (split.Length > 1)
                {
                    var leftSide = split[0];
                    if(leftSide.Contains("."))
                    {
                        var lastDot = leftSide.LastIndexOf('.');
                        var unqualifiedLeft = leftSide.Substring(lastDot + 1);
                        newValueAsList[i] = unqualifiedLeft + "=" + split[1];
                        split[0] = unqualifiedLeft;
                    }
                }

                if (split.Length == 2)
                {
                    var leftSide = split[0];
                    var rightSide = split[1];
                    if (leftSide == "Color" && rightSide.EndsWith(".Color"))
                    {
                        ExpandColorToRedGreenBlue(newValueAsList, i, rightSide);
                    }
                }
            }
        }

        var didChange = false;
        if (oldValueAsList == null && newValueAsList == null)
        {
            didChange = false;
        }
        else if (oldValueAsList == null && newValueAsList != null)
        {
            didChange = true;
        }
        else if (oldValueAsList != null && newValueAsList == null)
        {
            didChange = true;
        }
        else if (oldValueAsList.Count != newValueAsList.Count)
        {
            didChange = true;
        }
        else
        {
            // not null, same items, so let's loop
            for (int i = 0; i < oldValueAsList.Count; i++)
            {
                if (oldValueAsList[i] != newValueAsList[i])
                {
                    didChange = true;
                    break;
                }
            }
        }

        return didChange;
    }

    private static void QualifyInstanceVariables(List<string> newValueAsList, InstanceSave selectedInstance, int i, string[] split)
    {
        var asCSharp = EvaluatedSyntax.ConvertToCSharpSyntax(split[1]);

        var syntax = CSharpSyntaxTree.ParseText(asCSharp).GetCompilationUnitRoot();

        var itemsToReplace = syntax.DescendantNodes()
            .Where(item => item is IdentifierNameSyntax && 
                item.Parent is not MemberAccessExpressionSyntax
                    and not AliasQualifiedNameSyntax);

        var newTree = syntax.ReplaceNodes(
            itemsToReplace,
            (node, potentialExistingReplacement) =>
            {
                var toReturn =
                    SyntaxFactory.IdentifierName($"{selectedInstance.Name}.{node.ToString()}");



                return toReturn;
            });

        var newCSharp = newTree.ToString();

        split[1] = EvaluatedSyntax.ConvertToSlashSyntax(newCSharp);
        newValueAsList[i] = split[0] + "=" + split[1];


        //syntax.ReplaceNodes(

        //split[1] = selectedInstance.Name + "." + split[1];
        //newValueAsList[i] = split[0] + "=" + split[1];
    }

    private static void ExpandColorToRedGreenBlue(List<string> asList, int i, string rightSide)
    {
        // does this thing have a color value?
        // let's assume "no" for now, eventually may need to fix this up....
        var withoutVariable = rightSide.Substring(0, rightSide.Length - ".Color".Length);

        asList.RemoveAt(i);

        asList.Add($"Red = {withoutVariable}.Red");
        asList.Add($"Green = {withoutVariable}.Green");
        asList.Add($"Blue = {withoutVariable}.Blue");
    }

    private static string[] AddImpliedLeftSide(List<string> asList, int i, string[] split)
    {
        // need to prepend the equality here

        var rightSide = split[0]; // there is no left side, just right side
        var afterDot = rightSide.Substring(rightSide.LastIndexOf('.') + 1);

        if (rightSide.Contains("."))
        {
            // TODO: This is unused?
            var withoutVariable = rightSide.Substring(0, rightSide.LastIndexOf('.'));

            asList[i] = $"{afterDot} = {rightSide}";

            split = asList[i]
                .Split(equalsArray, StringSplitOptions.RemoveEmptyEntries)
                .Select(stringItem => stringItem.Trim()).ToArray();

        }
        return split;
    }


    #endregion
}
