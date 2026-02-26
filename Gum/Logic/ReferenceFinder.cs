using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using GumRuntime;

namespace Gum.Logic;

public class ReferenceFinder : IReferenceFinder
{
    private readonly IProjectState _projectState;

    public ReferenceFinder(IProjectState projectState)
    {
        _projectState = projectState;
    }

    public ElementRenameChanges GetReferencesToElement(ElementSave element, string elementName)
    {
        var changes = new ElementRenameChanges();
        var project = _projectState.GumProjectSave;

        string qualifiedOldName = element switch
        {
            ScreenSave => $"Screens/{elementName}",
            ComponentSave => $"Components/{elementName}",
            StandardElementSave => $"Standards/{elementName}",
            _ => elementName
        };

        foreach (var screen in project.Screens)
        {
            CollectChangesInElement(screen, elementName, qualifiedOldName, changes);
        }

        foreach (var component in project.Components)
        {
            CollectChangesInElement(component, elementName, qualifiedOldName, changes);
        }

        return changes;
    }

    private static void CollectChangesInElement(ElementSave element, string oldName, string qualifiedOldName, ElementRenameChanges changes)
    {
        if (element.BaseType == oldName)
        {
            changes.ElementsWithBaseTypeReference.Add(element);
        }

        foreach (var instance in element.Instances)
        {
            if (instance.BaseType == oldName)
            {
                changes.InstancesWithBaseTypeReference.Add((element, instance));
            }
        }

        foreach (var variable in element.DefaultState.Variables.Where(v => v.GetRootName() == "ContainedType"))
        {
            if (variable.Value as string == oldName)
            {
                changes.ContainedTypeVariableReferences.Add((element, variable));
            }
        }

        foreach (var state in element.AllStates)
        {
            foreach (var variableList in state.VariableLists)
            {
                if (variableList.GetRootName() != "VariableReferences") continue;

                for (int i = 0; i < variableList.ValueAsIList.Count; i++)
                {
                    var line = variableList.ValueAsIList[i];
                    if (line is not string asString || asString.StartsWith("//") || !asString.Contains("="))
                    {
                        continue;
                    }

                    var right = asString.Substring(asString.IndexOf("=") + 1).Trim();

                    if (right.StartsWith(qualifiedOldName + "."))
                    {
                        changes.VariableReferenceChanges.Add(new VariableReferenceChange
                        {
                            Container = element,
                            VariableReferenceList = variableList,
                            LineIndex = i,
                            ChangedSide = SideOfEquals.Right
                        });
                    }
                }
            }
        }
    }

    public InstanceRenameChanges GetReferencesToInstance(ElementSave containerElement, InstanceSave instance, string oldName)
    {
        var changes = new InstanceRenameChanges();

        // Variables across all states that reference this instance by name
        foreach (var state in containerElement.AllStates)
        {
            foreach (var variable in state.Variables)
            {
                if (variable.SourceObject == oldName)
                {
                    changes.VariablesToRename.Add((containerElement, variable));
                }
            }
        }

        // Events in the containing element
        foreach (var evt in containerElement.Events)
        {
            if (evt.GetSourceObject() == oldName)
            {
                changes.EventsToRename.Add(evt);
            }
        }

        // DefaultChildContainer in any state of the containing element
        foreach (var state in containerElement.AllStates)
        {
            if (state.Variables.Any(v => v.Name == "DefaultChildContainer" && (string?)v.Value == oldName))
            {
                changes.DefaultChildContainerWillChange = true;
                break;
            }
        }

        // Parent variable references in other elements that point to this instance by name.
        // This must run unconditionally — a Parent variable can reference any child instance
        // directly, not only via DefaultChildContainer.
        {
            var elementsReferencing = ObjectFinder.Self.GetElementsReferencing(containerElement);
            foreach (var otherElement in elementsReferencing)
            {
                foreach (var state in otherElement.AllStates)
                {
                    foreach (var variable in state.Variables)
                    {
                        if (variable.GetRootName() == "Parent" && (variable.Value as string)?.Contains(".") == true)
                        {
                            var value = (string)variable.Value;
                            var dotIndex = value.IndexOf(".");
                            var valueBeforeDot = value.Substring(0, dotIndex);
                            var valueAfterDot = value.Substring(dotIndex + 1);

                            if (valueAfterDot == oldName)
                            {
                                var parentInstance = otherElement.GetInstance(valueBeforeDot);
                                var parentInstanceElement = ObjectFinder.Self.GetElementSave(parentInstance);
                                if (parentInstanceElement == containerElement)
                                {
                                    changes.ParentVariablesInOtherElements.Add((otherElement, variable));
                                }
                            }
                        }
                    }
                }
            }
        }

        // VariableReferences entries in the container element and inheriting elements that
        // reference the instance name on the left or right side of the assignment
        var relevantElements = new List<ElementSave> { containerElement };
        relevantElements.AddRange(ObjectFinder.Self.GetElementsInheritingFrom(containerElement));

        foreach (var element in relevantElements)
        {
            foreach (var state in element.AllStates)
            {
                foreach (var variableList in state.VariableLists)
                {
                    if (variableList.GetRootName() != "VariableReferences") continue;

                    for (int i = 0; i < variableList.ValueAsIList.Count; i++)
                    {
                        var line = variableList.ValueAsIList[i];
                        if (line is not string asString || asString.StartsWith("//") ||
                            !asString.Contains("=") || !asString.Contains(oldName))
                        {
                            continue;
                        }

                        var equalIndex = asString.IndexOf("=");
                        var leftSide = asString.Substring(0, equalIndex).Trim();
                        var rightSide = asString.Substring(equalIndex + 1).Trim();

                        // Strip optional state prefix from right side (e.g. "Highlighted:OldName.X")
                        var rightForCheck = rightSide;
                        var colonIndex = rightSide.IndexOf(":");
                        if (colonIndex >= 0)
                        {
                            rightForCheck = rightSide.Substring(colonIndex + 1);
                        }

                        // Left side matches when the VariableReferences list is not owned by this
                        // instance itself (SourceObject != oldName), and the left side starts with
                        // "oldName." (i.e. it's an absolute reference within the element)
                        bool matchesLeft = variableList.SourceObject != oldName &&
                            leftSide.StartsWith(oldName + ".");

                        bool matchesRight = rightForCheck.StartsWith(oldName + ".");

                        if (matchesLeft || matchesRight)
                        {
                            changes.VariableReferenceChanges.Add(new VariableReferenceChange
                            {
                                Container = element,
                                VariableReferenceList = variableList,
                                LineIndex = i,
                                ChangedSide = (matchesLeft && matchesRight) ? SideOfEquals.Both
                                    : matchesLeft ? SideOfEquals.Left
                                    : SideOfEquals.Right
                            });
                        }
                    }
                }
            }
        }

        // Search all other elements for cross-component qualified references
        // e.g. "Width = Components/ComponentA.Sprite.Width" in ComponentB
        var project = _projectState.GumProjectSave;
        string? qualifiedElementPrefix = containerElement switch
        {
            ComponentSave => $"Components/{containerElement.Name}",
            ScreenSave => $"Screens/{containerElement.Name}",
            _ => null
        };

        if (qualifiedElementPrefix != null)
        {
            var qualifiedInstancePrefix = $"{qualifiedElementPrefix}.{oldName}.";

            foreach (var element in project.AllElements)
            {
                if (relevantElements.Contains(element)) continue;

                foreach (var state in element.AllStates)
                {
                    foreach (var variableList in state.VariableLists)
                    {
                        if (variableList.GetRootName() != "VariableReferences") continue;

                        for (int i = 0; i < variableList.ValueAsIList.Count; i++)
                        {
                            var line = variableList.ValueAsIList[i];
                            if (line is not string asString || asString.StartsWith("//") || !asString.Contains("="))
                            {
                                continue;
                            }

                            var equalIndex = asString.IndexOf("=");
                            var rightSide = asString.Substring(equalIndex + 1).Trim();

                            // Strip optional state prefix (e.g. "Highlighted:Components/...")
                            var colonIndex = rightSide.IndexOf(":");
                            var rightForCheck = colonIndex >= 0 ? rightSide.Substring(colonIndex + 1) : rightSide;

                            if (rightForCheck.StartsWith(qualifiedInstancePrefix))
                            {
                                changes.VariableReferenceChanges.Add(new VariableReferenceChange
                                {
                                    Container = element,
                                    VariableReferenceList = variableList,
                                    LineIndex = i,
                                    ChangedSide = SideOfEquals.Right
                                });
                            }
                        }
                    }
                }
            }
        }

        return changes;
    }

    public StateRenameChanges GetReferencesToState(StateSave state, string oldName, IStateContainer? container, StateSaveCategory? category)
    {
        var changes = new StateRenameChanges();

        string variableName = category != null ? category.Name + "State" : "State";

        if (container is ElementSave elementSave)
        {
            List<InstanceSave> instances = new List<InstanceSave>();
            ObjectFinder.Self.GetElementsReferencing(elementSave, foundInstances: instances);

            foreach (var instance in instances)
            {
                var parentOfInstance = instance.ParentContainer;
                var variableNameToLookFor = $"{instance.Name}.{variableName}";

                if (parentOfInstance != null)
                {
                    var variablesToFix = parentOfInstance.AllStates
                        .SelectMany(item => item.Variables)
                        .Where(item => item.Name == variableNameToLookFor)
                        .Where(item => (string?)item.Value == oldName);

                    foreach (var variable in variablesToFix)
                    {
                        changes.VariablesToUpdate.Add((parentOfInstance, variable));
                    }
                }
            }
        }

        return changes;
    }

    public CategoryRenameChanges GetReferencesToStateCategory(IStateContainer owner, StateSaveCategory category, string oldName)
    {
        var changes = new CategoryRenameChanges();

        var project = _projectState.GumProjectSave;

        var ownerAsElement = owner as ElementSave;

        List<ElementSave> inheritingElements = new List<ElementSave>();
        if (ownerAsElement != null)
        {
            inheritingElements.Add(ownerAsElement);
            inheritingElements.AddRange(ObjectFinder.Self.GetElementsInheritingFrom(ownerAsElement));
        }

        foreach (var screen in project.Screens)
        {
            CollectCategoryReferencesInElement(category, oldName, inheritingElements, screen, changes);
        }

        foreach (var component in project.Components)
        {
            CollectCategoryReferencesInElement(category, oldName, inheritingElements, component, changes);
        }

        // Standards cannot include instances so no need to loop through them

        return changes;
    }

    private static void CollectCategoryReferencesInElement(StateSaveCategory changedCategory, string oldName, ICollection<ElementSave> inheritingElements, ElementSave element, CategoryRenameChanges changes)
    {
        // If the element inherits from the owner of the category and if the screen has a variable of this type, change it.
        foreach (var state in element.AllStates)
        {
            foreach (var variable in state.Variables)
            {
                var variableType = variable.Type;

                if (variableType == oldName)
                {
                    if (string.IsNullOrEmpty(variable.SourceObject))
                    {
                        if (inheritingElements.Contains(element))
                        {
                            AddVariableToList(element, changes.VariableChanges, state, variable);
                        }
                    }
                    else
                    {
                        // only do it if the instance is in the inheritance chain
                        var instance = element.GetInstance(variable.SourceObject);
                        if (instance != null)
                        {
                            var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                            if (inheritingElements?.Contains(instanceElement) == true)
                            {
                                AddVariableToList(element, changes.VariableChanges, state, variable);
                            }
                        }
                    }
                }
            }
        }

        static void AddVariableToList(ElementSave element, List<VariableChange> toReturn, StateSave state, VariableSave variable)
        {
            var category = element.Categories.FirstOrDefault(item => item.States.Contains(state));

            toReturn.Add(new VariableChange
            {
                Container = element,
                Category = category,
                State = state,
                Variable = variable
            });
        }
    }

    public VariableChangeResponse GetReferencesToVariable(IStateContainer owner, string oldFullName, string oldStrippedOrExposedName)
    {
        List<VariableChange> variableChanges = new List<VariableChange>();
        List<VariableReferenceChange> variableReferenceChanges = new List<VariableReferenceChange>();

        var project = _projectState.GumProjectSave;

        var changedVariableOwnerElement = owner as ElementSave;

        List<ElementSave> inheritingElements = new List<ElementSave>();
        if (changedVariableOwnerElement != null)
        {
            inheritingElements.AddRange(ObjectFinder.Self.GetElementsInheritingFrom(changedVariableOwnerElement));
        }

        foreach (var item in inheritingElements)
        {
            foreach (var state in item.AllStates)
            {
                foreach (var variable in state.Variables)
                {
                    if (variable.ExposedAsName == oldStrippedOrExposedName)
                    {
                        variableChanges.Add(new VariableChange
                        {
                            Container = item,
                            Category = item.Categories.FirstOrDefault(item => item.States.Contains(state)),
                            State = state,
                            Variable = variable
                        });
                    }
                }
            }
        }

        foreach (var element in project.AllElements)
        {
            foreach (var state in element.AllStates)
            {
                foreach (var variable in state.Variables)
                {
                    if (variable.GetRootName() == oldStrippedOrExposedName && !string.IsNullOrEmpty(variable.SourceObject))
                    {
                        var instance = element.GetInstance(variable.SourceObject);
                        if (instance != null)
                        {
                            var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                            if (inheritingElements.Contains(instanceElement) || instanceElement == changedVariableOwnerElement)
                            {
                                variableChanges.Add(new VariableChange
                                {
                                    Container = element,
                                    Category = element.Categories.FirstOrDefault(item => item.States.Contains(state)),
                                    State = state,
                                    Variable = variable
                                });
                            }
                        }
                    }
                }

                foreach (var variableList in state.VariableLists)
                {
                    if (variableList.GetRootName() == "VariableReferences")
                    {
                        ElementSave? rootLeftSideElement = null;
                        if (string.IsNullOrEmpty(variableList.SourceObject))
                        {
                            rootLeftSideElement = element;
                        }
                        else
                        {
                            InstanceSave? leftSideInstance = element.GetInstance(variableList.SourceObject);
                            if (leftSideInstance != null)
                            {
                                rootLeftSideElement = ObjectFinder.Self.GetElementSave(leftSideInstance);
                            }
                        }

                        // loop through the items and see if any are using this:
                        for (int i = 0; i < variableList.ValueAsIList.Count; i++)
                        {
                            var line = variableList.ValueAsIList[i];

                            if (line is not string asString || asString.StartsWith("//") || asString.Contains("=") == false || asString.Contains(oldStrippedOrExposedName) == false)
                            {
                                continue;
                            }

                            var right = asString.Substring(asString.IndexOf("=") + 1).Trim();
                            var leftSide = asString.Substring(0, asString.IndexOf("=")).Trim();

                            var matchesLeft = false;
                            if (leftSide == oldStrippedOrExposedName)
                            {
                                string? oldSourceObject = null;
                                if (oldFullName.Contains("."))
                                {
                                    oldSourceObject = oldFullName.Substring(0, oldFullName.IndexOf("."));
                                }
                                // See if the element that contains the left side variable is a match...
                                matchesLeft = changedVariableOwnerElement == rootLeftSideElement || inheritingElements.Contains(rootLeftSideElement) ||
                                    // or if we are in the same instance as the one that owns the variable reference...
                                    (element == changedVariableOwnerElement && variableList.SourceObject == oldSourceObject);
                            }

                            var stateContainingRightSideVariable = state;
                            ElementSaveExtensions.GetRightSideAndState(ref right, ref stateContainingRightSideVariable);
                            var matchesRight = false;
                            if (right == oldStrippedOrExposedName || right.EndsWith("." + oldStrippedOrExposedName))
                            {
                                // see if the owner of the right side is this element or an inheriting element:
                                var rightSideOwner = stateContainingRightSideVariable.ParentContainer;

                                matchesRight = changedVariableOwnerElement == rightSideOwner || inheritingElements.Contains(rightSideOwner);
                            }

                            if (matchesLeft || matchesRight)
                            {
                                var referenceChange = new VariableReferenceChange();
                                referenceChange.Container = element;
                                referenceChange.VariableReferenceList = variableList;
                                referenceChange.LineIndex = i;
                                referenceChange.ChangedSide = (matchesLeft && matchesRight) ? SideOfEquals.Both
                                    : matchesLeft ? SideOfEquals.Left
                                    : SideOfEquals.Right;
                                variableReferenceChanges.Add(referenceChange);
                            }
                        }
                    }
                }
            }
        }

        return new VariableChangeResponse
        {
            VariableChanges = variableChanges,
            VariableReferenceChanges = variableReferenceChanges
        };
    }
}
