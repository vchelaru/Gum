using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// A live re-evaluation of a row that reads a Forms-only property (read from the Forms control) must cost
/// about what a row that reads a visual property costs: a game runs it every time an input changes.
/// </summary>
public class FormsPropertyReferenceAllocationTests : BaseTestClass
{
    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static string LocateRepoRoot()
    {
        string? directory = AppContext.BaseDirectory;
        while (directory != null && !File.Exists(Path.Combine(directory, "GumFull.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        return directory ?? throw new InvalidOperationException("Repo root not found.");
    }

    private static (GraphicalUiElement Slider, GraphicalUiElement Target) Build(string row)
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        GumExpressionService.Initialize();
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "DarkPro", "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave owner = new ComponentSave { Name = "Custom/AllocSlider", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = owner };
        owner.States.Add(state);
        owner.Instances.Add(new InstanceSave { Name = "Slider", BaseType = "DarkPro/Slider", ParentContainer = owner });
        owner.Instances.Add(new InstanceSave { Name = "Target", BaseType = "Container", ParentContainer = owner });
        VariableListSave<string> list = new VariableListSave<string> { Name = "Target.VariableReferences", Type = "string" };
        list.Value.Add(row);
        state.VariableLists.Add(list);
        project.Components.Add(owner);

        StandardRuntimeRegistrations.Register();
        foreach (string unrendered in new[] { "Circle", "Rectangle", "Polygon" })
        {
            ElementSaveExtensions.RegisterGueInstantiation(unrendered, () => new Gum.GueDeriving.ContainerRuntime());
        }
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        GraphicalUiElement ownerGue = owner.ToGraphicalUiElement();
        return (ownerGue.GetGraphicalUiElementByName("Slider")!, ownerGue.GetGraphicalUiElementByName("Target")!);
    }

    private static double Measure(string row)
    {
        (GraphicalUiElement slider, GraphicalUiElement target) = Build(row);
        bool toggle = false;
        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                slider.Width = toggle ? 10 : 20;
                toggle = !toggle;
            },
            attempts: 3,
            warmupIterations: 50,
            measuredIterations: 500);
        target.Width.ShouldNotBe(0f);
        return result.BytesPerIteration;
    }

    [Fact]
    public void LiveUpdate_RowReadingAFormsProperty_AllocatesNoMoreThanAVisualRowPlusTheBoxedValue()
    {
        double visual = Measure("Width = Slider.Width + Slider.Height");
        double forms = Measure("Width = Slider.Width + Slider.Maximum");

        // Both rows seed one extra input; the Forms one also boxes the double it reads.
        forms.ShouldBeLessThanOrEqualTo(visual + 48, $"visual row {visual} bytes per frame, Forms row {forms}");
    }

    private static double MeasureControlSetter(string row)
    {
        (GraphicalUiElement slider, GraphicalUiElement target) = Build(row);
        Slider control = (Slider)((InteractiveGue)slider).FormsControlAsObject!;
        bool toggle = false;
        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                control.Maximum = toggle ? 50 : 60;
                toggle = !toggle;
            },
            attempts: 3,
            warmupIterations: 50,
            measuredIterations: 500);
        target.Width.ShouldNotBe(0f);
        return result.BytesPerIteration;
    }

    [Fact]
    public void ControlSetter_SameValueAsBefore_AllocatesNothingEvenWhenARowReadsIt()
    {
        (GraphicalUiElement slider, _) = Build("Width = Slider.Minimum + Slider.Height");
        Slider control = (Slider)((InteractiveGue)slider).FormsControlAsObject!;
        control.Minimum = 5;
        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () => control.Minimum = 5,
            attempts: 3,
            warmupIterations: 50,
            measuredIterations: 500);

        result.BytesPerIteration.ShouldBe(0);
    }

    [Fact]
    public void ControlSetter_OnlyARowReadingLaidOutValuesExists_AllocatesNothing()
    {
        MeasureControlSetter("Width = Slider.AbsoluteWidth").ShouldBe(0);
    }

    [Fact]
    public void ControlSetter_NoRowReadsTheProperty_AllocatesNothing()
    {
        MeasureControlSetter("Width = Slider.Height").ShouldBe(0);
    }

    [Fact]
    public void ControlSetter_RowReadsTheProperty_AllocatesNoMoreThanASetterOfAVisualPropertyPlusTheBoxedValue()
    {
        double visual = Measure("Width = Slider.Width + Slider.Height");
        double forms = MeasureControlSetter("Width = Slider.Maximum + Slider.Height");

        forms.ShouldBeLessThanOrEqualTo(visual + 128, $"visual setter {visual} bytes per call, Forms setter {forms}");
    }
}

