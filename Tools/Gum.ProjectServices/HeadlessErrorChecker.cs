using Gum.Bundle;
using Gum.Content.AnimationChain;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;
using GumRuntime;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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
    private readonly Dictionary<string, Type?> _enumTypesByName;
    // Shared across calls so a check doesn't re-list every directory an element references.
    private readonly IFileNameCaseChecker _caseChecker;

    public HeadlessErrorChecker(ITypeResolver typeResolver)
        : this(typeResolver, Array.Empty<IAdditionalErrorSource>())
    {
    }

    public HeadlessErrorChecker(ITypeResolver typeResolver, IEnumerable<IAdditionalErrorSource> additionalErrorSources)
    {
        _typeResolver = typeResolver;
        _additionalErrorSources = new List<IAdditionalErrorSource>(additionalErrorSources);
        _enumTypesByName = new Dictionary<string, Type?>();
        _caseChecker = new FileNameCaseChecker();
    }

    /// <inheritdoc/>
    public IReadOnlyList<ErrorResult> GetErrorsFor(ElementSave element, GumProjectSave project)
    {
        ObjectFinder.Self.GumProjectSave = project;
        ObjectFinder.Self.EnableCache();
        try
        {
            return GetErrorsForInternal(element, project);
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<ErrorResult> GetAllErrors(GumProjectSave project)
    {
        var errors = new List<ErrorResult>();
        // Paths the element checks already reported as GUM0008, so the project pass skips them.
        var caseMismatchPaths = new HashSet<string>(StringComparer.Ordinal);

        ObjectFinder.Self.GumProjectSave = project;
        ObjectFinder.Self.EnableCache();
        try
        {
            foreach (var screen in project.Screens)
            {
                errors.AddRange(GetErrorsForInternal(screen, project, caseMismatchPaths));
            }
            foreach (var component in project.Components)
            {
                errors.AddRange(GetErrorsForInternal(component, project, caseMismatchPaths));
            }
            foreach (var standard in project.StandardElements)
            {
                errors.AddRange(GetErrorsForInternal(standard, project, caseMismatchPaths));
            }
            errors.AddRange(GetProjectFileCaseMismatchErrors(project, caseMismatchPaths));
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }

        return errors;
    }

    /// <inheritdoc/>
    public IReadOnlyList<ErrorResult> GetProjectErrors(GumProjectSave project)
    {
        // The element checks own the GUM0008 rows for element files and the files elements
        // reference; running only those two checks finds the paths to skip.
        var caseMismatchPaths = new HashSet<string>(StringComparer.Ordinal);

        ObjectFinder.Self.GumProjectSave = project;
        ObjectFinder.Self.EnableCache();
        try
        {
            foreach (var element in project.AllElements)
            {
                GetMissingSourceFileErrorsFor(element, project, caseMismatchPaths);
                GetMissingExternalFileErrorsFor(element, project, caseMismatchPaths);
            }
            return GetProjectFileCaseMismatchErrors(project, caseMismatchPaths);
        }
        finally
        {
            ObjectFinder.Self.DisableCache();
        }
    }

    /// <summary>
    /// Internal version that skips ObjectFinder setup (already done by caller).
    /// </summary>
    private List<ErrorResult> GetErrorsForInternal(ElementSave element, GumProjectSave project, ISet<string>? caseMismatchPaths = null)
    {
        var errors = new List<ErrorResult>();

        var asComponent = element as ComponentSave;
        if (asComponent != null)
        {
            errors.AddRange(GetBehaviorErrorsFor(asComponent, project));
        }
        errors.AddRange(GetMissingSourceFileErrorsFor(element, project, caseMismatchPaths));
        errors.AddRange(GetMissingExternalFileErrorsFor(element, project, caseMismatchPaths));
        errors.AddRange(GetMissingElementBaseTypeErrorFor(element));
        errors.AddRange(GetMissingBaseTypeErrorsFor(element));
        errors.AddRange(GetParentErrorsFor(element));
        errors.AddRange(GetInvalidVariableTypeErrorsFor(element));
        errors.AddRange(GetInvalidEnumValueErrorsFor(element));
        errors.AddRange(GetAchxOriginErrorsFor(element, project));
        errors.AddRange(GetVariableReferenceConflictErrorsFor(element));
        errors.AddRange(GetUnresolvableVariableReferenceErrorsFor(element));
        errors.AddRange(GetSelfReferentialCategoryStateErrorsFor(element));

        foreach (var source in _additionalErrorSources)
        {
            errors.AddRange(source.GetErrors(element, project));
        }

        return errors;
    }

    #region GUM0002 — VariableReference value disagrees with explicit set

    private List<ErrorResult> GetVariableReferenceConflictErrorsFor(ElementSave element)
    {
        var errors = new List<ErrorResult>();
        foreach (var state in element.AllStates)
        {
            CheckLocalReferenceConflicts(element, state, errors);
            CheckInheritedCategorizedReferenceConflicts(element, state, errors);
        }
        return errors;
    }

    private static void CheckLocalReferenceConflicts(ElementSave element, StateSave state, List<ErrorResult> errors)
    {
        foreach (var variableList in state.VariableLists)
        {
            if (variableList.GetRootName() != "VariableReferences" || variableList.ValueAsIList.Count == 0)
            {
                continue;
            }
            string? sourceObject = variableList.SourceObject;
            ElementSave? channelOwner = ElementSaveExtensions.ResolveChannelOwner(state, sourceObject);
            foreach (string referenceString in variableList.ValueAsIList)
            {
                // A collapsed composite line (e.g. "Color = Other.Color") never materializes a
                // literal "Color" scalar - only Red/Green/Blue do - so expand to the underlying
                // channels first, the same way ApplyVariableReferences does at apply time.
                foreach (string expandedReferenceString in ElementSaveExtensions.ExpandCompositeReferenceLine(referenceString, channelOwner))
                {
                    if (TryParseReference(expandedReferenceString, sourceObject, out string qualifiedLeft, out string right))
                    {
                        EmitConflictIfPresent(element, state, state, qualifiedLeft, right, errors);
                    }
                }
            }
        }
    }

    private static void CheckInheritedCategorizedReferenceConflicts(ElementSave element, StateSave state, List<ErrorResult> errors)
    {
        // For each instance state assignment in this state (e.g. TextInstance.TextCategoryState = "Title"),
        // walk the matched state on the instance's type (via inheritance) and check its VariableReferences
        // rows against the local explicit overrides on this state.
        for (int i = 0; i < state.Variables.Count; i++)
        {
            var variable = state.Variables[i];
            if (!variable.SetsValue || string.IsNullOrEmpty(variable.SourceObject)) continue;
            if (!variable.IsState(element, out _, out StateSaveCategory? category)) continue;
            if (category == null) continue; // only categorized states own references

            InstanceSave? instance = element.GetInstance(variable.SourceObject);
            if (instance == null) continue;
            ElementSave? instanceType = ObjectFinder.Self.GetElementSave(instance.BaseType);
            if (instanceType == null) continue;

            string? stateName = variable.Value as string;
            if (string.IsNullOrEmpty(stateName)) continue;
            StateSave? matchedState = instanceType.GetStateSaveRecursively(stateName);
            if (matchedState == null) continue;

            // If the source state already has a local VariableReferences row for this instance,
            // GetVariableListRecursive returns the local row immediately and the categorized
            // state's refs row never fires at apply time. Don't flag conflicts that won't happen.
            bool hasLocalRefsRow = state.VariableLists.Any(vl =>
                vl.GetRootName() == "VariableReferences" &&
                vl.SourceObject == variable.SourceObject);
            if (hasLocalRefsRow) continue;

            foreach (var variableList in matchedState.VariableLists)
            {
                if (variableList.GetRootName() != "VariableReferences" || variableList.ValueAsIList.Count == 0)
                {
                    continue;
                }
                ElementSave? channelOwner = ElementSaveExtensions.ResolveChannelOwner(matchedState, sourceObject: null);
                foreach (string referenceString in variableList.ValueAsIList)
                {
                    foreach (string expandedReferenceString in ElementSaveExtensions.ExpandCompositeReferenceLine(referenceString, channelOwner))
                    {
                        if (TryParseReference(expandedReferenceString, sourceObject: null, out string leftOnInstance, out string right))
                        {
                            string qualifiedLeft = $"{variable.SourceObject}.{leftOnInstance}";
                            EmitConflictIfPresent(element, state, matchedState, qualifiedLeft, right, errors);
                        }
                    }
                }
            }
        }
    }

    private static bool TryParseReference(string referenceString, string? sourceObject, out string qualifiedLeft, out string right)
    {
        qualifiedLeft = string.Empty;
        right = string.Empty;
        if (string.IsNullOrWhiteSpace(referenceString) || referenceString.StartsWith("//")) return false;
        string[] split = referenceString.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length != 2) return false;
        string left = split[0].Trim();
        right = split[1].Trim();
        qualifiedLeft = string.IsNullOrEmpty(sourceObject) ? left : $"{sourceObject}.{left}";
        return true;
    }

    private static void EmitConflictIfPresent(
        ElementSave element,
        StateSave localState,
        StateSave referenceOwnerState,
        string qualifiedLeft,
        string right,
        List<ErrorResult> errors)
    {
        VariableSave? materialized = localState.Variables.FirstOrDefault(v => v.Name == qualifiedLeft && v.SetsValue);
        if (materialized == null) return;

        // The Roslyn evaluator (GumExpressionService) returns null when desiredType is null
        // because its CastTo(null) returns false. Pass the materialized scalar's type so
        // type-aware evaluation succeeds for cross-element refs and literal RHSes.
        object? evaluated = EvaluateRightSide(referenceOwnerState, right, materialized.Type);
        if (evaluated == null) return;

        if (ValuesEqual(materialized.Value, evaluated)) return;

        errors.Add(new ErrorResult
        {
            ElementName = element.Name,
            Code = "GUM0002",
            Severity = ErrorSeverity.Warning,
            Message = $"{localState.Name}: \"{qualifiedLeft}\" is set explicitly to {FormatValue(materialized.Value)}, " +
                $"but the active VariableReference would set it to {FormatValue(evaluated)}. " +
                "The explicit value will win; remove the local override or change the reference to resolve the conflict."
        });
    }

    private static object? EvaluateRightSide(StateSave ownerState, string right, string? leftType)
    {
        // Mirror ElementSaveExtensions.GetRightSideValue: use the Roslyn-based
        // evaluator if it's wired (tool + CLI both initialize it via
        // GumExpressionService.Initialize), otherwise fall back to dot-path lookup.
        if (ElementSaveExtensions.CustomEvaluateExpression != null)
        {
            try
            {
                // No live wireframe in a headless check, so runtime-computed identifiers like
                // AbsoluteWidth are simply unresolvable here, same as any other unresolvable identifier.
                return ElementSaveExtensions.CustomEvaluateExpression(ownerState, right, leftType, null);
            }
            catch
            {
                return null;
            }
        }
        var rfv = new RecursiveVariableFinder(ownerState);
        return rfv.GetValue(right);
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a == null || b == null) return false;
        // Coerce numeric types so a materialized 14f compares equal to an evaluated 14.0.
        if (IsNumeric(a) && IsNumeric(b))
        {
            return Math.Abs(Convert.ToDouble(a) - Convert.ToDouble(b)) < 1e-6;
        }
        return a.Equals(b);
    }

    private static bool IsNumeric(object value) =>
        value is float || value is double || value is int || value is long || value is decimal;

    private static string FormatValue(object? value) => value?.ToString() ?? "(null)";

    #endregion

    #region GUM0009 — Variable reference reads from something the project does not have

    // An element-qualified path such as "Components/Folder/Styles.Primary.Red". Group 1 is the
    // element's path, group 2 the variable path after it (empty when the path names only the element).
    private static readonly Regex QualifiedReferencePathRegex = new Regex(
        @"(?<![\w.])((?:Components|Screens|Standards)/[\w/]+)((?:\.\w+)*)", RegexOptions.Compiled);

    // An unqualified path such as "Background.Width" or "Width", read from the reference's own element.
    private static readonly Regex LocalReferencePathRegex = new Regex(
        @"(?<![\w.:])[A-Za-z_]\w*(?:\.\w+)*", RegexOptions.Compiled);

    // String literals, and the reserved global:: identifiers (global::Localization.CurrentLanguage),
    // name nothing in the project.
    private static readonly Regex NonReferenceTextRegex = new Regex(
        @"""(?:[^""\\]|\\.)*""|global::[\w.]+", RegexOptions.Compiled);

    // A call to a supported function such as "Max(". Only the callee is matched
    // (the arguments are still scanned), so "Sin(Ghost.Width)" still reports Ghost.Width.
    private static readonly Regex FunctionCalleeRegex = new Regex(
        @"(?<![\w.:])(?:" + string.Join("|", Gum.Expressions.ExpressionFunctions.Names) + @")(?=\s*\()",
        RegexOptions.Compiled);

    private static readonly HashSet<string> ReferenceKeywords = new HashSet<string>(StringComparer.Ordinal)
    {
        "true", "false", "null",
    };

    // Resolved from the live layout in the tool (see EvaluatedSyntax), never stored on a state.
    private static readonly HashSet<string> RuntimeComputedVariableNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "AbsoluteX", "AbsoluteY", "AbsoluteLeft", "AbsoluteTop",
        "AbsoluteRight", "AbsoluteBottom", "AbsoluteWidth", "AbsoluteHeight",
        "Index",
    };

    /// <summary>
    /// Flags a <c>VariableReferences</c> line whose right side reads from an element, instance or
    /// variable the project does not have. Such a line never resolves, so the value it should set
    /// silently stays at its default. The usual causes are external edits and partial imports,
    /// since the tool's own renames keep references in sync.
    /// </summary>
    private static List<ErrorResult> GetUnresolvableVariableReferenceErrorsFor(ElementSave element)
    {
        var errors = new List<ErrorResult>();
        foreach (var state in element.AllStates)
        {
            foreach (var variableList in state.VariableLists)
            {
                if (variableList.GetRootName() != "VariableReferences" || variableList.ValueAsIList == null)
                {
                    continue;
                }
                ElementSave? channelOwner = ElementSaveExtensions.ResolveChannelOwner(state, variableList.SourceObject);
                foreach (object? item in variableList.ValueAsIList)
                {
                    if (item is not string line || string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("//"))
                    {
                        continue;
                    }
                    string? problem = ElementSaveExtensions.ExpandCompositeReferenceLine(line, channelOwner)
                        .Select(expanded => FindUnresolvableReference(element, variableList.SourceObject, expanded))
                        .FirstOrDefault(found => found != null);
                    if (problem == null)
                    {
                        continue;
                    }
                    errors.Add(new ErrorResult
                    {
                        ElementName = element.Name,
                        Code = "GUM0009",
                        Severity = ErrorSeverity.Warning,
                        Message = $"{state.Name}: the {variableList.Name} line \"{line}\" reads {problem}, so it never applies."
                    });
                }
            }
        }
        return errors;
    }

    /// <summary>
    /// Returns a description of the first thing the right side of <paramref name="line"/> reads
    /// that does not exist, or null when every path it reads resolves. An <c>@</c> prefix is resolved
    /// against <paramref name="ownerInstanceName"/>, the instance that owns the line (null on an
    /// element-level line, where <c>@</c> means the element itself).
    /// </summary>
    private static string? FindUnresolvableReference(ElementSave owner, string? ownerInstanceName, string line)
    {
        int equalsIndex = line.IndexOf('=');
        if (equalsIndex <= 0 || equalsIndex + 1 >= line.Length || line[equalsIndex + 1] == '=' || "!<>".IndexOf(line[equalsIndex - 1]) >= 0)
        {
            // Not an assignment; the tool comments such lines out and there is nothing to resolve.
            return null;
        }

        string rightSide = ElementSaveExtensions.ResolveOwnerPrefix(line.Substring(equalsIndex + 1), ownerInstanceName, ElementSaveExtensions.GetReferencableInstanceNames(owner));
        rightSide = NonReferenceTextRegex.Replace(rightSide, " ");
        rightSide = FunctionCalleeRegex.Replace(rightSide, " ");

        string? problem = null;
        rightSide = QualifiedReferencePathRegex.Replace(rightSide, match =>
        {
            if (problem == null)
            {
                string qualifiedElementName = match.Groups[1].Value;
                string elementName = qualifiedElementName.Substring(qualifiedElementName.IndexOf('/') + 1);
                string variablePath = match.Groups[2].Value.TrimStart('.');
                ElementSave? referencedElement = ObjectFinder.Self.GetElementSave(elementName);
                if (referencedElement == null || referencedElement.IsSourceFileMissing)
                {
                    problem = $"from {qualifiedElementName}, which the project does not have";
                }
                else if (variablePath.Length > 0 && !VariableExists(referencedElement, variablePath))
                {
                    problem = $"\"{variablePath}\" from {qualifiedElementName}, which has no such variable";
                }
            }
            return " ";
        });
        if (problem != null)
        {
            return problem;
        }

        foreach (Match match in LocalReferencePathRegex.Matches(rightSide))
        {
            string referencedName = ElementSaveExtensions.DecodeOwnerName(match.Value);
            if (!ReferenceKeywords.Contains(referencedName) && !VariableExists(owner, referencedName))
            {
                return $"\"{referencedName}\", which {owner.Name} does not have";
            }
        }
        return null;
    }

    /// <summary>
    /// Whether <paramref name="variablePath"/> (e.g. <c>Width</c> or <c>Instance.Red</c>) names a
    /// variable of <paramref name="element"/>, walking into instance types and base types the way
    /// reference evaluation does.
    /// </summary>
    private static bool VariableExists(ElementSave element, string variablePath)
    {
        VariableSave? exposed = SelfAndBaseElements(element)
            .Select(inChain => inChain.DefaultState?.Variables.FirstOrDefault(variable => variable.ExposedAsName == variablePath))
            .FirstOrDefault(found => found != null);
        if (exposed != null)
        {
            // An exposed variable is read through the instance variable it exposes. Only a dotted
            // name is followed: each step then shortens the path, so a hand-edited file where two
            // variables expose each other cannot recurse forever.
            return !exposed.Name.Contains('.') || VariableExists(element, exposed.Name);
        }

        int dot = variablePath.IndexOf('.');
        if (dot >= 0)
        {
            InstanceSave? instance = FindInstanceInInheritance(element, variablePath.Substring(0, dot));
            if (instance == null)
            {
                return false;
            }
            ElementSave? instanceType = ObjectFinder.Self.GetElementSave(instance.BaseType);
            // A missing instance type is already reported as a missing base type.
            return instanceType == null || VariableExists(instanceType, variablePath.Substring(dot + 1));
        }

        if (RuntimeComputedVariableNames.Contains(variablePath)
            || ObjectFinder.Self.GetRootVariable(variablePath, element) != null
            || ObjectFinder.Self.GetRootVariableList(variablePath, element) != null)
        {
            return true;
        }

        foreach (ElementSave inChain in SelfAndBaseElements(element))
        {
            if (inChain.AllStates.Any(state => state.Variables.Any(variable => variable.Name == variablePath)))
            {
                return true;
            }
            // Category selectors ("State", "ButtonCategoryState") are not stored until set.
            if (variablePath == "State"
                || (variablePath.EndsWith("State") && inChain.Categories.Any(category => category.Name + "State" == variablePath)))
            {
                return true;
            }
            // A project whose standard file predates a variable still has it through the defaults.
            if (inChain is StandardElementSave
                && StandardElementsManager.Self.TryGetDefaultStateFor(inChain.Name, throwExceptionOnMissing: false)?.Variables
                    .Any(variable => variable.Name == variablePath) == true)
            {
                return true;
            }
        }

        // A composite color name ("FillColor") stands for its channels ("FillRed", ...).
        if (variablePath.EndsWith("Color"))
        {
            return VariableExists(element, variablePath.Substring(0, variablePath.Length - "Color".Length) + "Red");
        }
        return false;
    }

    private static InstanceSave? FindInstanceInInheritance(ElementSave element, string instanceName)
    {
        foreach (ElementSave inChain in SelfAndBaseElements(element))
        {
            InstanceSave? instance = inChain.GetInstance(instanceName);
            if (instance != null)
            {
                return instance;
            }
        }
        return null;
    }

    private static IEnumerable<ElementSave> SelfAndBaseElements(ElementSave element)
    {
        var visited = new HashSet<ElementSave>();
        ElementSave? current = element;
        while (current != null && visited.Add(current))
        {
            yield return current;
            current = string.IsNullOrEmpty(current.BaseType) ? null : ObjectFinder.Self.GetElementSave(current.BaseType);
        }
    }

    #endregion

    #region GUM0003 — Category state sets its own category's selector

    /// <summary>
    /// Flags a state inside a category that sets that same category's selector variable
    /// (e.g. a <c>TextBoxCategory</c> state assigning <c>TextBoxCategoryState</c>). Such a
    /// self-reference is circular: applying the state re-drives the whole category, discarding
    /// the state's own authored values. It is never valid - the runtime only avoids an infinite
    /// loop via a recursion guard - and is the data corruption behind issue #3055.
    /// A child instance's selector (with a SourceObject, e.g. <c>Border.ColorCategoryState</c>)
    /// is the intended cascade and is deliberately not flagged.
    /// </summary>
    private static List<ErrorResult> GetSelfReferentialCategoryStateErrorsFor(ElementSave element)
    {
        var errors = new List<ErrorResult>();

        foreach (var category in element.Categories)
        {
            foreach (var state in category.States)
            {
                foreach (var variable in state.Variables)
                {
                    if (!variable.SetsValue || !string.IsNullOrEmpty(variable.SourceObject))
                    {
                        continue;
                    }

                    if (variable.IsState(element, out _, out StateSaveCategory? referencedCategory)
                        && referencedCategory != null
                        && referencedCategory.Name == category.Name)
                    {
                        errors.Add(new ErrorResult
                        {
                            ElementName = element.Name,
                            Code = "GUM0003",
                            Severity = ErrorSeverity.Warning,
                            Message = $"Category \"{category.Name}\" state \"{state.Name}\" sets its own " +
                                $"category selector \"{variable.Name}\". A state cannot select a state within " +
                                $"its own category - this is a circular reference that re-drives the category " +
                                $"when the state is applied, discarding the state's authored values. " +
                                $"Remove \"{variable.Name}\" from this state."
                        });
                    }
                }
            }
        }

        return errors;
    }

    #endregion

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
        var rfv = new RecursiveVariableFinder(component.DefaultState!);
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
                Message = message,
                Code = "GUM0001"
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

    #region GUM0004 — Missing source file

    /// <summary>
    /// Surfaces elements whose backing file (.gusx/.gucx/.gutx) was not found on disk when the
    /// project loaded. <see cref="ElementReference.ToElementSave{T}"/> sets
    /// <see cref="ElementSave.IsSourceFileMissing"/> in that case and the tree shows a red "!",
    /// but without this check the Errors tab stays empty - leaving the user no way to find out
    /// why the element is flagged (issue #3309).
    /// This is intentionally a listed error only; it must not block saving (see the historical
    /// comment in ElementReference.ToElementSave), because saving the element is exactly what
    /// recreates the missing file.
    /// </summary>
    private List<ErrorResult> GetMissingSourceFileErrorsFor(ElementSave element, GumProjectSave project, ISet<string>? caseMismatchPaths)
    {
        var errors = new List<ErrorResult>();

        var expectedRelativePath = $"{element.Subfolder}/{element.Name}." +
            element.GetFileExtension(GumProjectSave.IsJsonFormat(project?.FullFileName ?? ""));

        // A file that exists under a different case is GUM0008, whether or not this file system found it.
        if (TryGetCaseMismatchError(element.Name, project, element.Name, expectedRelativePath, caseMismatchPaths, out var caseMismatch))
        {
            errors.Add(caseMismatch);
            return errors;
        }

        if (element.IsSourceFileMissing)
        {
            errors.Add(new ErrorResult
            {
                ElementName = element.Name,
                Code = "GUM0004",
                Severity = ErrorSeverity.Error,
                Message = $"The source file for \"{element.Name}\" is missing. Gum expected it at " +
                    $"\"{expectedRelativePath}\" but could not find it on disk. The element still exists " +
                    $"in the project in memory - saving it will recreate the file, or restore the file " +
                    $"to resolve this error."
            });
        }

        return errors;
    }

    #endregion

    #region GUM0006 — Missing referenced external file

    /// <summary>
    /// Surfaces instance/element variables flagged IsFile (Sprite/Svg/LottieAnimation SourceFile,
    /// custom font files, etc.) whose value does not resolve to an existing file on disk. Reuses
    /// <see cref="GumProjectDependencyWalker"/> scoped to a single element - the same walk
    /// `gumcli pack` already performs to catch these before bundling - so missing references are
    /// no longer silently ignored by the interactive Errors tab / tree "!" (issue #4493).
    /// A Warning, not an Error: unlike a missing element/behavior file, a missing texture/font
    /// doesn't affect codegen correctness, and <c>CodegenCommand</c> treats any Error as blocking
    /// generation for that element - the same reasoning <see cref="GetAchxOriginErrorsFor"/>
    /// already uses for its content-drift warning.
    /// </summary>
    private List<ErrorResult> GetMissingExternalFileErrorsFor(ElementSave element, GumProjectSave project, ISet<string>? reportedCaseMismatchPaths)
    {
        var errors = new List<ErrorResult>();

        if (string.IsNullOrEmpty(project.FullFileName))
        {
            return errors;
        }

        var projectRootDirectory = FileManager.GetDirectory(project.FullFileName);
        var walker = new GumProjectDependencyWalker();
        var result = walker.Walk(project, projectRootDirectory, GumBundleInclusion.ExternalFiles, element);
        // A case-sensitive file system lists a mismatched file as missing and may also include it;
        // one GUM0008 per path, attributed to the instance when the walker names one.
        var caseMismatchPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var warning in result.MissingFiles)
        {
            if (TryGetCaseMismatchError(element.Name, project, warning.ReferencedFromElementName, warning.ReferencedPath, reportedCaseMismatchPaths, out var caseMismatch))
            {
                if (caseMismatchPaths.Add(warning.ReferencedPath))
                {
                    errors.Add(caseMismatch);
                }
                continue;
            }

            errors.Add(new ErrorResult
            {
                ElementName = element.Name,
                Code = "GUM0006",
                Severity = ErrorSeverity.Warning,
                Message = $"{warning.ReferencedFromElementName} references \"{warning.ReferencedPath}\", " +
                    $"which was not found on disk."
            });
        }

        // Files this file system found may still be spelled differently on disk (GUM0008).
        foreach (var includedFile in result.ExternalFiles.Concat(result.FontCacheFiles))
        {
            if (!caseMismatchPaths.Contains(includedFile)
                && TryGetCaseMismatchError(element.Name, project, element.Name, includedFile, reportedCaseMismatchPaths, out var caseMismatch))
            {
                caseMismatchPaths.Add(includedFile);
                errors.Add(caseMismatch);
            }
        }

        return errors;
    }

    #endregion

    #region GUM0008 — Referenced file name differs from the file on disk only by case

    /// <summary>
    /// A reference that resolves on Windows but not on a case-sensitive file system (Linux). Reported
    /// on every OS so the project is fixed where it was authored: a Warning where the file still
    /// loads, an Error where it does not. Replaces GUM0004/GUM0006 for that file, since "missing"
    /// would send the user looking for a file that is there. A found mismatch's path is added to
    /// <c>reportedPaths</c>.
    /// </summary>
    private bool TryGetCaseMismatchError(
        string elementName,
        GumProjectSave? project,
        string referencedFrom,
        string relativePath,
        ISet<string>? reportedPaths,
        out ErrorResult error,
        bool failsOnEveryFileSystem = false)
    {
        error = null!;
        if (string.IsNullOrEmpty(project?.FullFileName))
        {
            return false;
        }

        var projectRootDirectory = FileManager.GetDirectory(project!.FullFileName);
        var onDiskPath = _caseChecker.FindCaseMismatch(projectRootDirectory, relativePath);
        if (onDiskPath == null)
        {
            return false;
        }

        reportedPaths?.Add(relativePath);
        bool loadsHere = !failsOnEveryFileSystem && File.Exists(Path.Combine(projectRootDirectory, relativePath));
        error = new ErrorResult
        {
            ElementName = elementName,
            Code = "GUM0008",
            Severity = loadsHere ? ErrorSeverity.Warning : ErrorSeverity.Error,
            Message = $"{referencedFrom} references \"{relativePath}\", but the file on disk is named " +
                $"\"{onDiskPath}\". The names must match exactly on case-sensitive file systems.",
            FilePath = Path.Combine(projectRootDirectory, onDiskPath)
        };
        return true;
    }

    /// <summary>
    /// GUM0008 for the files the element checks don't reach: behavior and localization files,
    /// element animation files, and the files a referenced file loads (<c>.fnt</c> pages,
    /// <c>.achx</c> frames). Skips paths already in <paramref name="reportedPaths"/>.
    /// </summary>
    private List<ErrorResult> GetProjectFileCaseMismatchErrors(GumProjectSave project, ISet<string> reportedPaths)
    {
        var errors = new List<ErrorResult>();
        if (string.IsNullOrEmpty(project.FullFileName))
        {
            return errors;
        }

        var projectRootDirectory = FileManager.GetDirectory(project.FullFileName);
        var result = new GumProjectDependencyWalker().Walk(
            project, projectRootDirectory, GumBundleInclusion.Core | GumBundleInclusion.ExternalFiles);

        // The walker lists an animation file only when File.Exists finds it, which a case-sensitive
        // file system doesn't for a mismatched name, so they are listed here too. A mismatch is an
        // Error everywhere: the runtime names the animations after the file on disk and matches
        // that name to the element case-sensitively, so they never attach.
        var animationSuffix = ElementAnimationsSave.GetFileNameSuffix(GumProjectSave.IsJsonFormat(project.FullFileName));
        var animationFiles = new HashSet<string>(
            project.AllElements.Select(element => $"{element.Subfolder}/{element.Name}{animationSuffix}"),
            StringComparer.Ordinal);

        // The .gumx is opened by whatever path the caller passes, so its own name is not checked.
        var projectFileName = Path.GetFileName(project.FullFileName);

        var candidates = result.CoreFiles
            .Concat(result.ExternalFiles)
            .Concat(result.MissingFiles.Select(missing => missing.ReferencedPath))
            .Concat(animationFiles);

        foreach (var relativePath in candidates)
        {
            if (relativePath != projectFileName
                && !reportedPaths.Contains(relativePath)
                && TryGetCaseMismatchError("(project)", project, "The project", relativePath, reportedPaths, out var caseMismatch,
                    failsOnEveryFileSystem: animationFiles.Contains(relativePath)))
            {
                errors.Add(caseMismatch);
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

    #region GUM0007 — Enum variable value is not defined

    /// <summary>
    /// Enum variables are persisted as raw ints, so a hand-edited file, a merge, or a file written
    /// by a different Gum version can hold a number the enum does not define. Nothing on the load
    /// path rejects it, and the failure otherwise surfaces far from the data as a layout exception
    /// naming only the number.
    /// </summary>
    private IEnumerable<ErrorResult> GetInvalidEnumValueErrorsFor(ElementSave elementSave)
    {
        var errors = new List<ErrorResult>();

        foreach (var state in elementSave.AllStates)
        {
            foreach (var variable in state.Variables)
            {
                if (variable.Value == null || string.IsNullOrEmpty(variable.Type))
                {
                    continue;
                }

                var variableType = ResolveEnumType(variable.Type);
                if (variableType == null)
                {
                    continue;
                }

                if (IsDefinedValue(variableType, variable.Value))
                {
                    continue;
                }

                errors.Add(new ErrorResult
                {
                    ElementName = elementSave.Name,
                    Code = "GUM0007",
                    Severity = ErrorSeverity.Error,
                    Message =
                        $"The variable {variable.Name} in state {state.Name} has a value of " +
                        $"{variable.Value}, which is not a valid {variableType.Name}."
                });
            }
        }

        return errors;
    }

    /// <summary>
    /// Resolves a variable's type name to the enum behind it, unwrapping a nullable enum
    /// (<c>"Orientation?"</c>) to the enum itself, and returns null for anything that is not one.
    /// Results are cached because <see cref="ITypeResolver.GetTypeFromString"/> is a linear scan
    /// over every type in several assemblies, and this runs for every variable in every state.
    /// </summary>
    private Type? ResolveEnumType(string typeName)
    {
        if (_enumTypesByName.TryGetValue(typeName, out Type? cached))
        {
            return cached;
        }

        Type? resolved = _typeResolver.GetTypeFromString(typeName);
        resolved = Nullable.GetUnderlyingType(resolved ?? typeof(object)) ?? resolved;

        Type? toReturn = resolved?.IsEnum == true ? resolved : null;
        _enumTypesByName[typeName] = toReturn;
        return toReturn;
    }

    /// <summary>
    /// Returns whether <paramref name="value"/> names a member of <paramref name="enumType"/>.
    /// Values arrive either as the enum itself (once coerced on load) or as a raw number read from
    /// the file, which XML and JSON box as different integral types. Anything else - a string, a
    /// float - cannot be measured and is left to the variable-type checks. A combination of flags
    /// names no single member, so flags enums are treated as always valid.
    /// </summary>
    private static bool IsDefinedValue(Type enumType, object value)
    {
        if (enumType.IsDefined(typeof(FlagsAttribute), inherit: false))
        {
            return true;
        }

        if (value.GetType() == enumType)
        {
            return Enum.IsDefined(enumType, value);
        }

        if (value is not (byte or sbyte or short or ushort or int or uint or long or ulong))
        {
            return true;
        }

        try
        {
            return Enum.IsDefined(enumType, Convert.ChangeType(value, Enum.GetUnderlyingType(enumType)));
        }
        catch (OverflowException)
        {
            // Too large to be any member of the enum, so it is exactly the bad value being hunted.
            return false;
        }
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
        if (string.IsNullOrEmpty(sourceFile) ||
            !(sourceFile.EndsWith(".achx", StringComparison.OrdinalIgnoreCase) ||
              sourceFile.EndsWith(".achj", StringComparison.OrdinalIgnoreCase)))
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
