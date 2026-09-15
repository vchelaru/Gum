using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Gum.Avalonia.Shell;
using Gum.Menus;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The macOS menu-bar menu rendered from the shared <see cref="MenuModel"/>.</summary>
public class NativeMenuBuilderTests
{
    [AvaloniaFact]
    public void Build_MapsTheModel_AndFollowsItsChanges()
    {
        MenuModel model = new MenuModel();
        MenuItemModel file = new MenuItemModel("File");
        MenuItemModel save = new MenuItemModel("Save") { IsEnabled = false };
        MenuItemModel grid = new MenuItemModel("Show Grid") { IsCheckable = true, IsChecked = true };
        file.Items.Add(save);
        file.Items.Add(MenuItemModel.Separator());
        file.Items.Add(grid);
        model.TopLevelItems.Add(file);

        NativeMenu menu = AvaloniaNativeMenuBuilder.Build(model);

        NativeMenuItem fileItem = (NativeMenuItem)menu.Items[0];
        fileItem.Header.ShouldBe("File");
        NativeMenu fileMenu = fileItem.Menu.ShouldNotBeNull();
        NativeMenuItem saveItem = (NativeMenuItem)fileMenu.Items[0];
        saveItem.IsEnabled.ShouldBeFalse();
        saveItem.ToggleType.ShouldBe(NativeMenuItemToggleType.None);
        fileMenu.Items[1].ShouldBeOfType<NativeMenuItemSeparator>();
        NativeMenuItem gridItem = (NativeMenuItem)fileMenu.Items[2];
        gridItem.ToggleType.ShouldBe(NativeMenuItemToggleType.CheckBox);
        gridItem.IsChecked.ShouldBeTrue();

        save.Header = "Save Project";
        save.IsEnabled = true;
        grid.IsChecked = false;
        saveItem.Header.ShouldBe("Save Project");
        saveItem.IsEnabled.ShouldBeTrue();
        gridItem.IsChecked.ShouldBeFalse();

        file.Items.Add(new MenuItemModel("Exit"));
        model.TopLevelItems.Add(new MenuItemModel("Help"));
        ((NativeMenuItem)fileMenu.Items[3]).Header.ShouldBe("Exit");
        ((NativeMenuItem)menu.Items[1]).Header.ShouldBe("Help");
    }

    [AvaloniaFact]
    public void LeafClick_InvokesTheModel_OnlyAfterTheClickHasFinishedRouting()
    {
        int invoked = 0;
        MenuModel model = new MenuModel();
        MenuItemModel file = new MenuItemModel("File");
        file.Items.Add(new MenuItemModel("Leaf", () => invoked++));
        model.TopLevelItems.Add(file);
        NativeMenu menu = AvaloniaNativeMenuBuilder.Build(model);
        NativeMenuItem leaf = (NativeMenuItem)((NativeMenuItem)menu.Items[0]).Menu!.Items[0];

        ((INativeMenuItemExporterEventsImplBridge)leaf).RaiseClicked();

        invoked.ShouldBe(0);
        Dispatcher.UIThread.RunJobs();
        invoked.ShouldBe(1);
    }
}
