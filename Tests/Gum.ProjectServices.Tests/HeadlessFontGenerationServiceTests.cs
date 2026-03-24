using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices.FontGeneration;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Gum.ProjectServices.Tests;

public class HeadlessFontGenerationServiceTests : BaseTestClass
{
    private readonly HeadlessFontGenerationService _sut;

    public HeadlessFontGenerationServiceTests()
    {
        _sut = new HeadlessFontGenerationService();

        // Add font defaults to the Text standard element's Default state so
        // inheritance tests can resolve Font/FontSize via GetValueRecursive.
        StandardElementSave textStandard = Project.StandardElements.First(e => e.Name == "Text");
        StateSave textDefault = textStandard.DefaultState;
        textDefault.Variables.Add(new VariableSave { SetsValue = true, Name = "Font", Value = "Arial" });
        textDefault.Variables.Add(new VariableSave { SetsValue = true, Name = "FontSize", Value = 18 });

        ObjectFinder.Self.GumProjectSave = Project;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a state directly under an element (sets ParentContainer), adds it to the
    /// element's States list, and returns it.
    /// </summary>
    private static StateSave AddState(ElementSave element, string name = "Default")
    {
        StateSave state = new StateSave { Name = name };
        state.ParentContainer = element;
        element.States.Add(state);
        return state;
    }

    /// <summary>
    /// Creates a state inside a named category on an element and returns it.
    /// </summary>
    private static StateSave AddCategoryState(ElementSave element, string categoryName, string stateName)
    {
        StateSaveCategory category = element.Categories.FirstOrDefault(c => c.Name == categoryName)
            ?? new StateSaveCategory { Name = categoryName };

        if (!element.Categories.Contains(category))
        {
            element.Categories.Add(category);
        }

        StateSave state = new StateSave { Name = stateName };
        state.ParentContainer = element;
        category.States.Add(state);
        return state;
    }

    /// <summary>
    /// Adds a Text instance named <paramref name="instanceName"/> to <paramref name="element"/>.
    /// </summary>
    private static InstanceSave AddTextInstance(ElementSave element, string instanceName = "Label")
    {
        InstanceSave instance = new InstanceSave { Name = instanceName, BaseType = "Text" };
        element.Instances.Add(instance);
        return instance;
    }

    private static void SetVar(StateSave state, string name, object value)
    {
        state.Variables.Add(new VariableSave { SetsValue = true, Name = name, Value = value });
    }

    // -------------------------------------------------------------------------
    // TryGetBmfcSaveFor
    // -------------------------------------------------------------------------

    #region TryGetBmfcSaveFor

    [Fact]
    public void TryGetBmfcSaveFor_ShouldReturnNull_WhenFontNotSet()
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave state = AddState(screen);
        SetVar(state, "FontSize", 18);

        BmfcSave? result = _sut.TryGetBmfcSaveFor(null, state, fontRanges: "", spacingHorizontal: 1, spacingVertical: 1, forcedValues: null);

        result.ShouldBeNull();
    }

    [Fact]
    public void TryGetBmfcSaveFor_ShouldReturnNull_WhenFontSizeNotSet()
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave state = AddState(screen);
        SetVar(state, "Font", "Arial");

        BmfcSave? result = _sut.TryGetBmfcSaveFor(null, state, fontRanges: "", spacingHorizontal: 1, spacingVertical: 1, forcedValues: null);

        result.ShouldBeNull();
    }

    [Fact]
    public void TryGetBmfcSaveFor_ShouldReturnBmfcSave_WhenFontAndSizeAreSet()
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave state = AddState(screen);
        SetVar(state, "Font", "Arial");
        SetVar(state, "FontSize", 24);

        BmfcSave? result = _sut.TryGetBmfcSaveFor(null, state, fontRanges: "32-126", spacingHorizontal: 2, spacingVertical: 3, forcedValues: null);

        result.ShouldNotBeNull();
        result.FontName.ShouldBe("Arial");
        result.FontSize.ShouldBe(24);
        result.Ranges.ShouldBe("32-126");
        result.SpacingHorizontal.ShouldBe(2);
        result.SpacingVertical.ShouldBe(3);
    }

    [Fact]
    public void TryGetBmfcSaveFor_ShouldApplyDefaults_WhenOptionalPropertiesNotSet()
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave state = AddState(screen);
        SetVar(state, "Font", "Comic Sans MS");
        SetVar(state, "FontSize", 12);

        BmfcSave? result = _sut.TryGetBmfcSaveFor(null, state, fontRanges: "", spacingHorizontal: 1, spacingVertical: 1, forcedValues: null);

        result.ShouldNotBeNull();
        result.OutlineThickness.ShouldBe(0);
        result.UseSmoothing.ShouldBe(true);
        result.IsItalic.ShouldBe(false);
        result.IsBold.ShouldBe(false);
    }

    [Fact]
    public void TryGetBmfcSaveFor_ShouldReadOutlineItalicBold_WhenSetInState()
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave state = AddState(screen);
        SetVar(state, "Font", "Times New Roman");
        SetVar(state, "FontSize", 32);
        SetVar(state, "OutlineThickness", 2);
        SetVar(state, "IsItalic", true);
        SetVar(state, "IsBold", true);
        SetVar(state, "UseFontSmoothing", false);

        BmfcSave? result = _sut.TryGetBmfcSaveFor(null, state, fontRanges: "", spacingHorizontal: 1, spacingVertical: 1, forcedValues: null);

        result.ShouldNotBeNull();
        result.OutlineThickness.ShouldBe(2);
        result.IsItalic.ShouldBe(true);
        result.IsBold.ShouldBe(true);
        result.UseSmoothing.ShouldBe(false);
    }

    [Fact]
    public void TryGetBmfcSaveFor_ShouldUsePrefixedVariables_WhenInstanceProvided()
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        InstanceSave instance = AddTextInstance(screen, "MyLabel");
        StateSave state = AddState(screen);
        SetVar(state, "MyLabel.Font", "Verdana");
        SetVar(state, "MyLabel.FontSize", 16);

        BmfcSave? result = _sut.TryGetBmfcSaveFor(instance, state, fontRanges: "", spacingHorizontal: 1, spacingVertical: 1, forcedValues: null);

        result.ShouldNotBeNull();
        result.FontName.ShouldBe("Verdana");
        result.FontSize.ShouldBe(16);
    }

    [Fact]
    public void TryGetBmfcSaveFor_ForcedValues_ShouldOverrideStateValues()
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave state = AddState(screen);
        SetVar(state, "Font", "Arial");
        SetVar(state, "FontSize", 12);

        StateSave forced = new StateSave();
        SetVar(forced, "Font", "Impact");
        SetVar(forced, "FontSize", 48);

        BmfcSave? result = _sut.TryGetBmfcSaveFor(null, state, fontRanges: "", spacingHorizontal: 1, spacingVertical: 1, forcedValues: forced);

        result.ShouldNotBeNull();
        result.FontName.ShouldBe("Impact");
        result.FontSize.ShouldBe(48);
    }

    #endregion

    // -------------------------------------------------------------------------
    // CollectRequiredFonts — basic collection and deduplication
    // -------------------------------------------------------------------------

    #region CollectRequiredFonts — basic

    [Fact]
    public void CollectRequiredFonts_ShouldReturnEmpty_WhenProjectHasNoElements()
    {
        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, Enumerable.Empty<ElementSave>());

        result.ShouldBeEmpty();
    }

    [Fact]
    public void CollectRequiredFonts_ShouldReturnEmpty_WhenElementHasNoFontVariables()
    {
        ScreenSave screen = new ScreenSave { Name = "EmptyScreen" };
        StateSave state = AddState(screen);
        SetVar(state, "Width", 800);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { screen });

        result.ShouldBeEmpty();
    }

    [Fact]
    public void CollectRequiredFonts_ShouldReturn1Font_WhenDefaultStateHasFontAndSize()
    {
        ScreenSave screen = new ScreenSave { Name = "MainMenu" };
        StateSave state = AddState(screen);
        SetVar(state, "Font", "Arial");
        SetVar(state, "FontSize", 24);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { screen });

        result.ShouldHaveSingleItem();
        BmfcSave font = result.Values.Single();
        font.FontName.ShouldBe("Arial");
        font.FontSize.ShouldBe(24);
    }

    [Fact]
    public void CollectRequiredFonts_ShouldDeduplicate_WhenTwoStatesSpecifySameFontAndSize()
    {
        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave defaultState = AddState(screen, "Default");
        SetVar(defaultState, "Font", "Arial");
        SetVar(defaultState, "FontSize", 18);

        StateSave otherState = AddCategoryState(screen, "SizeCategory", "Large");
        SetVar(otherState, "Font", "Arial");
        SetVar(otherState, "FontSize", 18);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { screen });

        result.ShouldHaveSingleItem();
    }

    [Fact]
    public void CollectRequiredFonts_ShouldReturn2Fonts_WhenTwoStatesHaveDifferentSizes()
    {
        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave defaultState = AddState(screen, "Default");
        SetVar(defaultState, "Font", "Arial");
        SetVar(defaultState, "FontSize", 18);

        StateSave largeState = AddCategoryState(screen, "SizeCategory", "Large");
        SetVar(largeState, "Font", "Arial");
        SetVar(largeState, "FontSize", 32);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { screen });

        result.Count.ShouldBe(2);
        result.Values.ShouldContain(f => f.FontSize == 18);
        result.Values.ShouldContain(f => f.FontSize == 32);
    }

    [Fact]
    public void CollectRequiredFonts_ShouldReturn2Fonts_WhenOutlineThicknessDiffers()
    {
        ScreenSave screen = new ScreenSave { Name = "Screen" };
        StateSave defaultState = AddState(screen, "Default");
        SetVar(defaultState, "Font", "Arial");
        SetVar(defaultState, "FontSize", 24);
        SetVar(defaultState, "OutlineThickness", 0);

        StateSave outlineState = AddCategoryState(screen, "StyleCategory", "Outlined");
        SetVar(outlineState, "Font", "Arial");
        SetVar(outlineState, "FontSize", 24);
        SetVar(outlineState, "OutlineThickness", 2);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { screen });

        result.Count.ShouldBe(2);
        result.Values.ShouldContain(f => f.OutlineThickness == 0);
        result.Values.ShouldContain(f => f.OutlineThickness == 2);
    }

    [Fact]
    public void CollectRequiredFonts_ShouldAccumulateFonts_AcrossMultipleElements()
    {
        ScreenSave screen1 = new ScreenSave { Name = "Screen1" };
        StateSave state1 = AddState(screen1);
        SetVar(state1, "Font", "Arial");
        SetVar(state1, "FontSize", 18);

        ScreenSave screen2 = new ScreenSave { Name = "Screen2" };
        StateSave state2 = AddState(screen2);
        SetVar(state2, "Font", "Verdana");
        SetVar(state2, "FontSize", 24);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { screen1, screen2 });

        result.Count.ShouldBe(2);
        result.Values.ShouldContain(f => f.FontName == "Arial" && f.FontSize == 18);
        result.Values.ShouldContain(f => f.FontName == "Verdana" && f.FontSize == 24);
    }

    #endregion

    // -------------------------------------------------------------------------
    // CollectRequiredFonts — instance variables
    // -------------------------------------------------------------------------

    #region CollectRequiredFonts — instance variables

    [Fact]
    public void CollectRequiredFonts_ShouldCollectFont_WhenInstanceHasFontAndSizeInComponentState()
    {
        ComponentSave component = new ComponentSave { Name = "HudComponent", BaseType = "Container" };
        StateSave state = AddState(component);
        AddTextInstance(component, "ScoreLabel");

        // Font and FontSize set directly on the instance within this component's state.
        SetVar(state, "ScoreLabel.Font", "Impact");
        SetVar(state, "ScoreLabel.FontSize", 36);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { component });

        result.ShouldHaveSingleItem();
        BmfcSave font = result.Values.Single();
        font.FontName.ShouldBe("Impact");
        font.FontSize.ShouldBe(36);
    }

    [Fact]
    public void CollectRequiredFonts_ShouldCollectBothElementAndInstanceFonts_WhenBothAreSet()
    {
        // A Text-typed element with a font on its own default state...
        ComponentSave component = new ComponentSave { Name = "Panel", BaseType = "Container" };
        StateSave state = AddState(component);
        SetVar(state, "Font", "Arial");
        SetVar(state, "FontSize", 18);

        // ...plus a child Text instance with a different font.
        AddTextInstance(component, "Title");
        SetVar(state, "Title.Font", "Impact");
        SetVar(state, "Title.FontSize", 48);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { component });

        result.Count.ShouldBe(2);
        result.Values.ShouldContain(f => f.FontName == "Arial" && f.FontSize == 18);
        result.Values.ShouldContain(f => f.FontName == "Impact" && f.FontSize == 48);
    }

    [Fact]
    public void CollectRequiredFonts_ShouldReturnEmpty_WhenInstanceHasSizeButNoFont()
    {
        ComponentSave component = new ComponentSave { Name = "Panel", BaseType = "Container" };
        StateSave state = AddState(component);
        // Use a Sprite instance — Sprite has no Font variable in its standard element,
        // so FontSize alone can never produce a font entry.
        component.Instances.Add(new InstanceSave { Name = "Label", BaseType = "Sprite" });

        // Only FontSize — no Font anywhere in the hierarchy — so nothing should be collected.
        SetVar(state, "Label.FontSize", 20);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { component });

        result.ShouldBeEmpty();
    }

    [Fact]
    public void CollectRequiredFonts_ShouldReturn2Fonts_WhenInstanceHasDifferentSizePerCategoryState()
    {
        ComponentSave component = new ComponentSave { Name = "Panel", BaseType = "Container" };
        StateSave defaultState = AddState(component, "Default");
        StateSave largeState = AddCategoryState(component, "SizeCategory", "Large");
        AddTextInstance(component, "Label");

        SetVar(defaultState, "Label.Font", "Arial");
        SetVar(defaultState, "Label.FontSize", 18);

        SetVar(largeState, "Label.Font", "Arial");
        SetVar(largeState, "Label.FontSize", 32);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { component });

        result.Count.ShouldBe(2);
        result.Values.ShouldContain(f => f.FontSize == 18);
        result.Values.ShouldContain(f => f.FontSize == 32);
    }

    #endregion

    // -------------------------------------------------------------------------
    // CollectRequiredFonts — inheritance from Text standard element
    // -------------------------------------------------------------------------

    #region CollectRequiredFonts — inheritance

    [Fact]
    public void CollectRequiredFonts_ShouldInheritFont_WhenInstanceFontNotSetInComponentState()
    {
        // The component's state only sets FontSize on the instance.
        // Font should fall through to the Text standard element's default ("Arial").
        ComponentSave component = new ComponentSave { Name = "HudPanel", BaseType = "Container" };
        StateSave state = AddState(component);
        AddTextInstance(component, "Label");

        SetVar(state, "Label.FontSize", 24);
        // Note: "Label.Font" is intentionally not set — should resolve to "Arial" from Text standard.

        Project.Components.Add(component);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { component });

        result.ShouldHaveSingleItem();
        BmfcSave font = result.Values.Single();
        font.FontName.ShouldBe("Arial");
        font.FontSize.ShouldBe(24);
    }

    [Fact]
    public void CollectRequiredFonts_ShouldInheritFontSize_WhenInstanceFontSizeNotSetInComponentState()
    {
        // The component's state only sets Font on the instance.
        // FontSize should fall through to the Text standard element's default (18).
        ComponentSave component = new ComponentSave { Name = "HudPanel", BaseType = "Container" };
        StateSave state = AddState(component);
        AddTextInstance(component, "Label");

        SetVar(state, "Label.Font", "Verdana");
        // Note: "Label.FontSize" is intentionally not set — should resolve to 18 from Text standard.

        Project.Components.Add(component);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { component });

        result.ShouldHaveSingleItem();
        BmfcSave font = result.Values.Single();
        font.FontName.ShouldBe("Verdana");
        font.FontSize.ShouldBe(18);
    }

    [Fact]
    public void CollectRequiredFonts_ShouldReturnEmpty_WhenInstanceHasNoOverridesAndTextStandardHasNoFont()
    {
        // Replace the Text standard with one that has NO font variables.
        Project.StandardElements.Clear();
        StandardElementSave emptyText = new StandardElementSave { Name = "Text" };
        StateSave emptyDefault = new StateSave { Name = "Default" };
        emptyDefault.ParentContainer = emptyText;
        emptyText.States.Add(emptyDefault);
        Project.StandardElements.Add(emptyText);
        ObjectFinder.Self.GumProjectSave = Project;

        ComponentSave component = new ComponentSave { Name = "Panel", BaseType = "Container" };
        AddState(component);
        AddTextInstance(component, "Label");
        Project.Components.Add(component);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { component });

        result.ShouldBeEmpty();
    }

    [Fact]
    public void CollectRequiredFonts_ShouldProduceDifferentFonts_WhenCategoryStateOverridesFontSizeWithInheritedFont()
    {
        // Default state: instance FontSize=18 (Font inherited as "Arial" from Text standard).
        // Category state: instance FontSize=36 (Font still inherited as "Arial").
        // Both fonts should be collected — Arial@18 and Arial@36.
        ComponentSave component = new ComponentSave { Name = "Panel", BaseType = "Container" };
        StateSave defaultState = AddState(component, "Default");
        StateSave largeState = AddCategoryState(component, "SizeCategory", "Large");
        AddTextInstance(component, "Label");

        SetVar(defaultState, "Label.FontSize", 18);
        SetVar(largeState, "Label.FontSize", 36);

        Project.Components.Add(component);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { component });

        result.Count.ShouldBe(2);
        result.Values.ShouldContain(f => f.FontName == "Arial" && f.FontSize == 18);
        result.Values.ShouldContain(f => f.FontName == "Arial" && f.FontSize == 36);
    }

    #endregion

    // -------------------------------------------------------------------------
    // CollectRequiredFonts — component hierarchy (nested Text instances)
    // -------------------------------------------------------------------------

    #region CollectRequiredFonts — component hierarchy

    [Fact]
    public void CollectRequiredFonts_ShouldCollectFontsFromNestedTextInComponent_WhenScreenHasComponentInstance()
    {
        // Component "MyButton" has a Text instance "Label" with Font="Courier", FontSize=18.
        // Screen has an instance of MyButton.
        // CollectRequiredFonts on the screen should find the Courier@18 font.
        ComponentSave component = new ComponentSave { Name = "MyButton", BaseType = "Container" };
        StateSave componentState = AddState(component, "Default");
        AddTextInstance(component, "Label");
        SetVar(componentState, "Label.Font", "Courier");
        SetVar(componentState, "Label.FontSize", 18);
        Project.Components.Add(component);

        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        StateSave screenState = AddState(screen, "Default");
        screen.Instances.Add(new InstanceSave { Name = "ButtonInstance", BaseType = "MyButton" });
        Project.Screens.Add(screen);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { screen });

        result.Values.ShouldContain(f => f.FontName == "Courier" && f.FontSize == 18);
    }

    [Fact]
    public void CollectRequiredFonts_ShouldResolveExposedFontSize_WhenScreenOverridesIt()
    {
        // Component "MyButton" has Text "Label" with Font="Courier", FontSize=18.
        // Component exposes Label.FontSize as "FontSize".
        // Screen instance overrides FontSize to 36.
        // Should collect Courier@36 (not just Courier@18).
        ComponentSave component = new ComponentSave { Name = "MyButton", BaseType = "Container" };
        StateSave componentState = AddState(component, "Default");
        AddTextInstance(component, "Label");
        SetVar(componentState, "Label.Font", "Courier");
        SetVar(componentState, "Label.FontSize", 18);
        componentState.Variables.Add(new VariableSave
        {
            SetsValue = true,
            Name = "Label.FontSize",
            Value = 18,
            ExposedAsName = "FontSize"
        });
        Project.Components.Add(component);

        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        StateSave screenState = AddState(screen, "Default");
        screen.Instances.Add(new InstanceSave { Name = "ButtonInstance", BaseType = "MyButton" });
        SetVar(screenState, "ButtonInstance.FontSize", 36);
        Project.Screens.Add(screen);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { screen });

        result.Values.ShouldContain(f => f.FontName == "Courier" && f.FontSize == 36);
    }

    [Fact]
    public void CollectRequiredFonts_ShouldCollectAllSizes_WhenMultipleInstancesOverrideFontSize()
    {
        // Component "MyButton" has Text "Label" with Font="Courier", FontSize=18, exposes FontSize.
        // Screen has 3 instances with different FontSize overrides: 12, 24, 36.
        // Should collect Courier@12, Courier@24, and Courier@36.
        ComponentSave component = new ComponentSave { Name = "MyButton", BaseType = "Container" };
        StateSave componentState = AddState(component, "Default");
        AddTextInstance(component, "Label");
        SetVar(componentState, "Label.Font", "Courier");
        SetVar(componentState, "Label.FontSize", 18);
        componentState.Variables.Add(new VariableSave
        {
            SetsValue = true,
            Name = "Label.FontSize",
            Value = 18,
            ExposedAsName = "FontSize"
        });
        Project.Components.Add(component);

        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        StateSave screenState = AddState(screen, "Default");
        screen.Instances.Add(new InstanceSave { Name = "Button1", BaseType = "MyButton" });
        screen.Instances.Add(new InstanceSave { Name = "Button2", BaseType = "MyButton" });
        screen.Instances.Add(new InstanceSave { Name = "Button3", BaseType = "MyButton" });
        SetVar(screenState, "Button1.FontSize", 12);
        SetVar(screenState, "Button2.FontSize", 24);
        SetVar(screenState, "Button3.FontSize", 36);
        Project.Screens.Add(screen);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { screen });

        result.Values.ShouldContain(f => f.FontName == "Courier" && f.FontSize == 12);
        result.Values.ShouldContain(f => f.FontName == "Courier" && f.FontSize == 24);
        result.Values.ShouldContain(f => f.FontName == "Courier" && f.FontSize == 36);
    }

    [Fact]
    public void CollectRequiredFonts_ShouldResolveFontFromComponent_WhenScreenDoesNotOverrideFont()
    {
        // Component "MyButton" has Text "Label" with Font="Verdana", FontSize=18, exposes FontSize.
        // Screen overrides FontSize=24 but NOT Font.
        // Should collect Verdana@24 (Font resolved from component, FontSize from screen override).
        ComponentSave component = new ComponentSave { Name = "MyButton", BaseType = "Container" };
        StateSave componentState = AddState(component, "Default");
        AddTextInstance(component, "Label");
        SetVar(componentState, "Label.Font", "Verdana");
        SetVar(componentState, "Label.FontSize", 18);
        componentState.Variables.Add(new VariableSave
        {
            SetsValue = true,
            Name = "Label.FontSize",
            Value = 18,
            ExposedAsName = "FontSize"
        });
        Project.Components.Add(component);

        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        StateSave screenState = AddState(screen, "Default");
        screen.Instances.Add(new InstanceSave { Name = "ButtonInstance", BaseType = "MyButton" });
        SetVar(screenState, "ButtonInstance.FontSize", 24);
        Project.Screens.Add(screen);

        Dictionary<string, BmfcSave> result = _sut.CollectRequiredFonts(Project, new[] { screen });

        result.Values.ShouldContain(f => f.FontName == "Verdana" && f.FontSize == 24);
    }

    #endregion

    // -------------------------------------------------------------------------
    // Windows gate
    // -------------------------------------------------------------------------

    #region CreateFontIfNecessary

    [Fact]
    public void CreateFontIfNecessary_ShouldThrowPlatformNotSupportedException_WhenNotWindows()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        BmfcSave bmfcSave = new BmfcSave();
        bmfcSave.FontName = "Arial";
        bmfcSave.FontSize = 24;

        Should.Throw<PlatformNotSupportedException>(
            () => _sut.CreateFontIfNecessary(bmfcSave, projectDirectory: "/tmp/test", autoSizeFontOutputs: false));
    }

    #endregion

    // -------------------------------------------------------------------------
    // Windows gate
    // -------------------------------------------------------------------------

    #region Windows gate

    [Fact]
    public async Task CreateAllMissingFontFiles_ShouldThrowPlatformNotSupportedException_WhenNotWindows()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        GumProjectSave project = new GumProjectSave();

        await Should.ThrowAsync<PlatformNotSupportedException>(
            () => _sut.CreateAllMissingFontFiles(project, projectDirectory: "/tmp/test"));
    }

    [Fact]
    public void GenerateMissingFontsForReferencingElements_ShouldThrowPlatformNotSupportedException_WhenNotWindows()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        GumProjectSave project = new GumProjectSave();
        StateSave state = new StateSave();

        Should.Throw<PlatformNotSupportedException>(
            () => _sut.GenerateMissingFontsForReferencingElements(project, state, projectDirectory: "/tmp/test"));
    }

    #endregion
}
