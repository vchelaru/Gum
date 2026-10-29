using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.FileWatchPlugin;
using Gum.Services;
using Gum.ToolStates;
using Gum.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using ToolsUtilities;

namespace Gum.Presentation.Tests.Logic;

/// <summary>
/// A real <see cref="CopyPasteLogic"/> pasting into a real <see cref="MainFileWatchPlugin"/> and a real
/// <see cref="ProjectReferencedFiles"/> (#7039): the picture a pasted category or state names ends up in the
/// kept set, which the unit tests of each link only pin one hop at a time.
/// </summary>
public class CopyPasteReferencedFilesRealWiringTests : BaseTestClass
{
    /// <summary>Stands in for <c>PluginManager</c>, which turns these calls into the plugins' events.</summary>
    private sealed class ForwardingToPlugin : ICopyPastePluginNotifier
    {
        public PluginBase Plugin = null!;
        public void InstanceAdd(ElementSave element, InstanceSave instance) { }
        public void ElementDuplicate(ElementSave oldElement, ElementSave newElement) { }
        public void StateAdd(StateSave stateSave) => Plugin.CallStateAdd(stateSave);
        public void CategoryAdd(StateSaveCategory category) => Plugin.CallStateCategoryAdd(category);
    }

    private sealed class SynchronousDispatcher : IDispatcher
    {
        public void Invoke(Action action) => action();
        public void Post(Action action) => action();
    }

    private static VariableSave Picture(string file) => new VariableSave
    {
        Name = "Item.SourceFile", Type = "string", Value = file, IsFile = true, SetsValue = true,
    };

    [Theory]
    [InlineData(CopyType.Category)]
    [InlineData(CopyType.State)]
    public void APastedCategoryOrState_PutsItsPictureInTheKeptSetOfReferencedFiles(CopyType copyType)
    {
        string root = Path.Combine(Path.GetTempPath(), "Gum7039Wiring_" + Path.GetRandomFileName());
        Directory.CreateDirectory(root);
        try
        {
            GumProjectSave project = new GumProjectSave { FullFileName = Path.Combine(root, "Project.gumx") };
            ObjectFinder.Self.GumProjectSave = project;
            ComponentSave component = new ComponentSave { Name = "Card", BaseType = "Container" };
            StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
            defaultState.Variables.Add(Picture("Textures/Default.png"));
            component.States.Add(defaultState);
            component.Instances.Add(new InstanceSave { Name = "Item", BaseType = "Sprite", ParentContainer = component });
            StateSaveCategory category = new StateSaveCategory { Name = "Mood" };
            StateSave happy = new StateSave { Name = "Happy", ParentContainer = component };
            happy.Variables.Add(Picture("Textures/Happy.png"));
            StateSave sad = new StateSave { Name = "Sad", ParentContainer = component };
            sad.Variables.Add(Picture("Textures/Sad.png"));
            category.States.Add(happy);
            category.States.Add(sad);
            component.Categories.Add(category);
            project.Components.Add(component);
            project.ComponentReferences.Add(new ElementReference { Name = "Card", ElementType = ElementType.Component });

            AutoMocker mocker = new AutoMocker();
            Mock<ISelectedState> selectedState = mocker.GetMock<ISelectedState>();
            selectedState.Setup(x => x.SelectedElement).Returns(component);
            selectedState.Setup(x => x.SelectedStateCategorySave).Returns(category);
            selectedState.Setup(x => x.SelectedStateSave).Returns(happy);
            selectedState.Setup(x => x.SelectedInstances).Returns(new List<InstanceSave>());
            mocker.Use(new Lazy<IElementTreeRoots>(() => mocker.GetMock<IElementTreeRoots>().Object));
            mocker.Use<ICircularReferenceManager>(new CircularReferenceManager(ObjectFinder.Self));

            ProjectReferencedFiles referencedFiles = new ProjectReferencedFiles();
            Mock<IFileWatchManager> fileWatchManager = new Mock<IFileWatchManager>();
            fileWatchManager.Setup(m => m.InvalidateReferencedFiles()).Callback(referencedFiles.Invalidate);
            Mock<IPluginTab> tab = new Mock<IPluginTab>();
            tab.SetupAllProperties();
            Mock<ITabManager> tabManager = new Mock<ITabManager>();
            tabManager.Setup(m => m.AddControl(It.IsAny<object>(), "File Watch", It.IsAny<TabLocation>())).Returns(tab.Object);
            FileWatchLogic logic = new FileWatchLogic(
                fileWatchManager.Object, Mock.Of<IGuiCommands>(), Mock.Of<IProjectState>(), Mock.Of<IProjectManager>(),
                new SynchronousDispatcher());
            using PeriodicUiTimer timer = new PeriodicUiTimer(new SynchronousDispatcher(), Mock.Of<ILogger<PeriodicUiTimer>>());
            MainFileWatchPlugin plugin = new MainFileWatchPlugin(fileWatchManager.Object, logic, timer)
            {
                TabManager = tabManager.Object,
                Menu = new MenuModel(),
            };
            plugin.StartUp();
            mocker.Use<ICopyPastePluginNotifier>(new ForwardingToPlugin { Plugin = plugin });
            CopyPasteLogic copyPasteLogic = mocker.CreateInstance<CopyPasteLogic>();

            // Copy the original, remove it, and build the set without its picture; the paste brings it back.
            FilePath happyFile = new FilePath(Path.Combine(root, "Textures", "Happy.png"));
            copyPasteLogic.OnCopy(copyType);
            if (copyType == CopyType.Category)
            {
                component.Categories.Remove(category);
            }
            else
            {
                category.States.Remove(happy);
            }
            referencedFiles.Invalidate();
            referencedFiles.Contains(happyFile).ShouldBeFalse("the set is built without the picture the paste adds");

            copyPasteLogic.OnPaste(copyType);

            referencedFiles.Contains(happyFile).ShouldBeTrue();
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best-effort */ }
        }
    }
}
