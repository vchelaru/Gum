using System;
using System.IO;
using System.Linq;
using Gum.Commands;
using Gum.Menus;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using HtmlToGumPlugin;
using Moq;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.HtmlToGum;

/// <summary>
/// Content > Import > HTML… shows only when a converter folder is found, since packaged builds
/// do not ship the converter (#5543).
/// </summary>
public class MainHtmlToGumPluginMenuTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("converter", true)]
    public void StartUp_AddsTheHtmlImportItem_OnlyWhenAConverterIsFound(string? converterDir, bool expectItem)
    {
        MenuModel menu = new MenuModel();
        MainHtmlToGumPlugin plugin = CreatePlugin(menu);
        plugin.FindConverterDir = () => converterDir;

        plugin.StartUp();

        bool hasItem = menu.GetItem("Content")?.Items.SingleOrDefault(item => item.Header == "Import")?
            .Items.Any(item => item.Header == "HTML…") ?? false;
        hasItem.ShouldBe(expectItem);
    }

    [Fact]
    public void LocateConverterDir_ReturnsNull_WhenNoCandidateHoldsAConverter()
    {
        string baseDir = Path.Combine(Path.GetTempPath(), "GumHtmlLocate", Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;

        MainHtmlToGumPlugin.LocateConverterDir(environmentValue: null, baseDir).ShouldBeNull();
    }

    [Fact]
    public void LocateConverterDir_ReturnsTheCandidateHoldingTheConverterScript()
    {
        string baseDir = Path.Combine(Path.GetTempPath(), "GumHtmlLocate", Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;
        string converter = Path.Combine(baseDir, "converter");
        Directory.CreateDirectory(converter);
        File.WriteAllText(Path.Combine(converter, "convert.mjs"), "");
        try
        {
            MainHtmlToGumPlugin.LocateConverterDir(environmentValue: null, baseDir).ShouldBe(Path.GetFullPath(converter));
        }
        finally
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }

    private static MainHtmlToGumPlugin CreatePlugin(MenuModel menu) =>
        new MainHtmlToGumPlugin(
            Mock.Of<IProjectState>(),
            Mock.Of<IImportLogic>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<ISelectedState>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>())
        {
            Menu = menu,
        };
}
