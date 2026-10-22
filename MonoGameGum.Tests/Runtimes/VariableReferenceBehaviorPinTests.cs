using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using System.Linq;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Pins the cases where a label component's child <c>Shadow</c>, copying the component's own variable
/// through a same-component reference (<c>Text = Text</c>), already shows the label's value: component
/// defaults, code after creation, states, animation, expressions, chains, reparenting, Forms Label,
/// hot reload of defaults, localization and the manual overrides (#6385 sweep).
/// </summary>
public class VariableReferenceBehaviorPinTests : BaseTestClass
{
    private const string DefaultText = "Component default";

    public VariableReferenceBehaviorPinTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static void UseExpressions(bool useExpressions)
    {
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
    }

    private static GumProjectSave CreateProject()
    {
        GumProjectSave project = new GumProjectSave();
        foreach (string standard in new[] { "Container", "Text", "ColoredRectangle" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!);
            standardElement.DefaultState!.ParentContainer = standardElement;
            project.StandardElements.Add(standardElement);
        }
        ObjectFinder.Self.GumProjectSave = project;
        ElementSaveExtensions.RegisterGueInstantiation("Text", () => new TextRuntime());
        return project;
    }

    private static void AddVariable(StateSave state, string name, string type, object value)
    {
        state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
    }

    private static void AddRows(StateSave state, string instanceName, params string[] rows)
    {
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = instanceName + ".VariableReferences" };
        foreach (string row in rows)
        {
            list.Value.Add(row);
        }
        state.VariableLists.Add(list);
    }

    /// <summary>A Label component (BaseType Text) holding a Text instance named Shadow with the given rows.</summary>
    private static ComponentSave AddLabelComponent(GumProjectSave project, string name, params string[] shadowRows)
    {
        ComponentSave label = new ComponentSave { Name = name, BaseType = "Text" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = label };
        label.States.Add(state);
        label.Instances.Add(new InstanceSave { Name = "Shadow", BaseType = "Text", ParentContainer = label });
        AddVariable(state, "Text", "string", DefaultText);
        AddVariable(state, "Shadow.X", "float", 1f);
        AddRows(state, "Shadow", shadowRows);
        project.Components.Add(label);
        return label;
    }

    private static ScreenSave AddScreen(GumProjectSave project, string componentName, params string[] instanceNames)
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        foreach (string instanceName in instanceNames)
        {
            screen.Instances.Add(new InstanceSave { Name = instanceName, BaseType = componentName, ParentContainer = screen });
        }
        project.Screens.Add(screen);
        return screen;
    }

    private static TextRuntime ShadowOf(GraphicalUiElement screenRuntime, string labelName)
    {
        GraphicalUiElement label = screenRuntime.GetGraphicalUiElementByName(labelName)!;
        return (TextRuntime)label.GetGraphicalUiElementByName("Shadow")!;
    }

    // ---- The report's three cases, built with plain TextRuntime labels ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CodeSetAfterCreation_ShadowFollows(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text = "From code";

        ShadowOf(runtime, "Label1").Text.ShouldBe("From code");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FileOverrideThenSameValueFromCode_ShadowShowsTheValue(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        AddVariable(screen.DefaultState, "Label1.Text", "string", "Dreams");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text = "Dreams";

        ShadowOf(runtime, "Label1").Text.ShouldBe("Dreams");
    }

    [Fact]
    public void ComponentWithNoOverride_ShadowShowsTheComponentDefault()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ShadowOf(runtime, "Label1").Text.ShouldBe(DefaultText);
    }

    // ---- The same sources for every property, not only Text ----

    private sealed class PropertyCase
    {
        public string Name = "";
        public string Type = "";
        public object ComponentDefault = "";
        public object Authored = "";
        public object Code = "";
        public System.Func<TextRuntime, object> Get = _ => "";
        public System.Action<TextRuntime, object> Set = (_, _) => { };
    }

    private static PropertyCase GetCase(string name)
    {
        switch (name)
        {
            case "Text": return new PropertyCase { Name = name, Type = "string", ComponentDefault = DefaultText, Authored = "Authored", Code = "Code", Get = t => t.Text, Set = (t, v) => t.Text = (string)v };
            case "X": return new PropertyCase { Name = name, Type = "float", ComponentDefault = 7f, Authored = 45f, Code = 60f, Get = t => t.X, Set = (t, v) => t.X = (float)v };
            case "Y": return new PropertyCase { Name = name, Type = "float", ComponentDefault = 8f, Authored = 46f, Code = 61f, Get = t => t.Y, Set = (t, v) => t.Y = (float)v };
            case "Width": return new PropertyCase { Name = name, Type = "float", ComponentDefault = 70f, Authored = 150f, Code = 160f, Get = t => t.Width, Set = (t, v) => t.Width = (float)v };
            case "Height": return new PropertyCase { Name = name, Type = "float", ComponentDefault = 30f, Authored = 55f, Code = 56f, Get = t => t.Height, Set = (t, v) => t.Height = (float)v };
            case "Rotation": return new PropertyCase { Name = name, Type = "float", ComponentDefault = 3f, Authored = 12f, Code = 20f, Get = t => t.Rotation, Set = (t, v) => t.Rotation = (float)v };
            case "Visible": return new PropertyCase { Name = name, Type = "bool", ComponentDefault = true, Authored = false, Code = false, Get = t => t.Visible, Set = (t, v) => t.Visible = (bool)v };
            case "Red": return new PropertyCase { Name = name, Type = "int", ComponentDefault = 10, Authored = 200, Code = 100, Get = t => t.Red, Set = (t, v) => t.Red = (int)v };
            case "Alpha": return new PropertyCase { Name = name, Type = "int", ComponentDefault = 200, Authored = 100, Code = 50, Get = t => t.Alpha, Set = (t, v) => t.Alpha = (int)v };
            case "FontSize": return new PropertyCase { Name = name, Type = "int", ComponentDefault = 20, Authored = 40, Code = 30, Get = t => t.FontSize, Set = (t, v) => t.FontSize = (int)v };
            case "IsBold": return new PropertyCase { Name = name, Type = "bool", ComponentDefault = false, Authored = true, Code = true, Get = t => t.IsBold, Set = (t, v) => t.IsBold = (bool)v };
            case "IsItalic": return new PropertyCase { Name = name, Type = "bool", ComponentDefault = false, Authored = true, Code = true, Get = t => t.IsItalic, Set = (t, v) => t.IsItalic = (bool)v };
            case "OutlineThickness": return new PropertyCase { Name = name, Type = "int", ComponentDefault = 1, Authored = 3, Code = 4, Get = t => t.OutlineThickness, Set = (t, v) => t.OutlineThickness = (int)v };
            case "FontScale": return new PropertyCase { Name = name, Type = "float", ComponentDefault = 1f, Authored = 2f, Code = 3f, Get = t => t.FontScale, Set = (t, v) => t.FontScale = (float)v };
            default: throw new System.ArgumentException(name);
        }
    }

    private static object Normalize(object value)
    {
        return value is bool || value is string ? value : System.Convert.ToSingle(value);
    }

    public static IEnumerable<object[]> AllProperties()
    {
        foreach (string property in new[] { "Text", "X", "Y", "Width", "Height", "Rotation", "Visible", "Red", "Alpha", "FontSize", "IsBold", "IsItalic", "OutlineThickness", "FontScale" })
        {
            foreach (bool useExpressions in new[] { false, true })
            {
                yield return new object[] { property, useExpressions };
            }
        }
    }

    // The typed setters that report themselves.
    public static IEnumerable<object[]> TypedReportingProperties()
    {
        foreach (string property in new[] { "Text", "X", "Y", "Width", "Height", "Rotation", "Visible", "Red", "Alpha", "FontSize", "IsBold", "IsItalic", "OutlineThickness", "FontScale" })
        {
            foreach (bool useExpressions in new[] { false, true })
            {
                yield return new object[] { property, useExpressions };
            }
        }
    }

    private static (GraphicalUiElement screen, TextRuntime label, TextRuntime shadow) BuildPropertyScene(
        PropertyCase c, bool fileOverride)
    {
        GumProjectSave project = CreateProject();
        ComponentSave label = AddLabelComponent(project, "Label", $"{c.Name} = {c.Name}");
        if (c.Name != "Text")
        {
            AddVariable(label.DefaultState, c.Name, c.Type, c.ComponentDefault);
        }
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        if (fileOverride)
        {
            AddVariable(screen.DefaultState, "Label1." + c.Name, c.Type, c.Authored);
        }
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        TextRuntime labelRuntime = (TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!;
        return (runtime, labelRuntime, ShadowOf(runtime, "Label1"));
    }

    [Theory]
    [MemberData(nameof(AllProperties))]
    public void Property_AuthoredAsComponentDefault_ShadowShowsTheDefault(string property, bool useExpressions)
    {
        UseExpressions(useExpressions);
        PropertyCase c = GetCase(property);
        (GraphicalUiElement _, TextRuntime _, TextRuntime shadow) = BuildPropertyScene(c, fileOverride: false);

        Normalize(c.Get(shadow)).ShouldBe(Normalize(c.ComponentDefault));
    }

    [Theory]
    [MemberData(nameof(TypedReportingProperties))]
    public void Property_SetThroughTheTypedPropertyAfterCreation_ShadowFollows(string property, bool useExpressions)
    {
        UseExpressions(useExpressions);
        PropertyCase c = GetCase(property);
        (GraphicalUiElement _, TextRuntime label, TextRuntime shadow) = BuildPropertyScene(c, fileOverride: false);

        c.Set(label, c.Code);

        Normalize(c.Get(shadow)).ShouldBe(Normalize(c.Code));
    }

    [Theory]
    [MemberData(nameof(AllProperties))]
    public void Property_SetThroughSetPropertyAfterCreation_ShadowFollows(string property, bool useExpressions)
    {
        UseExpressions(useExpressions);
        PropertyCase c = GetCase(property);
        (GraphicalUiElement _, TextRuntime label, TextRuntime shadow) = BuildPropertyScene(c, fileOverride: false);

        label.SetProperty(c.Name, c.Code);

        Normalize(c.Get(shadow)).ShouldBe(Normalize(c.Code));
    }

    // ---- Expression rows ----

    [Fact]
    public void ExpressionRow_ComponentDefault_ShadowShowsTheEvaluatedDefault()
    {
        UseExpressions(true);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text + \"!\"");
        ScreenSave screen = AddScreen(project, "Label", "Label1");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ShadowOf(runtime, "Label1").Text.ShouldBe(DefaultText + "!");
    }

    [Fact]
    public void ExpressionRow_CodeSet_ShadowFollows()
    {
        UseExpressions(true);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text + \"!\"");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text = "Code";

        ShadowOf(runtime, "Label1").Text.ShouldBe("Code!");
    }

    [Fact]
    public void ExpressionRowWithoutExpressionsInstalled_Creation_DoesNotThrow()
    {
        UseExpressions(false);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text + \"!\"");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        AddVariable(screen.DefaultState, "Label1.Text", "string", "Testing");

        Should.NotThrow(() => screen.ToGraphicalUiElement());
    }

    // ---- Workarounds and manual overrides ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NotifyVariableChanged_OnTheLabelAfterCreation_FixesAFileOverride(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        AddVariable(screen.DefaultState, "Label1.Text", "string", "Testing");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        TextRuntime label = (TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!;

        label.NotifyVariableChanged("Text", label.Text);

        ShadowOf(runtime, "Label1").Text.ShouldBe("Testing");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefreshReferences_OnTheLabelAfterCreation_FixesAFileOverride(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        AddVariable(screen.DefaultState, "Label1.Text", "string", "Testing");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        TextRuntime label = (TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!;

        label.RefreshReferences("Text");

        ShadowOf(runtime, "Label1").Text.ShouldBe("Testing");
    }

    [Fact]
    public void ReapplyingTheScreenStateAfterCreation_ShadowFollowsTheOverride()
    {
        // Evidence for the cause: the same screen write notifies once creation is over.
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        AddVariable(screen.DefaultState, "Label1.Text", "string", "Testing");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        runtime.ApplyState(screen.DefaultState);

        ShadowOf(runtime, "Label1").Text.ShouldBe("Testing");
    }

    // ---- Several instances, independence ----

    [Fact]
    public void TwoInstances_CodeSetOnOne_DoesNotChangeTheOthersShadow()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1", "Label2");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text = "One";

        ShadowOf(runtime, "Label1").Text.ShouldBe("One");
        ShadowOf(runtime, "Label2").Text.ShouldBe(DefaultText);
    }

    // ---- Screen state / category state / animation ----

    [Fact]
    public void ScreenCategoryStateAppliedAfterCreation_ShadowFollows()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        StateSaveCategory category = new StateSaveCategory { Name = "Mood" };
        StateSave happy = new StateSave { Name = "Happy", ParentContainer = screen };
        AddVariable(happy, "Label1.Text", "string", "Yay");
        category.States.Add(happy);
        screen.Categories.Add(category);
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        runtime.ApplyState(happy);

        ShadowOf(runtime, "Label1").Text.ShouldBe("Yay");
    }

    [Fact]
    public void ComponentCategoryStateAppliedAfterCreation_ShadowFollows()
    {
        GumProjectSave project = CreateProject();
        ComponentSave label = AddLabelComponent(project, "Label", "Text = Text");
        StateSaveCategory category = new StateSaveCategory { Name = "Mood" };
        StateSave happy = new StateSave { Name = "Happy", ParentContainer = label };
        AddVariable(happy, "Text", "string", "Yay");
        category.States.Add(happy);
        label.Categories.Add(category);
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        GraphicalUiElement labelRuntime = runtime.GetGraphicalUiElementByName("Label1")!;

        labelRuntime.ApplyState(happy);

        ShadowOf(runtime, "Label1").Text.ShouldBe("Yay");
    }

    [Fact]
    public void ComponentCategoryStateSetByNameAfterCreation_ShadowFollows()
    {
        GumProjectSave project = CreateProject();
        ComponentSave label = AddLabelComponent(project, "Label", "Text = Text");
        StateSaveCategory category = new StateSaveCategory { Name = "Mood" };
        StateSave happy = new StateSave { Name = "Happy", ParentContainer = label };
        AddVariable(happy, "Text", "string", "Yay");
        category.States.Add(happy);
        label.Categories.Add(category);
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        GraphicalUiElement labelRuntime = runtime.GetGraphicalUiElementByName("Label1")!;

        labelRuntime.SetProperty("MoodState", "Happy");

        ShadowOf(runtime, "Label1").Text.ShouldBe("Yay");
    }

    private static AnimationRuntime CreateTextAnimation(ElementSave element, string prefix, string startText, string endText)
    {
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        foreach ((string name, string value) in new[] { ("Start", startText), ("End", endText) })
        {
            StateSave state = new StateSave { Name = name, ParentContainer = element };
            AddVariable(state, prefix + "Text", "string", value);
            category.States.Add(state);
        }
        element.Categories.Add(category);

        AnimationRuntime animation = new AnimationRuntime();
        animation.Keyframes.Add(new KeyframeRuntime { StateName = "Cat/Start", Time = 0, InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear });
        animation.Keyframes.Add(new KeyframeRuntime { StateName = "Cat/End", Time = 1, InterpolationType = FlatRedBall.Glue.StateInterpolation.InterpolationType.Linear });
        animation.RefreshCumulativeStates(element);
        return animation;
    }

    [Fact]
    public void ComponentAnimationOfTheText_ShadowFollowsTheLabel()
    {
        GumProjectSave project = CreateProject();
        ComponentSave label = AddLabelComponent(project, "Label", "Text = Text");
        AnimationRuntime animation = CreateTextAnimation(label, "", "Start text", "End text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        GraphicalUiElement labelRuntime = runtime.GetGraphicalUiElementByName("Label1")!;

        animation.ApplyAtTimeTo(1.0, labelRuntime);

        ((TextRuntime)labelRuntime).Text.ShouldBe("End text");
        ShadowOf(runtime, "Label1").Text.ShouldBe("End text");
    }

    [Fact]
    public void ScreenAnimationOfTheLabelText_ShadowFollowsTheLabel()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        AnimationRuntime animation = CreateTextAnimation(screen, "Label1.", "Start text", "End text");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        animation.ApplyAtTimeTo(1.0, runtime);

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text.ShouldBe("End text");
        ShadowOf(runtime, "Label1").Text.ShouldBe("End text");
    }

    // ---- Inheritance, nesting, exposed variables ----

    private static ComponentSave AddOuter(GumProjectSave project, string? innerText, bool exposeCaption)
    {
        ComponentSave outer = new ComponentSave { Name = "Outer", BaseType = "Container" };
        outer.States.Add(new StateSave { Name = "Default", ParentContainer = outer });
        outer.Instances.Add(new InstanceSave { Name = "Inner", BaseType = "Label", ParentContainer = outer });
        if (innerText != null)
        {
            AddVariable(outer.DefaultState, "Inner.Text", "string", innerText);
        }
        if (exposeCaption)
        {
            outer.DefaultState.Variables.First(v => v.Name == "Inner.Text").ExposedAsName = "Caption";
        }
        project.Components.Add(outer);
        return outer;
    }

    private static TextRuntime InnerShadow(GraphicalUiElement screenRuntime, out TextRuntime inner)
    {
        inner = (TextRuntime)screenRuntime.GetGraphicalUiElementByName("Outer1")!.GetGraphicalUiElementByName("Inner")!;
        return (TextRuntime)inner.GetGraphicalUiElementByName("Shadow")!;
    }

    [Fact]
    public void NestedComponent_ExposedCaptionSetThroughSetPropertyAfterCreation_InnerShadowFollows()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        AddOuter(project, "From outer", exposeCaption: true);
        ScreenSave screen = AddScreen(project, "Outer", "Outer1");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        runtime.GetGraphicalUiElementByName("Outer1")!.SetProperty("Caption", "Later");

        InnerShadow(runtime, out TextRuntime _).Text.ShouldBe("Later");
    }

    private static ComponentSave AddButtonLike(GumProjectSave project)
    {
        ComponentSave button = new ComponentSave { Name = "ButtonLike", BaseType = "Container" };
        button.States.Add(new StateSave { Name = "Default", ParentContainer = button });
        button.Instances.Add(new InstanceSave { Name = "TextInstance", BaseType = "Text", ParentContainer = button });
        button.Instances.Add(new InstanceSave { Name = "Shadow", BaseType = "Text", ParentContainer = button });
        AddVariable(button.DefaultState, "TextInstance.Text", "string", "Button default");
        button.DefaultState.Variables.First(v => v.Name == "TextInstance.Text").ExposedAsName = "Text";
        AddRows(button.DefaultState, "Shadow", "Text = TextInstance.Text");
        project.Components.Add(button);
        return button;
    }

    [Fact]
    public void ExposedTextOnAContainerComponent_SetPropertyAfterCreation_ShadowReadingTheChildFollows()
    {
        GumProjectSave project = CreateProject();
        AddButtonLike(project);
        ScreenSave screen = AddScreen(project, "ButtonLike", "Button1");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        GraphicalUiElement button1 = runtime.GetGraphicalUiElementByName("Button1")!;

        button1.SetProperty("Text", "Later");

        ((TextRuntime)button1.GetGraphicalUiElementByName("Shadow")!).Text.ShouldBe("Later");
    }

    [Fact]
    public void ExposedTextOnAContainerComponent_ChildTextSetInCode_ShadowReadingTheChildFollows()
    {
        GumProjectSave project = CreateProject();
        AddButtonLike(project);
        ScreenSave screen = AddScreen(project, "ButtonLike", "Button1");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        GraphicalUiElement button1 = runtime.GetGraphicalUiElementByName("Button1")!;

        ((TextRuntime)button1.GetGraphicalUiElementByName("TextInstance")!).Text = "Typed";

        ((TextRuntime)button1.GetGraphicalUiElementByName("Shadow")!).Text.ShouldBe("Typed");
    }

    // ---- Element-level rows (no instance prefix) and Container-based components ----

    private static GraphicalUiElement BuildBoxScene(string elementRow, string? childRow, float? screenWidth)
    {
        GumProjectSave project = CreateProject();
        ComponentSave box = new ComponentSave { Name = "Box", BaseType = "Container" };
        box.States.Add(new StateSave { Name = "Default", ParentContainer = box });
        box.Instances.Add(new InstanceSave { Name = "Child", BaseType = "Container", ParentContainer = box });
        AddVariable(box.DefaultState, "Width", "float", 50f);
        AddVariable(box.DefaultState, "Height", "float", 50f);
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        list.Value.Add(elementRow);
        box.DefaultState.VariableLists.Add(list);
        if (childRow != null)
        {
            AddRows(box.DefaultState, "Child", childRow);
        }
        project.Components.Add(box);
        ScreenSave screen = AddScreen(project, "Box", "Box1");
        if (screenWidth != null)
        {
            AddVariable(screen.DefaultState, "Box1.Width", "float", screenWidth.Value);
        }
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        return runtime.GetGraphicalUiElementByName("Box1")!;
    }

    [Fact]
    public void ElementRow_HeightFromWidth_NoOverride_UsesTheComponentDefault()
    {
        GraphicalUiElement box = BuildBoxScene("Height = Width", null, screenWidth: null);

        box.Height.ShouldBe(50f);
    }

    [Fact]
    public void ElementRow_HeightFromWidth_CodeSetWidth_HeightFollows()
    {
        GraphicalUiElement box = BuildBoxScene("Height = Width", null, screenWidth: null);

        box.Width = 90;

        box.Height.ShouldBe(90f);
    }

    [Fact]
    public void ChildRow_WidthFromContainerWidth_CodeSetWidth_ChildFollows()
    {
        GraphicalUiElement box = BuildBoxScene("Height = Height", "Width = Width", screenWidth: null);

        box.Width = 90;

        box.GetGraphicalUiElementByName("Child")!.Width.ShouldBe(90f);
    }

    // ---- Chains and cycles between rows of the component ----

    private static ScreenSave BuildChainScene(GumProjectSave project, string? screenText)
    {
        ComponentSave label = AddLabelComponent(project, "Label", "Text = Text");
        label.Instances.Add(new InstanceSave { Name = "Shadow2", BaseType = "Text", ParentContainer = label });
        AddRows(label.DefaultState, "Shadow2", "Text = Shadow.Text + \"?\"");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        if (screenText != null)
        {
            AddVariable(screen.DefaultState, "Label1.Text", "string", screenText);
        }
        return screen;
    }

    [Fact]
    public void Chain_SecondShadowReadsTheFirst_ComponentDefault_BothShowIt()
    {
        UseExpressions(true);
        ScreenSave screen = BuildChainScene(CreateProject(), null);

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        GraphicalUiElement label1 = runtime.GetGraphicalUiElementByName("Label1")!;
        ((TextRuntime)label1.GetGraphicalUiElementByName("Shadow2")!).Text.ShouldBe(DefaultText + "?");
    }

    [Fact]
    public void Chain_SecondShadowReadsTheFirst_CodeSet_BothFollow()
    {
        UseExpressions(true);
        ScreenSave screen = BuildChainScene(CreateProject(), null);
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        TextRuntime label1 = (TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!;

        label1.Text = "Code";

        ((TextRuntime)label1.GetGraphicalUiElementByName("Shadow")!).Text.ShouldBe("Code");
        ((TextRuntime)label1.GetGraphicalUiElementByName("Shadow2")!).Text.ShouldBe("Code?");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cycle_TwoShadowsReadingEachOther_CreationAndCodeSetDoNotThrow(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GumProjectSave project = CreateProject();
        ComponentSave label = AddLabelComponent(project, "Label", "Text = Shadow2.Text");
        label.Instances.Add(new InstanceSave { Name = "Shadow2", BaseType = "Text", ParentContainer = label });
        AddRows(label.DefaultState, "Shadow2", "Text = Shadow.Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        AddVariable(screen.DefaultState, "Label1.Text", "string", "Testing");

        GraphicalUiElement? runtime = null;
        Should.NotThrow(() => runtime = screen.ToGraphicalUiElement());
        Should.NotThrow(() => ((TextRuntime)runtime!.GetGraphicalUiElementByName("Label1")!).Text = "Code");
    }

    [Fact]
    public void SelfReadingRow_ShadowTextFromShadowText_DoesNotThrowOrLoop()
    {
        UseExpressions(true);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Shadow.Text + \"!\"");
        ScreenSave screen = AddScreen(project, "Label", "Label1");

        GraphicalUiElement? runtime = null;
        Should.NotThrow(() => runtime = screen.ToGraphicalUiElement());
        Should.NotThrow(() => ((TextRuntime)runtime!.GetGraphicalUiElementByName("Label1")!).Text = "Code");
    }

    // ---- Qualified, unqualified and @ ----

    [Fact]
    public void OwnerPrefixRow_ShadowYFromItsOwnX_Evaluates()
    {
        UseExpressions(true);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Y = @X + 5");
        ScreenSave screen = AddScreen(project, "Label", "Label1");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        // Shadow.X is authored as 1 on the component.
        ShadowOf(runtime, "Label1").Y.ShouldBe(6f);
    }

    [Fact]
    public void OwnerPrefixRow_ShadowYFromItsOwnX_CodeSetOnShadowX_Follows()
    {
        UseExpressions(true);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Y = @X + 5");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        TextRuntime shadow = ShadowOf(runtime, "Label1");

        shadow.X = 20;

        shadow.Y.ShouldBe(25f);
    }

    [Fact]
    public void QualifiedRow_ShadowTextFromAnotherInstanceInTheSameComponent_StaysCorrect()
    {
        GumProjectSave project = CreateProject();
        ComponentSave label = AddLabelComponent(project, "Label", "Text = Shadow2.Text");
        label.Instances.Add(new InstanceSave { Name = "Shadow2", BaseType = "Text", ParentContainer = label });
        AddVariable(label.DefaultState, "Shadow2.Text", "string", "Fixed");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        AddVariable(screen.DefaultState, "Label1.Text", "string", "Testing");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ShadowOf(runtime, "Label1").Text.ShouldBe("Fixed");
    }

    // ---- Screen-level rows reading instance variables ----

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScreenRow_BannerTextFromLabelText_ScreenOverride_BannerShowsIt(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        screen.Instances.Add(new InstanceSave { Name = "Banner", BaseType = "Text", ParentContainer = screen });
        AddVariable(screen.DefaultState, "Label1.Text", "string", "Testing");
        AddRows(screen.DefaultState, "Banner", "Text = Label1.Text");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Banner")!).Text.ShouldBe("Testing");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ScreenRow_BannerTextFromLabelText_CodeSet_BannerFollows(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        screen.Instances.Add(new InstanceSave { Name = "Banner", BaseType = "Text", ParentContainer = screen });
        AddRows(screen.DefaultState, "Banner", "Text = Label1.Text");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text = "Code";

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Banner")!).Text.ShouldBe("Code");
    }

    // ---- Instances made in code, reparenting ----

    [Fact]
    public void ComponentBuiltDirectly_CodeSet_ShadowFollows()
    {
        GumProjectSave project = CreateProject();
        ComponentSave label = AddLabelComponent(project, "Label", "Text = Text");
        TextRuntime built = (TextRuntime)label.ToGraphicalUiElement();

        built.Text = "Code";

        ((TextRuntime)built.GetGraphicalUiElementByName("Shadow")!).Text.ShouldBe("Code");
    }

    [Fact]
    public void ComponentBuiltDirectly_ShadowShowsTheComponentDefault()
    {
        GumProjectSave project = CreateProject();
        ComponentSave label = AddLabelComponent(project, "Label", "Text = Text");

        TextRuntime built = (TextRuntime)label.ToGraphicalUiElement();

        ((TextRuntime)built.GetGraphicalUiElementByName("Shadow")!).Text.ShouldBe(DefaultText);
    }

    [Fact]
    public void ComponentBuiltInCodeAndParentedToTheScreen_CodeSet_ShadowFollows()
    {
        GumProjectSave project = CreateProject();
        ComponentSave label = AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        TextRuntime built = (TextRuntime)label.ToGraphicalUiElement();
        built.Parent = runtime;

        built.Text = "Code";

        ((TextRuntime)built.GetGraphicalUiElementByName("Shadow")!).Text.ShouldBe("Code");
    }

    [Fact]
    public void InstanceReparentedAfterCreation_CodeSet_ShadowFollows()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        screen.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = screen });
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        TextRuntime label = (TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!;
        label.Parent = runtime.GetGraphicalUiElementByName("Holder")!;

        label.Text = "Code";

        ShadowOf(runtime, "Label1").Text.ShouldBe("Code");
    }

    // ---- Forms Label (LabelVisual: a TextRuntime that owns a Forms Label) ----

    private static (GraphicalUiElement screen, LabelVisual visual, Label label) BuildFormsScene(string? screenText)
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ElementSaveExtensions.RegisterGueInstantiation("Label", () => new LabelVisual());
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        if (screenText != null)
        {
            AddVariable(screen.DefaultState, "Label1.Text", "string", screenText);
        }
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        LabelVisual visual = (LabelVisual)runtime.GetGraphicalUiElementByName("Label1")!;
        return (runtime, visual, visual.FormsControl);
    }

    private static LabelVisual BuildReplacementVisual()
    {
        GumProjectSave project = ObjectFinder.Self.GumProjectSave!;
        ComponentSave other = AddLabelComponent(project, "Label2", "Text = Text");
        ElementSaveExtensions.RegisterGueInstantiation("Label2", () => new LabelVisual(fullInstantiation: true, tryCreateFormsObject: false));
        return (LabelVisual)other.ToGraphicalUiElement();
    }

    [Fact]
    public void FormsLabel_TextSetFromCode_ShadowFollows()
    {
        (GraphicalUiElement screen, LabelVisual _, Label label) = BuildFormsScene(null);

        label.Text = "Code";

        ShadowOf(screen, "Label1").Text.ShouldBe("Code");
    }

    [Fact]
    public void FormsLabel_SetTextNoTranslate_ShadowFollows()
    {
        (GraphicalUiElement screen, LabelVisual _, Label label) = BuildFormsScene(null);

        label.SetTextNoTranslate("Raw");

        ShadowOf(screen, "Label1").Text.ShouldBe("Raw");
    }

    [Fact]
    public void FormsLabel_VisualSwappedThenTextSet_ShadowOfTheNewVisualFollows()
    {
        (GraphicalUiElement _, LabelVisual _, Label label) = BuildFormsScene(null);
        LabelVisual replacement = BuildReplacementVisual();
        label.Visual = replacement;

        label.Text = "After swap";

        ((TextRuntime)replacement.GetGraphicalUiElementByName("Shadow")!).Text.ShouldBe("After swap");
    }

    [Fact]
    public void FormsLabel_VisualSwapped_ShadowOfTheNewVisualMatchesItsText()
    {
        (GraphicalUiElement _, LabelVisual _, Label label) = BuildFormsScene(null);
        LabelVisual replacement = BuildReplacementVisual();

        label.Visual = replacement;

        ((TextRuntime)replacement.GetGraphicalUiElementByName("Shadow")!).Text.ShouldBe(replacement.Text);
    }

    // ---- Hot reload ----

    private static void Reload(GraphicalUiElement root, GumProjectSave newProject)
    {
        ObjectFinder.Self.GumProjectSave = newProject;
        global::Gum.GumHotReloadManager.ApplyDiff(new[] { root }, newProject, RenderingLibrary.SystemManagers.Default);
    }

    [Fact]
    public void HotReload_ComponentDefaultChanged_ShadowFollowsTheNewDefault()
    {
        GumProjectSave first = CreateProject();
        AddLabelComponent(first, "Label", "Text = Text");
        ScreenSave firstScreen = AddScreen(first, "Label", "Label1");
        GraphicalUiElement runtime = firstScreen.ToGraphicalUiElement();

        GumProjectSave second = CreateProject();
        ComponentSave changed = AddLabelComponent(second, "Label", "Text = Text");
        changed.DefaultState.Variables.First(v => v.Name == "Text").Value = "Changed default";
        AddScreen(second, "Label", "Label1");
        Reload(runtime, second);

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text.ShouldBe("Changed default");
        ShadowOf(runtime, "Label1").Text.ShouldBe("Changed default");
    }

    [Fact]
    public void HotReload_AfterReload_CodeSetStillFollows()
    {
        GumProjectSave first = CreateProject();
        AddLabelComponent(first, "Label", "Text = Text");
        ScreenSave firstScreen = AddScreen(first, "Label", "Label1");
        GraphicalUiElement runtime = firstScreen.ToGraphicalUiElement();

        GumProjectSave second = CreateProject();
        AddLabelComponent(second, "Label", "Text = Text");
        AddScreen(second, "Label", "Label1");
        Reload(runtime, second);

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text = "Code";

        ShadowOf(runtime, "Label1").Text.ShouldBe("Code");
    }

    // ---- Localization and RefreshStyles ----

    private static Gum.Localization.LocalizationService CreateLocalization(int language)
    {
        Gum.Localization.LocalizationService service = new Gum.Localization.LocalizationService();
        service.AddDatabase(
            new Dictionary<string, string[]> { { "Greeting", new[] { "Hello", "Hola" } } },
            new List<string> { "English", "Spanish" });
        service.CurrentLanguage = language;
        return service;
    }

    [Fact]
    public void Localization_LanguageSwitched_ShadowMatchesTheTranslatedLabel()
    {
        Gum.Localization.ILocalizationService? previous = CustomSetPropertyOnRenderable.LocalizationService;
        try
        {
            Gum.Localization.LocalizationService service = CreateLocalization(0);
            CustomSetPropertyOnRenderable.LocalizationService = service;
            GumProjectSave project = CreateProject();
            AddLabelComponent(project, "Label", "Text = Text");
            ScreenSave screen = AddScreen(project, "Label", "Label1");
            AddVariable(screen.DefaultState, "Label1.Text", "string", "Greeting");
            GraphicalUiElement runtime = screen.ToGraphicalUiElement();
            runtime.AddToRoot();
            TextRuntime label = (TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!;
            TextRuntime shadow = ShadowOf(runtime, "Label1");
            label.Text.ShouldBe("Hello", "sanity: the label is translated");

            service.CurrentLanguage = 1;

            label.Text.ShouldBe("Hola");
            shadow.Text.ShouldBe("Hola");
        }
        finally
        {
            CustomSetPropertyOnRenderable.LocalizationService = previous;
        }
    }

    [Fact]
    public void RefreshStyles_AfterCodeSet_ShadowMatchesTheLabel()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        TextRuntime label = (TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!;
        label.Text = "Code";

        runtime.RefreshStyles();

        ShadowOf(runtime, "Label1").Text.ShouldBe(label.Text);
    }

    // ---- Same-value set (ADR 0022 pushes on change) ----

    [Fact]
    public void SameValueTypedSetOnTheLabelX_ShadowEditedByHand_IsNotOverwritten()
    {
        PropertyCase c = GetCase("X");
        (GraphicalUiElement _, TextRuntime label, TextRuntime shadow) = BuildPropertyScene(c, fileOverride: false);
        shadow.X = 99;

        label.X = label.X;

        shadow.X.ShouldBe(99f);
    }
}
