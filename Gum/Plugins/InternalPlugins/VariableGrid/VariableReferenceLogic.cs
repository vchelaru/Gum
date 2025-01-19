using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.InternalPlugins.VariableGrid;
public class VariableReferenceLogic
{
    public void DoVariableReferenceReaction(ElementSave parentElement, InstanceSave instance, string unqualifiedMember,
        StateSave stateSave, string qualifiedName, bool trySave)
    {
        DoVariableReferenceReaction(parentElement, unqualifiedMember, stateSave);

        var newValue = stateSave.GetValueRecursive(qualifiedName);

        // this could be a tunneled variable. If so, we may need to propagate the value to other instances one level deeper
        var didSetDeepReference = DoVariableReferenceReactionOnInstanceVariableSet(parentElement, instance, stateSave, unqualifiedMember, newValue);

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

    private void DoVariableReferenceReaction(ElementSave elementSave, string unqualifiedName, StateSave stateSave)
    {
        // apply references on this element first, then apply the values to the other references:
        ElementSaveExtensions.ApplyVariableReferences(elementSave, stateSave);

        if (unqualifiedName == "VariableReferences")
        {
            GumCommands.Self.GuiCommands.RefreshVariableValues();
        }

        // Oct 13, 2022
        // This should set 
        // values on all contained objects for this particular state
        // Maybe this could be slow? not sure, but this covers all cases so if
        // there are performance issues, will investigate later.
        var references = ObjectFinder.Self.GetElementReferencesToThis(elementSave);
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
        foreach (var elementToSave in elementsToSave)
        {
            GumCommands.Self.FileCommands.TryAutoSaveElement(elementToSave);
        }

    }

}
