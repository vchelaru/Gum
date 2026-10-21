using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.GueDeriving;
using Gum.StateAnimation.Runtime;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public partial class VariableReferenceCreationTests
{
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
            case "StackSpacing": return new PropertyCase { Name = name, Type = "float", ComponentDefault = 1f, Authored = 5f, Code = 6f, Get = t => t.StackSpacing, Set = (t, v) => t.StackSpacing = (float)v };
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
    public void Property_AuthoredAsScreenInstanceOverride_ShadowShowsTheOverride(string property, bool useExpressions)
    {
        UseExpressions(useExpressions);
        PropertyCase c = GetCase(property);
        (GraphicalUiElement _, TextRuntime label, TextRuntime shadow) = BuildPropertyScene(c, fileOverride: true);

        Normalize(c.Get(label)).ShouldBe(Normalize(c.Authored), "sanity: the label itself has the override");
        Normalize(c.Get(shadow)).ShouldBe(Normalize(c.Authored));
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
    [MemberData(nameof(AllProperties))]
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

    [Theory]
    [MemberData(nameof(AllProperties))]
    public void Property_FileOverrideThenSameValueTypedSet_ShadowShowsTheValue(string property, bool useExpressions)
    {
        UseExpressions(useExpressions);
        PropertyCase c = GetCase(property);
        (GraphicalUiElement _, TextRuntime label, TextRuntime shadow) = BuildPropertyScene(c, fileOverride: true);

        c.Set(label, c.Authored);

        Normalize(c.Get(shadow)).ShouldBe(Normalize(c.Authored));
    }

    // ---- Expression rows ----

    [Fact]
    public void ExpressionRow_ScreenOverride_ShadowShowsTheEvaluatedOverride()
    {
        UseExpressions(true);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text + \"!\"");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        AddVariable(screen.DefaultState, "Label1.Text", "string", "Testing");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ShadowOf(runtime, "Label1").Text.ShouldBe("Testing!");
    }

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
    public void ExpressionRow_WidthMath_ScreenOverride_ShadowShowsOverridePlusTen()
    {
        UseExpressions(true);
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Width = Width + 10");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        AddVariable(screen.DefaultState, "Label1.Width", "float", 150f);

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ShadowOf(runtime, "Label1").Width.ShouldBe(160f);
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
    public void TwoInstances_OneOverridden_OneDefault_EachShadowShowsItsOwnLabel()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1", "Label2");
        AddVariable(screen.DefaultState, "Label1.Text", "string", "One");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ShadowOf(runtime, "Label1").Text.ShouldBe("One");
        ShadowOf(runtime, "Label2").Text.ShouldBe(DefaultText);
    }

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
    public void ComponentCategoryStateAssignedInTheScreenDefaultState_ShadowShowsTheStateValue()
    {
        GumProjectSave project = CreateProject();
        ComponentSave label = AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        StateSaveCategory category = new StateSaveCategory { Name = "Mood" };
        StateSave happy = new StateSave { Name = "Happy", ParentContainer = label };
        AddVariable(happy, "Text", "string", "Yay");
        category.States.Add(happy);
        label.Categories.Add(category);
        AddVariable(screen.DefaultState, "Label1.MoodState", "Mood", "Happy");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text.ShouldBe("Yay");
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
}
