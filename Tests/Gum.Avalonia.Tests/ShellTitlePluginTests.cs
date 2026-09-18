using System.Linq;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Plugins;
using Gum.Avalonia.Shell;
using Gum.DataTypes;
using Gum.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>Covers <see cref="ShellTitlePlugin"/>'s window-title text, separately from the in-app header.</summary>
public class ShellTitlePluginTests
{
    [AvaloniaFact]
    public void UpdateTitle_UsesTheFileNameOnly_NotTheFullPath()
    {
        PluginManager pluginManager = TestAppBuilder.Services.GetRequiredService<PluginManager>();
        if (!pluginManager.IsInitialized)
        {
            pluginManager.Initialize();
        }
        ShellTitlePlugin plugin = pluginManager.Plugins.OfType<ShellTitlePlugin>().Single();
        ShellViewModel shell = TestAppBuilder.Services.GetRequiredService<ShellViewModel>();
        GumProjectSave project = new GumProjectSave { FullFileName = "C:/My/Folder/Is/Long/GumProject.gumj" };

        plugin.CallProjectLoad(project);

        shell.Title.ShouldBe("GumProject.gumj");
    }
}
