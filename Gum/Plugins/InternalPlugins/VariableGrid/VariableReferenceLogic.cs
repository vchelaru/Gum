using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumRuntime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Management.Instrumentation;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.VariableGrid;
public class VariableReferenceLogic
{
    private readonly GuiCommands _guiCommands;

    public VariableReferenceLogic(GuiCommands guiCommands)
    {
        _guiCommands = guiCommands;
    }

    public void DoVariableReferenceReaction(ElementSave parentElement, InstanceSave leftSideInstance, string unqualifiedMember,
        StateSave stateSave, string qualifiedName, bool trySave)
    {
        if(unqualifiedMember == "VariableReferences")
        {
            var newDirectValue = stateSave.GetVariableListSave(qualifiedName);

            if(newDirectValue != null)
            {
                var failures = GetIndividualFailures(parentElement, leftSideInstance, newDirectValue);

                if(failures.Count > 0)
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

        if(trySave)
        {
            foreach (var elementToSave in elementsToSave)
            {
                GumCommands.Self.FileCommands.TryAutoSaveElement(elementToSave);
            }
        }

        var newValue = stateSave.GetValueRecursive(qualifiedName);

        // this could be a tunneled variable. If so, we may need to propagate the value to other instances one level deeper
        var didSetDeepReference = DoVariableReferenceReactionOnInstanceVariableSet(parentElement, leftSideInstance, stateSave, unqualifiedMember, newValue);

        // now force save it if it's a variable reference:
        if (unqualifiedMember == "VariableReferences" && trySave)
        {
            GumCommands.Self.FileCommands.TryAutoSaveElement(parentElement);
        }

        // The MainEditorTabPlugin handles refreshing in most cases, but if there was a "deep" set due to variable references,
        // then we forcefully change the values here.
        if (didSetDeepReference)
        {
            GumCommands.Self.WireframeCommands.Refresh(forceLayout: true);
        }
    }

    public AssignmentExpressionSyntax GetAssignmentSyntax(string item)
    {
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

            GumCommands.Self.GuiCommands.ShowMessage(message, "Invalid Variable Reference");
        }
    }

    private List<(string, GeneralResponse)> GetIndividualFailures(ElementSave parentElement, InstanceSave leftSideInstance, VariableListSave newDirectValue)
    {
        var values = newDirectValue.ValueAsIList;

        var failures = new List<(string, GeneralResponse)>();

        foreach (string line in values)
        {
            if (line.StartsWith("//") || string.IsNullOrEmpty(line))
            {
                continue;
            }

            var assignmentSyntax = GetAssignmentSyntax(line);

            GeneralResponse response = GeneralResponse.SuccessfulResponse;

            if(assignmentSyntax == null)
            {
                response = GeneralResponse.UnsuccessfulWith("Could not parse line. This should be an assignment such as X=Y");
            }

            VariableSave leftSideVariable = null;

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

            EvaluatedSyntax evaulatedSyntax = null;

            if (response.Succeeded)
            {
                evaulatedSyntax = EvaluatedSyntax.FromSyntaxNode(assignmentSyntax.Right, parentElement.DefaultState);

                if (evaulatedSyntax == null)
                {
                    response = GeneralResponse.UnsuccessfulWith($"Could not evaluate right-side expression {assignmentSyntax}");
                }
            }

            if(response.Succeeded)
            { 
                var typeMatchResponse = CheckIfVariableTypesMatch(leftSideVariable, parentElement, leftSideInstance, evaulatedSyntax);

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

        return failures;
    }

    private GeneralResponse CheckIfVariableTypesMatch(VariableSave leftSideVariable, ElementSave parentElement, InstanceSave leftSideInstance, EvaluatedSyntax assignment)
    {
        var leftSideType = leftSideVariable.Type;
        string rightSideType = null;
        // get the right side type to compare:
        var ownerOfRightSideVariable = parentElement.DefaultState;

        rightSideType = assignment.EvaluatedType;

        if(rightSideType != leftSideType)
        {
            return GeneralResponse.UnsuccessfulWith($"Left side is of type [{leftSideType}] but right side is of type [{rightSideType}]");
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

    static char[] equalsArray = new char[] { '=' };
    
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

}
