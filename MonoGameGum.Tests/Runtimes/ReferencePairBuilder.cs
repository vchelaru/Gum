using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.GueDeriving;
using Gum.Managers;
using GumRuntime;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Builds the "Pair" component the variable-reference tests share: Text instances Source and Target with one
/// reference row on Target, registered as the current project.
/// </summary>
internal static class ReferencePairBuilder
{
    /// <summary>
    /// The first authored value is Target's, the second Source's. A variable's declared type comes from its value
    /// unless <paramref name="targetType"/> or <paramref name="sourceType"/> gives it. A null source value leaves the
    /// variable off the state, which is a reference to a variable that has no value. The instances are Text unless
    /// <paramref name="targetBaseType"/> or <paramref name="sourceBaseType"/> says Container.
    /// </summary>
    public static ComponentSave Build(string row, bool useExpressions, (string Name, object? Value)[] authored,
        string? targetType = null, string? sourceType = null, string targetBaseType = "Text", string sourceBaseType = "Text")
    {
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }

        GumProjectSave project = new GumProjectSave();
        foreach (string name in new[] { "Container", "Text" })
        {
            StandardElementSave element = new StandardElementSave { Name = name };
            element.States.Add(StandardElementsManager.Self.GetDefaultStateFor(name)!);
            element.DefaultState!.ParentContainer = element;
            project.StandardElements.Add(element);
        }
        StandardRuntimeRegistrations.Register();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave pair = new ComponentSave { Name = "Pair", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = pair };
        pair.States.Add(state);
        pair.Instances.Add(new InstanceSave { Name = "Source", BaseType = sourceBaseType, ParentContainer = pair });
        pair.Instances.Add(new InstanceSave { Name = "Target", BaseType = targetBaseType, ParentContainer = pair });
        AddAuthored(state, "Target", authored[0], targetType);
        AddAuthored(state, "Source", authored[1], sourceType);
        VariableListSave<string> rows = new VariableListSave<string> { Type = "string", Name = "Target.VariableReferences" };
        rows.Value.Add(row);
        state.VariableLists.Add(rows);
        project.Components.Add(pair);
        return pair;
    }

    private static void AddAuthored(StateSave state, string instance, (string Name, object? Value) variable, string? declaredType)
    {
        if (variable.Value == null)
        {
            return;
        }

        string type = declaredType ?? variable.Value switch
        {
            int => "int",
            float => "float",
            double => "double",
            bool => "bool",
            string => "string",
            _ => variable.Value.GetType().Name
        };
        if (declaredType == null
            && (variable.Name == "MinWidth" || variable.Name == "MaxWidth" || variable.Name == "MinHeight" || variable.Name == "MaxHeight" || variable.Name == "MaxNumberOfLines"))
        {
            type += "?";
        }
        state.Variables.Add(new VariableSave { Name = $"{instance}.{variable.Name}", Type = type, Value = variable.Value, SetsValue = true });
    }
}
