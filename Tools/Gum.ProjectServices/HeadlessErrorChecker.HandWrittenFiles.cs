using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Gum.ProjectServices;

// Checks for shapes the Gum tool never saves but a hand-written or agent-written project can hold (#7120 to #7124).
public partial class HeadlessErrorChecker
{
    // Project-level codes that also stop codegen for the element they name (see CodegenCommand).
    public static readonly IReadOnlyList<string> CodegenGatingProjectCodes = new[] { "GUM0015", "GUM0023" };

    private static readonly HashSet<string> NumericTypes = new HashSet<string>(StringComparer.Ordinal)
    {
        "int", "int?", "long", "long?", "float", "float?", "double", "double?", "decimal", "decimal?"
    };

    private static readonly HashSet<string> BoolTypes = new HashSet<string>(StringComparer.Ordinal) { "bool", "bool?" };

    private static ErrorResult Error(ElementSave element, string code, string message, ErrorSeverity severity = ErrorSeverity.Error) =>
        new ErrorResult { ElementName = element.Name, Code = code, Severity = severity, Message = message };

    #region GUM0019 — Value does not fit the variable's type

    private static List<ErrorResult> GetValueTypeMismatchErrorsFor(ElementSave element)
    {
        List<ErrorResult> errors = new List<ErrorResult>();
        HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (StateSave state in element.AllStates)
        {
            foreach (VariableSave variable in state.Variables)
            {
                if (!variable.SetsValue || variable.Value == null || string.IsNullOrEmpty(variable.Type))
                {
                    continue;
                }

                bool isNumeric = NumericTypes.Contains(variable.Type);
                bool isBool = BoolTypes.Contains(variable.Type);
                if ((!isNumeric && !isBool) || FitsType(variable.Value, isNumeric) || !reported.Add(state.Name + "|" + variable.Name))
                {
                    continue;
                }

                errors.Add(Error(element, "GUM0019",
                    $"{variable.Name} in state {state.Name} is a {variable.Type} variable but holds the {variable.Value.GetType().Name} value '{variable.Value}'. " +
                    $"Set it to a {(isBool ? "true or false" : "number")}."));
            }
        }

        return errors;
    }

    private static bool FitsType(object value, bool isNumeric)
    {
        if (value is string text)
        {
            // The runtime skips a value it cannot convert, so an empty one leaves the property unset.
            return text.Length == 0 || (isNumeric
                ? double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                : bool.TryParse(text, out _));
        }
        return isNumeric ? value is not bool : value is bool;
    }

    #endregion

    #region GUM0020 / GUM0021 / GUM0022 — Duplicate names inside one element

    // Lookups by name return the first match, so the second item is silently dead (or arbitrary after a re-sort).
    private static List<ErrorResult> GetDuplicateNameErrorsFor(ElementSave element)
    {
        List<ErrorResult> errors = new List<ErrorResult>();

        foreach (string name in DuplicatesOf(element.Instances.Select(instance => instance.Name)))
        {
            errors.Add(Error(element, "GUM0020",
                $"{element.Name} has more than one instance named {name}. Rename or delete all but one; which one is used is arbitrary."));
        }

        foreach (string name in DuplicatesOf(element.States.Select(state => state.Name)))
        {
            errors.Add(Error(element, "GUM0021", $"{element.Name} has more than one state named {name}. Rename or delete all but one."));
        }
        foreach (string name in DuplicatesOf(element.Categories.Select(category => category.Name)))
        {
            errors.Add(Error(element, "GUM0021", $"{element.Name} has more than one state category named {name}. Rename or delete all but one."));
        }
        foreach (StateSaveCategory category in element.Categories)
        {
            foreach (string name in DuplicatesOf(category.States.Select(state => state.Name)))
            {
                errors.Add(Error(element, "GUM0021", $"{element.Name} has more than one state named {name} in the category {category.Name}. Rename or delete all but one."));
            }
        }

        foreach (StateSave state in element.AllStates)
        {
            foreach (string name in DuplicatesOf(state.Variables.Select(variable => variable.Name)))
            {
                errors.Add(Error(element, "GUM0022",
                    $"State {state.Name} of {element.Name} sets {name} more than once. Delete all but one; which value wins is arbitrary."));
            }
        }

        return errors;
    }

    private static IEnumerable<string> DuplicatesOf(IEnumerable<string?> names) =>
        names.Where(name => !string.IsNullOrEmpty(name)).GroupBy(name => name!, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).Select(group => group.Key);

    #endregion

    #region GUM0023 — Element or behavior referenced twice in the project file

    // Project-level: each reference loads its own copy, so there are two elements with one name.
    private static List<ErrorResult> GetDuplicateReferenceErrors(GumProjectSave project)
    {
        List<ErrorResult> errors = new List<ErrorResult>();
        (string Kind, IEnumerable<string?> Names)[] lists =
        {
            ("Screen", project.ScreenReferences.Select(reference => reference.Name)),
            ("Component", project.ComponentReferences.Select(reference => reference.Name)),
            ("Standard element", project.StandardElementReferences.Select(reference => reference.Name)),
            ("Behavior", project.BehaviorReferences.Select(reference => reference.Name)),
        };

        foreach ((string kind, IEnumerable<string?> names) in lists)
        {
            foreach (string name in DuplicatesOf(names))
            {
                errors.Add(new ErrorResult
                {
                    ElementName = name,
                    Code = "GUM0023",
                    Severity = ErrorSeverity.Error,
                    Message = $"{kind} {name} is referenced more than once in the project file, so it loads as two items with one name. Delete the extra reference."
                });
            }
        }

        return errors;
    }

    #endregion

    #region GUM0024 — Parent cycle

    // Following Parent from an instance must end at an instance with no parent (or at the element itself).
    private static List<ErrorResult> GetParentCycleErrorsFor(ElementSave element)
    {
        List<ErrorResult> errors = new List<ErrorResult>();
        HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
        Dictionary<string, string> inherited = new Dictionary<string, string>(StringComparer.Ordinal);

        // Parents set by base elements apply unless a derived element overrides them; base-most first.
        foreach (ElementSave inChain in SelfAndBaseElements(element).Reverse())
        {
            AddParents(inChain.DefaultState, inherited);
        }

        foreach (StateSave state in element.AllStates)
        {
            Dictionary<string, string> parents = new Dictionary<string, string>(inherited, StringComparer.Ordinal);
            AddParents(state, parents);

            foreach (string start in parents.Keys)
            {
                List<string> path = new List<string> { start };
                string current = start;
                while (parents.TryGetValue(current, out string? parent))
                {
                    int seen = path.IndexOf(parent);
                    if (seen >= 0)
                    {
                        List<string> cycle = path.Skip(seen).ToList();
                        if (reported.Add(string.Join("|", cycle.OrderBy(name => name, StringComparer.Ordinal))))
                        {
                            errors.Add(Error(element, "GUM0024",
                                $"Parents in {element.Name} form a loop: {string.Join(" -> ", cycle.Append(cycle[0]))}. Clear or change the Parent of one of them."));
                        }
                        break;
                    }
                    path.Add(parent);
                    current = parent;
                }
            }
        }

        return errors;
    }

    private static void AddParents(StateSave? state, Dictionary<string, string> parents)
    {
        foreach (VariableSave variable in state?.Variables ?? new List<VariableSave>())
        {
            if (!string.IsNullOrEmpty(variable.SourceObject) && variable.GetRootName() == "Parent" && variable.Value is string value)
            {
                // An empty value in a derived element clears the parent a base element set.
                int dot = value.IndexOf('.');
                if (value.Length == 0)
                {
                    parents.Remove(variable.SourceObject!);
                }
                else
                {
                    parents[variable.SourceObject!] = dot >= 0 ? value.Substring(0, dot) : value;
                }
            }
        }
    }

    #endregion

    #region GUM0025 — Screen used as a type

    // A Screen is a whole page: it can be neither an instance nor a component's base. A name shared by a Screen and
    // a Component or Standard Element is not reported: an instance can only be the latter, and the plain lookup
    // would pick either depending on whether the cache is on.
    private static bool IsOnlyAScreen(string? name) =>
        !string.IsNullOrEmpty(name)
        && ObjectFinder.Self.GetScreen(name) != null
        && ObjectFinder.Self.GetComponent(name) == null
        && ObjectFinder.Self.GetStandardElement(name) == null;

    private static List<ErrorResult> GetScreenAsTypeErrorsFor(ElementSave element)
    {
        List<ErrorResult> errors = new List<ErrorResult>();

        foreach (InstanceSave instance in element.Instances)
        {
            if (IsOnlyAScreen(instance.BaseType))
            {
                errors.Add(Error(element, "GUM0025",
                    $"Instance {instance.Name} of {element.Name} has the Screen {instance.BaseType} as its type. A Screen cannot be an instance; use a Component."));
            }
        }

        if (element is ComponentSave && !string.IsNullOrEmpty(element.BaseType) && IsOnlyAScreen(element.BaseType))
        {
            errors.Add(Error(element, "GUM0025",
                $"Component {element.Name} has the Screen {element.BaseType} as its base type. A component can only derive from a component or a standard element."));
        }

        return errors;
    }

    #endregion

    #region GUM0026 — State variable names a state its category does not have

    private static List<ErrorResult> GetUnknownStateValueErrorsFor(ElementSave element)
    {
        List<ErrorResult> errors = new List<ErrorResult>();
        HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (StateSave state in element.AllStates)
        {
            foreach (VariableSave variable in state.Variables)
            {
                string rootName = variable.GetRootName();
                if (!rootName.EndsWith("State", StringComparison.Ordinal)
                    || variable.Value is not string stateName || stateName.Length == 0
                    || (!string.IsNullOrEmpty(variable.SourceObject) && FindInstanceInInheritance(element, variable.SourceObject!) == null)
                    || !variable.IsState(element, out ElementSave? categoryContainer, out StateSaveCategory? category)
                    || categoryContainer == null)
                {
                    continue;
                }
                // A category state that selects a state of its own category is GUM0003.
                if (category != null && element.Categories.Any(item => item.Name == category.Name && item.States.Contains(state)))
                {
                    continue;
                }

                List<string> available = category != null
                    ? SelfAndBaseElements(categoryContainer).SelectMany(inChain => inChain.Categories).Where(item => item.Name == category.Name)
                        .SelectMany(item => item.States).Select(item => item.Name).ToList()
                    : categoryContainer.States.Select(item => item.Name).ToList();

                if (!available.Contains(stateName) && reported.Add(variable.Name + "|" + stateName))
                {
                    string where = category != null ? $"the category {category.Name}" : categoryContainer.Name;
                    errors.Add(Error(element, "GUM0026",
                        $"{element.Name} sets {variable.Name} to {stateName}, but {where} has no state with that name (it has {(available.Count == 0 ? "none" : string.Join(", ", available))})."));
                }
            }
        }

        return errors;
    }

    #endregion
}
