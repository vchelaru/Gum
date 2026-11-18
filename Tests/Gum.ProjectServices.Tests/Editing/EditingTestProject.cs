using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.ProjectServices.Editing;

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

    public static ScreenSave AddScreen(GumProjectSave project, string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        project.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave = project;
        return screen;
    }

    public static InstanceSave AddInstance(ElementSave element, string name, string baseType)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = baseType, ParentContainer = element };
        element.Instances.Add(instance);
        return instance;
    }

    public static StateSaveCategory AddCategory(ElementSave element, string name, params string[] stateNames)
    {
        StateSaveCategory category = new StateSaveCategory { Name = name };
        foreach (string stateName in stateNames)
        {
            category.States.Add(new StateSave { Name = stateName, ParentContainer = element });
        }
        element.Categories.Add(category);
        return category;
    }

    public static ReferenceFinder CreateReferenceFinder() => new ReferenceFinder(new ObjectFinderProjectProvider());
}
