using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.GumCommon;

public class StateSaveFileValueTests : BaseTestClass
{
    [Fact]
    public void SetValue_AbsoluteFilePathInAnUnsavedProject_KeepsThePathAbsolute()
    {
        // A project that was never saved has no file name, so there is nothing to make the path relative to.
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        StandardElementSave sprite = new StandardElementSave { Name = "Sprite" };
        StateSave state = StandardElementsManager.Self.GetDefaultStateFor("Sprite")!.Clone();
        state.Name = "Default";
        state.ParentContainer = sprite;
        sprite.States.Add(state);
        project.StandardElements.Add(sprite);

        state.SetValue("SourceFile", "/images/image.png", instanceSave: null, variableType: "string");

        state.GetValue("SourceFile").ShouldBe("/images/image.png");
    }
}
