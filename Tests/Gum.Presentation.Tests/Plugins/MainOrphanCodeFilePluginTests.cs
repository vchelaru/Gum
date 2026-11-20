using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Localization;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins.InternalPlugins.OrphanCodeFiles;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.Plugins;

public class MainOrphanCodeFilePluginTests
{
    [Fact]
    public void ProjectLoad_PostsTheScan_AndDropsItWhenClosedFirst()
    {
        List<Action> posted = new List<Action>();
        Mock<IDispatcher> dispatcher = new Mock<IDispatcher>();
        dispatcher.Setup(d => d.Post(It.IsAny<Action>())).Callback<Action>(posted.Add);
        Mock<IProjectState> projectState = new Mock<IProjectState>();
        GumProjectSave project = new GumProjectSave();
        projectState.Setup(p => p.GumProjectSave).Returns(project);

        MainOrphanCodeFilePlugin plugin = new MainOrphanCodeFilePlugin(
            Mock.Of<INameVerifier>(), new LocalizationService(), projectState.Object,
            Mock.Of<IOutputManager>(), Mock.Of<IFileCommands>(), Mock.Of<IDialogService>(),
            new WeakReferenceMessenger(), dispatcher.Object, Mock.Of<IGuiCommands>(),
            Mock.Of<IRetryService>(), TestProjectRestrictions.Create())
        {
            Menu = new MenuModel(),
        };
        plugin.StartUp();

        plugin.CallProjectLoad(project);
        posted.Count.ShouldBe(1);

        projectState.Setup(p => p.GumProjectSave).Returns((GumProjectSave?)null);
        Should.NotThrow(() => posted[0]());
        posted.Count.ShouldBe(1);
    }
}
