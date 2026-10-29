using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Gum.Logic;
using CommunityToolkit.Mvvm.Messaging;
using GumRuntime;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// A behavior (or an element's Forms values) reloaded from disk after an external edit must re-apply the
/// behavior's tool-only references to the components that implement it and to the elements holding
/// instances of those components. The tool stores a reference's result as a materialized value in the
/// default state, so the reload alone leaves it at what the old reference produced.
/// </summary>
public class FileChangeReloadBehaviorTests : BaseTestClass
{
    private const string BehaviorName = "ButtonBehavior";
    private const string ButtonName = "Controls/ButtonStandard";
    private const string OtherButtonName = "Controls/OtherButton";
    private const string ScreenName = "MenuScreen";
    private const string CategoryReference = "ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"";
    private const string EditedCategoryReference = "ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Greyed\"";

    private readonly string _tempDirectory;
    private GumProjectSave _project = null!;
    private FileChangeReactionLogic _reaction = null!;
    private VariableReferenceLogic _referenceLogic = null!;
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IWireframeObjectManager> _wireframe = new();
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly Mock<IPluginManager> _pluginManager = new();
    private IUndoManager _undo = new Mock<IUndoManager>().Object;
    private ElementSave? _selected;

    public FileChangeReloadBehaviorTests()
    {
        GumExpressionService.Initialize();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumReloadBehavior_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        try { Directory.Delete(_tempDirectory, recursive: true); } catch { /* best-effort */ }
        base.Dispose();
    }

    private string BehaviorFile => Path.Combine(_tempDirectory, "Behaviors", BehaviorName + ".behj");

    private static BehaviorSave BuildBehavior(params string[] references)
    {
        BehaviorSave behavior = new BehaviorSave { Name = BehaviorName };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        behavior.ToolOnlyVariableReferences.AddRange(references);
        return behavior;
    }

    private static ComponentSave BuildButton(string name, bool implementsBehavior, bool isEnabled = false)
    {
        ComponentSave button = new ComponentSave { Name = name, BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = button };
        defaultState.Variables.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = isEnabled, SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Type = "string", Name = "ButtonCategoryState", Value = isEnabled ? "Enabled" : "Disabled", SetsValue = true });
        button.States.Add(defaultState);
        if (implementsBehavior)
        {
            button.Behaviors.Add(new ElementBehaviorReference { BehaviorName = BehaviorName });
        }
        return button;
    }

    /// <summary>A screen with a disabled instance of each button, holding the value the old reference produced.</summary>
    private static ScreenSave BuildScreen(params string[] buttonNames)
    {
        ScreenSave screen = new ScreenSave { Name = ScreenName };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);
        foreach (string buttonName in buttonNames)
        {
            string instanceName = "Play" + buttonName.Substring(buttonName.IndexOf('/') + 1);
            screen.Instances.Add(new InstanceSave { Name = instanceName, BaseType = buttonName, ParentContainer = screen });
            defaultState.Variables.Add(new VariableSave { Type = "bool", Name = instanceName + ".IsEnabled", Value = false, SetsValue = true });
            defaultState.Variables.Add(new VariableSave { Type = "string", Name = instanceName + ".ButtonCategoryState", Value = "Disabled", SetsValue = true });
        }
        return screen;
    }

    /// <summary>Saves the project and the behavior, then loads them back the way the tool does.</summary>
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
        authored.BehaviorReferences.Add(new BehaviorReference { Name = BehaviorName });
        authored.Behaviors.Add(BuildBehavior(CategoryReference));
        ObjectFinder.Self.GumProjectSave = authored;
        authored.Initialize();
        authored.Save(authored.FullFileName, saveElements: true);
        Directory.CreateDirectory(Path.GetDirectoryName(BehaviorFile)!);
        authored.Behaviors[0].Save(BehaviorFile);

        _project = GumProjectSave.Load(authored.FullFileName);
        ObjectFinder.Self.GumProjectSave = _project;
        _project.Initialize();

        _fileCommands.Setup(f => f.ProjectDirectory).Returns(new FilePath(_tempDirectory + Path.DirectorySeparatorChar));
        Mock<IProjectState> projectState = new();
        projectState.Setup(p => p.GumProjectSave).Returns(_project);
        _wireframe.Setup(w => w.AllIpsos).Returns(new List<GraphicalUiElement>());

        VariableReferenceLogic referenceLogic = new VariableReferenceLogic(
            _guiCommands.Object,
            new Mock<IWireframeCommands>().Object,
            new Mock<IDialogService>().Object,
            _fileCommands.Object,
            new Mock<ICompositeMemberRegistry>().Object,
            new Mock<IDispatcher>().Object,
            _wireframe.Object);

        _referenceLogic = referenceLogic;
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
            referenceLogic,
            _undo);
    }

    private void EditBehaviorOnDisk(params string[] references) =>
        BuildBehavior(references.Length > 0 ? references : new[] { EditedCategoryReference }).Save(BehaviorFile);

    private void ReloadBehavior() => _reaction.ReactToFileChanged(new FilePath(BehaviorFile));

    private string ElementFile(string name) => Path.Combine(_tempDirectory, name == ScreenName ? "Screens" : "Components", name + (name == ScreenName ? ".gusj" : ".gucj"));

    private object? GetValue(string elementName, string variableName)
    {
        ElementSave? element = ObjectFinder.Self.GetElementSave(elementName);
        element.ShouldNotBeNull(elementName);
        return element.DefaultState.Variables.FirstOrDefault(v => v.Name == variableName)?.Value;
    }

    private void VerifySaved(string elementName, Times times) =>
        _fileCommands.Verify(f => f.TryAutoSaveElement(It.Is<ElementSave>(e => e.Name == elementName)), times);

    [Fact]
    public void ReactToFileChanged_BehaviorFile_ShouldReapplyItsReferencesToTheImplementingComponent()
    {
        Arrange(BuildButton(ButtonName, implementsBehavior: true));
        EditBehaviorOnDisk();

        ReloadBehavior();

        GetValue(ButtonName, "ButtonCategoryState").ShouldBe("Greyed");
    }

    [Fact]
    public void ReactToFileChanged_BehaviorFile_ShouldReapplyItsReferencesToInstancesOfTheImplementingComponent()
    {
        Arrange(BuildButton(ButtonName, implementsBehavior: true, isEnabled: true), BuildScreen(ButtonName));
        EditBehaviorOnDisk();

        ReloadBehavior();

        GetValue(ScreenName, "PlayButtonStandard.ButtonCategoryState").ShouldBe("Greyed");
    }

    [Fact]
    public void ReactToFileChanged_BehaviorFile_ShouldSaveTheElementsItChanged()
    {
        Arrange(BuildButton(ButtonName, implementsBehavior: true));
        EditBehaviorOnDisk();

        ReloadBehavior();

        VerifySaved(ButtonName, Times.AtLeastOnce());
    }

    [Fact]
    public void ReactToFileChanged_BehaviorFile_ShouldSaveTheElementsHoldingInstancesOfTheComponent()
    {
        Arrange(BuildButton(ButtonName, implementsBehavior: true, isEnabled: true), BuildScreen(ButtonName));
        EditBehaviorOnDisk();

        ReloadBehavior();

        VerifySaved(ScreenName, Times.AtLeastOnce());
    }

    [Fact]
    public void ReactToFileChanged_BehaviorFile_ShouldRefreshTheCanvasAndTheVariablesTab()
    {
        Arrange(BuildButton(ButtonName, implementsBehavior: true, isEnabled: true), BuildScreen(ButtonName));
        EditBehaviorOnDisk();

        ReloadBehavior();

        _wireframe.Verify(w => w.RefreshAll(It.IsAny<bool>(), It.IsAny<bool>()), Times.AtLeastOnce());
        _guiCommands.Verify(g => g.RefreshVariables(true), Times.AtLeastOnce());
    }

    [Fact]
    public void ReactToFileChanged_BehaviorFile_ShouldLeaveElementsThatDoNotUseTheBehaviorAlone()
    {
        Arrange(
            BuildButton(ButtonName, implementsBehavior: true, isEnabled: true),
            BuildButton(OtherButtonName, implementsBehavior: false, isEnabled: true),
            BuildScreen(OtherButtonName));
        EditBehaviorOnDisk();

        ReloadBehavior();

        GetValue(OtherButtonName, "ButtonCategoryState").ShouldBe("Enabled");
        GetValue(ScreenName, "PlayOtherButton.ButtonCategoryState").ShouldBe("Disabled");
        VerifySaved(OtherButtonName, Times.Never());
        VerifySaved(ScreenName, Times.Never());
    }

    [Fact]
    public void ReactToFileChanged_BehaviorFileWithNothingNewToApply_ShouldNotSaveAnything()
    {
        Arrange(BuildButton(ButtonName, implementsBehavior: true, isEnabled: true), BuildScreen(ButtonName));

        ReloadBehavior();

        _fileCommands.Verify(f => f.TryAutoSaveElement(It.IsAny<ElementSave>()), Times.Never());
    }

    [Fact]
    public void DeferDependentUpdates_BehaviorFile_ShouldApplyOnceTheScopeCloses()
    {
        Arrange(BuildButton(ButtonName, implementsBehavior: true, isEnabled: true), BuildScreen(ButtonName));
        EditBehaviorOnDisk();

        using (_reaction.DeferDependentUpdates())
        {
            ReloadBehavior();

            GetValue(ScreenName, "PlayButtonStandard.ButtonCategoryState").ShouldBe("Disabled");
            _fileCommands.Verify(f => f.TryAutoSaveElement(It.IsAny<ElementSave>()), Times.Never());
        }

        GetValue(ScreenName, "PlayButtonStandard.ButtonCategoryState").ShouldBe("Greyed");
        VerifySaved(ScreenName, Times.AtLeastOnce());
    }

    [Fact]
    public void ReactToFileChanged_ComponentFile_ShouldReapplyTheBehaviorReferencesToTheReloadedComponent()
    {
        // The class of the behavior case: a Forms value edited outside the tool changes what the
        // component's behavior derives from it.
        Arrange(BuildButton(ButtonName, implementsBehavior: true, isEnabled: true));
        string file = ElementFile(ButtonName);
        string text = File.ReadAllText(file);
        Regex isEnabled = new(@"(""Name"": ""IsEnabled"",\s*""Type"": ""bool"",\s*""SetsValue"": true,\s*""ValueAsBool"": )true");
        isEnabled.IsMatch(text).ShouldBeTrue("the saved file should hold IsEnabled");
        File.WriteAllText(file, isEnabled.Replace(text, "${1}false"));

        _reaction.ReactToFileChanged(new FilePath(file));

        GetValue(ButtonName, "ButtonCategoryState").ShouldBe("Disabled");
    }

    private void EditIsEnabledOnDisk(string elementName, bool from, bool to)
    {
        string file = ElementFile(elementName);
        string text = File.ReadAllText(file);
        Regex isEnabled = new(@"(""Name"": ""IsEnabled"",\s*""Type"": ""bool"",\s*""SetsValue"": true,\s*""ValueAsBool"": )" + from.ToString().ToLowerInvariant());
        isEnabled.IsMatch(text).ShouldBeTrue("the saved file should hold IsEnabled");
        File.WriteAllText(file, isEnabled.Replace(text, "${1}" + to.ToString().ToLowerInvariant()));
    }

    [Fact]
    public void ReactToFileChanged_ComponentFileWithIsEnabledBackToTrue_ShouldResetTheDerivedValueItHeld()
    {
        // The component stored "Disabled" because IsEnabled was false. IsEnabled now decides again, and the
        // reference resolves to the resting value, which must replace the stale "Disabled".
        Arrange(BuildButton(ButtonName, implementsBehavior: true, isEnabled: false));
        EditIsEnabledOnDisk(ButtonName, from: false, to: true);

        _reaction.ReactToFileChanged(new FilePath(ElementFile(ButtonName)));

        GetValue(ButtonName, "ButtonCategoryState").ShouldBe("Enabled");
    }

    // ButtonStandard is disabled and implements the behavior; OtherButton is enabled, implements it, and the
    // screen holds a disabled instance of it (an instance equal to its component's value stores nothing).
    private void ArrangeDisabledButtonAndScreenWithDisabledInstanceOfAnother(ComponentSave? otherButton = null) =>
        Arrange(
            BuildButton(ButtonName, implementsBehavior: true, isEnabled: false),
            otherButton ?? BuildButton(OtherButtonName, implementsBehavior: true, isEnabled: true),
            BuildScreen(OtherButtonName));

    private const string InstanceCategory = "PlayOtherButton.ButtonCategoryState";
    private const string InstanceLabel = "PlayOtherButton.ButtonLabelState";
    private const string LabelReference = "ButtonLabelState = IsEnabled ? \"On\" : \"Off\"";

    [Fact]
    public void ReactToFileChanged_BehaviorFileThatNowResolvesToTheRestingValue_ShouldResetTheComponentAndItsInstances()
    {
        ArrangeDisabledButtonAndScreenWithDisabledInstanceOfAnother();
        EditBehaviorOnDisk("ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Enabled\"");

        ReloadBehavior();

        GetValue(ButtonName, "ButtonCategoryState").ShouldBe("Enabled");
        // The instance override equals what its component resolves to now, so it is gone.
        GetValue(ScreenName, InstanceCategory).ShouldBeNull();
    }

    [Fact]
    public void ReactToFileChanged_BehaviorFileWithALineRemoved_ShouldRemoveTheValuesItMaterialized()
    {
        ArrangeDisabledButtonAndScreenWithDisabledInstanceOfAnother();
        EditBehaviorOnDisk("// no references left");

        ReloadBehavior();

        GetValue(ButtonName, "ButtonCategoryState").ShouldBeNull();
        GetValue(ScreenName, InstanceCategory).ShouldBeNull();
        VerifySaved(ButtonName, Times.AtLeastOnce());
        VerifySaved(ScreenName, Times.AtLeastOnce());
    }

    [Fact]
    public void ReactToFileChanged_BehaviorFileWithOneOfTwoLinesRemoved_ShouldKeepTheOtherLinesValue()
    {
        ArrangeDisabledButtonAndScreenWithDisabledInstanceOfAnother();
        EditBehaviorOnDisk(CategoryReference, LabelReference);
        ReloadBehavior();
        GetValue(ButtonName, "ButtonLabelState").ShouldBe("Off");
        GetValue(ScreenName, InstanceLabel).ShouldBe("Off");

        EditBehaviorOnDisk(CategoryReference);
        ReloadBehavior();

        GetValue(ButtonName, "ButtonLabelState").ShouldBeNull();
        GetValue(ScreenName, InstanceLabel).ShouldBeNull();
        GetValue(ButtonName, "ButtonCategoryState").ShouldBe("Disabled");
        GetValue(ScreenName, InstanceCategory).ShouldBe("Disabled");
    }

    [Fact]
    public void ReactToFileChanged_BehaviorFileWithALineAdded_ShouldMaterializeItOnTheComponentAndItsInstances()
    {
        ArrangeDisabledButtonAndScreenWithDisabledInstanceOfAnother();
        EditBehaviorOnDisk(CategoryReference, LabelReference);

        ReloadBehavior();

        GetValue(ButtonName, "ButtonLabelState").ShouldBe("Off");
        GetValue(ScreenName, InstanceLabel).ShouldBe("Off");
    }

    [Fact]
    public void ReactToFileChanged_ComponentFileThatNoLongerImplementsTheBehavior_ShouldRemoveTheValuesItMaterialized()
    {
        ArrangeDisabledButtonAndScreenWithDisabledInstanceOfAnother();
        BuildButton(ButtonName, implementsBehavior: false, isEnabled: false).Save(ElementFile(ButtonName));

        _reaction.ReactToFileChanged(new FilePath(ElementFile(ButtonName)));

        GetValue(ButtonName, "ButtonCategoryState").ShouldBeNull();
    }

    [Fact]
    public void ReactToFileChanged_ComponentFileThatNoLongerImplementsTheBehavior_ShouldRemoveTheValuesItsInstancesHold()
    {
        ArrangeDisabledButtonAndScreenWithDisabledInstanceOfAnother();
        BuildButton(OtherButtonName, implementsBehavior: false, isEnabled: true).Save(ElementFile(OtherButtonName));

        _reaction.ReactToFileChanged(new FilePath(ElementFile(OtherButtonName)));

        GetValue(ScreenName, InstanceCategory).ShouldBeNull();
    }

    [Fact]
    public void ReactToFileChanged_BehaviorFileWhenReadersFormALoop_ShouldFinish()
    {
        // The button's Background.Red reads Other.Primary.FillRed, which reads the button's Background.Red.
        ComponentSave button = BuildButton(ButtonName, implementsBehavior: true, isEnabled: false);
        button.Instances.Add(new InstanceSave { Name = "Background", BaseType = "NineSlice", ParentContainer = button });
        button.DefaultState.Variables.Add(new VariableSave { Name = "Background.Red", Type = "int", Value = 1, SetsValue = true });
        ComponentSave other = BuildButton(OtherButtonName, implementsBehavior: false, isEnabled: true);
        other.Instances.Add(new InstanceSave { Name = "Primary", BaseType = "Rectangle", ParentContainer = other });
        other.DefaultState.Variables.Add(new VariableSave { Name = "Primary.FillRed", Type = "int", Value = 2, SetsValue = true });
        button.DefaultState.VariableLists.Add(new VariableListSave<string>
        {
            Name = "Background.VariableReferences", Type = "string",
            Value = new List<string> { "Red = " + ElementReference.GetQualifiedName(other, other.Name) + ".Primary.FillRed" },
        });
        other.DefaultState.VariableLists.Add(new VariableListSave<string>
        {
            Name = "Primary.VariableReferences", Type = "string",
            Value = new List<string> { "FillRed = " + ElementReference.GetQualifiedName(button, button.Name) + ".Background.Red" },
        });
        Arrange(button, other);
        EditBehaviorOnDisk();

        System.Threading.Tasks.Task reload = System.Threading.Tasks.Task.Run(ReloadBehavior);

        reload.Wait(TimeSpan.FromSeconds(20)).ShouldBeTrue("the reload should finish even when elements read each other");
        GetValue(ButtonName, "ButtonCategoryState").ShouldBe("Greyed");
    }

    // The undo tests use the real UndoManager, selecting the element they edit.
    private void ArrangeWithRealUndo(params ElementSave[] elements)
    {
        _selectedState.Setup(x => x.SelectedElement).Returns(() => _selected);
        _selectedState.Setup(x => x.SelectedComponent).Returns(() => _selected as ComponentSave);
        _selectedState.Setup(x => x.SelectedStateSave).Returns(() => _selected?.DefaultState);
        Mock<IReferenceFinderProjectProvider> projectProvider = new();
        projectProvider.Setup(x => x.GumProjectSave).Returns(() => ObjectFinder.Self.GumProjectSave);
        _undo = new UndoManager(_selectedState.Object, new Mock<IUndoRenameLogic>().Object, _guiCommands.Object,
            _fileCommands.Object, new Mock<IMessenger>().Object, new Mock<IUndoPluginNotifier>().Object,
            new Mock<IAnimationUndoProvider>().Object, projectProvider.Object,
            new Lazy<IVariableReferenceLogic>(() => _referenceLogic));
        Arrange(elements);
    }

    private ElementSave Select(string elementName)
    {
        _selected = ObjectFinder.Self.GetElementSave(elementName);
        _selected.ShouldNotBeNull(elementName);
        return _selected;
    }

    // What typing the variable does: the edit, the tool-only references it feeds, one history entry.
    private void Type(string variableName, string type, object value)
    {
        ElementSave element = _selected!;
        _undo.RecordState();
        VariableSave? variable = element.DefaultState.Variables.FirstOrDefault(item => item.Name == variableName);
        if (variable == null)
        {
            variable = new VariableSave { Name = variableName, Type = type };
            element.DefaultState.Variables.Add(variable);
        }
        variable.Value = value;
        variable.SetsValue = true;
        BehaviorToolOnlyReferencesApplier.Apply(element, element.DefaultState);
        _undo.RecordUndo();
    }

    // Every save of the selected element, so a stale value that reaches the file is seen.
    private List<object?> RecordSavedValues(string variableName)
    {
        List<object?> saved = new List<object?>();
        _fileCommands.Setup(f => f.TryAutoSaveCurrentElement()).Callback(() =>
            saved.Add(_selected!.DefaultState.Variables.FirstOrDefault(v => v.Name == variableName)?.Value));
        return saved;
    }

    [Fact]
    public void PerformUndo_OfAnEarlierEditAfterABehaviorReload_ShouldKeepTheReappliedValue()
    {
        ArrangeWithRealUndo(BuildButton(ButtonName, implementsBehavior: true, isEnabled: false));
        Select(ButtonName);
        Type("Width", "float", 80f);
        EditBehaviorOnDisk();
        ReloadBehavior();
        GetValue(ButtonName, "ButtonCategoryState").ShouldBe("Greyed");
        List<object?> saved = RecordSavedValues("ButtonCategoryState");

        _undo.PerformUndo();

        GetValue(ButtonName, "Width").ShouldBeNull();
        GetValue(ButtonName, "ButtonCategoryState").ShouldBe("Greyed");
        saved.ShouldAllBe(value => (string?)value == "Greyed");
    }

    [Fact]
    public void PerformRedo_OfATypedIsEnabledEditAfterABehaviorReload_ShouldGiveTheReappliedValue()
    {
        ArrangeWithRealUndo(BuildButton(ButtonName, implementsBehavior: true, isEnabled: true));
        Select(ButtonName);
        Type("IsEnabled", "bool", false);
        Type("Width", "float", 80f);
        EditBehaviorOnDisk();
        ReloadBehavior();
        List<object?> saved = RecordSavedValues("ButtonCategoryState");

        _undo.PerformUndo();
        _undo.PerformUndo();
        GetValue(ButtonName, "ButtonCategoryState").ShouldBe("Enabled");
        _undo.PerformRedo();

        GetValue(ButtonName, "IsEnabled").ShouldBe(false);
        GetValue(ButtonName, "ButtonCategoryState").ShouldBe("Greyed");
        saved.ShouldNotContain("Disabled");
    }

    [Fact]
    public void PerformRedo_AfterABehaviorReloadWhileUndone_ShouldGiveTheReappliedValue()
    {
        ArrangeWithRealUndo(BuildButton(ButtonName, implementsBehavior: true, isEnabled: true));
        Select(ButtonName);
        Type("IsEnabled", "bool", false);
        _undo.PerformUndo();
        EditBehaviorOnDisk();
        ReloadBehavior();

        _undo.PerformRedo();

        GetValue(ButtonName, "ButtonCategoryState").ShouldBe("Greyed");
    }

    [Fact]
    public void PerformUndo_OfATypedIsEnabledEditWhoseOldValueTheReloadReplaced_ShouldGiveTheReappliedValue()
    {
        // The reload changes what a disabled button derives while the button is enabled, so nothing stored
        // on the element changes, and the edit's undo snapshot still holds the old derived value.
        ArrangeWithRealUndo(BuildButton(ButtonName, implementsBehavior: true, isEnabled: false));
        Select(ButtonName);
        Type("IsEnabled", "bool", true);
        EditBehaviorOnDisk();
        ReloadBehavior();
        List<object?> saved = RecordSavedValues("ButtonCategoryState");

        _undo.PerformUndo();

        GetValue(ButtonName, "IsEnabled").ShouldBe(false);
        GetValue(ButtonName, "ButtonCategoryState").ShouldBe("Greyed");
        saved.ShouldNotContain("Disabled");
    }

    [Fact]
    public void PerformUndo_OfATypedInstanceIsEnabledEditAfterABehaviorReload_ShouldGiveTheReappliedValue()
    {
        // A screen holds the derived value of its disabled instance.
        ArrangeWithRealUndo(BuildButton(ButtonName, implementsBehavior: true, isEnabled: true), BuildScreen(ButtonName));
        Select(ScreenName);
        Type("PlayButtonStandard.IsEnabled", "bool", true);
        EditBehaviorOnDisk();
        ReloadBehavior();
        List<object?> saved = RecordSavedValues("PlayButtonStandard.ButtonCategoryState");

        _undo.PerformUndo();

        GetValue(ScreenName, "PlayButtonStandard.IsEnabled").ShouldBe(false);
        GetValue(ScreenName, "PlayButtonStandard.ButtonCategoryState").ShouldBe("Greyed");
        saved.ShouldNotContain("Disabled");
        _undo.PerformRedo();
        GetValue(ScreenName, "PlayButtonStandard.IsEnabled").ShouldBe(true);
        GetValue(ScreenName, "PlayButtonStandard.ButtonCategoryState").ShouldBeNull();
    }

    [Fact]
    public void PerformUndo_OfATypedIsEnabledEditAfterABehaviorReload_ShouldGiveTheEnabledValue()
    {
        ArrangeWithRealUndo(BuildButton(ButtonName, implementsBehavior: true, isEnabled: true));
        Select(ButtonName);
        Type("IsEnabled", "bool", false);
        EditBehaviorOnDisk();
        ReloadBehavior();

        _undo.PerformUndo();

        GetValue(ButtonName, "IsEnabled").ShouldBe(true);
        GetValue(ButtonName, "ButtonCategoryState").ShouldBe("Enabled");
    }
}
