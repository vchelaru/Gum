using EditorTabPlugin_XNA.Services;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;

namespace Gum.Presentation.Tests.Plugins.InternalPlugins.EditorTab;

public class FileDropTargetFilterTests : BaseTestClass
{
    [Fact]
    public void CanReceiveSourceFile_InstanceOfComponentWithMissingBaseType_ReturnsFalse()
    {
        ComponentSave orphan = new ComponentSave { Name = "Orphan", BaseType = "DeletedComponent" };
        orphan.States.Add(new StateSave { Name = "Default", ParentContainer = orphan });
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(orphan);
        ObjectFinder.Self.GumProjectSave = project;
        InstanceSave instance = new InstanceSave { Name = "OrphanInstance", BaseType = "Orphan" };
        FileDropTargetFilter filter = new FileDropTargetFilter();

        bool result = filter.CanReceiveSourceFile(instance);

        result.ShouldBeFalse();
    }

    [Fact]
    public void CanReceiveSourceFile_SpriteInstance_ReturnsTrue()
    {
        StandardElementSave sprite = new StandardElementSave { Name = "Sprite" };
        StateSave spriteDefault = new StateSave { Name = "Default", ParentContainer = sprite };
        spriteDefault.Variables.Add(new VariableSave { Type = "string", Name = "SourceFile", SetsValue = true });
        sprite.States.Add(spriteDefault);
        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(sprite);
        ObjectFinder.Self.GumProjectSave = project;
        InstanceSave instance = new InstanceSave { Name = "SpriteInstance", BaseType = "Sprite" };
        FileDropTargetFilter filter = new FileDropTargetFilter();

        bool result = filter.CanReceiveSourceFile(instance);

        result.ShouldBeTrue();
    }
}
