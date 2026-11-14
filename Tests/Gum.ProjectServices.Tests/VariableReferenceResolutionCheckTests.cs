using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// GUM0009 against a real project (the DarkPro Forms theme): every right-side shape the editor
/// resolves must not be flagged, and every truly broken one must still be (#8020).
/// </summary>
public class VariableReferenceResolutionCheckTests : IDisposable
{
    private readonly string _directory;
    private readonly GumProjectSave _project;
    private readonly HeadlessErrorChecker _sut = new HeadlessErrorChecker(new DefaultTypeResolver());

    public VariableReferenceResolutionCheckTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "Gum8020R_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_directory);
        StandardElementsManager.Self.Initialize();
        string themeDirectory = new FormsThemeLocator(new[] { FindThemesRoot() }).FindThemeDirectory("DarkPro")!;
        string projectPath = Path.Combine(_directory, "Project.gumx");
        new FormsTemplateCreator().CreateFromTheme(projectPath, themeDirectory);
        _project = new ProjectLoader().Load(projectPath).Project!;
        ObjectFinder.Self.GumProjectSave = _project;
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, true); } catch { }
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

    private ComponentSave AddComponent(string name, string baseType = "Container")
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        _project.Components.Add(component);
        return component;
    }

    private ScreenSave AddScreen(string name)
    {
        ScreenSave screen = new ScreenSave { Name = name };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        _project.Screens.Add(screen);
        return screen;
    }

    private static InstanceSave AddInstance(ElementSave owner, string name, string baseType)
    {
        InstanceSave instance = new InstanceSave { Name = name, BaseType = baseType, ParentContainer = owner };
        owner.Instances.Add(instance);
        return instance;
    }

    private static void SetDouble(ElementSave element, string name, double value, string? exposedAs = null)
    {
        element.DefaultState!.Variables.Add(new VariableSave
        {
            Name = name, Type = "System.Double", Value = value, SetsValue = true, ExposedAsName = exposedAs
        });
    }

    private static void Expose(ElementSave element, string name, string exposedAs)
    {
        element.DefaultState!.Variables.Add(new VariableSave { Name = name, Type = "System.Double", ExposedAsName = exposedAs });
    }

    // An owner with a Slider-typed instance "Slider" and a Text instance "MinText".
    private ComponentSave AddSliderOwner(string name = "Custom/Owner", string sliderType = "DarkPro/Slider", string baseType = "Container")
    {
        ComponentSave owner = AddComponent(name, baseType);
        AddInstance(owner, "Slider", sliderType);
        AddInstance(owner, "MinText", "Text");
        return owner;
    }

    private List<string> Gum0009(ElementSave element, string line, string? sourceObject = "MinText")
    {
        VariableListSave<string> list = new VariableListSave<string>
        {
            Name = sourceObject == null ? "VariableReferences" : sourceObject + ".VariableReferences",
            Type = "string"
        };
        list.Value.Add(line);
        element.DefaultState!.VariableLists.Add(list);
        return _sut.GetErrorsFor(element, _project).Where(item => item.Code == "GUM0009").Select(item => item.Message).ToList();
    }

    private void ShouldResolve(ElementSave element, string line, string? sourceObject = "MinText") =>
        Gum0009(element, line, sourceObject).ShouldBeEmpty();

    private void ShouldReport(ElementSave element, string line, string? sourceObject = "MinText") =>
        Gum0009(element, line, sourceObject).ShouldHaveSingleItem();

    #region Dotted Instance.Var

    [Fact]
    public void Resolves_WhenValueIsSetOnOwner()
    {
        ComponentSave owner = AddSliderOwner();
        SetDouble(owner, "Slider.Minimum", 1);
        ShouldResolve(owner, "Text = Slider.Minimum");
    }

    [Fact]
    public void Resolves_WhenValueIsSetOnBaseOfOwner()
    {
        ComponentSave baseOwner = AddSliderOwner("Custom/BaseOwner");
        SetDouble(baseOwner, "Slider.Minimum", 1);
        ComponentSave owner = AddComponent("Custom/Owner", "Custom/BaseOwner");
        AddInstance(owner, "MinText2", "Text");
        ShouldResolve(owner, "Text = Slider.Minimum", "MinText2");
    }

    [Fact]
    public void Resolves_WhenValueIsSetOnInstanceType()
    {
        ComponentSave mySlider = AddComponent("Custom/MySlider", "DarkPro/Slider");
        SetDouble(mySlider, "Minimum", 2);
        ComponentSave owner = AddSliderOwner(sliderType: "Custom/MySlider");
        ShouldResolve(owner, "Text = Slider.Minimum");
    }

    [Fact]
    public void Resolves_WhenValueIsInheritedFromBaseOfInstanceType()
    {
        ComponentSave mySlider = AddComponent("Custom/MySlider", "DarkPro/Slider");
        SetDouble(mySlider, "Minimum", 2);
        AddComponent("Custom/MySliderDerived", "Custom/MySlider");
        ComponentSave owner = AddSliderOwner(sliderType: "Custom/MySliderDerived");
        ShouldResolve(owner, "Text = Slider.Minimum");
    }

    [Fact]
    public void Reports_WhenOnlyBehaviorDefaultExists_Slider()
    {
        ComponentSave owner = AddSliderOwner();
        ShouldReport(owner, "Text = Slider.Minimum");
    }

    [Fact]
    public void Reports_WhenOnlyBehaviorDefaultExists_ScrollBar()
    {
        ComponentSave owner = AddComponent("Custom/Owner");
        AddInstance(owner, "Bar", "DarkPro/ScrollBar");
        AddInstance(owner, "MinText", "Text");
        ShouldReport(owner, "Text = Bar.Maximum");
    }

    [Fact]
    public void Resolves_WhenReadingCheckBoxIsCheckedStoredOnTheControl()
    {
        ComponentSave owner = AddComponent("Custom/Owner");
        AddInstance(owner, "Box", "DarkPro/CheckBox");
        AddInstance(owner, "MinText", "Text");
        ShouldResolve(owner, "Visible = Box.IsChecked");
    }

    [Fact]
    public void Reports_WhenBehaviorIsDeclaredOnBaseOfInstanceType()
    {
        AddComponent("Custom/MySlider", "DarkPro/Slider");
        ComponentSave owner = AddSliderOwner(sliderType: "Custom/MySlider");
        ShouldReport(owner, "Text = Slider.Minimum");
    }

    [Fact]
    public void Reports_WhenInstanceTypeHasNoSuchVariableOrBehaviorProperty()
    {
        ComponentSave owner = AddSliderOwner();
        ShouldReport(owner, "Text = Slider.NoSuchProperty");
    }

    [Fact]
    public void Reports_WhenInstanceDoesNotExist()
    {
        ComponentSave owner = AddSliderOwner();
        ShouldReport(owner, "Text = Ghost.Minimum");
    }

    [Fact]
    public void Reports_WhenInstanceTypeHasNoBehaviorDeclaringTheProperty()
    {
        ComponentSave owner = AddComponent("Custom/Owner");
        AddInstance(owner, "Panel", "Container");
        AddInstance(owner, "MinText", "Text");
        ShouldReport(owner, "Text = Panel.Minimum");
    }

    #endregion

    #region Exposed variables

    [Fact]
    public void Resolves_WhenExposedVariableIsSetOnOwner()
    {
        ComponentSave owner = AddSliderOwner();
        SetDouble(owner, "Slider.Minimum", 1, exposedAs: "SliderMin");
        ShouldResolve(owner, "Text = SliderMin");
    }

    [Fact]
    public void Resolves_WhenExposedVariableIsOnBaseOfOwner()
    {
        ComponentSave baseOwner = AddSliderOwner("Custom/BaseOwner");
        SetDouble(baseOwner, "Slider.Minimum", 1, exposedAs: "SliderMin");
        ComponentSave owner = AddComponent("Custom/Owner", "Custom/BaseOwner");
        AddInstance(owner, "MinText2", "Text");
        ShouldResolve(owner, "Text = SliderMin", "MinText2");
    }

    [Fact]
    // Reports because the exposure holds no Value (Expose() stores none), not because of SetsValue:
    // the exposed-behavior-property rule needs a stored Value.
    public void Reports_WhenExposedBehaviorPropertyHasNoStoredValue()
    {
        ComponentSave owner = AddSliderOwner();
        Expose(owner, "Slider.Minimum", "SliderMin");
        ShouldReport(owner, "Text = SliderMin");
    }

    [Fact]
    public void Reports_WhenExposedVariableNamesPropertyTheInstanceTypeLacks()
    {
        ComponentSave owner = AddSliderOwner();
        Expose(owner, "Slider.NoSuchProperty", "Broken");
        ShouldReport(owner, "Text = Broken");
    }

    #endregion

    #region @ owner prefix

    [Fact]
    public void Resolves_WhenOwnerPrefixReadsValueSetOnOwner()
    {
        ComponentSave owner = AddSliderOwner();
        SetDouble(owner, "Slider.Minimum", 1);
        ShouldResolve(owner, "Value = @Minimum", "Slider");
    }

    [Fact]
    public void Reports_WhenOwnerPrefixReadsBehaviorDefault()
    {
        ComponentSave owner = AddSliderOwner();
        ShouldReport(owner, "Value = @Maximum", "Slider");
    }

    [Fact]
    public void Reports_WhenOwnerPrefixReadsNothingTheInstanceHas()
    {
        ComponentSave owner = AddSliderOwner();
        ShouldReport(owner, "Value = @NoSuchProperty", "Slider");
    }

    [Fact]
    public void Resolves_WhenOwnerPrefixIsOnElementRow()
    {
        ComponentSave owner = AddSliderOwner();
        ShouldResolve(owner, "Width = @Height", sourceObject: null);
    }

    [Fact]
    public void Reports_WhenOwnerPrefixOnElementRowReadsMissingVariable()
    {
        ComponentSave owner = AddSliderOwner();
        ShouldReport(owner, "Width = @NoSuchProperty", sourceObject: null);
    }

    #endregion

    #region Qualified Components/X.Instance.Var

    [Fact]
    public void Resolves_WhenQualifiedValueIsStoredOnReferencedElement()
    {
        ComponentSave other = AddSliderOwner("Custom/Other");
        SetDouble(other, "Slider.Minimum", 1);
        ComponentSave owner = AddComponent("Custom/Owner");
        AddInstance(owner, "MinText", "Text");
        ShouldResolve(owner, "Text = Components/Custom/Other.Slider.Minimum");
    }

    [Fact]
    public void Reports_WhenQualifiedValueIsBehaviorDefaultOfReferencedElementsInstance()
    {
        AddSliderOwner("Custom/Other");
        ComponentSave owner = AddComponent("Custom/Owner");
        AddInstance(owner, "MinText", "Text");
        ShouldReport(owner, "Text = Components/Custom/Other.Slider.Minimum");
    }

    [Fact]
    public void Reports_WhenQualifiedPropertyDoesNotExistOnReferencedElementsInstance()
    {
        AddSliderOwner("Custom/Other");
        ComponentSave owner = AddComponent("Custom/Owner");
        AddInstance(owner, "MinText", "Text");
        ShouldReport(owner, "Text = Components/Custom/Other.Slider.NoSuchProperty");
    }

    #endregion

    #region Category states, composites

    [Fact]
    public void Resolves_WhenReadingOwnCategoryState()
    {
        ComponentSave owner = AddSliderOwner();
        owner.Categories.Add(new StateSaveCategory { Name = "Theme" });
        ShouldResolve(owner, "Text = ThemeState");
    }

    [Fact]
    public void Resolves_WhenReadingCategoryStateOfBaseOfOwner()
    {
        ComponentSave baseOwner = AddComponent("Custom/BaseOwner");
        baseOwner.Categories.Add(new StateSaveCategory { Name = "Theme" });
        ComponentSave owner = AddComponent("Custom/Owner", "Custom/BaseOwner");
        AddInstance(owner, "MinText", "Text");
        ShouldResolve(owner, "Text = ThemeState");
    }

    [Fact]
    public void Reports_WhenReadingCategoryStateNoElementHas()
    {
        ComponentSave owner = AddSliderOwner();
        ShouldReport(owner, "Text = ThemeState");
    }

    [Fact]
    public void Resolves_WhenReadingCompositeColorOfInstance()
    {
        ComponentSave owner = AddComponent("Custom/Owner");
        AddInstance(owner, "Background", "Rectangle");
        AddInstance(owner, "Foreground", "Rectangle");
        ShouldResolve(owner, "Color = Background.FillColor", "Foreground");
    }

    [Fact]
    public void Reports_WhenReadingCompositeColorTheInstanceLacks()
    {
        ComponentSave owner = AddComponent("Custom/Owner");
        AddInstance(owner, "Background", "Rectangle");
        AddInstance(owner, "Foreground", "Rectangle");
        ShouldReport(owner, "Color = Background.NoSuchColor", "Foreground");
    }

    #endregion

    #region Nested instances

    [Fact]
    public void Resolves_WhenNestedInstanceValueIsSetOnInnerComponent()
    {
        ComponentSave inner = AddSliderOwner("Custom/Inner");
        SetDouble(inner, "Slider.Minimum", 1);
        ComponentSave owner = AddComponent("Custom/Owner");
        AddInstance(owner, "Panel", "Custom/Inner");
        AddInstance(owner, "MinText", "Text");
        ShouldResolve(owner, "Text = Panel.Slider.Minimum");
    }

    [Fact]
    public void Reports_WhenNestedInstanceValueIsBehaviorDefault()
    {
        AddSliderOwner("Custom/Inner");
        ComponentSave owner = AddComponent("Custom/Owner");
        AddInstance(owner, "Panel", "Custom/Inner");
        AddInstance(owner, "MinText", "Text");
        ShouldReport(owner, "Text = Panel.Slider.Minimum");
    }

    [Fact]
    public void Resolves_WhenNestedInstanceValueIsSetByOwnerThroughDottedPath()
    {
        AddSliderOwner("Custom/Inner");
        ComponentSave owner = AddComponent("Custom/Owner");
        AddInstance(owner, "Panel", "Custom/Inner");
        AddInstance(owner, "MinText", "Text");
        SetDouble(owner, "Panel.Slider.Minimum", 4);
        ShouldResolve(owner, "Text = Panel.Slider.Minimum");
    }

    [Fact]
    public void Reports_WhenNestedInstanceHasNoSuchProperty()
    {
        AddSliderOwner("Custom/Inner");
        ComponentSave owner = AddComponent("Custom/Owner");
        AddInstance(owner, "Panel", "Custom/Inner");
        AddInstance(owner, "MinText", "Text");
        ShouldReport(owner, "Text = Panel.Slider.NoSuchProperty");
    }

    #endregion

    #region Screen owner

    [Fact]
    public void Resolves_WhenScreenSetsValueOnItsOwnInstance()
    {
        ScreenSave screen = AddScreen("Custom/MainScreen");
        AddInstance(screen, "Slider", "DarkPro/Slider");
        AddInstance(screen, "MinText", "Text");
        SetDouble(screen, "Slider.Minimum", 1);
        ShouldResolve(screen, "Text = Slider.Minimum");
    }

    [Fact]
    public void Reports_WhenScreenReadsBehaviorDefaultOfItsInstance()
    {
        ScreenSave screen = AddScreen("Custom/MainScreen");
        AddInstance(screen, "Slider", "DarkPro/Slider");
        AddInstance(screen, "MinText", "Text");
        ShouldReport(screen, "Text = Slider.Minimum");
    }

    [Fact]
    public void Reports_WhenScreenReadsMissingInstanceProperty()
    {
        ScreenSave screen = AddScreen("Custom/MainScreen");
        AddInstance(screen, "Slider", "DarkPro/Slider");
        AddInstance(screen, "MinText", "Text");
        ShouldReport(screen, "Text = Slider.NoSuchProperty");
    }

    #endregion

    #region Functions and ternaries

    [Fact]
    public void Reports_WhenFunctionMixesBehaviorValueWithLiteral()
    {
        ComponentSave owner = AddSliderOwner();
        ShouldReport(owner, "Width = Max(Slider.Minimum, 3)");
    }

    [Fact]
    public void Reports_WhenTernaryMixesBehaviorValueWithLiteral()
    {
        ComponentSave owner = AddSliderOwner();
        ShouldReport(owner, "Width = Slider.Minimum > 2 ? 1 : 0");
    }

    [Fact]
    public void Resolves_WhenFunctionMixesOwnerSetValueWithLiteral()
    {
        ComponentSave owner = AddSliderOwner();
        SetDouble(owner, "Slider.Minimum", 1);
        ShouldResolve(owner, "Width = Max(Slider.Minimum, 3)");
    }

    [Fact]
    public void Reports_WhenFunctionArgumentIsMissingBehaviorProperty()
    {
        ComponentSave owner = AddSliderOwner();
        ShouldReport(owner, "Width = Max(Slider.NoSuchProperty, 3)");
    }

    [Fact]
    public void Reports_WhenTernaryBranchIsMissingBehaviorProperty()
    {
        ComponentSave owner = AddSliderOwner();
        ShouldReport(owner, "Width = Slider.Minimum > 2 ? Slider.NoSuchProperty : 0");
    }

    #endregion
}
