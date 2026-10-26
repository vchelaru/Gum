using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Gum.Logic.FileWatch;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Logic;
using Gum.Undo;
using MonoGameGum.Tests;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// An element reloaded from disk after an external edit must reach every element whose variable
/// references read it, directly or through a chain. The tool stores a reference's value as a
/// materialized scalar on the reader, so the reload alone leaves the reader at the old value.
/// Source = the reloaded element ("Styles"); Theme = an element in the middle of a chain; the reader is
/// a component (ButtonStandard) or a screen (MenuScreen) whose Background.Red reads the element above it.
/// </summary>
public class FileChangeReloadDependentsTests : BaseTestClass
{
    private const string ReaderComponent = "Controls/ButtonStandard";
    private const string ReaderScreen = "MenuScreen";

    private readonly string _tempDirectory;
    private GumProjectSave _project = null!;
    private FileChangeReactionLogic _reaction = null!;
    private VariableReferenceLogic _referenceLogic = null!;
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IWireframeObjectManager> _wireframe = new();
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly Mock<IPluginManager> _pluginManager = new();
    private Gum.Undo.IUndoManager _undo = new Mock<Gum.Undo.IUndoManager>().Object;

    public FileChangeReloadDependentsTests()
    {
        GumExpressionService.Initialize();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumReloadDependents_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        try { Directory.Delete(_tempDirectory, recursive: true); } catch { /* best-effort */ }
        base.Dispose();
    }

    /// <summary>Saves the elements as a project, then loads it back the way the tool does.</summary>
    private void Arrange(params ElementSave[] elements)
    {
        GumProjectSave authored = new GumProjectSave { FullFileName = Path.Combine(_tempDirectory, "Project.gumj") };
        foreach (ElementSave element in elements)
        {
            if (element is ScreenSave screen)
            {
                authored.Screens.Add(screen);
                authored.ScreenReferences.Add(new ElementReference { Name = screen.Name, ElementType = ElementType.Screen });
            }
            else
            {
                authored.Components.Add((ComponentSave)element);
                authored.ComponentReferences.Add(new ElementReference { Name = element.Name, ElementType = ElementType.Component });
            }
        }
        ObjectFinder.Self.GumProjectSave = authored;
        authored.Initialize();
        authored.Save(authored.FullFileName, saveElements: true);

        _project = GumProjectSave.Load(authored.FullFileName);
        ObjectFinder.Self.GumProjectSave = _project;
        _project.Initialize();

        _fileCommands.Setup(f => f.ProjectDirectory).Returns(new FilePath(_tempDirectory + Path.DirectorySeparatorChar));
        Mock<IProjectState> projectState = new();
        projectState.Setup(p => p.GumProjectSave).Returns(_project);
        _wireframe.Setup(w => w.AllIpsos).Returns(new List<GraphicalUiElement>());

        _referenceLogic = new VariableReferenceLogic(
            _guiCommands.Object,
            new Mock<IWireframeCommands>().Object,
            new Mock<IDialogService>().Object,
            _fileCommands.Object,
            new Mock<ICompositeMemberRegistry>().Object,
            new Mock<IDispatcher>().Object,
            _wireframe.Object);

        _reaction = new FileChangeReactionLogic(
            _selectedState.Object,
            new Mock<IWireframeCommands>().Object,
            _guiCommands.Object,
            _fileCommands.Object,
            new Mock<IOutputManager>().Object,
            _wireframe.Object,
            projectState.Object,
            new Mock<IStandardElementsManagerGumTool>().Object,
            _pluginManager.Object,
            new Mock<IDialogService>().Object,
            new UnsavedChangesTracker(),
            _referenceLogic,
            _undo);
    }

    private static string QualifiedName(ElementSave element) => ElementReference.GetQualifiedName(element, element.Name);

    private static ElementSave NewElement(bool isScreen, string name)
    {
        ElementSave element = isScreen ? new ScreenSave { Name = name } : new ComponentSave { Name = name };
        element.States.Add(new StateSave { Name = "Default", ParentContainer = element });
        return element;
    }

    /// <summary>An element holding Primary.FillRed, which the elements built by the other helpers read.</summary>
    private static ElementSave BuildSource(bool isScreen, string name, int fillRed)
    {
        ElementSave source = NewElement(isScreen, name);
        source.Instances.Add(new InstanceSave { Name = "Primary", BaseType = "Rectangle", ParentContainer = source });
        source.DefaultState.Variables.Add(new VariableSave { Name = "Primary.FillRed", Type = "int", Value = fillRed, SetsValue = true });
        return source;
    }

    /// <summary>A source that takes its own Primary.FillRed from <paramref name="reads"/>.</summary>
    private static ElementSave BuildTheme(string name, ElementSave reads)
    {
        ElementSave theme = BuildSource(isScreen: false, name, fillRed: 21);
        theme.DefaultState.VariableLists.Add(new VariableListSave<string>
        {
            Name = "Primary.VariableReferences",
            Type = "string",
            Value = new List<string> { "FillRed = " + QualifiedName(reads) + ".Primary.FillRed" },
        });
        return theme;
    }

    private static ElementSave BuildReader(bool isScreen, string name, ElementSave reads)
    {
        ElementSave reader = NewElement(isScreen, name);
        reader.Instances.Add(new InstanceSave { Name = "Background", BaseType = "NineSlice", ParentContainer = reader });
        reader.DefaultState.Variables.Add(new VariableSave { Name = "Background.Red", Type = "int", Value = 21, SetsValue = true });
        reader.DefaultState.VariableLists.Add(new VariableListSave<string>
        {
            Name = "Background.VariableReferences",
            Type = "string",
            Value = new List<string> { "Red = " + QualifiedName(reads) + ".Primary.FillRed" },
        });
        return reader;
    }

    private string FileOf(ElementSave element) => element is ScreenSave
        ? Path.Combine(_tempDirectory, "Screens", element.Name + ".gusj")
        : Path.Combine(_tempDirectory, "Components", element.Name + ".gucj");

    private void EditFillRedOnDisk(ElementSave element, int newValue) => EditIntOnDisk(element, "Primary.FillRed", newValue);

    private void EditIntOnDisk(ElementSave element, string variableName, int newValue)
    {
        string file = FileOf(element);
        string text = File.ReadAllText(file);
        Regex variable = new(@"(""Name"": """ + Regex.Escape(variableName) + @""",\s*""Type"": ""int"",\s*""SetsValue"": true,\s*""ValueAsInt"": )\d+");
        variable.IsMatch(text).ShouldBeTrue("the saved file should hold " + variableName + " as an int");
        File.WriteAllText(file, variable.Replace(text, "${1}" + newValue));
    }

    private void Reload(ElementSave element) => _reaction.ReactToFileChanged(new FilePath(FileOf(element)));

    private object? GetValue(string elementName, string variableName)
    {
        ElementSave? element = ObjectFinder.Self.GetElementSave(elementName);
        element.ShouldNotBeNull(elementName);
        return element.DefaultState.Variables.First(v => v.Name == variableName).Value;
    }

    private void VerifySaved(string elementName, Times times) =>
        _fileCommands.Verify(f => f.TryAutoSaveElement(It.Is<ElementSave>(e => e.Name == elementName)), times);

    [Fact]
    public void ReactToFileChanged_ShouldReloadTheEditedElementFromDisk()
    {
        ElementSave styles = BuildSource(isScreen: false, "Styles", fillRed: 21);
        Arrange(styles, BuildReader(isScreen: false, ReaderComponent, styles));
        EditFillRedOnDisk(styles, 200);

        Reload(styles);

        GetValue("Styles", "Primary.FillRed").ShouldBe(200);
    }

    [Fact]
    public void ApplyReferencesToElement_AfterAReload_UpdatesTheDependent()
    {
        // Pins the existing in-tool cascade the reload has to mirror: it resolves the reader from the
        // reloaded element's values.
        ElementSave styles = BuildSource(isScreen: false, "Styles", fillRed: 21);
        Arrange(styles, BuildReader(isScreen: false, ReaderComponent, styles));
        EditFillRedOnDisk(styles, 200);
        Reload(styles);

        _referenceLogic.ApplyReferencesToElement(ObjectFinder.Self.GetElementSave("Styles")!, trySave: false);

        GetValue(ReaderComponent, "Background.Red").ShouldBe(200);
    }

    // sourceIsScreen: the reloaded element is a screen or a component.
    // readerIsScreen: the element reading it is a screen or a component.
    // viaTheme: the reader reads Theme, which reads the reloaded element (only the reloaded file changes).
    // readerSelected: the reader is the element selected on the canvas and shown in the Variables tab.
    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, true, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, true, false)]
    [InlineData(true, true, true, false)]
    [InlineData(false, false, false, true)]
    [InlineData(true, true, true, true)]
    public void ReactToFileChanged_ShouldUpdateEveryElementWhoseReferencesReadTheReloadedElement(
        bool sourceIsScreen, bool readerIsScreen, bool viaTheme, bool readerSelected)
    {
        ElementSave source = BuildSource(sourceIsScreen, sourceIsScreen ? "HudScreen" : "Styles", fillRed: 21);
        ElementSave? theme = viaTheme ? BuildTheme("Theme", source) : null;
        ElementSave reader = BuildReader(readerIsScreen, readerIsScreen ? ReaderScreen : ReaderComponent, theme ?? source);
        Arrange(theme == null ? new[] { source, reader } : new[] { source, theme, reader });
        // The tool's in-memory elements are the ones loaded from disk.
        reader = ObjectFinder.Self.GetElementSave(reader.Name)!;
        if (readerSelected)
        {
            _selectedState.Setup(s => s.SelectedElement).Returns(reader);
        }
        EditFillRedOnDisk(source, 200);

        Reload(source);

        GetValue(reader.Name, "Background.Red").ShouldBe(200);
        if (theme != null)
        {
            GetValue(theme.Name, "Primary.FillRed").ShouldBe(200);
            VerifySaved(theme.Name, Times.AtLeastOnce());
        }
        // The reader's file follows its new value, as it does after an edit in the tool.
        VerifySaved(reader.Name, Times.AtLeastOnce());
        // The reloaded element is the file that just changed, so saving it would only rewrite it.
        VerifySaved(source.Name, Times.Never());
        // The canvas and the Variables tab show the new value.
        _wireframe.Verify(w => w.RefreshAll(It.IsAny<bool>(), It.IsAny<bool>()), Times.AtLeastOnce());
        _guiCommands.Verify(g => g.RefreshVariables(true), Times.AtLeastOnce());
    }

    [Fact]
    public void ReactToFileChanged_WithNoReaders_ShouldNotSaveAnything()
    {
        ElementSave styles = BuildSource(isScreen: false, "Styles", fillRed: 21);
        Arrange(styles);
        EditFillRedOnDisk(styles, 200);

        Reload(styles);

        _fileCommands.Verify(f => f.TryAutoSaveElement(It.IsAny<ElementSave>()), Times.Never());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DeferDependentUpdates_WhenTheReaderIsAlsoReloaded_ShouldNotSaveItBeforeItsOwnReload(bool readerFirst)
    {
        // A git checkout changes Theme and Styles together. Updating the readers of Theme before Styles is
        // reloaded would write Theme's old copy of the Styles value over the file git just wrote.
        ElementSave styles = BuildSource(isScreen: false, "Styles", fillRed: 21);
        ElementSave theme = BuildTheme("Theme", styles);
        ElementSave reader = BuildReader(isScreen: false, ReaderComponent, theme);
        Arrange(styles, theme, reader);
        EditFillRedOnDisk(styles, 200);
        EditFillRedOnDisk(theme, 7);

        using (_reaction.DeferDependentUpdates())
        {
            Reload(readerFirst ? theme : styles);
            Reload(readerFirst ? styles : theme);

            _fileCommands.Verify(f => f.TryAutoSaveElement(It.IsAny<ElementSave>()), Times.Never());
        }

        // The reference sets the final value, not the 7 an editor wrote into Theme's own scalar.
        GetValue("Theme", "Primary.FillRed").ShouldBe(200);
        GetValue(ReaderComponent, "Background.Red").ShouldBe(200);
    }

    private sealed class SynchronousDispatcher : IDispatcher
    {
        public void Invoke(Action action) => action();
        public void Post(Action action) => action();
    }

    [Fact]
    public void Flush_WithTheSourceAndAReaderChanged_ShouldReloadBothBeforeSavingAnyReader()
    {
        // The queue is a ConcurrentDictionary, so the order the two files are processed in is not
        // controllable; without the deferral every order saves a reader after only one of them reloaded.
        ElementSave styles = BuildSource(isScreen: false, "Styles", fillRed: 21);
        ElementSave theme = BuildTheme("Theme", styles);
        theme.DefaultState.Variables.Add(new VariableSave { Name = "Primary.FillGreen", Type = "int", Value = 1, SetsValue = true });
        ElementSave reader = BuildReader(isScreen: false, ReaderComponent, theme);
        Arrange(styles, theme, reader);
        EditFillRedOnDisk(styles, 200);
        EditIntOnDisk(theme, "Primary.FillGreen", 99);

        int reloaded = 0;
        _pluginManager.Setup(p => p.ElementReloaded(It.IsAny<ElementSave>())).Callback<ElementSave>(_ => reloaded++);
        List<int> reloadedAtEachSave = new();
        _fileCommands.Setup(f => f.TryAutoSaveElement(It.IsAny<ElementSave>())).Callback<ElementSave?>(_ => reloadedAtEachSave.Add(reloaded));

        Mock<IFileWatchIgnoreList> ignoreList = new();
        ignoreList.Setup(i => i.TryGetIgnoreFileChange(It.IsAny<FilePath>())).Returns(false);
        Mock<IProjectManager> projectManager = new();
        projectManager.Setup(p => p.GumProjectSave).Returns(_project);
        FileWatchManager watch = new(_guiCommands.Object, projectManager.Object, _reaction, ignoreList.Object,
            new SynchronousDispatcher(), new FileSystemWatcherFactory());
        try
        {
            watch.EnableWithDirectories(new HashSet<FilePath> { new FilePath(_tempDirectory + "/") });
            foreach (ElementSave changed in new[] { styles, theme })
            {
                watch.HandleFileSystemChange(null, new FileSystemEventArgs(WatcherChangeTypes.Changed,
                    Path.GetDirectoryName(FileOf(changed))!, Path.GetFileName(FileOf(changed))));
            }
            Thread.Sleep(700);

            watch.Flush();
        }
        finally
        {
            watch.Disable();
        }

        reloaded.ShouldBe(2);
        reloadedAtEachSave.ShouldNotBeEmpty();
        reloadedAtEachSave.ShouldAllBe(count => count == 2);
        GetValue("Theme", "Primary.FillRed").ShouldBe(200);
        GetValue("Theme", "Primary.FillGreen").ShouldBe(99);
        GetValue(ReaderComponent, "Background.Red").ShouldBe(200);
    }

    [Fact]
    public void ReactToFileChanged_WhenTwoElementsReadEachOther_ShouldTerminateAtWhatTheToolCascadeGives()
    {
        ElementSave styles = BuildSource(isScreen: false, "Styles", fillRed: 21);
        ElementSave theme = BuildTheme("Theme", styles);
        styles.DefaultState.VariableLists.Add(new VariableListSave<string>
        {
            Name = "Primary.VariableReferences",
            Type = "string",
            Value = new List<string> { "FillRed = Components/Theme.Primary.FillRed" },
        });
        Arrange(styles, theme);
        EditFillRedOnDisk(styles, 200);

        Task reload = Task.Run(() => Reload(styles));
        reload.Wait(TimeSpan.FromSeconds(20)).ShouldBeTrue("the reload should not cascade forever");

        // The reloaded value stands and its reader follows it, as after an in-tool edit of Styles.
        GetValue("Styles", "Primary.FillRed").ShouldBe(200);
        GetValue("Theme", "Primary.FillRed").ShouldBe(200);
        // Nothing is left to change, and nothing was saved more than once.
        _referenceLogic.ApplyReferencesToElement(ObjectFinder.Self.GetElementSave("Styles")!, trySave: false).ShouldBeEmpty();
        VerifySaved("Theme", Times.Once());
        VerifySaved("Styles", Times.Never());
    }

    private enum SweepStep { Edit, AddRow, Reload, Undo, Redo }

    /// <summary>
    /// Seeded sequences of in-tool edits, a reference row added in-tool, outside reloads of the source, undo and
    /// redo on a reader (direct or through a middle element). Memory must equal disk after every step, the value
    /// a reference produced must be the source's whenever it was not restored from a snapshot, and an element
    /// that reads nothing keeps its own undo history intact.
    /// </summary>
    [Fact]
    public void ReloadUndoSweep_ShouldKeepMemoryAndDiskEqual_AndLeaveNonReadersAlone()
    {
        int cases = SweepScale.Cases(300);
        for (int index = 0; index < cases; index++)
        {
            RunSweepCase(new Random(1000 + index), index);
        }
    }

    private void RunSweepCase(Random random, int caseIndex)
    {
        bool viaTheme = random.Next(2) == 0;
        bool rowFromStart = random.Next(2) == 0;
        string description = $"case {caseIndex} viaTheme={viaTheme} rowFromStart={rowFromStart}";

        ElementSave styles = BuildSource(isScreen: false, "Styles", fillRed: 21);
        ElementSave? theme = viaTheme ? BuildTheme("Theme", styles) : null;
        ElementSave reader = BuildReader(isScreen: false, ReaderComponent, theme ?? styles);
        ElementSave other = BuildSource(isScreen: false, "Other", fillRed: 21);
        // A reader that starts without its row has neither the row nor the value it produces.
        if (!rowFromStart)
        {
            reader.DefaultState.VariableLists.Clear();
            reader.DefaultState.Variables.RemoveAll(v => v.Name == "Background.Red");
        }
        Arrange(theme == null ? new[] { styles, reader, other } : new[] { styles, theme, reader, other });

        ElementSave selected = ObjectFinder.Self.GetElementSave(ReaderComponent)!;
        ElementSave otherElement = ObjectFinder.Self.GetElementSave("Other")!;
        _selectedState.Setup(s => s.SelectedElement).Returns(() => selected);
        _selectedState.Setup(s => s.SelectedStateSave).Returns(() => selected.DefaultState);
        Mock<IReferenceFinderProjectProvider> projectProvider = new();
        projectProvider.Setup(p => p.GumProjectSave).Returns(_project);
        _fileCommands.Invocations.Clear();
        void SaveToDisk(ElementSave? element) => element?.Save(FileOf(element), useCompactFormat: true);
        _fileCommands.Setup(f => f.TryAutoSaveElement(It.IsAny<ElementSave>())).Callback<ElementSave?>(SaveToDisk);
        _fileCommands.Setup(f => f.TryAutoSaveCurrentElement()).Callback(() => SaveToDisk(selected));
        UndoManager undoManager = new(_selectedState.Object, new Mock<IUndoRenameLogic>().Object, _guiCommands.Object,
            _fileCommands.Object, new Mock<IMessenger>().Object, new Mock<IUndoPluginNotifier>().Object,
            new Mock<IAnimationUndoProvider>().Object, projectProvider.Object,
            new Lazy<IVariableReferenceLogic>(() => _referenceLogic));
        _undo = undoManager;
        RebuildReactionLogic();

        // Other reads nothing; its own history holds the value it started with.
        using (undoManager.RequestLock(otherElement))
        {
            otherElement.DefaultState.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = 80f, SetsValue = true });
        }
        undoManager.RecordState();

        int sourceValue = 21;
        List<SweepStep> recorded = new();
        int position = -1;
        bool restoredFromSnapshot = false;
        string steps = "";
        for (int step = 0; step < 8; step++)
        {
            SweepStep action = (SweepStep)random.Next(5);
            ElementSave current = selected;
            bool hasRow = current.DefaultState.VariableLists.Count > 0;
            switch (action)
            {
                case SweepStep.Edit:
                    using (undoManager.RequestLock())
                    {
                        current.DefaultState.Variables.RemoveAll(v => v.Name == "Width");
                        current.DefaultState.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = (float)(100 + step), SetsValue = true });
                    }
                    SaveToDisk(current);
                    Record(SweepStep.Edit);
                    break;
                case SweepStep.AddRow:
                    if (hasRow)
                    {
                        continue;
                    }
                    using (undoManager.RequestLock())
                    {
                        current.DefaultState.VariableLists.Add(new VariableListSave<string>
                        {
                            Name = "Background.VariableReferences",
                            Type = "string",
                            Value = new List<string> { "Red = " + QualifiedName(theme ?? styles) + ".Primary.FillRed" },
                        });
                        // An undone row add keeps the value the row wrote (#6884), so the variable may already exist.
                        current.DefaultState.Variables.RemoveAll(v => v.Name == "Background.Red");
                        current.DefaultState.Variables.Add(new VariableSave { Name = "Background.Red", Type = "int", Value = sourceValue, SetsValue = true });
                    }
                    SaveToDisk(current);
                    Record(SweepStep.AddRow);
                    restoredFromSnapshot = false;
                    break;
                case SweepStep.Reload:
                    sourceValue = 22 + step * 10 + random.Next(9);
                    EditFillRedOnDisk(styles, sourceValue);
                    Reload(styles);
                    restoredFromSnapshot = false;
                    break;
                case SweepStep.Undo:
                    if (position < 0)
                    {
                        continue;
                    }
                    undoManager.PerformUndo();
                    restoredFromSnapshot |= recorded[position] == SweepStep.AddRow;
                    position--;
                    break;
                case SweepStep.Redo:
                    if (position + 1 >= recorded.Count)
                    {
                        continue;
                    }
                    undoManager.PerformRedo();
                    position++;
                    restoredFromSnapshot |= recorded[position] == SweepStep.AddRow;
                    break;
            }
            steps += action + " ";
            AssertMemoryEqualsDisk(selected, description + ": " + steps);
            if (theme != null)
            {
                AssertMemoryEqualsDisk(ObjectFinder.Self.GetElementSave("Theme")!, description + ": " + steps);
            }
            object? red = selected.DefaultState.Variables.FirstOrDefault(v => v.Name == "Background.Red")?.Value;
            if (selected.DefaultState.VariableLists.Count > 0 && !restoredFromSnapshot)
            {
                red.ShouldBe(sourceValue, description + ": " + steps);
            }

            void Record(SweepStep kind)
            {
                recorded.RemoveRange(position + 1, recorded.Count - position - 1);
                recorded.Add(kind);
                position = recorded.Count - 1;
            }
        }

        // The element that reads nothing still undoes to its own value.
        selected = otherElement;
        undoManager.RecordState();
        undoManager.PerformUndo();
        otherElement.DefaultState.Variables.Any(v => v.Name == "Width").ShouldBeFalse(description + ": " + steps);
        otherElement.DefaultState.Variables.First(v => v.Name == "Primary.FillRed").Value.ShouldBe(21, description + ": " + steps);
    }

    private void RebuildReactionLogic()
    {
        Mock<IProjectState> projectState = new();
        projectState.Setup(p => p.GumProjectSave).Returns(_project);
        _reaction = new FileChangeReactionLogic(
            _selectedState.Object, new Mock<IWireframeCommands>().Object, _guiCommands.Object, _fileCommands.Object,
            new Mock<IOutputManager>().Object, _wireframe.Object, projectState.Object,
            new Mock<IStandardElementsManagerGumTool>().Object, _pluginManager.Object, new Mock<IDialogService>().Object,
            new UnsavedChangesTracker(), _referenceLogic, _undo);
    }

    private void AssertMemoryEqualsDisk(ElementSave element, string context)
    {
        ComponentSave onDisk = ElementReference.DeserializeElement<ComponentSave>(FileOf(element), GumProjectSave.NativeVersion);
        foreach (string name in new[] { "Width", "Background.Red", "Primary.FillRed" })
        {
            onDisk.DefaultState.Variables.FirstOrDefault(v => v.Name == name)?.Value
                .ShouldBe(element.DefaultState.Variables.FirstOrDefault(v => v.Name == name)?.Value, context + " " + name);
        }
        onDisk.DefaultState.VariableLists.Count.ShouldBe(element.DefaultState.VariableLists.Count, context + " rows");
    }
}
