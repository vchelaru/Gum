using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class ElementSaveToFormsTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static ComponentSave CreateComponent(string name, bool registerButtonVisual)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });

        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(new StateSave { Name = "Default", ParentContainer = container });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;

        if (registerButtonVisual)
        {
            ElementSaveExtensions.RegisterGueInstantiation(name, () =>
            {
                Button button = new Button();
                return (GraphicalUiElement)button.Visual;
            });
        }

        return component;
    }

    [Fact]
    public void ToForms_ComponentWithFormsControl_ReturnsControl()
    {
        ComponentSave component = CreateComponent("MyButton", registerButtonVisual: true);

        FrameworkElement? forms = Gum.ElementSaveFormsExtensions.ToForms(component);

        forms.ShouldBeOfType<Button>();
    }

    [Fact]
    public void ToForms_ComponentWithoutFormsControl_ReturnsNull()
    {
        ComponentSave component = CreateComponent("PlainContainer", registerButtonVisual: false);

        FrameworkElement? forms = Gum.ElementSaveFormsExtensions.ToForms(component);

        forms.ShouldBeNull();
    }

    [Fact]
    public void ToFormsGeneric_MatchingType_ReturnsTypedControl()
    {
        ComponentSave component = CreateComponent("MyButton", registerButtonVisual: true);

        Button? button = Gum.ElementSaveFormsExtensions.ToForms<Button>(component);

        button.ShouldNotBeNull();
    }

    [Fact]
    public void ToFormsGeneric_MismatchedType_ReturnsNull()
    {
        ComponentSave component = CreateComponent("MyButton", registerButtonVisual: true);

        Label? label = Gum.ElementSaveFormsExtensions.ToForms<Label>(component);

        label.ShouldBeNull();
    }

    [Fact]
    public void ToForms_DoesNotAddVisualToManagers()
    {
        ComponentSave component = CreateComponent("MyButton", registerButtonVisual: true);

        FrameworkElement? forms = Gum.ElementSaveFormsExtensions.ToForms(component);

        forms!.Visual.Parent.ShouldBeNull();
        forms.Visual.Visible.ShouldBeTrue();
        forms.IsVisible.ShouldBeTrue();
        GumService.Default.Root.Children.ShouldNotContain(forms.Visual);
    }
}
