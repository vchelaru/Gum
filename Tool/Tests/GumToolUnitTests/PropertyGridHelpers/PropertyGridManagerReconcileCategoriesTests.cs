using Gum.Managers;
using Shouldly;
using WpfDataUi.DataTypes;

namespace GumToolUnitTests.PropertyGridHelpers;

public class PropertyGridManagerReconcileCategoriesTests : BaseTestClass
{
    [Fact]
    public void ReconcileCategories_NewCategoryNotPreviouslyShown_IsInserted()
    {
        List<MemberCategory> gridCategories = new List<MemberCategory>
        {
            MakeCategory("Position")
        };
        List<MemberCategory> newCategories = new List<MemberCategory>
        {
            MakeCategory("Position"),
            MakeCategory("Text")
        };

        PropertyGridManager.ReconcileCategories(gridCategories, newCategories, instanceIdentityChanged: true);

        gridCategories.Select(c => c.Name).ShouldContain("Text");
    }

    [Fact]
    public void ReconcileCategories_OldCategoryNotInNewList_IsRemoved()
    {
        List<MemberCategory> gridCategories = new List<MemberCategory>
        {
            MakeCategory("Position"),
            MakeCategory("Text")
        };
        List<MemberCategory> newCategories = new List<MemberCategory>
        {
            MakeCategory("Position")
        };

        PropertyGridManager.ReconcileCategories(gridCategories, newCategories, instanceIdentityChanged: true);

        gridCategories.Select(c => c.Name).ShouldNotContain("Text");
    }

    private static MemberCategory MakeCategory(string name)
    {
        return new MemberCategory { Name = name };
    }
}
