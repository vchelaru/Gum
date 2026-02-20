using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using ToolsUtilities;

namespace Gum.Managers;

public class ErrorChecker : IErrorChecker
{
    private readonly ITypeManager _typeManager;
    private readonly IPluginManager _pluginManager;

    public ErrorChecker(ITypeManager typeManager, IPluginManager pluginManager)
    {
        _typeManager = typeManager;
        _pluginManager = pluginManager;
    }

    public ErrorViewModel[] GetErrorsFor(ElementSave? element, GumProjectSave project)
    {
        var list = new List<ErrorViewModel>();

        if(element != null)
        {
            ObjectFinder.Self.EnableCache();
            try
            {
                var asComponent = element as ComponentSave;
                if(asComponent != null)
                {
                    list.AddRange(GetBehaviorErrorsFor(asComponent, project));
                }
                list.AddRange(GetMissingBaseTypeErrorsFor(element));

                list.AddRange(GetParentErrorsFor(element));

                list.AddRange(GetInvalidVariableTypeErrorsFor(element));

                _pluginManager.FillWithErrors(list);
            }
            finally
            {
                ObjectFinder.Self.DisableCache();
            }
        }

        return list.ToArray();
    }

    public ErrorViewModel[] GetErrorsFor(ElementSave? element, PluginBase plugin)
    {
        var list = new List<ErrorViewModel>();

        if (element != null)
        {
            ObjectFinder.Self.EnableCache();
            try
            {
                _pluginManager.FillWithErrors(list, plugin);
            }
            finally
            {
                ObjectFinder.Self.DisableCache();
            }
        }

        return list.ToArray();
    }


    #region Behavior Errors

    private List<ErrorViewModel> GetBehaviorErrorsFor(ComponentSave component, GumProjectSave project)
    {
        List<ErrorViewModel> toReturn = new List<ErrorViewModel>();

        foreach(var behaviorReference in component.Behaviors)
        {
            var behavior = project.Behaviors.FirstOrDefault(item => item.Name == behaviorReference.BehaviorName);

            if(behavior == null)
            {
                toReturn.Add(new ErrorViewModel
                {
                    Message = $"Missing reference to behavior {behaviorReference.BehaviorName}"
                });
            }
            else
            {
                AddBehaviorErrors(component, toReturn, behavior);
            }
        }

        return toReturn;
    }

    private static void AddBehaviorErrors(ComponentSave component, List<ErrorViewModel> errorList, DataTypes.Behaviors.BehaviorSave behavior)
    {
        foreach (var behaviorInstance in behavior.RequiredInstances)
        {
            AddErrorsForBehaviorInstance(component, errorList, behavior, behaviorInstance);
        }

        // January 23, 2025
        // Vic says:
        // Not sure if we even support veraible lists in behaviors at this point, so
        // let's just worry about behaviors
        foreach(var behaviorVariable in behavior.RequiredVariables.Variables)
        {
            AddErrorsForBehaviorVariable(component, errorList, behavior, behaviorVariable);
        }
    }

    private static void AddErrorsForBehaviorVariable(ComponentSave component, List<ErrorViewModel> toReturn, BehaviorSave behavior, VariableSave behaviorVariable)
    {
        var rfv = new RecursiveVariableFinder(component.DefaultState);
        var variable = rfv.GetVariable(behaviorVariable.Name);

        if(variable == null)
        {

            toReturn.Add(new ErrorViewModel
            {
                Message = $"The behavior {behavior} " +
                        $"requires a variable named {behaviorVariable.Name} but this variable doesn't exist. " +
                        $"Add a custom variable or expose a variable and give it the required name to solve this error."
            });
        }
        else if(variable.Type != behaviorVariable.Type)
        {
            toReturn.Add(new ErrorViewModel
            {
                Message = $"The behavior {behavior} " +
                        $"requires a variable named {behaviorVariable.Name} with type {behaviorVariable.Type}. " +
                        $"This variable exists but it has the wrong type {variable.Type};"
            });
        }

        // no errors
    }

    private static void AddErrorsForBehaviorInstance(ComponentSave component, List<ErrorViewModel> toReturn, BehaviorSave behavior, BehaviorInstanceSave behaviorInstance)
    {
        var candidateInstances = component.Instances.Where(item => item.Name == behaviorInstance.Name).ToList();
        if (!string.IsNullOrEmpty(behaviorInstance.BaseType))
        {
            candidateInstances = candidateInstances.Where(item => item.IsOfType(behaviorInstance.BaseType)).ToList();
        }

        if (behaviorInstance.Behaviors.Any())
        {
            var requiredBehaviorNames = behaviorInstance.Behaviors.Select(item => item.Name);
            candidateInstances = candidateInstances.Where(item =>
            {
                bool fulfillsRequirements = false;
                var element = ObjectFinder.Self.GetComponent(item.BaseType);
                if (element != null)
                {
                    var implementedBehaviorNames = element.Behaviors.Select(implementedBehavior => implementedBehavior.BehaviorName);

                    fulfillsRequirements = requiredBehaviorNames.All(required => implementedBehaviorNames.Contains(required));

                }
                return fulfillsRequirements;
            }).ToList();

        }

        if (!candidateInstances.Any())
        {
            string message = $"Missing instance with name {behaviorInstance.Name}";
            if (!string.IsNullOrEmpty(behaviorInstance.BaseType))
            {
                message += $" of type {behaviorInstance.BaseType}";
            }
            if (behaviorInstance.Behaviors.Any())
            {
                if (behaviorInstance.Behaviors.Count == 1)
                {
                    message += " with behavior type ";
                }
                else
                {
                    message += " with behavior types ";
                }
                var behaviorsJoined = string.Join(", ", behaviorInstance.Behaviors.Select(item => item.Name).ToArray());
                message += behaviorsJoined;
            }

            message += $" needed by behavior {behavior.Name}";

            toReturn.Add(new ErrorViewModel
            {
                Message = message
            });
        }
    }

    #endregion

    #region Parent Errors

    List<ErrorViewModel> GetParentErrorsFor(ElementSave elementSave)
    {
        var toReturn = new List<ErrorViewModel>();
        // Do we want to use the RecursiveVariableFinder
        // to report parenting errors recursively? I vote "no"
        // because if we report an error in a derived element or
        // state, the user may fix the parent error there, when it
        // really should be fixed in the base.
        foreach(var state in  elementSave.AllStates)
        {
            foreach(var variable in state.Variables)
            {
                if (!string.IsNullOrEmpty(variable.SourceObject) && variable.GetRootName() == "Parent")
                {
                    var value = variable.Value as string;

                    if(!string.IsNullOrEmpty(value))
                    {
                        var instanceName = value!;
                        if(value?.Contains('.') == true)
                        {
                            instanceName = value.Substring(0, value.IndexOf('.'));
                        }

                        // for now if it's a sub-instance we'll assume it's a valid reference.
                        var instance = elementSave.GetInstance(instanceName);

                        if(instance == null)
                        {
                            var error = new ErrorViewModel();
                            error.Message = $"{variable.SourceObject} has a parent set to {value} which does not exist in the state {state.Name}";
                            toReturn.Add(error);
                        }
                    }
                }
            }
        }

        return toReturn;
    }

    #endregion

    #region Instance BaseType Errors

    List<ErrorViewModel> GetMissingBaseTypeErrorsFor(ElementSave elementSave)
    {
        var toReturn = new List<ErrorViewModel>();

        foreach(var instance in elementSave.Instances)
        {
            var instanceElement = ObjectFinder.Self.GetElementSave(instance);

            if(instanceElement == null)
            {
                var error = new ErrorViewModel
                {
                    Message = $"{instance.Name} references {instance.BaseType} which is an invalid element"
                };
                toReturn.Add(error);
            }
        }
        return toReturn;
    }

    #endregion

    #region Invalid variable types

    static HashSet<string> KnowBaseTypes = new HashSet<string>
    {
        "int",
        "int?",
        "bool",
        "bool?",
        "float",
        "float?",
        "double",
        "double?",
        "string",
        "string?",
        "State",
    };
    private IEnumerable<ErrorViewModel> GetInvalidVariableTypeErrorsFor(ElementSave elementSave)
    {
        var toReturn = new List<ErrorViewModel>();

        foreach(var state in elementSave.AllStates)
        {
            foreach(var variable in state.Variables)
            {
                var variableType = variable.Type;

                if(string.IsNullOrEmpty(variableType))
                {
                    continue;
                }
                if(KnowBaseTypes.Contains(variableType))
                {
                    continue;
                }
                if(variable.IsState(elementSave))
                {
                    continue;
                }
                if(_typeManager.GetTypeFromString(variableType) != null)
                {
                    continue;
                }

                // It's possible that this is an old variable that was created before "State" suffix was needed for state variables
                // Therefore, we should check for that. If so, let's notify the user that this is the case:
                var variableClone = FileManager.CloneSaveObject<VariableSave>(variable);
                variableClone.Type += "State";
                if(variableClone.IsState(elementSave))
                {
                    toReturn.Add(new ErrorViewModel
                    {
                        Message =
                                $"The variable {variable.Name} uses a type of {variable.Type}. " +
                                $"This type is probably referencing a category, but the type should be {variableClone.Type} (with the word State suffix). " +
                                $"This can cause code generation problems."
                    });
                    continue;
                }

                variableClone.Name += "State";
                if (variableClone.IsState(elementSave))
                {
                    toReturn.Add(new ErrorViewModel
                    {
                        Message =
                                $"The variable {variable.Name} uses a type of {variable.Type}. " +
                                $"This type is probably referencing a category, but name should be {variableClone.Name} (with the word State suffix). " +
                                $"This can cause code generation problems."
                    });
                    continue;
                }


                // If we got here, we don't know the type, so this is an error:
                toReturn.Add(new ErrorViewModel
                {
                    Message =
                            $"The variable {variable.Name} uses an unknown type of {variable.Type}. " +
                            $"This can cause code generation problems."
                });
            }
        }

        return toReturn;
    }

    #endregion
}
