using System;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Covers <see cref="FormsUtilities.RegisterFromFileFormRuntimeDefaults"/> swapping the code-built
/// default tooltip visual for the project's own Tooltip-behavior component (issue #4856) - tooltips
/// are the one control the framework instantiates itself, so a from-file project never gets a
/// chance to pick the visual through an instance in a screen.
/// </summary>
public class FromFileTooltipTemplateTests : BaseTestClass
{
    private readonly VisualTemplate? _templateBefore;

    public FromFileTooltipTemplateTests()
    {
        FrameworkElement.DefaultFormsTemplates.TryGetValue(typeof(Tooltip), out _templateBefore);
    }

    public override void Dispose()
    {
        if (_templateBefore != null)
        {
            FrameworkElement.DefaultFormsTemplates[typeof(Tooltip)] = _templateBefore;
        }
        else
        {
            FrameworkElement.DefaultFormsTemplates.Remove(typeof(Tooltip));
        }
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static ComponentSave AddTooltipComponent(GumProjectSave project, string name)
    {
        // The standards must exist in the project for ObjectFinder to resolve the child instances.
        foreach (string standardName in new[] { "Container", "Text" })
        {
            if (project.StandardElements.Any(standard => standard.Name == standardName))
            {
                continue;
            }
            StandardElementSave standard = new StandardElementSave { Name = standardName };
            standard.States.Add(new StateSave { Name = "Default", ParentContainer = standard });
            project.StandardElements.Add(standard);
        }

        ComponentSave tooltipComponent = new ComponentSave { Name = name, BaseType = "Container" };
        tooltipComponent.States.Add(new StateSave { Name = "Default", ParentContainer = tooltipComponent });
        tooltipComponent.Instances.Add(new InstanceSave { Name = "TextInstance", BaseType = "Text", ParentContainer = tooltipComponent });
        tooltipComponent.Behaviors.Add(new ElementBehaviorReference { BehaviorName = StandardFormsBehaviorNames.TooltipBehaviorName });
        project.Components.Add(tooltipComponent);
        return tooltipComponent;
    }

    [Fact]
    public void Tooltip_WithATooltipBehaviorComponentInTheProject_UsesThatComponentAsItsVisual()
    {
        GumProjectSave project = new GumProjectSave();
        ComponentSave tooltipComponent = AddTooltipComponent(project, "Controls/Tooltip");
        ObjectFinder.Self.GumProjectSave = project;

        FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        Tooltip tooltip = new Tooltip();

        tooltip.Visual.ElementSave.ShouldBeSameAs(tooltipComponent);
        tooltip.Visual.GetGraphicalUiElementByName("TextInstance").ShouldNotBeNull();
        tooltip.Visual.FormsControlAsObject.ShouldBeSameAs(tooltip);
    }

    [Fact]
    public void Tooltip_WithSeveralTooltipComponents_UsesTheBehaviorsDefaultImplementation()
    {
        GumProjectSave project = new GumProjectSave();
        AddTooltipComponent(project, "Controls/Tooltip");
        ComponentSave darkTooltip = AddTooltipComponent(project, "Controls/TooltipDark");
        project.Behaviors.Add(new BehaviorSave
        {
            Name = StandardFormsBehaviorNames.TooltipBehaviorName,
            DefaultImplementation = "Controls/TooltipDark",
        });
        ObjectFinder.Self.GumProjectSave = project;

        FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        new Tooltip().Visual.ElementSave.ShouldBeSameAs(darkTooltip);
    }

    [Fact]
    public void Tooltip_WithATemplateRegisteredByUserCode_KeepsTheUserTemplate()
    {
        GumProjectSave project = new GumProjectSave();
        AddTooltipComponent(project, "Controls/Tooltip");
        ObjectFinder.Self.GumProjectSave = project;
        VisualTemplate userTemplate = new VisualTemplate((_, _) => new InteractiveGue());
        FrameworkElement.DefaultFormsTemplates[typeof(Tooltip)] = userTemplate;

        FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        FrameworkElement.DefaultFormsTemplates[typeof(Tooltip)].ShouldBeSameAs(userTemplate);
    }

    [Fact]
    public void Tooltip_WithNoTooltipBehaviorComponentInTheProject_KeepsTheDefaultTemplate()
    {
        GumProjectSave project = new GumProjectSave();
        ComponentSave plain = new ComponentSave { Name = "Plain", BaseType = "Container" };
        plain.States.Add(new StateSave { Name = "Default", ParentContainer = plain });
        project.Components.Add(plain);
        ObjectFinder.Self.GumProjectSave = project;

        FormsUtilities.RegisterFromFileFormRuntimeDefaults();

        FrameworkElement.DefaultFormsTemplates[typeof(Tooltip)].ShouldBeSameAs(_templateBefore);
    }
}
