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
    public void AddImportMenuEntryIfConverterFound_AddsTheItemOnce_AfterAConverterTurnsUp()
    {
        MenuModel menu = new MenuModel();
        MainHtmlToGumPlugin plugin = CreatePlugin(menu);
        string? converterDir = null;
        plugin.FindConverterDir = () => converterDir;
        plugin.StartUp();

        converterDir = "converter";
        plugin.AddImportMenuEntryIfConverterFound();
        plugin.AddImportMenuEntryIfConverterFound();

        menu.GetItem("Content")!.Items.Single(item => item.Header == "Import")
            .Items.Count(item => item.Header == "HTML…").ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("missing")]
    public void LocateConverterDir_ReturnsNull_WhenNeitherTheEnvironmentNorACandidateHoldsAConverter(string? environmentValue)
    {
        string baseDir = Path.Combine(Path.GetTempPath(), "GumHtmlLocate", Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;
        string? environmentDir = environmentValue == "missing" ? Path.Combine(baseDir, "missing") : environmentValue;

        MainHtmlToGumPlugin.LocateConverterDir(environmentDir, baseDir).ShouldBeNull();
    }

    [Theory]
    [InlineData("convert.mjs")]
    [InlineData("convert.ts")]
    public void LocateConverterDir_ReturnsTheCandidateHoldingTheConverterScript(string script)
    {
        string baseDir = Path.Combine(Path.GetTempPath(), "GumHtmlLocate", Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;
        string converter = Path.Combine(baseDir, "converter");
        Directory.CreateDirectory(converter);
        File.WriteAllText(Path.Combine(converter, script), "");
        try
        {
            MainHtmlToGumPlugin.LocateConverterDir(environmentValue: null, baseDir).ShouldBe(Path.GetFullPath(converter));
        }
        finally
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }

    [Fact]
    public void LocateConverterDir_PrefersAnExistingEnvironmentFolder_EvenWithoutAScript()
    {
        string baseDir = Path.Combine(Path.GetTempPath(), "GumHtmlLocate", Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;
        string candidate = Path.Combine(baseDir, "converter");
        Directory.CreateDirectory(candidate);
        File.WriteAllText(Path.Combine(candidate, "convert.mjs"), "");
        string environmentDir = Path.Combine(baseDir, "fromEnvironment");
        Directory.CreateDirectory(environmentDir);
        try
        {
            MainHtmlToGumPlugin.LocateConverterDir(environmentDir, baseDir).ShouldBe(Path.GetFullPath(environmentDir));
        }
        finally
        {
            Directory.Delete(baseDir, recursive: true);
        }
    }

    [Fact]
    public void ResolveConverterDir_UsesTheFoundFolder_OrElseTheFirstCandidate()
    {
        string baseDir = Path.Combine(Path.GetTempPath(), "GumHtmlLocate", "bin") + Path.DirectorySeparatorChar;
        string found = Path.Combine(Path.GetTempPath(), "found");

        MainHtmlToGumPlugin.ResolveConverterDir(found, baseDir).ShouldBe(found);
        MainHtmlToGumPlugin.ResolveConverterDir(null, baseDir).ShouldBe(MainHtmlToGumPlugin.GetConverterDirCandidates(baseDir)[0]);
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
