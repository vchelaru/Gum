using System;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

// Issue #4947: under OutputLibrary.MonoGameForms, CodeGenerator.FillWithNewVariables puts a
// custom ("new") variable's property on the generated Forms class, while the element's state is
// applied to a separate ContainerRuntime visual. Issue #4891's fallback reflects onto the visual,
// so it never found the property and the authored value was dropped. Compounding it, the generated
// template creates the Forms control one line *after* the state is applied, so the property's owner
// does not exist yet when the value arrives.
//
// Several tests here also pin why the two rejected fixes were wrong - see the anti-regression
// comments on each.
public class FormsCustomVariablePopulationTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    // Mirrors what codegen emits under MonoGameForms: the custom variable lands on the Forms class,
    // not on the visual.
    private class ButtonWithCustomVariable : Button
    {
        public ButtonWithCustomVariable(InteractiveGue visual) : base(visual)
        {
        }

        public string? OriginalSourceFile { get; set; }
    }

    // Records what the visual looked like at the moment the Forms control was constructed.
    private class ButtonObservingVisual : Button
    {
        public ButtonObservingVisual(InteractiveGue visual) : base(visual)
        {
        }

        public GraphicalUiElement? BackgroundSeenWhenConstructed { get; private set; }

        protected override void ReactToVisualChanged()
        {
            base.ReactToVisualChanged();
            BackgroundSeenWhenConstructed = Visual?.GetGraphicalUiElementByName("Background");
        }
    }

    // A non-FrameworkElement stand-in, so a test can assign it as a second Forms control without
    // tripping FrameworkElement's "this visual already belongs to another element" guard.
    private class CustomVariableHolder
    {
        public string? OriginalSourceFile { get; set; }

        public float Width { get; set; }
    }

    // A visual that is not an InteractiveGue, so it can never have a Forms control.
    private class PlainVisual : GraphicalUiElement
    {
        public PlainVisual() : base(new InvisibleRenderable(), null)
        {
        }
    }

    // Mirrors codegen's other shape: the generated class derives from GraphicalUiElement, so the
    // custom variable is on the visual itself (issue #4891's case).
    private class ContainerWithCustomVariable : ContainerRuntime
    {
        public string? OriginalSourceFile { get; set; }
    }

    private static ComponentSave CreateComponentWithCustomVariable(string elementName, string variableName, object? value)
    {
        ComponentSave component = new ComponentSave { Name = elementName, BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Name = variableName,
            Value = value,
            Type = "string",
            IsCustomVariable = true,
            SetsValue = true
        });
        component.States.Add(defaultState);

        // Instances are only created for base types the project actually declares.
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(new StateSave { Name = "Default", ParentContainer = container });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;

        return component;
    }

    // Reproduces the generated template verbatim: build the visual, apply the element's state to
    // it, and only then construct the Forms control that owns the custom variable's property.
    private static void RegisterGeneratedTemplate<T>(string elementName, Func<InteractiveGue, T> createFormsControl)
        where T : class
    {
        ElementSaveExtensions.RegisterGueInstantiation(elementName, () =>
        {
            ContainerRuntime visual = new ContainerRuntime();
            ElementSave element = ObjectFinder.Self.GetElementSave(elementName)!;
            element.SetGraphicalUiElement(visual, SystemManagers.Default);
            visual.FormsControlAsObject = createFormsControl(visual);
            return visual;
        });
    }

    [Fact]
    public void ApplyState_CategoryStateSettingCustomVariable_ReachesFormsControl()
    {
        ComponentSave component = CreateComponentWithCustomVariable("MyButton", "OriginalSourceFile", "default.png");

        StateSaveCategory category = new StateSaveCategory { Name = "SkinCategory" };
        StateSave altState = new StateSave { Name = "Alternate", ParentContainer = component };
        altState.Variables.Add(new VariableSave
        {
            Name = "OriginalSourceFile",
            Value = "alternate.png",
            Type = "string",
            IsCustomVariable = true,
            SetsValue = true
        });
        category.States.Add(altState);
        component.Categories.Add(category);

        RegisterGeneratedTemplate("MyButton", visual => new ButtonWithCustomVariable(visual));

        GraphicalUiElement created = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        ButtonWithCustomVariable forms = ((InteractiveGue)created).FormsControlAsObject
            .ShouldBeOfType<ButtonWithCustomVariable>();

        created.ApplyState(altState);

        forms.OriginalSourceFile.ShouldBe("alternate.png");
    }

    [Fact]
    public void FormsControlAsObject_ReassignedAfterFlush_DoesNotReapplyStalePendingValue()
    {
        CreateComponentWithCustomVariable("MyButton", "OriginalSourceFile", "UISpriteSheet.png");
        RegisterGeneratedTemplate("MyButton", visual => new ButtonWithCustomVariable(visual));

        GraphicalUiElement created = ObjectFinder.Self.GetElementSave("MyButton")!
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        InteractiveGue visual = (InteractiveGue)created;
        visual.FormsControlAsObject.ShouldBeOfType<ButtonWithCustomVariable>()
            .OriginalSourceFile.ShouldBe("UISpriteSheet.png");

        CustomVariableHolder secondControl = new CustomVariableHolder();
        visual.FormsControlAsObject = secondControl;

        // The value was consumed by the first Forms control; it must not be replayed onto a later one.
        secondControl.OriginalSourceFile.ShouldBeNull();
    }

    // Anti-regression for rejected fix B (construct the Forms control before applying state).
    // SetGraphicalUiElement is what creates the child instances, so doing that would hand every
    // generated Forms class null child fields and run CustomInitialize against an empty visual.
    [Fact]
    public void FormsControlConstructedAfterStateApply_SeesChildInstances()
    {
        ComponentSave component = CreateComponentWithCustomVariable("MyButton", "OriginalSourceFile", "UISpriteSheet.png");
        component.Instances.Add(new InstanceSave
        {
            Name = "Background",
            BaseType = "Container",
            ParentContainer = component
        });

        RegisterGeneratedTemplate("MyButton", visual => new ButtonObservingVisual(visual));

        GraphicalUiElement created = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        ButtonObservingVisual forms = ((InteractiveGue)created).FormsControlAsObject
            .ShouldBeOfType<ButtonObservingVisual>();

        forms.BackgroundSeenWhenConstructed.ShouldNotBeNull();
    }

    // Anti-regression for rejected fix A (codegen bakes the authored value into the Forms
    // constructor as a literal). A baked literal is per-type, so it cannot express a per-instance
    // override.
    [Fact]
    public void InstanceOverride_OfCustomVariable_WinsOverComponentDefault()
    {
        ComponentSave component = CreateComponentWithCustomVariable("MyButton", "OriginalSourceFile", "component-default.png");

        ScreenSave screen = new ScreenSave { Name = "MyScreen" };
        screen.Instances.Add(new InstanceSave
        {
            Name = "ButtonInstance",
            BaseType = component.Name,
            ParentContainer = screen
        });
        StateSave screenState = new StateSave { Name = "Default", ParentContainer = screen };
        screenState.Variables.Add(new VariableSave
        {
            Name = "ButtonInstance.OriginalSourceFile",
            Value = "instance-override.png",
            Type = "string",
            IsCustomVariable = true,
            SetsValue = true
        });
        screen.States.Add(screenState);
        ObjectFinder.Self.GumProjectSave!.Screens.Add(screen);

        RegisterGeneratedTemplate("MyButton", visual => new ButtonWithCustomVariable(visual));

        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        InteractiveGue buttonVisual = (InteractiveGue)screenVisual.GetGraphicalUiElementByName("ButtonInstance")!;

        buttonVisual.FormsControlAsObject.ShouldBeOfType<ButtonWithCustomVariable>()
            .OriginalSourceFile.ShouldBe("instance-override.png");
    }

    [Fact]
    public void SetGraphicalUiElement_CustomVariableMatchingNoProperty_DoesNotThrow()
    {
        CreateComponentWithCustomVariable("MyButton", "NoSuchProperty", "ignored.png");
        RegisterGeneratedTemplate("MyButton", visual => new ButtonWithCustomVariable(visual));

        GraphicalUiElement created = ObjectFinder.Self.GetElementSave("MyButton")!
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        ((InteractiveGue)created).FormsControlAsObject.ShouldBeOfType<ButtonWithCustomVariable>()
            .OriginalSourceFile.ShouldBeNull();
    }

    [Fact]
    public void SetGraphicalUiElement_CustomVariableOnFormsControl_IsPopulated()
    {
        CreateComponentWithCustomVariable("MyButton", "OriginalSourceFile", "UISpriteSheet.png");
        RegisterGeneratedTemplate("MyButton", visual => new ButtonWithCustomVariable(visual));

        GraphicalUiElement created = ObjectFinder.Self.GetElementSave("MyButton")!
            .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        ((InteractiveGue)created).FormsControlAsObject.ShouldBeOfType<ButtonWithCustomVariable>()
            .OriginalSourceFile.ShouldBe("UISpriteSheet.png");
    }

    // Anti-regression for rejected fix A: under FindByName the loaded project is the runtime source
    // of truth, so editing the authored value must take effect without regenerating any C#. A value
    // baked into generated code would return the same string both times.
    [Fact]
    public void SetGraphicalUiElement_EditedElementSaveValue_IsReadPerInstantiation()
    {
        ComponentSave component = CreateComponentWithCustomVariable("MyButton", "OriginalSourceFile", "first.png");
        RegisterGeneratedTemplate("MyButton", visual => new ButtonWithCustomVariable(visual));

        GraphicalUiElement first = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        component.DefaultState.SetValue("OriginalSourceFile", "second.png", "string");
        GraphicalUiElement second = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);

        ((InteractiveGue)first).FormsControlAsObject.ShouldBeOfType<ButtonWithCustomVariable>()
            .OriginalSourceFile.ShouldBe("first.png");
        ((InteractiveGue)second).FormsControlAsObject.ShouldBeOfType<ButtonWithCustomVariable>()
            .OriginalSourceFile.ShouldBe("second.png");
    }

    [Fact]
    public void SetProperty_BuiltInVariable_IsNotForwardedToFormsControl()
    {
        ContainerRuntime visual = new ContainerRuntime();
        CustomVariableHolder holder = new CustomVariableHolder();
        visual.FormsControlAsObject = holder;

        visual.SetProperty("Width", 123f);

        visual.Width.ShouldBe(123f);
        holder.Width.ShouldBe(0f);
    }

    [Fact]
    public void SetProperty_UnmatchedVariableOnVisualWithoutFormsSupport_DoesNotThrow()
    {
        PlainVisual visual = new PlainVisual();

        Should.NotThrow(() => visual.SetProperty("OriginalSourceFile", "UISpriteSheet.png"));
    }

    [Fact]
    public void SetProperty_WhenVisualAndFormsBothDeclareName_PrefersVisual()
    {
        ContainerWithCustomVariable visual = new ContainerWithCustomVariable();
        CustomVariableHolder holder = new CustomVariableHolder();
        visual.FormsControlAsObject = holder;

        visual.SetProperty("OriginalSourceFile", "UISpriteSheet.png");

        visual.OriginalSourceFile.ShouldBe("UISpriteSheet.png");
        holder.OriginalSourceFile.ShouldBeNull();
    }
}
