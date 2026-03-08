using Gum.DataTypes;
using Gum.Managers;

namespace Gum.ProjectServices.Tests;

public class BaseTestClass : IDisposable
{
    protected GumProjectSave Project { get; }

    public BaseTestClass()
    {
        Project = new GumProjectSave();

        Project.StandardElements.Add(new StandardElementSave { Name = "Container" });
        Project.StandardElements.Add(new StandardElementSave { Name = "NineSlice" });
        Project.StandardElements.Add(new StandardElementSave { Name = "Sprite" });
        Project.StandardElements.Add(new StandardElementSave { Name = "Text" });
        Project.StandardElements.Add(new StandardElementSave { Name = "ColoredRectangle" });
    }

    public virtual void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }
}
