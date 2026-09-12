using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Gum.Avalonia.Shell;
using Gum.Menus;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The main menu rendered from the shared <see cref="MenuModel"/>.</summary>
public class MenuBuilderTests
{
    [AvaloniaFact]
    public void LeafClick_InvokesTheModel_OnlyAfterTheClickHasFinishedRouting()
    {
        // Many menu actions show a dialog synchronously (a nested dispatcher loop). Invoked inside
        // the click, the dialog opened while the menu was still open, and the menu's light-dismiss
        // swallowed the first click into the dialog. The action must run after the menu has closed.
        int invoked = 0;
        MenuModel model = new MenuModel();
        MenuItemModel file = new MenuItemModel("File");
        file.Items.Add(new MenuItemModel("Leaf", () => invoked++));
        model.TopLevelItems.Add(file);
        Menu menu = AvaloniaMenuBuilder.Build(model);
        Window window = new Window { Content = menu, Width = 300, Height = 200 };
        window.Show();
        MenuItem top = (MenuItem)menu.Items[0]!;
        MenuItem leaf = (MenuItem)top.Items[0]!;

        leaf.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

        invoked.ShouldBe(0);
        Dispatcher.UIThread.RunJobs();
        invoked.ShouldBe(1);
        window.Close();
    }
}
