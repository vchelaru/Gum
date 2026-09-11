using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Avalonia.Tests.DataUi;

/// <summary>
/// The Avalonia grid over the neutral model: rows get the editor the registry picks, the filter and
/// empty-category hiding flow through to the view, and a refresh re-reads every row.
/// </summary>
public class DataUiGridTests
{
    private static (DataUiGrid Grid, Window Window) ShowGrid(params MemberCategory[] categories)
    {
        DataUiGrid grid = new DataUiGrid();
        grid.SetCategories(categories.ToList());
        Window window = new Window { Content = grid, Width = 500, Height = 700 };
        window.Show();
        window.UpdateLayout();
        return (grid, window);
    }

    private static (DataUiGrid Grid, Window Window) ShowGridWithDefaultStyling(MemberCategory category, bool overridesIsDefaultStyling)
    {
        DataUiGrid grid = new DataUiGrid();
        DataUiGrid.SetOverridesIsDefaultStyling(grid, overridesIsDefaultStyling);
        grid.SetCategories(new List<MemberCategory> { category });
        Window window = new Window { Content = grid, Width = 500, Height = 700 };
        window.Show();
        window.UpdateLayout();
        return (grid, window);
    }

    private static MemberCategory Category(string name, EditorFixture fixture, params string[] propertyNames)
    {
        MemberCategory category = new MemberCategory(name);
        foreach (string propertyName in propertyNames)
        {
            category.Members.Add(fixture.Member(propertyName));
        }
        return category;
    }

    [AvaloniaFact]
    public void Rows_GetTheEditorForTheirType()
    {
        EditorFixture fixture = new EditorFixture();
        (DataUiGrid grid, Window window) = ShowGrid(Category("GridEditors", fixture,
            nameof(EditorFixture.Text), nameof(EditorFixture.Flag), nameof(EditorFixture.Maybe), nameof(EditorFixture.Choice)));

        Dictionary<string, Type> editorByMember = grid.LiveContainers
            .ToDictionary(container => container.Member!.Name, container => container.Displayer!.GetType());

        editorByMember[nameof(EditorFixture.Text)].ShouldBe(typeof(TextBoxDisplay));
        editorByMember[nameof(EditorFixture.Flag)].ShouldBe(typeof(CheckBoxDisplay));
        editorByMember[nameof(EditorFixture.Maybe)].ShouldBe(typeof(NullableBoolDisplay));
        editorByMember[nameof(EditorFixture.Choice)].ShouldBe(typeof(ComboBoxDisplay));
        window.Close();
    }

    [AvaloniaFact]
    public void PreferredDisplayerKey_ResolvesThroughTheGridsRegistry_AndAppliesDisplayerProperties()
    {
        EditorFixture fixture = new EditorFixture();
        InstanceMember member = fixture.Member(nameof(EditorFixture.Number));
        member.PreferredDisplayer = typeof(StandardDisplayers.Slider);
        member.PropertiesToSetOnDisplayer["MaxValue"] = 5.0;
        MemberCategory category = new MemberCategory("GridPreferred");
        category.Members.Add(member);

        (DataUiGrid grid, Window window) = ShowGrid(category);

        SliderDisplay slider = grid.LiveContainers.Single().Displayer.ShouldBeOfType<SliderDisplay>();
        slider.MaxValue.ShouldBe(5.0);
        window.Close();
    }

    [AvaloniaFact]
    public void Filter_RemovesRows_AndAnEmptyCategoryHidesItsHeader()
    {
        EditorFixture fixture = new EditorFixture();
        MemberCategory kept = Category("GridFilterKept", fixture, nameof(EditorFixture.Text), nameof(EditorFixture.Number));
        MemberCategory emptied = Category("GridFilterEmptied", fixture, nameof(EditorFixture.Flag));
        (DataUiGrid grid, Window window) = ShowGrid(kept, emptied);

        grid.ApplyMemberFilter(member => member.Name == nameof(EditorFixture.Text));
        window.UpdateLayout();

        grid.LiveContainers.Select(container => container.Member!.Name).ShouldBe(new[] { nameof(EditorFixture.Text) });
        emptied.IsVisible.ShouldBeFalse();
        window.GetVisualDescendantsOfType<DataUiCategoryView>().Count(view => view.IsVisible).ShouldBe(1);
        window.Close();
    }

    [AvaloniaFact]
    public void Refresh_RereadsEveryRow()
    {
        EditorFixture fixture = new EditorFixture { Text = "before" };
        (DataUiGrid grid, Window window) = ShowGrid(Category("GridRefresh", fixture, nameof(EditorFixture.Text)));
        TextBoxDisplay display = grid.LiveContainers.Single().Displayer.ShouldBeOfType<TextBoxDisplay>();

        fixture.Text = "after";
        grid.Refresh();

        display.TextBox.Text.ShouldBe("after");
        window.Close();
    }

    [AvaloniaFact]
    public void SettingAValueThroughARow_RaisesPropertyChangeWithTheOldValue()
    {
        EditorFixture fixture = new EditorFixture { Count = 3 };
        (DataUiGrid grid, Window window) = ShowGrid(Category("GridPropertyChange", fixture, nameof(EditorFixture.Count)));
        TextBoxDisplay display = grid.LiveContainers.Single().Displayer.ShouldBeOfType<TextBoxDisplay>();
        object? oldValue = null;
        grid.PropertyChange += (_, args) => oldValue = args.OldValue;

        display.TextBox.Text = "9";
        WpfDataUi.IDataUiExtensionMethods.TrySetValueOnInstance(display);

        fixture.Count.ShouldBe(9);
        oldValue.ShouldBe(3);
        window.Close();
    }

    [AvaloniaFact]
    public void CategoryHeader_ClickCollapsesAndExpandsItsRows()
    {
        EditorFixture fixture = new EditorFixture();
        MemberCategory category = Category("GridToggle", fixture, nameof(EditorFixture.Text));
        (DataUiGrid _, Window window) = ShowGrid(category);
        DataUiCategoryView view = window.GetVisualDescendantsOfType<DataUiCategoryView>().Single();
        global::Avalonia.Point header = view.Header.TranslatePoint(new global::Avalonia.Point(20, 5), window)!.Value;

        window.MouseDown(header, MouseButton.Left);
        window.MouseUp(header, MouseButton.Left);
        window.UpdateLayout();

        category.IsExpanded.ShouldBeFalse();
        view.Rows.IsVisible.ShouldBeFalse();

        window.MouseDown(header, MouseButton.Left);
        window.MouseUp(header, MouseButton.Left);
        window.UpdateLayout();

        category.IsExpanded.ShouldBeTrue();
        view.Rows.IsVisible.ShouldBeTrue();
        window.Close();
    }

    [AvaloniaFact]
    public void CategoryHeader_UsesTheHeaderColor_OrTheGridsHeaderBrush_AndLeavesTheRowsUnpainted()
    {
        EditorFixture fixture = new EditorFixture();
        MemberCategory colored = Category("GridColored", fixture, nameof(EditorFixture.Text));
        colored.HeaderColor = System.Drawing.Color.FromArgb(255, 10, 20, 30);
        MemberCategory plain = Category("GridPlain", fixture, nameof(EditorFixture.Flag));
        DataUiGrid grid = new DataUiGrid { CategoryHeaderBackground = global::Avalonia.Media.Brushes.Orange };
        grid.SetCategories(new List<MemberCategory> { colored, plain });
        Window window = new Window { Content = grid, Width = 500, Height = 700 };
        window.Show();
        window.UpdateLayout();

        DataUiCategoryView[] views = window.GetVisualDescendantsOfType<DataUiCategoryView>().ToArray();
        DataUiCategoryView coloredView = views.Single(view => view.DataContext == colored);
        DataUiCategoryView plainView = views.Single(view => view.DataContext == plain);

        coloredView.Header.Background.ShouldBeAssignableTo<global::Avalonia.Media.ISolidColorBrush>()!
            .Color.ShouldBe(global::Avalonia.Media.Color.FromRgb(10, 20, 30));
        plainView.Header.Background.ShouldBe(global::Avalonia.Media.Brushes.Orange);
        coloredView.Background.ShouldBeNull();
        coloredView.Rows.Background.ShouldBeNull();
        window.Close();
    }

    [AvaloniaFact]
    public void OverridesIsDefaultStyling_LeavesDefaultValuedFieldsUntinted()
    {
        EditorFixture fixture = new EditorFixture();
        MemberCategory tinted = new MemberCategory("GridDefaultTinted");
        tinted.Members.Add(new DefaultValuedMember(nameof(EditorFixture.Text), fixture));
        MemberCategory untinted = new MemberCategory("GridDefaultUntinted");
        untinted.Members.Add(new DefaultValuedMember(nameof(EditorFixture.Text), fixture));
        // Set on both grids: the head's styles turn it on for every grid in the app.
        (DataUiGrid tintingGrid, Window tintingWindow) = ShowGridWithDefaultStyling(tinted, overridesIsDefaultStyling: false);
        (DataUiGrid overridingGrid, Window overridingWindow) = ShowGridWithDefaultStyling(untinted, overridesIsDefaultStyling: true);

        TextBox tintedBox = tintingGrid.LiveContainers.Single().Displayer.ShouldBeOfType<TextBoxDisplay>().TextBox;
        TextBox untintedBox = overridingGrid.LiveContainers.Single().Displayer.ShouldBeOfType<TextBoxDisplay>().TextBox;

        tintedBox.Background.ShouldBe(DataUiValueStateBrushes.DefaultValueBackground);
        untintedBox.Background.ShouldNotBe(DataUiValueStateBrushes.DefaultValueBackground);
        tintingWindow.Close();
        overridingWindow.Close();
    }

    private sealed class DefaultValuedMember : InstanceMember
    {
        public DefaultValuedMember(string name, object instance) : base(name, instance)
        {
        }

        public override bool IsDefault
        {
            get => true;
            set { }
        }
    }
}

internal static class VisualTreeTestExtensions
{
    public static IEnumerable<T> GetVisualDescendantsOfType<T>(this global::Avalonia.Visual root) where T : global::Avalonia.Visual =>
        global::Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(root).OfType<T>();
}
