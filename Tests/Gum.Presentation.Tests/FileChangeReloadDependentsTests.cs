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
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// An external edit to Components/Styles.gucj (Primary.FillRed 21 -> 200) must reach the components whose
/// variable references read Styles (ButtonStandard.Background.Red = Components/Styles.Primary.FillRed).
/// The tool stores a reference's value as a materialized scalar on the dependent, so the reload alone
/// leaves the dependent at the old value.
/// </summary>
public class FileChangeReloadDependentsTests : BaseTestClass
{
    private readonly string _tempDirectory;
    private readonly GumProjectSave _project;
    private readonly FileChangeReactionLogic _reaction;
    private readonly VariableReferenceLogic _referenceLogic;

    public FileChangeReloadDependentsTests()
    {
        GumExpressionService.Initialize();
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumReloadDependents_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);

        GumProjectSave authored = new GumProjectSave { FullFileName = Path.Combine(_tempDirectory, "Project.gumj") };
        authored.Components.Add(BuildStyles());
        authored.Components.Add(BuildButton());
        foreach (ComponentSave component in authored.Components)
        {
            authored.ComponentReferences.Add(new ElementReference { Name = component.Name, ElementType = ElementType.Component });
        }
        ObjectFinder.Self.GumProjectSave = authored;
        authored.Initialize();
        authored.Save(authored.FullFileName, saveElements: true);

        // Load it back the way the tool does, so the element references used by a reload exist.
        _project = GumProjectSave.Load(authored.FullFileName);
        ObjectFinder.Self.GumProjectSave = _project;
        _project.Initialize();

        Mock<IFileCommands> fileCommands = new();
        fileCommands.Setup(f => f.ProjectDirectory).Returns(new FilePath(_tempDirectory + Path.DirectorySeparatorChar));
        Mock<IProjectState> projectState = new();
        projectState.Setup(p => p.GumProjectSave).Returns(_project);
        Mock<IWireframeObjectManager> wireframe = new();
        wireframe.Setup(w => w.AllIpsos).Returns(new List<GraphicalUiElement>());

        _reaction = new FileChangeReactionLogic(
            new Mock<ISelectedState>().Object,
            new Mock<IWireframeCommands>().Object,
            new Mock<IGuiCommands>().Object,
            fileCommands.Object,
            new Mock<IOutputManager>().Object,
            wireframe.Object,
            projectState.Object,
            new Mock<IStandardElementsManagerGumTool>().Object,
            new Mock<IPluginManager>().Object,
            new Mock<IDialogService>().Object,
            new UnsavedChangesTracker());

        _referenceLogic = new VariableReferenceLogic(
            new Mock<IGuiCommands>().Object,
            new Mock<IWireframeCommands>().Object,
            new Mock<IDialogService>().Object,
            fileCommands.Object,
            new Mock<ICompositeMemberRegistry>().Object,
            new Mock<IDispatcher>().Object,
            wireframe.Object);
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        try { Directory.Delete(_tempDirectory, recursive: true); } catch { /* best-effort */ }
        base.Dispose();
    }

    private static ComponentSave BuildStyles()
    {
        ComponentSave styles = new() { Name = "Styles" };
        StateSave state = new() { Name = "Default", ParentContainer = styles };
        styles.States.Add(state);
        styles.Instances.Add(new InstanceSave { Name = "Primary", BaseType = "Rectangle", ParentContainer = styles });
        state.Variables.Add(new VariableSave { Name = "Primary.FillRed", Type = "int", Value = 21, SetsValue = true });
        return styles;
    }

    private static ComponentSave BuildButton()
    {
        ComponentSave button = new() { Name = "Controls/ButtonStandard" };
        StateSave state = new() { Name = "Default", ParentContainer = button };
        button.States.Add(state);
        button.Instances.Add(new InstanceSave { Name = "Background", BaseType = "NineSlice", ParentContainer = button });
        state.Variables.Add(new VariableSave { Name = "Background.Red", Type = "int", Value = 21, SetsValue = true });
        state.VariableLists.Add(new VariableListSave<string>
        {
            Name = "Background.VariableReferences",
            Type = "string",
            Value = new List<string> { "Red = Components/Styles.Primary.FillRed" },
        });
        return button;
    }

    private void EditStylesFileOnDisk(int newValue)
    {
        string file = Path.Combine(_tempDirectory, "Components", "Styles.gucj");
        string text = File.ReadAllText(file);
        Regex fillRed = new(@"(""Name"": ""Primary\.FillRed"",\s*""Type"": ""int"",\s*""SetsValue"": true,\s*""ValueAsInt"": )\d+");
        fillRed.IsMatch(text).ShouldBeTrue("the saved Styles file should hold Primary.FillRed as an int");
        File.WriteAllText(file, fillRed.Replace(text, "${1}" + newValue));
    }

    private object? GetValue(string elementName, string variableName)
    {
        ElementSave? element = ObjectFinder.Self.GetElementSave(elementName);
        element.ShouldNotBeNull($"{elementName}; project has [{string.Join(",", _project.Components.Select(c => c.Name))}] refs [{string.Join(",", _project.ComponentReferences.Select(c => c.Name))}]");
        return element.DefaultState.Variables.First(v => v.Name == variableName).Value;
    }

    [Fact]
    public void ReactToFileChanged_ShouldReloadTheEditedElementFromDisk()
    {
        EditStylesFileOnDisk(200);

        _reaction.ReactToFileChanged(new FilePath(Path.Combine(_tempDirectory, "Components", "Styles.gucj")));

        GetValue("Styles", "Primary.FillRed").ShouldBe(200);
    }

    [Fact]
    public void ReactToFileChanged_ShouldUpdateComponentsWhoseVariableReferencesReadTheReloadedElement()
    {
        EditStylesFileOnDisk(200);

        _reaction.ReactToFileChanged(new FilePath(Path.Combine(_tempDirectory, "Components", "Styles.gucj")));

        GetValue("Controls/ButtonStandard", "Background.Red").ShouldBe(200);
    }

    [Fact]
    public void ApplyReferencesToElement_AfterAReload_UpdatesTheDependent()
    {
        // Shows the missing piece is only the call: the tool's existing cascade resolves the
        // dependent from the reloaded element's values.
        EditStylesFileOnDisk(200);
        _reaction.ReactToFileChanged(new FilePath(Path.Combine(_tempDirectory, "Components", "Styles.gucj")));

        _referenceLogic.ApplyReferencesToElement(ObjectFinder.Self.GetElementSave("Styles")!, trySave: false);

        GetValue("Controls/ButtonStandard", "Background.Red").ShouldBe(200);
    }
}
