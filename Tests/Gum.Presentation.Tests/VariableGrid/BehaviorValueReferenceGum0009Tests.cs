using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using ToolsUtilities;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// #8020: a project made from the DarkPro Forms theme, with a Slider instance whose Minimum is set
/// through the Variables tab's Behavior row, then read back by a VariableReferences line.
/// </summary>
public class BehaviorValueReferenceGum0009Tests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly string _directory;
    private readonly GumProjectSave _project;
    private readonly ComponentSave _component;
    private readonly InstanceSave _slider;
    private readonly StateSave _defaultState;
    private readonly ElementSaveDisplayer _displayer;
    private readonly HeadlessErrorChecker _checker = new HeadlessErrorChecker(new DefaultTypeResolver());

    public BehaviorValueReferenceGum0009Tests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "Gum8020_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        StandardElementsManager.Self.Initialize();
        string themeDirectory = new FormsThemeLocator(new[] { FindThemesRoot() }).FindThemeDirectory("DarkPro")!;
        string projectPath = Path.Combine(_directory, "Project.gumx");
        new FormsTemplateCreator().CreateFromTheme(projectPath, themeDirectory);
        _project = new ProjectLoader().Load(projectPath).Project!;
        ObjectFinder.Self.GumProjectSave = _project;

        _component = new ComponentSave { Name = "Custom/PropertySlider", BaseType = "Container" };
        _defaultState = new StateSave { Name = "Default", ParentContainer = _component };
        _component.States.Add(_defaultState);
        _slider = new InstanceSave { Name = "Slider", BaseType = "DarkPro/Slider", ParentContainer = _component };
        _component.Instances.Add(_slider);
        _component.Instances.Add(new InstanceSave { Name = "MinText", BaseType = "Text", ParentContainer = _component });
        _project.Components.Add(_component);

        _mocker.Use(StandardElementsManager.Self);
        Gum.Reflection.TypeManager typeManager = new Gum.Reflection.TypeManager();
        typeManager.Initialize();
        _mocker.Use(typeManager);
        _mocker.GetMock<IPluginManager>().Setup(x => x.GetAttributesFor(It.IsAny<VariableSave>())).Returns(new List<Attribute>());
        _mocker.Use<IVariableSaveLogic>(_mocker.CreateInstance<VariableSaveLogic>());
        _mocker.GetMock<IProjectState>().Setup(x => x.GumProjectSave).Returns(_project);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(_defaultState);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(_component);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns(_slider);
        _mocker.GetMock<ISetVariableLogic>()
            .Setup(x => x.PropertyValueChanged(It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<InstanceSave?>(), It.IsAny<StateSave>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(GeneralResponse.SuccessfulResponse);
        _displayer = _mocker.CreateInstance<ElementSaveDisplayer>();
    }

    public override void Dispose()
    {
        try { Directory.Delete(_directory, true); } catch { }
        base.Dispose();
    }

    private static string FindThemesRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            current = Path.GetDirectoryName(current) ?? throw new InvalidOperationException("Themes folder not found");
        }
        throw new InvalidOperationException("Themes folder not found");
    }

    private void SetThroughBehaviorRow(string propertyName, double value)
    {
        List<VariableCategoryDescriptor> categories = _displayer.GetCategories(_component, _slider, _defaultState, null);
        VariableGridEntry entry = categories.Single(item => item.Name == "Behavior").Members.Single(item => item.Name == "Slider." + propertyName);
        entry.SetValue(_slider, value, VariablePropertyCommitType.Full).Succeeded.ShouldBeTrue();
    }

    private void AddReferenceLine(string line)
    {
        VariableListSave<string> list = new VariableListSave<string> { Name = "MinText.VariableReferences", Type = "string" };
        list.Value.Add(line);
        _defaultState.VariableLists.Add(list);
    }

    private List<string> Gum0009Messages()
    {
        string path = Path.Combine(_directory, "Components", "Custom", "PropertySlider.gucx");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _component.Save(path);
        return _checker.GetErrorsFor(_component, _project).Where(item => item.Code == "GUM0009").Select(item => item.Message).ToList();
    }

    [Fact]
    public void StoredShape_AfterSettingMinimumThroughBehaviorRow_IsSetsValueDouble()
    {
        SetThroughBehaviorRow("Minimum", 1.0);

        VariableSave stored = _defaultState.Variables.Single(item => item.Name == "Slider.Minimum");
        File.WriteAllText(Path.Combine(_directory, "shape.txt"), $"{stored.Name}|{stored.Type}|{stored.SetsValue}|{stored.Value}|{stored.Value?.GetType()}");
        stored.Type.ShouldBe("System.Double");
        stored.SetsValue.ShouldBeTrue();
        stored.Value.ShouldBe(1.0);
    }

    [Fact]
    public void GetErrorsFor_ShouldNotReportGum0009_WhenLineReadsMinimumSetThroughBehaviorRow()
    {
        SetThroughBehaviorRow("Minimum", 1.0);
        SetThroughBehaviorRow("Maximum", 10.0);
        AddReferenceLine("Text = Slider.Minimum");

        Gum0009Messages().ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_ShouldNotReportGum0009_WhenLineReadsBehaviorPropertyNobodySetButDeclaresADefault()
    {
        SetThroughBehaviorRow("Minimum", 1.0);
        AddReferenceLine("Text = Slider.Maximum");

        Gum0009Messages().ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_ShouldNotReportGum0009_WhenLineReadsBehaviorDefaultWithNothingSetOnOwner()
    {
        AddReferenceLine("Text = Slider.Minimum");

        Gum0009Messages().ShouldBeEmpty();
    }

    [Fact]
    public void GetErrorsFor_ShouldReportGum0009_WhenLineReadsNameNoBehaviorDeclares()
    {
        AddReferenceLine("Text = Slider.NotAFormsProperty");

        Gum0009Messages().ShouldHaveSingleItem();
    }

    // Editor side: the same lines applied the way the tool applies them on a reference edit.
    private object? ApplyAndReadMinTextText(string line)
    {
        Gum.Expressions.GumExpressionService.Initialize();
        AddReferenceLine(line);
        GumRuntime.ElementSaveExtensions.ApplyVariableReferences(_component, _defaultState, null, true);
        BehaviorToolOnlyReferencesApplier.Apply(_component, _defaultState);
        return _defaultState.Variables.FirstOrDefault(item => item.Name == "MinText.Text")?.Value;
    }

    [Fact]
    public void ApplyVariableReferences_WritesOwnerValue_WhenOwnerSetsMinimum()
    {
        SetThroughBehaviorRow("Minimum", 1.0);
        ApplyAndReadMinTextText("Text = Slider.Minimum").ShouldBe("1");
    }

    [Fact]
    public void ApplyVariableReferences_WritesBehaviorDefault_WhenOnlyBehaviorDefaultExists()
    {
        ApplyAndReadMinTextText("Text = Slider.Minimum").ShouldBe("0");
    }

    [Fact]
    public void ApplyVariableReferences_WritesBehaviorDefault_WhenMaximumIsUnset()
    {
        SetThroughBehaviorRow("Minimum", 1.0);
        ApplyAndReadMinTextText("Text = Slider.Maximum").ShouldBe("100");
    }
}
