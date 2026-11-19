using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.Managers;

/// <summary>
/// The tool shares one font collection between the passes over an unchanged project; these pin when a
/// new one starts, which is what keeps an edit from being answered with fonts collected before it.
/// </summary>
public class ProjectFontReferencesTests
{
    [Fact]
    public void For_ReturnsTheSameCache_WhileNothingChanged()
    {
        ProjectChangeVersion version = new ProjectChangeVersion();
        ProjectFontReferences references = new ProjectFontReferences(version);
        GumProjectSave project = new GumProjectSave();

        FontReferenceCache first = references.For(project);

        references.For(project).ShouldBeSameAs(first);
    }

    [Fact]
    public void For_StartsANewCache_WhenTheVersionMoves()
    {
        ProjectChangeVersion version = new ProjectChangeVersion();
        ProjectFontReferences references = new ProjectFontReferences(version);
        GumProjectSave project = new GumProjectSave();
        FontReferenceCache first = references.For(project);

        version.MarkChanged();

        references.For(project).ShouldNotBeSameAs(first);
    }

    [Fact]
    public void For_StartsANewCache_ForAnotherProject()
    {
        ProjectFontReferences references = new ProjectFontReferences(new ProjectChangeVersion());
        FontReferenceCache first = references.For(new GumProjectSave());

        references.For(new GumProjectSave()).ShouldNotBeSameAs(first);
    }

    public static IEnumerable<object[]> FontSettingEdits() => new[]
    {
        new object[] { "horizontal spacing", (Action<GumProjectSave>)(project => project.FontSpacingHorizontal += 3) },
        new object[] { "vertical spacing", (Action<GumProjectSave>)(project => project.FontSpacingVertical += 3) },
        new object[] { "default ranges", (Action<GumProjectSave>)(project => project.FontRanges = "32-90") },
        new object[] { "a font's ranges", (Action<GumProjectSave>)(project =>
            project.FontRangeEntries.Add(new FontRangeEntry { FontFamily = "Arial", Ranges = "32-64" })) },
        new object[] { "project version", (Action<GumProjectSave>)(project => project.Version -= 1) },
    };

    // The Fonts settings page writes these straight to the project, with no plugin notification.
    [Theory]
    [MemberData(nameof(FontSettingEdits))]
    public void For_StartsANewCache_WhenAProjectFontSettingChanges(string setting, Action<GumProjectSave> edit)
    {
        ProjectFontReferences references = new ProjectFontReferences(new ProjectChangeVersion());
        GumProjectSave project = new GumProjectSave();
        FontReferenceCache first = references.For(project);

        edit(project);

        references.For(project).ShouldNotBeSameAs(first, setting);
    }

    public static IEnumerable<object[]> NotificationsThatFollowEdits() => new[]
    {
        Notification(m => m.VariableSet(new ComponentSave(), null, "Font", "Arial")),
        Notification(m => m.VariableSetThroughReference(new ComponentSave(), null, "Font", "Arial")),
        Notification(m => m.VariableAdd(new ComponentSave(), "Font")),
        Notification(m => m.VariableDelete(new ComponentSave(), "Font")),
        Notification(m => m.InstanceAdd(new ComponentSave(), new InstanceSave())),
        Notification(m => m.InstanceDelete(new ComponentSave(), new InstanceSave())),
        Notification(m => m.InstancesDelete(new ComponentSave(), new[] { new InstanceSave() })),
        Notification(m => m.InstanceRename(new ComponentSave(), new InstanceSave(), "Old")),
        Notification(m => m.InstanceReordered(new InstanceSave())),
        Notification(m => m.ElementAdd(new ComponentSave())),
        Notification(m => m.ElementDelete(new ComponentSave())),
        Notification(m => m.ElementRename(new ComponentSave(), "Old")),
        Notification(m => m.ElementDuplicate(new ComponentSave(), new ComponentSave())),
        Notification(m => m.ElementImported(new ComponentSave())),
        Notification(m => m.ElementReloaded(new ComponentSave())),
        Notification(m => m.StateAdd(new StateSave())),
        Notification(m => m.StateDelete(new StateSave())),
        Notification(m => m.StateRename(new StateSave(), "Old")),
        Notification(m => m.CategoryAdd(new StateSaveCategory())),
        Notification(m => m.CategoryDelete(new StateSaveCategory())),
        Notification(m => m.CategoryRename(new StateSaveCategory(), "Old")),
        Notification(m => m.BehaviorReferencesChanged(new ComponentSave())),
        Notification(m => m.AfterUndo()),
        Notification(m => m.ProjectLoad(new GumProjectSave())),
        Notification(m => m.ProjectPropertySet("FontSpacingHorizontal")),
        Notification(m => m.ReactToFileChanged(new ToolsUtilities.FilePath("/game/Components/Button.gucx"))),
        Notification(m => m.ModifyDefaultStandardState("Text", new StateSave())),
    };

    private static object[] Notification(Action<PluginManager> notify) => new object[] { notify };

    // Before, so a result computed before the edit isn't reused by its handlers; after, because an earlier
    // handler may have collected fonts that a later handler's change made stale.
    [Theory]
    [MemberData(nameof(NotificationsThatFollowEdits))]
    public void Notification_ThatCanFollowAnEdit_MovesTheVersionBeforeAndAfterItsHandlers(Action<PluginManager> notify)
    {
        ProjectChangeVersion version = new ProjectChangeVersion();
        PluginManager manager = CreateManager(version, out VersionReadingPlugin plugin);

        notify(manager);

        plugin.VersionSeen.ShouldNotBeNull();
        plugin.VersionSeen!.Value.ShouldBeGreaterThan(0);
        version.Version.ShouldBeGreaterThan(plugin.LastVersionSeen!.Value);
    }

    public static IEnumerable<object[]> NotificationsThatChangeNothing() => new[]
    {
        Notification(m => m.ElementSelected(new ComponentSave())),
        Notification(m => m.InstanceSelected(new ComponentSave(), new InstanceSave())),
        Notification(m => m.ReactToStateSaveSelected(new StateSave())),
        Notification(m => m.TreeNodeSelected(null)),
        Notification(m => m.BeforeRender()),
        Notification(m => m.AfterRender()),
        Notification(m => m.CameraChanged()),
        Notification(m => m.WireframeRefreshed()),
        Notification(m => m.RefreshElementTreeView()),
        Notification(m => m.FocusSearch()),
    };

    [Theory]
    [MemberData(nameof(NotificationsThatChangeNothing))]
    public void Notification_ThatOnlyReportsSelectionOrDrawing_LeavesTheVersion(Action<PluginManager> notify)
    {
        ProjectChangeVersion version = new ProjectChangeVersion();
        PluginManager manager = CreateManager(version, out _);

        notify(manager);

        version.Version.ShouldBe(0);
    }

    // The list holds method names as strings at runtime; a renamed method would otherwise drop out of
    // it silently and start discarding the shared collection on every selection or frame.
    [Fact]
    public void NotificationsThatChangeNothing_NamesOnlyPluginManagerMethods()
    {
        HashSet<string> methodNames = typeof(PluginManager)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Select(method => method.Name)
            .ToHashSet();

        PluginManager.NotificationsThatChangeNothing.Where(name => !methodNames.Contains(name)).ShouldBeEmpty();
    }

    private static PluginManager CreateManager(ProjectChangeVersion version, out VersionReadingPlugin plugin)
    {
        PluginManager manager = new PluginManager(
            new Mock<IPluginEnablementStore>().Object,
            new Mock<IPluginHostConfiguration>().Object,
            version);
        plugin = new VersionReadingPlugin(version);
        manager.Plugins = new List<PluginBase> { plugin };
        PluginManager.StartupPlugin(plugin, manager);
        return manager;
    }

    /// <summary>Reads the version from inside the handlers of the notifications the tests send.</summary>
    private sealed class VersionReadingPlugin : PluginBase
    {
        private readonly ProjectChangeVersion _version;

        public VersionReadingPlugin(ProjectChangeVersion version)
        {
            _version = version;
        }

        /// <summary>The version the first handler to run saw, or null when none ran.</summary>
        public long? VersionSeen { get; private set; }

        public long? LastVersionSeen { get; private set; }

        public override string FriendlyName => "VersionReading";

        public override void StartUp()
        {
            VariableSet += (_, _, _, _, _) => Record();
            VariableSetThroughReference += (_, _, _, _, _) => Record();
            VariableAdd += (_, _) => Record();
            VariableDelete += (_, _) => Record();
            InstanceAdd += (_, _) => Record();
            InstanceDelete += (_, _) => Record();
            InstancesDelete += (_, _) => Record();
            InstanceRename += (_, _, _) => Record();
            InstanceReordered += _ => Record();
            ElementAdd += _ => Record();
            ElementDelete += _ => Record();
            ElementRename += (_, _) => Record();
            ElementDuplicate += (_, _) => Record();
            ElementImported += _ => Record();
            ElementReloaded += _ => Record();
            StateAdd += _ => Record();
            StateDelete += _ => Record();
            StateRename += (_, _) => Record();
            CategoryAdd += _ => Record();
            CategoryDelete += _ => Record();
            CategoryRename += (_, _) => Record();
            BehaviorReferencesChanged += _ => Record();
            AfterUndo += Record;
            ProjectLoad += _ => Record();
            ProjectPropertySet += _ => Record();
            ReactToFileChanged += _ => Record();
            AddAndRemoveVariablesForType += (_, _) => Record();
        }

        private void Record()
        {
            VersionSeen ??= _version.Version;
            LastVersionSeen = _version.Version;
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }
}
