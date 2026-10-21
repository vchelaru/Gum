using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public partial class VariableReferenceCreationTests
{
    // ---- Inheritance, nesting, exposed variables ----

    private static ScreenSave BuildDerivedScene(GumProjectSave project, string? derivedText, string? screenText)
    {
        AddLabelComponent(project, "Label", "Text = Text");
        ComponentSave derived = new ComponentSave { Name = "FancyLabel", BaseType = "Label" };
        derived.States.Add(new StateSave { Name = "Default", ParentContainer = derived });
        // A derived component carries the base component's instances, marked DefinedByBase, as the tool saves them.
        derived.Instances.Add(new InstanceSave { Name = "Shadow", BaseType = "Text", DefinedByBase = true, ParentContainer = derived });
        if (derivedText != null)
        {
            AddVariable(derived.DefaultState, "Text", "string", derivedText);
        }
        project.Components.Add(derived);
        ScreenSave screen = AddScreen(project, "FancyLabel", "Label1");
        if (screenText != null)
        {
            AddVariable(screen.DefaultState, "Label1.Text", "string", screenText);
        }
        return screen;
    }

    [Fact]
    public void DerivedComponentDefault_RowsDefinedOnTheBase_ShadowShowsTheDerivedDefault()
    {
        ScreenSave screen = BuildDerivedScene(CreateProject(), "Derived default", null);

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ShadowOf(runtime, "Label1").Text.ShouldBe("Derived default");
    }

    [Fact]
    public void DerivedComponentWithScreenOverride_RowsDefinedOnTheBase_ShadowShowsTheOverride()
    {
        ScreenSave screen = BuildDerivedScene(CreateProject(), null, "Override");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ShadowOf(runtime, "Label1").Text.ShouldBe("Override");
    }

    [Fact]
    public void DerivedComponentCodeSet_RowsDefinedOnTheBase_ShadowFollows()
    {
        ScreenSave screen = BuildDerivedScene(CreateProject(), null, null);
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text = "Code";

        ShadowOf(runtime, "Label1").Text.ShouldBe("Code");
    }

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
    public void NestedComponent_InnerTextAuthoredInTheOuterDefault_InnerShadowShowsIt()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        AddOuter(project, "From outer", exposeCaption: false);
        ScreenSave screen = AddScreen(project, "Outer", "Outer1");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        TextRuntime shadow = InnerShadow(runtime, out TextRuntime inner);
        inner.Text.ShouldBe("From outer");
        shadow.Text.ShouldBe("From outer");
    }

    [Fact]
    public void NestedComponent_ExposedCaptionOverriddenInTheScreen_InnerShadowShowsIt()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        AddOuter(project, "From outer", exposeCaption: true);
        ScreenSave screen = AddScreen(project, "Outer", "Outer1");
        AddVariable(screen.DefaultState, "Outer1.Caption", "string", "From screen");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        TextRuntime shadow = InnerShadow(runtime, out TextRuntime inner);
        inner.Text.ShouldBe("From screen");
        shadow.Text.ShouldBe("From screen");
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

    [Fact]
    public void NestedComponent_OuterRowFeedsInnerText_ScreenOverridesOuterVariable_InnerShadowFollows()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ComponentSave outer = AddOuter(project, null, exposeCaption: false);
        AddVariable(outer.DefaultState, "Title", "string", "Outer title");
        AddRows(outer.DefaultState, "Inner", "Text = Title");
        ScreenSave screen = AddScreen(project, "Outer", "Outer1");
        AddVariable(screen.DefaultState, "Outer1.Title", "string", "Screen title");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        TextRuntime shadow = InnerShadow(runtime, out TextRuntime inner);
        inner.Text.ShouldBe("Screen title");
        shadow.Text.ShouldBe("Screen title");
    }

    [Fact]
    public void NestedComponent_OuterRowFeedsInnerText_OuterDefaultNoOverride_InnerShadowShowsTheTitle()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ComponentSave outer = AddOuter(project, null, exposeCaption: false);
        AddVariable(outer.DefaultState, "Title", "string", "Outer title");
        AddRows(outer.DefaultState, "Inner", "Text = Title");
        ScreenSave screen = AddScreen(project, "Outer", "Outer1");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        InnerShadow(runtime, out TextRuntime _).Text.ShouldBe("Outer title");
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
    public void ExposedTextOnAContainerComponent_ScreenOverride_ShadowReadingTheChildShowsIt()
    {
        // The Forms-button shape: Text is exposed from a TextInstance child, and a sibling reads it.
        GumProjectSave project = CreateProject();
        AddButtonLike(project);
        ScreenSave screen = AddScreen(project, "ButtonLike", "Button1");
        AddVariable(screen.DefaultState, "Button1.Text", "string", "Hi");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        GraphicalUiElement button1 = runtime.GetGraphicalUiElementByName("Button1")!;
        ((TextRuntime)button1.GetGraphicalUiElementByName("TextInstance")!).Text.ShouldBe("Hi");
        ((TextRuntime)button1.GetGraphicalUiElementByName("Shadow")!).Text.ShouldBe("Hi");
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

    [Fact]
    public void CustomVariableOverriddenInTheScreen_RowReadingItOnTheComponent_ShadowShowsIt()
    {
        GumProjectSave project = CreateProject();
        ComponentSave card = new ComponentSave { Name = "Card", BaseType = "Container" };
        card.States.Add(new StateSave { Name = "Default", ParentContainer = card });
        card.Instances.Add(new InstanceSave { Name = "Shadow", BaseType = "Text", ParentContainer = card });
        card.DefaultState.Variables.Add(new VariableSave { Name = "Title", Type = "string", Value = "Card title", SetsValue = true, IsCustomVariable = true });
        AddRows(card.DefaultState, "Shadow", "Text = Title");
        project.Components.Add(card);
        ScreenSave screen = AddScreen(project, "Card", "Card1");
        AddVariable(screen.DefaultState, "Card1.Title", "string", "Screen title");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Card1")!.GetGraphicalUiElementByName("Shadow")!).Text.ShouldBe("Screen title");
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ElementRow_HeightFromWidth_ScreenOverrideOfWidth_HeightFollows(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement box = BuildBoxScene("Height = Width", null, screenWidth: 200f);

        box.Width.ShouldBe(200f);
        box.Height.ShouldBe(200f);
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
    public void ElementRow_ExpressionHalfWidth_ScreenOverrideOfWidth_HeightFollows()
    {
        UseExpressions(true);
        GraphicalUiElement box = BuildBoxScene("Height = Width / 2", null, screenWidth: 200f);

        box.Height.ShouldBe(100f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChildRow_WidthFromContainerWidth_ScreenOverrideOfWidth_ChildFollows(bool useExpressions)
    {
        UseExpressions(useExpressions);
        GraphicalUiElement box = BuildBoxScene("Height = Height", "Width = Width", screenWidth: 200f);

        box.GetGraphicalUiElementByName("Child")!.Width.ShouldBe(200f);
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
    public void Chain_SecondShadowReadsTheFirst_ScreenOverride_BothShowIt()
    {
        UseExpressions(true);
        ScreenSave screen = BuildChainScene(CreateProject(), "Testing");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        GraphicalUiElement label1 = runtime.GetGraphicalUiElementByName("Label1")!;
        ((TextRuntime)label1.GetGraphicalUiElementByName("Shadow")!).Text.ShouldBe("Testing");
        ((TextRuntime)label1.GetGraphicalUiElementByName("Shadow2")!).Text.ShouldBe("Testing?");
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

    [Fact]
    public void InstanceParentedInTheFileToAnotherInstance_ScreenOverride_ShadowShowsIt()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        screen.Instances.Add(new InstanceSave { Name = "Holder", BaseType = "Container", ParentContainer = screen });
        AddVariable(screen.DefaultState, "Label1.Parent", "string", "Holder");
        AddVariable(screen.DefaultState, "Label1.Text", "string", "Testing");

        GraphicalUiElement runtime = screen.ToGraphicalUiElement();

        ShadowOf(runtime, "Label1").Text.ShouldBe("Testing");
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
    public void FormsLabel_ScreenOverride_ShadowShowsIt()
    {
        (GraphicalUiElement screen, LabelVisual visual, Label _) = BuildFormsScene("Testing");

        visual.Text.ShouldBe("Testing");
        ShadowOf(screen, "Label1").Text.ShouldBe("Testing");
    }

    [Fact]
    public void FormsLabel_TextSetFromCode_ShadowFollows()
    {
        (GraphicalUiElement screen, LabelVisual _, Label label) = BuildFormsScene(null);

        label.Text = "Code";

        ShadowOf(screen, "Label1").Text.ShouldBe("Code");
    }

    [Fact]
    public void FormsLabel_ScreenOverrideThenSameValueFromCode_ShadowShowsTheValue()
    {
        (GraphicalUiElement screen, LabelVisual _, Label label) = BuildFormsScene("Dreams");

        label.Text = "Dreams";

        ShadowOf(screen, "Label1").Text.ShouldBe("Dreams");
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
    public void HotReload_ScreenOverrideChanged_ShadowFollowsTheNewValue()
    {
        GumProjectSave first = CreateProject();
        AddLabelComponent(first, "Label", "Text = Text");
        ScreenSave firstScreen = AddScreen(first, "Label", "Label1");
        AddVariable(firstScreen.DefaultState, "Label1.Text", "string", "Before");
        GraphicalUiElement runtime = firstScreen.ToGraphicalUiElement();

        GumProjectSave second = CreateProject();
        AddLabelComponent(second, "Label", "Text = Text");
        ScreenSave secondScreen = AddScreen(second, "Label", "Label1");
        AddVariable(secondScreen.DefaultState, "Label1.Text", "string", "After");
        Reload(runtime, second);

        ((TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!).Text.ShouldBe("After");
        ShadowOf(runtime, "Label1").Text.ShouldBe("After");
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
    public void HotReload_RowAdded_ShadowEvaluatesTheNewRow()
    {
        GumProjectSave first = CreateProject();
        AddLabelComponent(first, "Label");
        ScreenSave firstScreen = AddScreen(first, "Label", "Label1");
        AddVariable(firstScreen.DefaultState, "Label1.Text", "string", "Testing");
        GraphicalUiElement runtime = firstScreen.ToGraphicalUiElement();

        GumProjectSave second = CreateProject();
        AddLabelComponent(second, "Label", "Text = Text");
        ScreenSave secondScreen = AddScreen(second, "Label", "Label1");
        AddVariable(secondScreen.DefaultState, "Label1.Text", "string", "Testing");
        Reload(runtime, second);

        ShadowOf(runtime, "Label1").Text.ShouldBe("Testing");
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
    public void Localization_ShadowAtCreation_IsTranslatedLikeTheLabel()
    {
        Gum.Localization.ILocalizationService? previous = CustomSetPropertyOnRenderable.LocalizationService;
        try
        {
            CustomSetPropertyOnRenderable.LocalizationService = CreateLocalization(1);
            GumProjectSave project = CreateProject();
            AddLabelComponent(project, "Label", "Text = Text");
            ScreenSave screen = AddScreen(project, "Label", "Label1");
            AddVariable(screen.DefaultState, "Label1.Text", "string", "Greeting");

            GraphicalUiElement runtime = screen.ToGraphicalUiElement();

            ShadowOf(runtime, "Label1").Text.ShouldBe("Hola");
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

    [Fact]
    public void RefreshStyles_AfterScreenOverride_ShadowMatchesTheLabel()
    {
        GumProjectSave project = CreateProject();
        AddLabelComponent(project, "Label", "Text = Text");
        ScreenSave screen = AddScreen(project, "Label", "Label1");
        AddVariable(screen.DefaultState, "Label1.Text", "string", "Testing");
        GraphicalUiElement runtime = screen.ToGraphicalUiElement();
        TextRuntime label = (TextRuntime)runtime.GetGraphicalUiElementByName("Label1")!;

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
