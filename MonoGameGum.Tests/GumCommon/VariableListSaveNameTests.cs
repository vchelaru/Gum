using Gum.DataTypes.Variables;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.GumCommon;

public class VariableListSaveNameTests
{
    [Theory]
    [InlineData("Background.VariableReferences", "Background", "VariableReferences")]
    [InlineData("Child.Grandchild.Items", "Child", "Grandchild.Items")]
    [InlineData("VariableReferences", null, "VariableReferences")]
    [InlineData(".Items", "", ".Items")]
    public void Name_SplitsIntoSourceObjectAndRootName(string name, string? sourceObject, string rootName)
    {
        VariableListSave<string> list = new() { Name = name };

        list.SourceObject.ShouldBe(sourceObject);
        list.GetRootName().ShouldBe(rootName);
    }

    [Fact]
    public void Name_Renamed_SplitFollowsTheNewName()
    {
        VariableListSave<string> list = new() { Name = "A.Items" };

        list.Name = "B.Things";

        list.SourceObject.ShouldBe("B");
        list.GetRootName().ShouldBe("Things");
    }

    [Fact]
    public void Name_ReadTwice_ReturnsTheSameStrings()
    {
        VariableListSave<string> list = new() { Name = "Background.VariableReferences" };

        list.GetRootName().ShouldBeSameAs(list.GetRootName());
        list.SourceObject.ShouldBeSameAs(list.SourceObject);
    }
}
