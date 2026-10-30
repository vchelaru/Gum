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

    // "Ghost.Text" in an element with no instance Ghost (in itself or any base element) sets nothing.
    private static List<ErrorResult> GetMissingInstanceVariableErrorsFor(ElementSave element)
    {
        List<ErrorResult> errors = new List<ErrorResult>();
        HashSet<string> reported = new HashSet<string>(StringComparer.Ordinal);

        foreach (StateSave state in element.AllStates)
        {
            foreach (VariableSave variable in state.Variables)
            {
                string? instanceName = variable.SourceObject;
                if (!string.IsNullOrEmpty(instanceName)
                    && FindInstanceInInheritance(element, instanceName!) == null
                    && reported.Add(instanceName!))
                {
                    errors.Add(Error(element, "GUM0027",
                        $"{element.Name} sets {variable.Name} in state {state.Name}, but {element.Name} has no instance named {instanceName}. " +
                        "Add the instance or delete the variable."));
                }
            }
        }

        return errors;
    }

    #endregion

    #region GUM0028 — Variable with a type name nothing can resolve

    // A state variable's type is the name of a category, which any element of the project can define (older projects
    // wrote it without the State suffix, and an exposed variable keeps the type of the variable it exposes).
    private static bool IsCategoryNameInProject(string typeName)
    {
        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        return project != null
            && project.AllElements.Any(element => element.Categories.Any(category => category.Name == typeName));
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
                // A category selector's type is a category name; whether the category exists is GUM0026's business.
                if (string.IsNullOrEmpty(type) || KnownBaseTypes.Contains(type!) || variable.GetRootName().EndsWith("State", StringComparison.Ordinal)
                    || _typeResolver.GetTypeFromString(type!) != null || IsCategoryNameInProject(type!))
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
