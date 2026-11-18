using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.ProjectServices.Editing;

/// <summary>
/// Adds a new Screen or Component to a loaded project: the data change behind the tool's Add Screen and Add
/// Component, without saving, selection or plugin notification.
/// </summary>
public interface IElementCreationLogic
{
    /// <summary>
    /// Gives <paramref name="screen"/> the Screen defaults and adds it and its reference to <paramref name="project"/>.
    /// </summary>
    void AddScreen(GumProjectSave project, ScreenSave screen);

    /// <summary>
    /// Names a new component and gives it the default and Component variables, with X and Y left unset and
    /// HasEvents on, as the tool creates components.
    /// </summary>
    /// <param name="component">The new component.</param>
    /// <param name="name">The name, including any folder (for example <c>Controls/Counter</c>).</param>
    /// <param name="baseType">The standard element or component it derives from.</param>
    void PrepareNewComponent(ComponentSave component, string name, string baseType);

    /// <summary>
    /// Adds <paramref name="component"/> and its reference to <paramref name="project"/>.
    /// </summary>
    void AddComponent(GumProjectSave project, ComponentSave component);
}

/// <inheritdoc/>
public class ElementCreationLogic : IElementCreationLogic
{
    /// <inheritdoc/>
    public void AddScreen(GumProjectSave project, ScreenSave screen)
    {
        screen.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Screen"));
        project.ScreenReferences.Add(new ElementReference { Name = screen.Name, ElementType = ElementType.Screen });
        project.Screens.Add(screen);
        project.SortElementsAndReferencesByName();
    }

    /// <inheritdoc/>
    public void PrepareNewComponent(ComponentSave component, string name, string baseType)
    {
        component.Name = name;
        component.BaseType = baseType;
        component.InitializeDefaultAndComponentVariables();

        // Components shouldn't set their positions to 0 by default.
        StateSave defaultState = component.GetDefaultStateOrThrow();
        foreach (string positionName in new[] { "X", "Y" })
        {
            if (defaultState.GetVariableSave(positionName) is { } position)
            {
                position.Value = null;
                position.SetsValue = false;
            }
        }

        if (defaultState.GetVariableSave("HasEvents") is { } hasEvents)
        {
            hasEvents.Value = true;
        }
    }

    /// <inheritdoc/>
    public void AddComponent(GumProjectSave project, ComponentSave component)
    {
        project.ComponentReferences.Add(new ElementReference { Name = component.Name, ElementType = ElementType.Component });
        project.Components.Add(component);
        project.SortElementsAndReferencesByName();
    }
}
