using Gum.Menus;
using Shouldly;

namespace Gum.Presentation.Tests.Menus;

public class MenuModelTests
{
    [Fact]
    public void AddMenuItem_CreatesTopLevelMenuBeforeHelp()
    {
        MenuModel model = new MenuModel();
        model.TopLevelItems.Add(new MenuItemModel("File"));
        model.TopLevelItems.Add(new MenuItemModel("Help"));

        MenuItemModel item = model.AddMenuItem(new[] { "Content", "Import" });

        model.TopLevelItems.Select(i => i.Header).ShouldBe(new[] { "File", "Content", "Help" });
        model.GetItem("Content")!.Items.ShouldContain(item);
        item.Header.ShouldBe("Import");
    }

    [Fact]
    public void AddMenuItem_CreatesIntermediateSubmenus()
    {
        MenuModel model = new MenuModel();
        model.TopLevelItems.Add(new MenuItemModel("File"));

        MenuItemModel item = model.AddMenuItem(new[] { "File", "Export", "As Image" });

        MenuItemModel export = model.GetItem("File")!.Items.Single(i => i.Header == "Export");
        export.Items.ShouldContain(item);
    }

    [Fact]
    public void ApplyLayout_GroupsKnownItemsAndAppendsLeftovers()
    {
        MenuModel model = new MenuModel();
        model.TopLevelItems.Add(new MenuItemModel("Help"));
        model.AddMenuItem(new[] { "Content", "View Font Cache" });
        model.AddMenuItem(new[] { "Content", "Third Party Thing" });
        model.AddMenuItem(new[] { "Content", "Find file references..." });
        model.AddMenuItem(new[] { "Content", "Import" });

        List<string> headers = model.GetItem("Content")!.Items
            .Select(i => i.IsSeparator ? "-" : i.Header).ToList();

        headers.ShouldBe(new[]
        {
            "Find file references...", "-", "Import", "-", "View Font Cache", "-", "Third Party Thing",
        });
    }

    [Fact]
    public void Invoke_TogglesCheckStateThenRunsClick()
    {
        bool clicked = false;
        MenuItemModel item = new MenuItemModel("Toggle", () => clicked = true) { IsCheckable = true };

        item.Invoke();

        item.IsChecked.ShouldBeTrue();
        clicked.ShouldBeTrue();
    }
}
