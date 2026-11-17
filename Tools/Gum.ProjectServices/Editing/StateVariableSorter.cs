using Gum.DataTypes;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Sorts every state's variables by name, the order a project save writes them in, so a file's bytes do not
/// depend on which save wrote it.
/// </summary>
public interface IStateVariableSorter
{
    /// <summary>
    /// Sorts the variables of every state in <paramref name="container"/> by <see cref="GumProjectSave.CompareNames"/>.
    /// </summary>
    void SortStateVariables(IStateContainer container);
}

/// <inheritdoc/>
public class StateVariableSorter : IStateVariableSorter
{
    /// <inheritdoc/>
    public void SortStateVariables(IStateContainer container)
    {
        foreach (var state in container.AllStates)
        {
            state.Variables.Sort((first, second) => GumProjectSave.CompareNames(first.Name, second.Name));
        }
    }
}
