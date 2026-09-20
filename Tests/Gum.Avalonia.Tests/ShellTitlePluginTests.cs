using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Plugins;
using Gum.Avalonia.Shell;
using Gum.DataTypes;
using Gum.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Covers the window title and the in-app header: the OS title (and so the taskbar) shows only the
/// project's name, while the header's tooltip and context menu keep the full path.
/// </summary>
public class ShellTitlePluginTests
{
    [AvaloniaFact]
    public void ProjectLoad_TitlesTheWindowWithTheProjectName_AndKeepsTheFullPath()
    {
        ShellTitlePlugin plugin = GetPlugin();
        ShellViewModel shell = TestAppBuilder.Services.GetRequiredService<ShellViewModel>();
        GumProjectSave project = new GumProjectSave { FullFileName = "C:/My/Folder/Is/Long/GumProject.gumj" };

        plugin.CallProjectLoad(project);

        shell.Title.ShouldBe("GumProject");
        shell.ProjectFilePath.ShouldBe("C:/My/Folder/Is/Long/GumProject.gumj");
    }

    [AvaloniaFact]
    public void ProjectLoad_WithNoProject_TitlesTheWindowGum()
    {
        ShellTitlePlugin plugin = GetPlugin();
        ShellViewModel shell = TestAppBuilder.Services.GetRequiredService<ShellViewModel>();
        plugin.CallProjectLoad(new GumProjectSave { FullFileName = "C:/Folder/GumProject.gumj" });

        plugin.CallProjectLoad(null!);

        shell.Title.ShouldBe("Gum");
        shell.ProjectFilePath.ShouldBeNull();
    }

    [AvaloniaFact]
    public void MainWindow_ShowsTheProjectNameInTheTitleAndHeader_AndTheFullPathInTheHeaderTooltip()
    {
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        ShellViewModel shell = (ShellViewModel)window.DataContext!;
        DockPanel content = ((Panel)window.Content!).Children.OfType<DockPanel>().Single();
        Grid titleRow = (Grid)content.Children.Single(child => DockPanel.GetDock(child) == Dock.Top);
        TextBlock header = titleRow.Children.OfType<TextBlock>().Single();

        shell.ProjectFilePath = "C:/My/Folder/Is/Long/GumProject.gumj";

        window.Title.ShouldBe("GumProject");
        header.Text.ShouldBe("GumProject");
        ToolTip.GetTip(header).ShouldBe("C:/My/Folder/Is/Long/GumProject.gumj");
    }

    private static ShellTitlePlugin GetPlugin()
    {
        PluginManager pluginManager = TestAppBuilder.Services.GetRequiredService<PluginManager>();
        if (!pluginManager.IsInitialized)
        {
            pluginManager.Initialize();
        }
        return pluginManager.Plugins.OfType<ShellTitlePlugin>().Single();
    }
}
