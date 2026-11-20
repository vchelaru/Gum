using Gum.DataTypes;
using Gum.ProjectServices.Editing;
using Shouldly;

namespace Gum.ProjectServices.Tests.Editing;

public class FavoriteComponentListTests : BaseTestClass
{
    [Fact]
    public void RemoveElement_FavoritedComponent_DropsItFromTheFavorites()
    {
        ComponentSave style = EditingTestProject.AddComponent(Project, "StyleDemo");
        EditingTestProject.AddComponent(Project, "Other");
        Project.FavoriteComponents.AddRange(new[] { "StyleDemo", "Other" });

        new ElementRemovalLogic().RemoveElement(Project, style);

        Project.FavoriteComponents.ShouldBe(new[] { "Other" });
    }

    [Fact]
    public void RemoveElement_ScreenWithAFavoriteOfTheSameName_KeepsTheFavorite()
    {
        ScreenSave screen = EditingTestProject.AddScreen(Project, "Shared");
        Project.FavoriteComponents.Add("Shared");

        new ElementRemovalLogic().RemoveElement(Project, screen);

        Project.FavoriteComponents.ShouldBe(new[] { "Shared" });
    }

    [Fact]
    public void Remove_UnlistedName_ReturnsFalse()
    {
        FavoriteComponentList.Remove(Project, "Missing").ShouldBeFalse();
    }

    [Fact]
    public void Rename_ListedName_ReplacesItInPlace()
    {
        Project.FavoriteComponents.AddRange(new[] { "A", "Old", "C" });

        FavoriteComponentList.Rename(Project, "Old", "New").ShouldBeTrue();

        Project.FavoriteComponents.ShouldBe(new[] { "A", "New", "C" });
        FavoriteComponentList.Rename(Project, "Old", "Again").ShouldBeFalse();
    }
}
