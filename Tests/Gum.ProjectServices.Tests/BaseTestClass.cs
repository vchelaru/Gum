using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Tests.Shared;

namespace Gum.ProjectServices.Tests;

public class BaseTestClass : IDisposable
{
    // Captured before the constructor body runs, so it is the state the previous test left.
    private readonly FileManagerState _fileManagerState = FileManagerState.Capture();

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
        ObjectFinder.Self.ClearFallbackStandardElements();
        _fileManagerState.Restore();
    }
}
