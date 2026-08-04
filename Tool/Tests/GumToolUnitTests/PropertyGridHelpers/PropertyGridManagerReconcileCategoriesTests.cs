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

    [Fact]
    public void ReconcileCategories_MultipleNewCategories_AllInserted()
    {
        List<MemberCategory> gridCategories = new List<MemberCategory>
        {
            MakeCategory("Position")
        };
        List<MemberCategory> newCategories = new List<MemberCategory>
        {
            MakeCategory("Position"),
            MakeCategory("Text"),
            MakeCategory("Appearance")
        };

        PropertyGridManager.ReconcileCategories(gridCategories, newCategories, instanceIdentityChanged: true);

        gridCategories.Select(c => c.Name).ShouldBe(new[] { "Position", "Text", "Appearance" }, ignoreOrder: true);
    }

    [Fact]
    public void ReconcileCategories_NewListEmpty_RemovesEveryExistingCategory()
    {
        List<MemberCategory> gridCategories = new List<MemberCategory>
        {
            MakeCategory("Position"),
            MakeCategory("Text")
        };
        List<MemberCategory> newCategories = new List<MemberCategory>();

        PropertyGridManager.ReconcileCategories(gridCategories, newCategories, instanceIdentityChanged: true);

        gridCategories.ShouldBeEmpty();
    }

    /// <summary>
    /// Reproduces the reported bug end to end: switching from a Text instance (which has a "Text"
    /// category the Rectangle doesn't) to a Rectangle instance (which has an "Appearance" category
    /// the Text instance didn't show) in one selection change.
    /// </summary>
    [Fact]
    public void ReconcileCategories_SwitchingInstanceTypes_DropsOldCategoryAndAddsNewOne()
    {
        List<MemberCategory> gridCategories = new List<MemberCategory>
        {
            MakeCategory("Position"),
            MakeCategory("Text")
        };
        List<MemberCategory> newCategories = new List<MemberCategory>
        {
            MakeCategory("Position"),
            MakeCategory("Appearance")
        };

        PropertyGridManager.ReconcileCategories(gridCategories, newCategories, instanceIdentityChanged: true);

        gridCategories.Select(c => c.Name).ShouldBe(new[] { "Position", "Appearance" }, ignoreOrder: true);
    }

    [Fact]
    public void ReconcileCategories_MatchingCategoryWithNonRetargetableMembers_ReplacesCategoryObject()
    {
        MemberCategory oldCategory = MakeCategory("Position", new InstanceMember { Name = "X" });
        List<MemberCategory> gridCategories = new List<MemberCategory> { oldCategory };
        MemberCategory newCategory = MakeCategory("Position", new InstanceMember { Name = "X" });
        List<MemberCategory> newCategories = new List<MemberCategory> { newCategory };

        PropertyGridManager.ReconcileCategories(gridCategories, newCategories, instanceIdentityChanged: true);

        // Plain InstanceMember rows aren't StateReferencingInstanceMember, so they can't be
        // retargeted in place - the category must be swapped for the new instance's category object.
        gridCategories.ShouldHaveSingleItem();
        gridCategories[0].ShouldBeSameAs(newCategory);
        gridCategories[0].ShouldNotBeSameAs(oldCategory);
    }

    [Fact]
    public void ReconcileCategories_UnchangedCategoryNoIdentityChange_LeavesCategoryObjectAlone()
    {
        MemberCategory oldCategory = MakeCategory("Position", new InstanceMember { Name = "X" });
        List<MemberCategory> gridCategories = new List<MemberCategory> { oldCategory };
        List<MemberCategory> newCategories = new List<MemberCategory>
        {
            MakeCategory("Position", new InstanceMember { Name = "X" })
        };

        PropertyGridManager.ReconcileCategories(gridCategories, newCategories, instanceIdentityChanged: false);

        gridCategories.ShouldHaveSingleItem();
        gridCategories[0].ShouldBeSameAs(oldCategory);
    }

    private static MemberCategory MakeCategory(string name, params InstanceMember[] members)
    {
        MemberCategory category = new MemberCategory { Name = name };
        foreach (InstanceMember member in members)
        {
            category.Members.Add(member);
        }
        return category;
    }
}
