using Gum.Content.AnimationChain;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace Gum.ProjectServices;

/// <inheritdoc/>
public class HeadlessErrorChecker : IHeadlessErrorChecker
{
    private static readonly HashSet<string> KnownBaseTypes = new HashSet<string>
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

    private readonly ITypeResolver _typeResolver;
    private readonly List<IAdditionalErrorSource> _additionalErrorSources;

    public HeadlessErrorChecker(ITypeResolver typeResolver)
    {
        _typeResolver = typeResolver;
        _additionalErrorSources = new List<IAdditionalErrorSource>();
    }

    public HeadlessErrorChecker(ITypeResolver typeResolver, IEnumerable<IAdditionalErrorSource> additionalErrorSources)
    {
        _typeResolver = typeResolver;
        _additionalErrorSources = new List<IAdditionalErrorSource>(additionalErrorSources);
    }

    /// <inheritdoc/>
    public IReadOnlyList<ErrorResult> GetErrorsFor(ElementSave element, GumProjectSave project)
    {
        var errors = new List<ErrorResult>();

        ObjectFinder.Self.GumProjectSave = project;
        ObjectFinder.Self.EnableCache();
        try
        {
            var asComponent = element as ComponentSave;
            if (asComponent != null)
            {
                errors.AddRange(GetBehaviorErrorsFor(asComponent, project));
            }
            errors.AddRange(GetMissingElementBaseTypeErrorFor(element));
            errors.AddRange(GetMissingBaseTypeErrorsFor(element));
            errors.AddRange(GetParentErrorsFor(element));
            errors.AddRange(GetInvalidVariableTypeErrorsFor(element));
            errors.AddRange(GetAchxOriginErrorsFor(element, project));

            foreach (var source in _additionalErrorSources)
            {
                errors.AddRange(source.GetErrors(element, project));
            }
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }

        return errors;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ErrorResult> GetAllErrors(GumProjectSave project)
    {
        var errors = new List<ErrorResult>();

        ObjectFinder.Self.GumProjectSave = project;
        ObjectFinder.Self.EnableCache();
        try
        {
            foreach (var screen in project.Screens)
            {
                errors.AddRange(GetErrorsForInternal(screen, project));
            }
            foreach (var component in project.Components)
            {
                errors.AddRange(GetErrorsForInternal(component, project));
            }
            foreach (var standard in project.StandardElements)
            {
                errors.AddRange(GetErrorsForInternal(standard, project));
            }
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }

        return errors;
    }

    /// <summary>
    /// Internal version that skips ObjectFinder setup (already done by caller).
    /// </summary>
    private List<ErrorResult> GetErrorsForInternal(ElementSave element, GumProjectSave project)
    {
        var errors = new List<ErrorResult>();

        var asComponent = element as ComponentSave;
        if (asComponent != null)
        {
            errors.AddRange(GetBehaviorErrorsFor(asComponent, project));
        }
        errors.AddRange(GetMissingElementBaseTypeErrorFor(element));
        errors.AddRange(GetMissingBaseTypeErrorsFor(element));
        errors.AddRange(GetParentErrorsFor(element));
        errors.AddRange(GetInvalidVariableTypeErrorsFor(element));
        errors.AddRange(GetAchxOriginErrorsFor(element, project));

        foreach (var source in _additionalErrorSources)
        {
            errors.AddRange(source.GetErrors(element, project));
        }

        return errors;
    }

    #region Behavior Errors

    private List<ErrorResult> GetBehaviorErrorsFor(ComponentSave component, GumProjectSave project)
    {
        var errors = new List<ErrorResult>();

        foreach (var behaviorReference in component.Behaviors)
        {
            var behavior = project.Behaviors.FirstOrDefault(item => item.Name == behaviorReference.BehaviorName);

            if (behavior == null)
            {
                errors.Add(new ErrorResult
                {
                    ElementName = component.Name,
                    Message = $"Missing reference to behavior {behaviorReference.BehaviorName}"
                });
            }
            else
            {
                AddBehaviorErrors(component, errors, behavior);
            }
        }

        return errors;
    }

    private static void AddBehaviorErrors(ComponentSave component, List<ErrorResult> errors, BehaviorSave behavior)
    {
        foreach (var behaviorInstance in behavior.RequiredInstances)
        {
            AddErrorsForBehaviorInstance(component, errors, behavior, behaviorInstance);
        }

        foreach (var behaviorVariable in behavior.RequiredVariables.Variables)
        {
            AddErrorsForBehaviorVariable(component, errors, behavior, behaviorVariable);
        }
    }

    private static void AddErrorsForBehaviorVariable(ComponentSave component, List<ErrorResult> errors, BehaviorSave behavior, VariableSave behaviorVariable)
    {
        var rfv = new RecursiveVariableFinder(component.DefaultState);
        var variable = rfv.GetVariable(behaviorVariable.Name);

        if (variable == null)
        {
            errors.Add(new ErrorResult
            {
                ElementName = component.Name,
                Message = $"The behavior {behavior} " +
                        $"requires a variable named {behaviorVariable.Name} but this variable doesn't exist. " +
                        $"Add a custom variable or expose a variable and give it the required name to solve this error."
            });
        }
        else if (variable.Type != behaviorVariable.Type)
        {
            errors.Add(new ErrorResult
            {
                ElementName = component.Name,
                Message = $"The behavior {behavior} " +
                        $"requires a variable named {behaviorVariable.Name} with type {behaviorVariable.Type}. " +
                        $"This variable exists but it has the wrong type {variable.Type};"
            });
        }
    }

    private static void AddErrorsForBehaviorInstance(ComponentSave component, List<ErrorResult> errors, BehaviorSave behavior, BehaviorInstanceSave behaviorInstance)
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
                var element = ObjectFinder.Self.GetComponent(item.BaseType);
                if (element != null)
                {
                    var implementedBehaviorNames = element.Behaviors.Select(b => b.BehaviorName);
                    return requiredBehaviorNames.All(required => implementedBehaviorNames.Contains(required));
                }
                return false;
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

            errors.Add(new ErrorResult
            {
                ElementName = component.Name,
                Message = message
            });
        }
    }

    #endregion

    #region Parent Errors

    private static List<ErrorResult> GetParentErrorsFor(ElementSave elementSave)
    {
        var errors = new List<ErrorResult>();

        foreach (var state in elementSave.AllStates)
        {
            foreach (var variable in state.Variables)
            {
                if (!string.IsNullOrEmpty(variable.SourceObject) && variable.GetRootName() == "Parent")
                {
                    var value = variable.Value as string;

                    if (!string.IsNullOrEmpty(value))
                    {
                        var instanceName = value!;
                        if (value?.Contains('.') == true)
                        {
                            instanceName = value.Substring(0, value.IndexOf('.'));
                        }

                        var instance = elementSave.GetInstance(instanceName);

                        if (instance == null)
                        {
                            errors.Add(new ErrorResult
                            {
                                ElementName = elementSave.Name,
                                Message = $"{variable.SourceObject} has a parent set to {value} which does not exist in the state {state.Name}"
                            });
                        }
                    }
                }
            }
        }

        return errors;
    }

    #endregion

    #region Element BaseType Errors

    private static List<ErrorResult> GetMissingElementBaseTypeErrorFor(ElementSave elementSave)
    {
        var errors = new List<ErrorResult>();

        if (!string.IsNullOrEmpty(elementSave.BaseType))
        {
            var baseElement = ObjectFinder.Self.GetElementSave(elementSave.BaseType);
            if (baseElement == null)
            {
                errors.Add(new ErrorResult
                {
                    ElementName = elementSave.Name,
                    Message = $"{elementSave.Name} has a base type of {elementSave.BaseType} which does not exist"
                });
            }
        }

        return errors;
    }

    #endregion

    #region Instance BaseType Errors

    private static List<ErrorResult> GetMissingBaseTypeErrorsFor(ElementSave elementSave)
    {
        var errors = new List<ErrorResult>();

        foreach (var instance in elementSave.Instances)
        {
            var instanceElement = ObjectFinder.Self.GetElementSave(instance);

            if (instanceElement == null)
            {
                errors.Add(new ErrorResult
                {
                    ElementName = elementSave.Name,
                    Message = $"{instance.Name} references {instance.BaseType} which is an invalid element"
                });
            }
        }

        return errors;
    }

    #endregion

    #region Invalid Variable Type Errors

    private IEnumerable<ErrorResult> GetInvalidVariableTypeErrorsFor(ElementSave elementSave)
    {
        var errors = new List<ErrorResult>();

        foreach (var state in elementSave.AllStates)
        {
            foreach (var variable in state.Variables)
            {
                var variableType = variable.Type;

                if (string.IsNullOrEmpty(variableType))
                {
                    continue;
                }
                if (!string.IsNullOrEmpty(variable.SourceObject))
                {
                    continue;
                }
                if (KnownBaseTypes.Contains(variableType))
                {
                    continue;
                }
                if (variable.IsState(elementSave))
                {
                    continue;
                }
                if (_typeResolver.GetTypeFromString(variableType) != null)
                {
                    continue;
                }

                // It's possible that this is an old variable that was created before "State" suffix was needed for state variables.
                // Therefore, we should check for that. If so, let's notify the user that this is the case:
                var variableClone = FileManager.CloneSaveObject<VariableSave>(variable);
                variableClone.Type += "State";
                if (variableClone.IsState(elementSave))
                {
                    errors.Add(new ErrorResult
                    {
                        ElementName = elementSave.Name,
                        Message =
                                $"The variable {variable.Name} uses a type of {variable.Type}. " +
                                $"This type is probably referencing a category, but the type should be {variableClone.Type} (with the word State suffix). " +
                                $"This can cause code generation problems.",
                        Severity = ErrorSeverity.Warning
                    });
                    continue;
                }

                variableClone.Name += "State";
                if (variableClone.IsState(elementSave))
                {
                    errors.Add(new ErrorResult
                    {
                        ElementName = elementSave.Name,
                        Message =
                                $"The variable {variable.Name} uses a type of {variable.Type}. " +
                                $"This type is probably referencing a category, but name should be {variableClone.Name} (with the word State suffix). " +
                                $"This can cause code generation problems.",
                        Severity = ErrorSeverity.Warning
                    });
                    continue;
                }
            }
        }

        return errors;
    }

    #endregion

    #region ACHX Origin Errors

    private static List<ErrorResult> GetAchxOriginErrorsFor(ElementSave elementSave, GumProjectSave project)
    {
        var errors = new List<ErrorResult>();

        if (elementSave.DefaultState == null)
        {
            return errors;
        }

        var rfv = new RecursiveVariableFinder(elementSave.DefaultState);

        if (elementSave.IsOfType("Sprite"))
        {
            AddAchxOriginErrorIfNeeded(errors, elementSave, project, rfv, instanceName: null);
        }

        foreach (var instance in elementSave.Instances)
        {
            if (instance.IsOfType("Sprite"))
            {
                AddAchxOriginErrorIfNeeded(errors, elementSave, project, rfv, instance.Name);
            }
        }

        return errors;
    }

    private static void AddAchxOriginErrorIfNeeded(
        List<ErrorResult> errors,
        ElementSave elementSave,
        GumProjectSave project,
        RecursiveVariableFinder rfv,
        string? instanceName)
    {
        var prefix = instanceName == null ? string.Empty : instanceName + ".";

        var sourceFile = rfv.GetValue<string>(prefix + "SourceFile");
        if (string.IsNullOrEmpty(sourceFile) || !sourceFile.EndsWith(".achx", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var absolutePath = sourceFile;
        if (FileManager.IsRelative(absolutePath) && !string.IsNullOrEmpty(project.FullFileName))
        {
            absolutePath = FileManager.GetDirectory(project.FullFileName) + sourceFile;
        }

        if (!System.IO.File.Exists(absolutePath))
        {
            // A missing source file is surfaced separately by IsSourceFileMissing.
            return;
        }

        AnimationChainListSave? achx;
        try
        {
            achx = AnimationChainListSave.FromFile(absolutePath);
        }
        catch
        {
            return;
        }

        if (achx?.AnimationChains == null || !HasAnyNonZeroFrameOffset(achx))
        {
            return;
        }

        var xOrigin = rfv.GetValue<HorizontalAlignment>(prefix + "XOrigin");
        var yOrigin = rfv.GetValue<VerticalAlignment>(prefix + "YOrigin");

        if (xOrigin == HorizontalAlignment.Center && yOrigin == VerticalAlignment.Center)
        {
            return;
        }

        var who = instanceName ?? elementSave.Name;
        errors.Add(new ErrorResult
        {
            ElementName = elementSave.Name,
            Message =
                $"Sprite {who} references an .achx ({sourceFile}) with per-frame offsets, " +
                $"but XOrigin/YOrigin is not Center. The FlatRedBall AnimationEditor authors " +
                $"RelativeX/RelativeY values against a center-anchored Sprite, so a non-Center " +
                $"origin will cause the animation to drift as frames change size. " +
                $"Set XOrigin and YOrigin to Center.",
            Severity = ErrorSeverity.Warning
        });
    }

    private static bool HasAnyNonZeroFrameOffset(AnimationChainListSave achx)
    {
        foreach (var chain in achx.AnimationChains)
        {
            if (chain.Frames == null) continue;
            foreach (var frame in chain.Frames)
            {
                if (frame.RelativeX != 0 || frame.RelativeY != 0)
                {
                    return true;
                }
            }
        }
        return false;
    }

    #endregion
}
