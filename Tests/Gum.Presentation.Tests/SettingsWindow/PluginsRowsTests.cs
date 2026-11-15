using System.Linq;
using Gum.Plugins;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.SettingsWindow;
using Moq;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.SettingsWindow;

/// <summary>
/// The rows of the Application > Plugins page: one toggle per plugin that enables and disables it exactly as
/// the Manage Plugins dialog does, with a chip and hint for its state, then the folder scan.
/// </summary>
public class PluginsRowsTests
{
    private readonly Mock<IPluginManager> _pluginManager = new Mock<IPluginManager>();
    private readonly Mock<IDialogService> _dialogs = new Mock<IDialogService>();
    private readonly Mock<IClipboardService> _clipboard = new Mock<IClipboardService>();

    private PluginsRows CreateRows(PluginScanReport? report = null, params PluginSummary[] plugins)
    {
        _pluginManager.Setup(x => x.GetAllPluginSummaries()).Returns(plugins);
        _pluginManager.Setup(x => x.GetPluginScanReport()).Returns(report);
        return new PluginsRows(new PluginsDialogViewModel(_dialogs.Object, _pluginManager.Object, _clipboard.Object));
    }

    private static InstanceMember Row(PluginsRows rows, string name) =>
        rows.BuildCategories().SelectMany(section => section.Members).Single(member => member.Name == name);

    [Fact]
    public void EveryRow_HasALabel_AHint_AndTheSharedRowShape()
    {
        PluginsRows rows = CreateRows(new PluginScanReport(@"C:\Gum\Plugins", true, new[]
            {
                new PluginFileScan("A.dll", PluginFileOutcome.Loaded, true, null),
            }),
            new PluginSummary("Alpha", "Alpha", true, false, new object()),
            new PluginSummary("Core", "Core", true, false, new object(), CanBeDisabled: false, IsBuiltIn: true));

        InstanceMember[] members = rows.BuildCategories().SelectMany(section => section.Members).ToArray();

        members.Select(member => member.DisplayName).ShouldBe(new[] { "Alpha", "Core", "Plugins folder", "A.dll" });
        foreach (InstanceMember member in members)
        {
            member.IsSpaciousRow.ShouldBeTrue();
            member.FirstGridLength.ShouldBe(SettingsRows.LabelColumnWidth);
        }
    }

    [Fact]
    public void HealthyPluginRows_AreOneLine_WithNoHint()
    {
        PluginsRows rows = CreateRows(new PluginScanReport(@"C:\Gum\Plugins", true, new[]
            {
                new PluginFileScan("A.dll", PluginFileOutcome.Loaded, true, null),
            }),
            new PluginSummary("Alpha", "Alpha", true, false, new object(), SourceFile: "AlphaPlugin.dll"),
            new PluginSummary("Core", "Core", true, false, new object(), CanBeDisabled: false, IsBuiltIn: true));

        Row(rows, "Plugin:Alpha").DetailText.ShouldBeNull();
        Row(rows, "Plugin:Core").DetailText.ShouldBeNull();
        Row(rows, "ScanFile:A.dll").DetailText.ShouldBeNull();
    }

    [Fact]
    public void RequiredPlugins_AreInACollapsedGumInternalsSection_ApartFromThePluginsAUserCanTurnOff()
    {
        PluginsRows rows = CreateRows(null,
            new PluginSummary("Alpha", "Alpha", true, false, new object()),
            new PluginSummary("Beta", "Beta", true, false, new object(), IsBuiltIn: true),
            new PluginSummary("Core", "Core", true, false, new object(), CanBeDisabled: false, IsBuiltIn: true));

        MemberCategory[] sections = rows.BuildCategories().ToArray();

        sections.Select(section => section.Name).ShouldBe(new[] { "Installed plugins", "Gum internals", "Folder scan" });
        sections[0].Members.Select(member => member.DisplayName).ShouldBe(new[] { "Alpha", "Beta" });
        sections[0].IsExpanded.ShouldBeTrue();
        sections[1].Members.Select(member => member.DisplayName).ShouldBe(new[] { "Core" });
        sections[1].IsExpanded.ShouldBeFalse();
    }

    [Fact]
    public void Search_ByARequiredPluginsName_OpensGumInternals_AndMarksTheRow()
    {
        PluginsRows rows = CreateRows(null,
            new PluginSummary("Alpha", "Alpha", true, false, new object()),
            new PluginSummary("Core Thing", "Core Thing", true, false, new object(), CanBeDisabled: false));
        MemberCategory[] sections = rows.BuildCategories().ToArray();

        int marked = SettingsRows.MarkMatches(sections, "core thing");

        marked.ShouldBe(1);
        sections[1].IsExpanded.ShouldBeTrue();
        sections[1].Members.Single().IsSearchMatch.ShouldBeTrue();
        sections[0].Members.Single().IsSearchMatch.ShouldBeFalse();
    }

    [Fact]
    public void SearchTerms_IncludeEachPluginName()
    {
        PluginsRows rows = CreateRows(null,
            new PluginSummary("Alpha", "Alpha", true, false, new object(), SourceFile: "AlphaPlugin.dll"));

        string[] terms = rows.SearchTerms().ToArray();

        terms.ShouldContain("Alpha");
        terms.ShouldContain("Plugins folder");
    }

    [Fact]
    public void ToggleRow_ReadsTheState_AndDisablesAndEnablesThePlugin()
    {
        object handle = new object();
        PluginSummary on = new PluginSummary("Alpha", "Alpha", true, false, handle);
        PluginSummary off = on with { IsEnabled = false };
        PluginsRows rows = CreateRows(null, on);
        _pluginManager.Setup(x => x.DisableUserPlugin(handle)).Returns(off);
        _pluginManager.Setup(x => x.TryEnablePlugin(handle)).Returns(on);
        InstanceMember row = Row(rows, "Plugin:Alpha");

        row.Value.ShouldBe(true);
        row.Value = false;
        _pluginManager.Verify(x => x.DisableUserPlugin(handle), Times.Once);
        row.Value.ShouldBe(false);

        row.Value = true;
        _pluginManager.Verify(x => x.TryEnablePlugin(handle), Times.Once);
        row.Value.ShouldBe(true);
    }

    [Fact]
    public void RequiredPlugin_IsReadOnly_WithARequiredChip_AndAnExplainingTooltip()
    {
        PluginsRows rows = CreateRows(null,
            new PluginSummary("Core", "Core", true, false, new object(), CanBeDisabled: false));

        InstanceMember row = Row(rows, "Plugin:Core");

        row.IsReadOnly.ShouldBeTrue();
        row.ChipText.ShouldBe("Required");
        row.ToolTipText.ShouldBe("Gum needs this plugin, so it cannot be turned off.");
    }

    [Fact]
    public void CrashedPlugin_ShowsFailedChip_AndTurningItOnAsksFirst()
    {
        object handle = new object();
        PluginsRows rows = CreateRows(null, new PluginSummary("Alpha", "Alpha(Failed in StartUp)", false, true, handle));
        _dialogs.Setup(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()))
            .Returns(MessageDialogResult.Negative);
        InstanceMember row = Row(rows, "Plugin:Alpha");

        row.ChipText.ShouldBe("Failed");
        row.DetailText.ShouldContain("Failed in StartUp");
        row.IsReadOnly.ShouldBeFalse();

        row.Value = true;

        _pluginManager.Verify(x => x.TryEnablePlugin(It.IsAny<object>()), Times.Never);
        row.Value.ShouldBe(false);
    }

    [Fact]
    public void RefusedPlugin_ShowsNotLoadedChip_ItsReason_AndCannotBeToggled()
    {
        PluginsRows rows = CreateRows(new PluginScanReport(@"C:\Gum\Plugins", true, new[]
        {
            new PluginFileScan("MyWpf.dll", PluginFileOutcome.NotHostable, true, "needs Windows"),
        }));

        InstanceMember row = Row(rows, "Plugin:MyWpf.dll");

        row.ChipText.ShouldBe("Not loaded");
        row.WarningText.ShouldBe("needs Windows");
        row.IsReadOnly.ShouldBeTrue();
        row.Value.ShouldBe(false);
    }

    [Fact]
    public void BuiltInPlugin_ShowsABuiltInChip()
    {
        PluginsRows rows = CreateRows(null, new PluginSummary("Alpha", "Alpha", true, false, new object(), IsBuiltIn: true));

        InstanceMember row = Row(rows, "Plugin:Alpha");

        row.ChipText.ShouldBe("Built-in");
        row.IsReadOnly.ShouldBeFalse();
    }

    [Fact]
    public void ToggleRow_RefreshesItsChip_WhenThePluginChanges()
    {
        object handle = new object();
        PluginSummary on = new PluginSummary("Alpha", "Alpha", true, false, handle);
        PluginsRows rows = CreateRows(null, on);
        _pluginManager.Setup(x => x.DisableUserPlugin(handle)).Returns(on with { DisplayText = "Alpha(Failed in ShutDown)", HasFailureDetails = true, IsEnabled = false });
        rows.Reloaded += () => { };
        InstanceMember row = Row(rows, "Plugin:Alpha");

        row.Value = false;

        row.ChipText.ShouldBe("Failed");
    }

    [Fact]
    public void FolderScanSection_ShowsTheFolder_TheFilesHoldingPlugins_AndCopyScanCopiesTheScan()
    {
        PluginScanReport report = new PluginScanReport(@"C:\Gum\Plugins", true, new[]
        {
            new PluginFileScan("Good.dll", PluginFileOutcome.Loaded, true, null),
            new PluginFileScan("Dependency.dll", PluginFileOutcome.Loaded, false, null),
            new PluginFileScan("Broken.dll", PluginFileOutcome.LoadFailed, true, "bad image"),
        });
        PluginsRows rows = CreateRows(report);

        InstanceMember folder = Row(rows, "PluginsFolder");
        folder.Value.ShouldBe(@"C:\Gum\Plugins");
        folder.ChipText.ShouldBe("Found");
        Row(rows, "ScanFile:Good.dll").Value.ShouldBe("Loaded");
        Row(rows, "ScanFile:Broken.dll").Value.ShouldBe("Failed to load");
        Row(rows, "ScanFile:Broken.dll").DetailText.ShouldBe("bad image");
        rows.BuildCategories().SelectMany(section => section.Members).Any(member => member.Name == "ScanFile:Dependency.dll").ShouldBeFalse();

        folder.DetailAction.ShouldNotBeNull().Navigate();

        _clipboard.Verify(x => x.SetText(report.Describe()), Times.Once);
    }
}
