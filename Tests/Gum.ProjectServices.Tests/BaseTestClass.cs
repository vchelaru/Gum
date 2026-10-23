using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Tests.Shared;

namespace Gum.ProjectServices.Tests;

public class BaseTestClass : IDisposable
{
    // Captured before the constructor body runs, so it is the state the previous test left.
    private readonly FileManagerState _fileManagerState = FileManagerState.Capture();
    private readonly GraphicalUiElementState _graphicalUiElementState = GraphicalUiElementState.Capture();

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
        ObjectFinderState.Reset();
        _fileManagerState.Restore();
        _graphicalUiElementState.Restore();
    }
}
