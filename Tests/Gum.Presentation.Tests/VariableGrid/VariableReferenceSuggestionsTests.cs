using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// #8023: names offered while typing the right side of a VariableReferences line, on a project made
/// from the DarkPro Forms theme (Slider declares Minimum/Maximum as Forms properties).
/// </summary>
public class VariableReferenceSuggestionsTests : BaseTestClass
{
    private readonly string _directory;
    private readonly GumProjectSave _project;
    private readonly ComponentSave _component;
    private readonly StateSave _defaultState;
    private readonly VariableReferenceSuggestions _suggestions = new VariableReferenceSuggestions();
    private readonly HeadlessErrorChecker _checker = new HeadlessErrorChecker(new DefaultTypeResolver());

    public VariableReferenceSuggestionsTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "Gum8023_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        StandardElementsManager.Self.Initialize();
        string themeDirectory = new FormsThemeLocator(new[] { FindThemesRoot() }).FindThemeDirectory("DarkPro")!;
        string projectPath = Path.Combine(_directory, "Project.gumx");
        new FormsTemplateCreator().CreateFromTheme(projectPath, themeDirectory);
        _project = new ProjectLoader().Load(projectPath).Project!;
        ObjectFinder.Self.GumProjectSave = _project;

        _component = AddComponent("Custom/PropertySlider", "Container");
        _defaultState = _component.DefaultState!;
        AddInstance(_component, "Slider", "DarkPro/Slider");
        AddInstance(_component, "MinText", "Text");
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

    private ComponentSave AddComponent(string name, string baseType)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        _project.Components.Add(component);
        return component;
    }

    private static InstanceSave AddInstance(ElementSave element, string name, string baseType)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = baseType, ParentContainer = element };
        element.Instances.Add(instance);
        return instance;
    }

    private List<string> Texts(string line, string? owningInstance = "MinText", ElementSave? owner = null)
    {
        VariableReferenceSuggestionResult result = _suggestions.GetSuggestions(owner ?? _component, owningInstance, line, line.Length);
        return result.Items.Select(item => item.Text).ToList();
    }

    [Fact]
    public void GetSuggestions_ShouldOfferInstancesBeforeVariables_ForBarePrefix()
    {
        List<string> texts = Texts("Text = S");

        texts.ShouldContain("Slider");
        texts.ShouldContain("State");
        texts.IndexOf("Slider").ShouldBeLessThan(texts.IndexOf("State"));
        texts.ShouldNotContain("MinText");
    }

    [Fact]
    public void GetSuggestions_ShouldMatchCaseInsensitivelyAndReportReplaceRange()
    {
        VariableReferenceSuggestionResult result = _suggestions.GetSuggestions(_component, "MinText", "Text = slid", 11);

        result.Items.Select(item => item.Text).ShouldContain("Slider");
        result.ReplaceStart.ShouldBe(7);
        result.ReplaceLength.ShouldBe(4);
    }

    [Fact]
    public void GetSuggestions_ShouldMarkInstanceAsContinuation()
    {
        VariableReferenceSuggestion slider = _suggestions.GetSuggestions(_component, "MinText", "Text = Sli", 10).Items.Single(item => item.Text == "Slider");

        slider.IsContinuation.ShouldBeTrue();
        slider.InsertText.ShouldBe("Slider.");
    }

    [Fact]
    public void GetSuggestions_ShouldListFormsPropertiesOfInstance_AfterDot()
    {
        List<string> texts = Texts("Text = Slider.");

        texts.ShouldContain("Slider.Minimum");
        texts.ShouldContain("Slider.Maximum");
        texts.ShouldContain("Slider.Width");
        texts.ShouldAllBe(item => item.StartsWith("Slider."));
    }

    [Fact]
    public void GetSuggestions_ShouldFilterMembersByPrefix()
    {
        List<string> texts = Texts("Text = Slider.Max");

        texts.ShouldContain("Slider.Maximum");
        texts.ShouldNotContain("Slider.Minimum");
    }

    [Fact]
    public void GetSuggestions_ShouldOfferBehaviorPropertiesThroughInstanceOfDerivedComponent()
    {
        AddComponent("Custom/MySlider", "DarkPro/Slider");
        AddInstance(_component, "Mine", "Custom/MySlider");

        Texts("Text = Mine.M").ShouldContain("Mine.Minimum");
    }

    [Fact]
    public void GetSuggestions_ShouldOfferExposedVariableOfInstanceComponent()
    {
        _defaultState.Variables.Add(new VariableSave { Name = "MinText.Text", Type = "string", Value = "x", ExposedAsName = "CaptionText" });
        ComponentSave host = AddComponent("Custom/Host", "Container");
        AddInstance(host, "Prop", "Custom/PropertySlider");
        AddInstance(host, "Other", "Text");

        Texts("Text = Prop.Cap", "Other", host).ShouldContain("Prop.CaptionText");
    }

    [Fact]
    public void GetSuggestions_ShouldOfferCategoryStateSelector()
    {
        StateSaveCategory category = new StateSaveCategory { Name = "ModeCategory" };
        _component.Categories.Add(category);

        Texts("Text = ModeC").ShouldContain("ModeCategoryState");
    }

    [Fact]
    public void GetSuggestions_ShouldOfferCrossElementPaths_WhenQualifiedPrefixTyped()
    {
        Texts("Text = Components/Cust").ShouldContain("Components/Custom/PropertySlider");
        Texts("Text = Comp").ShouldContain("Components/Custom/PropertySlider");
        Texts("Text = Components/Custom/PropertySlider.Sl").ShouldContain("Components/Custom/PropertySlider.Slider");
        // GUM0009 does not accept a behavior default read through another element, so it is not offered there.
        List<string> crossElement = Texts("Text = Components/Custom/PropertySlider.Slider.Min");
        crossElement.ShouldContain("Components/Custom/PropertySlider.Slider.MinWidth");
        crossElement.ShouldNotContain("Components/Custom/PropertySlider.Slider.Minimum");
    }

    [Fact]
    public void GetSuggestions_ShouldOfferNothing_OnLeftSideCommentOrInsideString()
    {
        _suggestions.GetSuggestions(_component, "MinText", "Sli", 3).Items.ShouldBeEmpty();
        _suggestions.GetSuggestions(_component, "MinText", "Te = Slider.Minimum", 2).Items.ShouldBeEmpty();
        Texts("// Text = Sli").ShouldBeEmpty();
        Texts("Text = \"Sli").ShouldBeEmpty();
    }

    [Fact]
    public void GetSuggestions_ShouldOfferNothing_ForUnknownPrefix()
    {
        Texts("Text = Zzzz").ShouldBeEmpty();
        Texts("Text = Nope.").ShouldBeEmpty();
    }

    [Fact]
    public void GetSuggestions_ShouldOnlyOfferNamesGum0009Accepts()
    {
        _defaultState.Variables.Add(new VariableSave { Name = "MinText.Text", Type = "string", Value = "x", ExposedAsName = "CaptionText" });
        AddComponent("Custom/MySlider", "DarkPro/Slider");
        AddInstance(_component, "Mine", "Custom/MySlider");
        List<string> lines = new List<string>();
        string[] typedPrefixes = { "", "S", "Slider.", "MinText.", "Mine.", "Components/Custom/PropertySlider.", "Components/Custom/PropertySlider.Slider.", "Components/Custom/PropertySlider.MinText." };
        foreach (string typed in typedPrefixes)
        {
            string line = "Text = " + typed;
            foreach (VariableReferenceSuggestion item in _suggestions.GetSuggestions(_component, "MinText", line, line.Length).Items.Where(item => !item.IsContinuation))
            {
                lines.Add("Text = " + item.Text);
            }
        }
        lines.Count.ShouldBeGreaterThan(20);

        VariableListSave<string> list = new VariableListSave<string> { Name = "MinText.VariableReferences", Type = "string" };
        list.Value.AddRange(lines);
        _defaultState.VariableLists.Add(list);
        string path = Path.Combine(_directory, "Components", "Custom", "PropertySlider.gucx");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _component.Save(path);

        List<string> problems = _checker.GetErrorsFor(_component, _project).Where(item => item.Code == "GUM0009").Select(item => item.Message).ToList();
        problems.ShouldBeEmpty();
    }
}
