using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;

namespace Gum.ProjectServices.Tests;

public class BaseTestClass : IDisposable
{
    protected GumProjectSave Project { get; }

    public BaseTestClass()
    {
        StandardElementsManager.Self.Initialize();

        Project = new GumProjectSave();

        foreach (string name in new[] { "Container", "NineSlice", "Sprite", "Text", "ColoredRectangle" })
        {
            StandardElementSave element = new StandardElementSave { Name = name };
            StateSave defaultState = new StateSave { Name = "Default" };
            defaultState.ParentContainer = element;
            element.States.Add(defaultState);
            Project.StandardElements.Add(element);
        }
    }

    public virtual void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }
}
