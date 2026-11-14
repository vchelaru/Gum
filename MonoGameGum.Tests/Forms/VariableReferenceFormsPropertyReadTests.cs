using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A reference that reads a Forms-only property of an instance (Width = Slider.Minimum) in a project made from
/// the DarkPro theme: the property lives on the Forms control and is stored as an "Instance.Name" variable in
/// the owner's state, so the live visual cannot answer and the stored value (or nothing) is read. Pins the
/// current answers; see the 8024 model doc.
/// </summary>
public class VariableReferenceFormsPropertyReadTests : BaseTestClass
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

    private sealed class Built
    {
        public required GumProjectSave Project;
        public required ComponentSave Owner;
        public required GraphicalUiElement OwnerGue;
        public required GraphicalUiElement Target;
        public required GraphicalUiElement SliderGue;
        public Slider SliderControl => (Slider)(((InteractiveGue)SliderGue).FormsControlAsObject ?? throw new InvalidOperationException("No Forms control."));
    }

    // Owner "Custom/PropertySlider": a DarkPro Slider instance and a Container Target, with one row on Target.
    private static Built Build(bool useExpressions, double? storedMinimum, string row, (string Name, double Value)? extra = null)
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "DarkPro", "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _) ?? throw new InvalidOperationException("DarkPro project did not load.");
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;

        ComponentSave owner = new ComponentSave { Name = "Custom/PropertySlider", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = owner };
        owner.States.Add(state);
        owner.Instances.Add(new InstanceSave { Name = "Slider", BaseType = "DarkPro/Slider", ParentContainer = owner });
        owner.Instances.Add(new InstanceSave { Name = "Target", BaseType = "Container", ParentContainer = owner });
        if (storedMinimum != null)
        {
            state.Variables.Add(new VariableSave { Name = "Slider.Minimum", Type = "System.Double", Value = storedMinimum.Value, SetsValue = true });
        }
        if (extra != null)
        {
            state.Variables.Add(new VariableSave { Name = extra.Value.Name, Type = "System.Double", Value = extra.Value.Value, SetsValue = true });
        }
        VariableListSave<string> list = new VariableListSave<string> { Name = "Target.VariableReferences", Type = "string" };
        list.Value.Add(row);
        state.VariableLists.Add(list);
        project.Components.Add(owner);

        project.ApplyAllVariableReferences();
        StandardRuntimeRegistrations.Register();
        // Circle, Rectangle and Polygon need a rendering SystemManagers a headless test lacks; the references do not read them.
        foreach (string unrendered in new[] { "Circle", "Rectangle", "Polygon" })
        {
            ElementSaveExtensions.RegisterGueInstantiation(unrendered, () => new Gum.GueDeriving.ContainerRuntime());
        }
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        GraphicalUiElement ownerGue = owner.ToGraphicalUiElement();
        return new Built
        {
            Project = project,
            Owner = owner,
            OwnerGue = ownerGue,
            Target = ownerGue.GetGraphicalUiElementByName("Target")!,
            SliderGue = ownerGue.GetGraphicalUiElementByName("Slider")!
        };
    }


    [Fact]
    public void FirstApply_MinimumStoredInOwnerState_WithExpressions_TargetAndControlHoldTheStoredValue()
    {
        Built built = Build(true, 12.0, "Width = Slider.Minimum");

        built.Target.Width.ShouldBe(12f);
        built.SliderControl.Minimum.ShouldBe(12.0);
    }

    [Fact]
    public void FirstApply_MinimumStoredInOwnerState_WithoutExpressions_RowIsNotApplied()
    {
        Built built = Build(false, 12.0, "Width = Slider.Minimum");

        // pins current behavior; see model doc, inconsistency 4
        built.Target.Width.ShouldBe(150f);
        built.SliderControl.Minimum.ShouldBe(12.0);
    }

    [Fact]
    public void FirstApply_MaximumNeverStored_RowReadsTheControlsBehaviorDefault()
    {
        Built built = Build(true, null, "Width = Slider.Maximum");

        built.SliderControl.Maximum.ShouldBe(100.0);
        built.Target.Width.ShouldBe(100f);
    }

    [Fact]
    public void FirstApply_MinimumAuthoredByTheContainer_RowReadsWhatTheContainerAuthored()
    {
        // The behavior default is 0; the container authors 5 on its own Slider instance.
        Built built = Build(true, 5.0, "Width = Slider.Minimum");

        built.SliderControl.Minimum.ShouldBe(5.0);
        built.Target.Width.ShouldBe(5f);
    }

    [Fact]
    public void FirstApply_MinimumNeverStored_RowReadsTheBehaviorDefaultZero()
    {
        Built built = Build(true, null, "Width = Slider.Minimum + 7");

        built.Target.Width.ShouldBe(7f);
    }

    [Fact]
    public void Live_ValueRaisedByCodeAboveMaximumAndRefreshed_RowReadsTheClampedControlValue()
    {
        Built built = Build(true, 12.0, "Width = Slider.Value");

        built.SliderControl.Value = 500.0;
        built.OwnerGue.RefreshReferences("Slider.Value");

        built.SliderControl.Value.ShouldBe(100.0);
        built.Target.Width.ShouldBe(100f);
    }

    [Fact]
    public void ToolApply_MaximumNeverStored_WritesTheBehaviorDeclaredDefault()
    {
        Built built = Build(true, null, "Width = Slider.Maximum");
        built.Owner.DefaultState!.SetValue("Target.Width", 1f, "float");

        built.Owner.ApplyVariableReferences(built.Owner.DefaultState!);

        built.Owner.DefaultState!.GetValueRecursive("Target.Width").ShouldBe(100f);
    }

    [Fact]
    public void ToolApply_MinimumStoredAndDefaultDeclared_StoredValueWins()
    {
        Built built = Build(true, 12.0, "Width = Slider.Minimum");
        built.Owner.DefaultState!.SetValue("Target.Width", 1f, "float");

        built.Owner.ApplyVariableReferences(built.Owner.DefaultState!);

        built.Owner.DefaultState!.GetValueRecursive("Target.Width").ShouldBe(12f);
    }

    [Fact]
    public void FirstApply_MaximumNeverStored_WithoutExpressions_DoubleIntoFloatStaysRefused()
    {
        Built built = Build(false, null, "Width = Slider.Maximum");

        // The documented refusal of a double into a float without Gum.Expressions is unchanged.
        built.Target.Width.ShouldBe(150f);
    }

    [Fact]
    public void Live_MinimumSetByCodeOnTheControl_TargetStillHasTheStoredValue()
    {
        Built built = Build(true, 12.0, "Width = Slider.Minimum");

        built.SliderControl.Minimum = 5.0;

        // pins current behavior; see model doc, inconsistency 5
        built.Target.Width.ShouldBe(12f);
    }

    [Fact]
    public void Live_MinimumSetByCodeThenRefreshReferences_RowReadsTheControl()
    {
        Built built = Build(true, 12.0, "Width = Slider.Minimum");

        built.SliderControl.Minimum = 5.0;
        built.OwnerGue.RefreshReferences("Slider.Minimum");

        built.Target.Width.ShouldBe(5f);
    }

    [Fact]
    public void Live_MinimumSetByCodeThenAnotherInputChanges_RowReadsTheControl()
    {
        Built built = Build(true, 12.0, "Width = Slider.Minimum + Slider.Width");

        built.SliderControl.Minimum = 5.0;
        built.SliderGue.Width = 20;

        built.Target.Width.ShouldBe(25f);
    }

    [Fact]
    public void Live_MinimumNeverStoredSetByCodeThenRefreshed_RowReadsTheControl()
    {
        Built built = Build(true, null, "Width = Slider.Minimum");

        built.SliderControl.Minimum = 5.0;
        built.OwnerGue.RefreshReferences("Slider.Minimum");

        built.Target.Width.ShouldBe(5f);
    }

    [Fact]
    public void Live_MinimumWrittenThroughOwnerSetProperty_RowAndControlBothTakeIt()
    {
        Built built = Build(true, 12.0, "Width = Slider.Minimum");

        built.OwnerGue.SetProperty("Slider.Minimum", 7.0);

        built.Target.Width.ShouldBe(7f);
        built.SliderControl.Minimum.ShouldBe(7.0);
    }

    [Fact]
    public void Live_VisualPropertyOfTheSameInstance_FollowsTheVisual()
    {
        Built built = Build(true, 12.0, "Width = Slider.Width");

        built.SliderGue.Width = 61;

        built.Target.Width.ShouldBe(61f);
    }

    [Fact]
    public void ToolApply_MinimumStoredAndControlChanged_ReadsTheStoredValue()
    {
        Built built = Build(true, 12.0, "Width = Slider.Minimum");
        built.SliderControl.Minimum = 5.0;
        built.Owner.DefaultState!.SetValue("Target.Width", 1f, "float");

        built.Owner.ApplyVariableReferences(built.Owner.DefaultState!);

        built.Owner.DefaultState!.GetValueRecursive("Target.Width").ShouldBe(12f);
    }

    [Fact]
    public void ApplyAll_MinimumStored_WritesTheStoredValueToTheStateAndLeavesTheControl()
    {
        Built built = Build(true, 12.0, "Width = Slider.Minimum");
        built.SliderControl.Minimum = 5.0;
        built.Owner.DefaultState!.SetValue("Target.Width", 1f, "float");

        built.Project.ApplyAllVariableReferences();

        built.Owner.DefaultState!.GetValueRecursive("Target.Width").ShouldBe(12f);
        built.SliderControl.Minimum.ShouldBe(5.0);
    }

    [Fact]
    public void FirstApply_ValueStoredAboveMaximum_ControlClampsAndRowReadsTheClampedValue()
    {
        Built built = Build(true, 12.0, "Width = Slider.Value", extra: ("Slider.Value", 150.0));

        built.SliderControl.Value.ShouldBe(100.0);
        built.Target.Width.ShouldBe(100f);
    }

    [Fact]
    public void Live_MinimumRaisedByCodeAboveValue_ControlMovesValueButRowReadsTheStoredValue()
    {
        Built built = Build(true, 12.0, "Width = Slider.Value", extra: ("Slider.Value", 20.0));

        built.SliderControl.Minimum = 30.0;

        // pins current behavior; see model doc, inconsistency 6
        built.SliderControl.Value.ShouldBe(30.0);
        built.Target.Width.ShouldBe(20f);
    }

    [Fact]
    public void Live_MinimumSetByACategoryState_RowAndControlBothTakeIt()
    {
        Built built = Build(true, 12.0, "Width = Slider.Minimum");
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        StateSave state = new StateSave { Name = "Low", ParentContainer = built.Owner };
        state.Variables.Add(new VariableSave { Name = "Slider.Minimum", Type = "System.Double", Value = 3.0, SetsValue = true });
        category.States.Add(state);
        built.Owner.Categories.Add(category);
        GraphicalUiElement fresh = built.Owner.ToGraphicalUiElement();

        fresh.ApplyState("Cat", "Low");

        InteractiveGue slider = (InteractiveGue)fresh.GetGraphicalUiElementByName("Slider")!;
        fresh.GetGraphicalUiElementByName("Target")!.Width.ShouldBe(3f);
        ((Slider)slider.FormsControlAsObject!).Minimum.ShouldBe(3.0);
    }
}
