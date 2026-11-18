using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.ProjectServices.Tests.Editing;

/// <summary>
/// Builds the elements the editing tests share and registers the project with <see cref="ObjectFinder"/>.
/// </summary>
internal static class EditingTestProject
{
    public static ComponentSave AddComponent(GumProjectSave project, string name, string baseType = "Container")
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = project;
        return component;
    }
}
