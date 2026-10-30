using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.ProjectServices;

// Variables that name something the project does not have (#7119).
public partial class HeadlessErrorChecker
{
    #region GUM0027 — Variable names an instance the element does not have

    /// <summary>
    /// Whether GUM0027 is reported. Off by default: the Gum tool leaves such variables behind and hides them, so its
    /// Errors tab must not list them. <c>gumcli check</c> turns it on, as a warning.
    /// </summary>
    public bool ReportsLeftoverInstanceVariables { get; init; }

    // "Ghost.Text" in an element with no instance Ghost (in itself or any base element) sets nothing. A warning, not an
    // error: the tool leaves such variables behind (e.g. when an instance is deleted or its base type changes) and
    // hides them, so the user cannot always remove them.
    private List<ErrorResult> GetMissingInstanceVariableErrorsFor(ElementSave element)
    {
        List<ErrorResult> errors = new List<ErrorResult>();
        HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (StateSave state in element.AllStates)
        {
            foreach (VariableSave variable in state.Variables)
            {
                string? instanceName = variable.SourceObject;
                if (ReportsLeftoverInstanceVariables
                    && !string.IsNullOrEmpty(instanceName)
                    && FindInstanceInInheritance(element, instanceName!) == null
                    && reported.Add(instanceName!))
                {
                    errors.Add(Error(element, "GUM0027",
                        $"{element.Name} sets {variable.Name} in state {state.Name}, but {element.Name} has no instance named {instanceName}. " +
                        "The value is ignored.", ErrorSeverity.Warning));
                }
            }
        }

        return errors;
    }

    #endregion

    #region GUM0028 — Variable with a type name nothing can resolve

    private static bool IsExposedOnInstanceType(ElementSave element, VariableSave variable)
    {
        string? instanceName = variable.SourceObject;
        if (string.IsNullOrEmpty(instanceName))
        {
            return false;
        }
        InstanceSave? instance = FindInstanceInInheritance(element, instanceName!);
        ElementSave? instanceType = instance == null ? null : ObjectFinder.Self.GetElementSave(instance.BaseType);
        return instanceType != null && FindExposedVariable(instanceType, variable.GetRootName()) != null;
    }

    // A state variable's type is the name of a category, which any element of the project can define (older projects
    // wrote it without the State suffix, and an exposed variable keeps the type of the variable it exposes).
    private static bool IsCategoryNameInProject(string typeName)
    {
        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        return project != null
            && project.AllElements.Any(element => element.Categories.Any(category => category.Name == typeName));
    }

    // Names the tool writes that no .NET resolver in the headless checker knows: the XNA/Skia/Numerics value types
    // the tool and generated code use, and the C# keywords.
    private static readonly HashSet<string> WellKnownTypeNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "bool", "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong", "float", "double", "decimal", "char", "string", "object",
        "Color", "SKColor", "Vector2", "Vector3", "Vector4", "Point", "Rectangle", "Matrix", "Quaternion", "Texture2D", "SKBitmap",
        "Blend", "State"
    };

    // Unwraps nullable, array and generic syntax and checks every inner name, so "Dictionary<string, Color>[]?" is
    // resolvable when each of its names is. A namespace prefix is ignored.
    private bool IsResolvableTypeName(string typeName)
    {
        string name = typeName.Trim();
        if (name.Length == 0)
        {
            return true;
        }
        if (name.EndsWith("?", StringComparison.Ordinal))
        {
            return IsResolvableTypeName(name.Substring(0, name.Length - 1));
        }
        if (name.EndsWith("[]", StringComparison.Ordinal))
        {
            return IsResolvableTypeName(name.Substring(0, name.Length - 2));
        }

        int open = name.IndexOf('<');
        if (open > 0 && name.EndsWith(">", StringComparison.Ordinal))
        {
            string outer = name.Substring(0, open);
            return (WellKnownGenericNames.Contains(outer.Substring(outer.LastIndexOf('.') + 1)) || IsResolvableTypeName(outer))
                && SplitTopLevel(name.Substring(open + 1, name.Length - open - 2)).All(IsResolvableTypeName);
        }

        string shortName = name.Substring(name.LastIndexOf('.') + 1);
        return KnownBaseTypes.Contains(name)
            || WellKnownTypeNames.Contains(shortName)
            || _typeResolver.GetTypeFromString(name) != null
            || _gumTypes.GetTypeFromString(name) != null
            || _typeResolver.GetTypeFromString(shortName) != null
            || ObjectFinder.Self.GetElementSave(name) != null
            || IsCategoryNameInProject(name);
    }

    // The tool's own type names (HorizontalAlignment, GradientType, ...) resolve whatever resolver the caller injected.
    private static readonly DefaultTypeResolver _gumTypes = new DefaultTypeResolver();

    private static readonly HashSet<string> WellKnownGenericNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "List", "IList", "IEnumerable", "ICollection", "Dictionary", "IDictionary", "HashSet", "ObservableCollection", "Nullable"
    };

    private static IEnumerable<string> SplitTopLevel(string arguments)
    {
        int depth = 0;
        int start = 0;
        for (int i = 0; i < arguments.Length; i++)
        {
            if (arguments[i] == '<') depth++;
            else if (arguments[i] == '>') depth--;
            else if (arguments[i] == ',' && depth == 0)
            {
                yield return arguments.Substring(start, i - start);
                start = i + 1;
            }
        }
        yield return arguments.Substring(start);
    }

    private List<ErrorResult> GetUnknownVariableTypeErrorsFor(ElementSave element)
    {
        List<ErrorResult> errors = new List<ErrorResult>();
        HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (StateSave state in element.AllStates)
        {
            foreach (VariableSave variable in state.Variables)
            {
                string? type = variable.Type;
                if (IsExposedOnInstanceType(element, variable))
                {
                    // An exposed variable keeps the type it had when it was set; the tool does not rewrite it when the
                    // category or enum behind it is renamed.
                    continue;
                }
                // A category selector's type is a category name; whether the category exists is GUM0026's business.
                // A category selector's type is a category name; whether the category exists is GUM0026's business.
                if (string.IsNullOrEmpty(type) || variable.GetRootName().EndsWith("State", StringComparison.Ordinal)
                    || IsResolvableTypeName(type!))
                {
                    continue;
                }
                if (reported.Add(variable.Name + "|" + type))
                {
                    errors.Add(Error(element, "GUM0028",
                        $"{variable.Name} in state {state.Name} has the type {type}, which is not a known type or state category.", ErrorSeverity.Warning));
                }
            }
        }
        return errors;
    }

    #endregion
}
