using Gum.DataTypes;
using Gum.DataTypes.Variables;

namespace Gum.Managers;

/// <summary>
/// Answers whether a project's engine profile hides a standard variable. A hidden variable is dormant: it
/// stays in the file but is not shown, not rendered and not generated.
/// </summary>
public interface IVariableRestrictions
{
    /// <summary>
    /// True when the project's variable rules hide the variable for the standard type, using the same
    /// exemptions as the Variables tab. A null type matches only unqualified rules.
    /// </summary>
    bool IsVariableHidden(string? standardTypeName, string variableRootName);
}

/// <summary>
/// Element-aware queries on <see cref="IVariableRestrictions"/> shared by the Variables tab and code generation.
/// </summary>
public static class VariableRestrictionsExtensions
{
    /// <summary>
    /// True when the profile's rules hide <paramref name="variable"/>. Custom variables and state variables
    /// are never hidden. The standard type is the one that owns the variable: the root standard type of
    /// <paramref name="instance"/>, else of the instance <see cref="VariableSave.SourceObject"/> names in
    /// <paramref name="element"/>, else of <paramref name="element"/> itself.
    /// </summary>
    public static bool IsVariableHidden(this IVariableRestrictions restrictions, ElementSave element, InstanceSave? instance, VariableSave variable)
    {
        if (variable.IsCustomVariable || variable.IsState(element, out _, out _))
        {
            return false;
        }

        StandardElementSave? rootStandard;
        if (instance != null)
        {
            rootStandard = ObjectFinder.Self.GetRootStandardElementSave(instance);
        }
        else if (!string.IsNullOrEmpty(variable.SourceObject) &&
            element.GetInstance(variable.SourceObject) is InstanceSave sourceInstance)
        {
            rootStandard = ObjectFinder.Self.GetRootStandardElementSave(sourceInstance);
        }
        else
        {
            rootStandard = ObjectFinder.Self.GetRootStandardElementSave(element);
        }

        return restrictions.IsVariableHidden(rootStandard?.Name, variable.GetRootName());
    }
}

/// <summary>
/// An <see cref="IVariableRestrictions"/> backed by one loaded <see cref="EngineProfile"/>. Used where there is
/// no tool to track the project, such as <c>gumcli codegen</c>.
/// </summary>
public class EngineProfileVariableRestrictions : IVariableRestrictions
{
    private readonly EngineProfile _profile;
    private readonly ProjectVariableRuleMatcher _matcher = new ProjectVariableRuleMatcher();

    /// <summary>Creates restrictions that apply <paramref name="profile"/>.</summary>
    public EngineProfileVariableRestrictions(EngineProfile profile)
    {
        _profile = profile;
    }

    /// <inheritdoc/>
    public bool IsVariableHidden(string? standardTypeName, string variableRootName) =>
        _matcher.IsHiddenByProfile(_profile, standardTypeName, variableRootName);
}
