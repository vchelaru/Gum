using System;
using System.Collections.Generic;
using System.Linq;
using Shouldly;
using WpfDataUi;
using WpfDataUi.DataTypes;
using Xunit;

namespace GumToolUnitTests.VariableGrid;

public class MemberCategoryFilterTests
{
    private static Func<InstanceMember, bool> Contains(string text) =>
        member => member.Name.Contains(text, StringComparison.OrdinalIgnoreCase);

    private static MemberCategory CreateCategory(string name, params string[] memberNames)
    {
        MemberCategory category = new MemberCategory(name);
        foreach (string memberName in memberNames)
        {
            category.Members.Add(new InstanceMember(memberName, new object()));
        }
        return category;
    }

    private static List<string> NamesIn(MemberCategory category) =>
        category.Members.Select(member => member.Name).ToList();

    [Fact]
    public void Apply_ShouldCollapseCategoryWithNoMatches()
    {
        MemberCategory display = CreateCategory("Display", "Visible", "Alpha");
        MemberCategory position = CreateCategory("Position", "X", "Y");
        List<MemberCategory> categories = new List<MemberCategory> { display, position };
        MemberCategoryFilter filter = new MemberCategoryFilter();

        filter.Apply(categories, Contains("vis"));

        NamesIn(display).ShouldBe(new[] { "Visible" });
        NamesIn(position).ShouldBeEmpty();
        // MemberCategory.Visibility already collapses an empty category, so emptying it hides the header.
        position.Visibility.ShouldBe(System.Windows.Visibility.Collapsed);
    }

    [Fact]
    public void Apply_ShouldExpandCategoryHoldingMatchesAndRestoreExpansionOnClear()
    {
        MemberCategory display = CreateCategory("Display", "Visible", "Alpha");
        display.IsExpanded = false;
        List<MemberCategory> categories = new List<MemberCategory> { display };
        MemberCategoryFilter filter = new MemberCategoryFilter();

        filter.Apply(categories, Contains("vis"));

        // A match hidden inside a collapsed section is not a result the user can see.
        display.IsExpanded.ShouldBeTrue();

        filter.Apply(categories, isMatch: null);

        // Clearing must not leave the user's sections reorganized by a search they already dismissed.
        display.IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void Apply_ShouldRestoreOriginalMemberOrderOnClear()
    {
        MemberCategory display = CreateCategory("Display", "Visible", "Alpha", "Blend");
        List<MemberCategory> categories = new List<MemberCategory> { display };
        MemberCategoryFilter filter = new MemberCategoryFilter();

        filter.Apply(categories, Contains("l"));
        NamesIn(display).ShouldBe(new[] { "Visible", "Alpha", "Blend" });

        filter.Apply(categories, Contains("al"));
        NamesIn(display).ShouldBe(new[] { "Alpha" });

        filter.Apply(categories, isMatch: null);

        NamesIn(display).ShouldBe(new[] { "Visible", "Alpha", "Blend" });
    }

    [Fact]
    public void GetPreFilterIsExpanded_ShouldReportTheUsersStateWhileAFilterForcesExpansion()
    {
        MemberCategory display = CreateCategory("Display", "Visible");
        display.IsExpanded = false;
        List<MemberCategory> categories = new List<MemberCategory> { display };
        MemberCategoryFilter filter = new MemberCategoryFilter();

        filter.Apply(categories, Contains("vis"));

        // DataUiGrid persists expansion across selection changes; it must persist what the user chose,
        // not the expansion this filter forced.
        filter.GetPreFilterIsExpanded(display).ShouldBeFalse();
        display.IsExpanded.ShouldBeTrue();
    }

    [StaFact]
    public void DataUiGrid_ShouldNotPersistFilterForcedExpansionAcrossSelectionChanges()
    {
        // Unique names: DataUiGrid's expansion memory is a static dictionary keyed by category name.
        MemberCategory display = CreateCategory("FilterPersistenceDisplay", "Visible", "Alpha");
        display.IsExpanded = false;
        DataUiGrid grid = new DataUiGrid();
        grid.SetCategories(new List<MemberCategory> { display });
        display.IsExpanded = false;

        grid.ApplyMemberFilter(Contains("vis"));
        display.IsExpanded.ShouldBeTrue();

        // Selecting a different object rebuilds the grid, which is where expansion gets persisted. The
        // filter is still active, so the rebuilt category stays expanded to keep showing its match.
        MemberCategory rebuilt = CreateCategory("FilterPersistenceDisplay", "Visible", "Alpha");
        grid.SetCategories(new List<MemberCategory> { rebuilt });
        rebuilt.IsExpanded.ShouldBeTrue();

        // Dismissing the search has to hand back the collapsed section the user chose, not the expansion
        // the filter forced and the rebuild would otherwise have persisted as a preference.
        grid.ApplyMemberFilter(isMatch: null);

        rebuilt.IsExpanded.ShouldBeFalse();
    }

    [StaFact]
    public void DataUiGrid_ShouldReapplyAnActiveFilterAfterCategoriesAreRebuilt()
    {
        DataUiGrid grid = new DataUiGrid();
        grid.SetCategories(new List<MemberCategory> { CreateCategory("FilterReapplyDisplay", "Visible", "Alpha") });
        grid.ApplyMemberFilter(Contains("vis"));

        // The box still shows "vis" after selecting a different object, so the grid must still be narrowed.
        MemberCategory rebuilt = CreateCategory("FilterReapplyDisplay", "Visible", "Alpha");
        grid.SetCategories(new List<MemberCategory> { rebuilt });

        NamesIn(rebuilt).ShouldBe(new[] { "Visible" });

        grid.ApplyMemberFilter(isMatch: null);
        NamesIn(rebuilt).ShouldBe(new[] { "Visible", "Alpha" });
    }

    [StaFact]
    public void DataUiGrid_ShouldFilterCategoriesRebuiltByAClearRatherThanBySetCategories()
    {
        DataUiGrid grid = new DataUiGrid();
        grid.SetCategories(new List<MemberCategory> { CreateCategory("FilterResetDisplay", "Visible", "Alpha") });
        grid.ApplyMemberFilter(Contains("vis"));

        // PopulateCategories() clears and refills Categories directly when Instance changes, without
        // going through SetCategories. The snapshot describes the discarded categories from here on.
        MemberCategory rebuilt = CreateCategory("FilterResetDisplay", "Visible", "Alpha");
        grid.Categories.Clear();
        grid.Categories.Add(rebuilt);

        grid.ApplyMemberFilter(Contains("vis"));

        NamesIn(rebuilt).ShouldBe(new[] { "Visible" });
    }

    [StaFact]
    public void DataUiGrid_ShouldKeepFilteringWhenCategoriesAreReconciledInPlace()
    {
        DataUiGrid grid = new DataUiGrid();
        grid.SetCategories(new List<MemberCategory> { CreateCategory("FilterSwapDisplay", "Visible", "Alpha") });
        grid.ApplyMemberFilter(Contains("vis"));

        // Selecting a different element goes through PropertyGridManager.ReconcileCategories, which
        // swaps categories one at a time instead of replacing the collection, so no Reset is fired.
        MemberCategory replacement = CreateCategory("FilterSwapDisplay", "Visible", "Alpha");
        grid.Categories.RemoveAt(0);
        grid.Categories.Insert(0, replacement);

        NamesIn(replacement).ShouldBe(new[] { "Visible" });
    }

    [StaFact]
    public void DataUiGrid_ShouldKeepFilteringWhenACategoryIsAppendedInPlace()
    {
        DataUiGrid grid = new DataUiGrid();
        grid.SetCategories(new List<MemberCategory> { CreateCategory("FilterAppendDisplay", "Visible") });
        grid.ApplyMemberFilter(Contains("vis"));

        // A newly selected instance can bring a category the previous one didn't have.
        MemberCategory added = CreateCategory("FilterAppendText", "Visible", "Alpha");
        grid.Categories.Add(added);

        NamesIn(added).ShouldBe(new[] { "Visible" });
    }

    [StaFact]
    public void DataUiGrid_ShouldKeepFilteringWhenInstanceIsSetBeforeSetCategories()
    {
        DataUiGrid grid = new DataUiGrid();
        grid.SetCategories(new List<MemberCategory> { CreateCategory("FilterInstanceDisplay", "Visible", "Alpha") });
        grid.ApplyMemberFilter(Contains("vis"));

        // PropertyGridManager assigns Instance (which runs PopulateCategories) and only then calls
        // SetCategories, so the real selection-change sequence is not SetCategories on its own.
        grid.Instance = new FakeInstance();
        MemberCategory rebuilt = CreateCategory("FilterInstanceDisplay", "Visible", "Alpha");
        grid.SetCategories(new List<MemberCategory> { rebuilt });

        NamesIn(rebuilt).ShouldBe(new[] { "Visible" });
    }

    private class FakeInstance
    {
        public string SomeProperty { get; set; } = "";
    }

    [Fact]
    public void Invalidate_ShouldDropTheSnapshotSoNewCategoriesAreNotRestoredOver()
    {
        MemberCategory display = CreateCategory("Display", "Visible", "Alpha");
        List<MemberCategory> categories = new List<MemberCategory> { display };
        MemberCategoryFilter filter = new MemberCategoryFilter();
        filter.Apply(categories, Contains("vis"));

        // Selecting a different object rebuilds the grid, so the remembered pre-filter rows are stale.
        filter.Invalidate();
        MemberCategory rebuilt = CreateCategory("Display", "Text", "Font");
        List<MemberCategory> rebuiltCategories = new List<MemberCategory> { rebuilt };

        filter.Apply(rebuiltCategories, isMatch: null);

        NamesIn(rebuilt).ShouldBe(new[] { "Text", "Font" });
        filter.IsFiltering.ShouldBeFalse();
    }
}
