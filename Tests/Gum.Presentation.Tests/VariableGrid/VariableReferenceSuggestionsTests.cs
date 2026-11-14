using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices;
using MonoGameGum.Tests;
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
    public void GetSuggestions_ShouldOfferNothing_OnLeftSide()
    {
        _suggestions.GetSuggestions(_component, "MinText", "Sli", 3).Items.ShouldBeEmpty();
    }

    [Fact]
    public void GetSuggestions_ShouldOfferNothing_WhenCaretIsBeforeTheEquals()
    {
        _suggestions.GetSuggestions(_component, "MinText", "Te = Slider.Minimum", 2).Items.ShouldBeEmpty();
    }

    [Fact]
    public void GetSuggestions_ShouldOfferNothing_OnCommentedLine()
    {
        Texts("// Text = Sli").ShouldBeEmpty();
    }

    [Fact]
    public void GetSuggestions_ShouldOfferNothing_InsideStringLiteral()
    {
        Texts("Text = \"Sli").ShouldBeEmpty();
    }

    [Fact]
    public void GetSuggestions_ShouldOfferNestedInstances_AndTheirMembers()
    {
        ComponentSave inner = AddComponent("Custom/Inner", "Container");
        AddInstance(inner, "Sub", "Text");
        AddInstance(_component, "Inner", "Custom/Inner");

        VariableReferenceSuggestion sub = _suggestions.GetSuggestions(_component, "MinText", "Text = Inner.S", 14).Items.Single(item => item.Text == "Inner.Sub");
        sub.IsContinuation.ShouldBeTrue();
        Texts("Text = Inner.Sub.Te").ShouldContain("Inner.Sub.Text");
        Texts("Text = Components/Custom/PropertySlider.Inner.Sub.Te").ShouldContain("Components/Custom/PropertySlider.Inner.Sub.Text");
    }

    [Fact]
    public void GetSuggestions_ShouldOfferChildInstancesOfAFormsControl()
    {
        ElementSave slider = ObjectFinder.Self.GetElementSave("DarkPro/Slider")!;
        slider.Instances.ShouldNotBeEmpty();
        string child = slider.Instances[0].Name;

        Texts("Text = Slider.").ShouldContain("Slider." + child);
    }

    [Fact]
    public void GetSuggestions_ShouldOfferEveryNameGum0009Accepts_ForSampledNamesAndInstancePaths()
    {
        ComponentSave inner = AddComponent("Custom/Inner", "Container");
        AddInstance(inner, "Sub", "Text");
        AddInstance(_component, "Inner", "Custom/Inner");
        _defaultState.Variables.Add(new VariableSave { Name = "MinText.Text", Type = "string", Value = "x", ExposedAsName = "CaptionText" });
        ComponentSave host = AddComponent("Custom/Host", "Container");
        AddInstance(host, "Prop", "Custom/PropertySlider");
        AddInstance(host, "Other", "Text");

        // The universe of names is gathered from every saved variable and Forms property in the project, not from the suggestion class.
        HashSet<string> names = new HashSet<string>(StringComparer.Ordinal) { "State", "Index", "AbsoluteWidth", "CaptionText" };
        foreach (ElementSave element in _project.AllElements)
        {
            foreach (StateSave state in element.AllStates)
            {
                foreach (VariableSave variable in state.Variables.Where(item => !item.Name.Contains('.')))
                {
                    names.Add(variable.Name);
                }
            }
        }
        foreach (BehaviorSave behavior in _project.Behaviors)
        {
            foreach (VariableSave declaration in behavior.FormsProperties)
            {
                names.Add(declaration.Name);
            }
        }

        List<(ElementSave Owner, string Qualifier)> scopes = new List<(ElementSave, string)>
        {
            (_component, string.Empty), (_component, "Slider."), (_component, "Inner."), (_component, "Inner.Sub."),
            (host, "Prop."), (host, "Prop.Inner."), (host, "Prop.Inner.Sub."),
            (host, "Components/Custom/PropertySlider."), (host, "Components/Custom/PropertySlider.Inner.Sub."),
        };
        List<(ElementSave Owner, string Path)> accepted = new List<(ElementSave, string)>();
        foreach ((ElementSave owner, string qualifier) in scopes)
        {
            foreach (string name in names)
            {
                string path = qualifier + name;
                if (path.Length > 2 && HeadlessErrorChecker.FindUnresolvableReference(owner, null, "X = " + path) == null)
                {
                    accepted.Add((owner, path));
                }
            }
        }
        accepted.Count.ShouldBeGreaterThan(100);

        Random random = new Random(8023);
        int cases = SweepScale.Cases(300);
        List<string> missing = new List<string>();
        for (int i = 0; i < cases; i++)
        {
            (ElementSave owner, string path) = accepted[random.Next(accepted.Count)];
            string line = "X = " + path.Substring(0, path.Length - 1);
            VariableReferenceSuggestionResult result = _suggestions.GetSuggestions(owner, null, line, line.Length);
            if (!result.Items.Any(item => item.Text == path))
            {
                missing.Add($"{owner.Name}: {path}");
            }
        }
        missing.ShouldBeEmpty();
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
