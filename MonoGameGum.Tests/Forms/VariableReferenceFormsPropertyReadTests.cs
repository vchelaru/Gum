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
        public Slider SliderControl => (Slider)((InteractiveGue)SliderGue).FormsControlAsObject;
    }

    // Owner "Custom/PropertySlider": a DarkPro Slider instance and a Container Target, with one row on Target.
    private static Built Build(bool useExpressions, double? storedMinimum, string row)
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
        string gumx = Path.Combine(LocateRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "DarkPro", "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
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
        VariableListSave<string> list = new VariableListSave<string> { Name = "Target.VariableReferences", Type = "string" };
        list.Value.Add(row);
        state.VariableLists.Add(list);
        project.Components.Add(owner);

        project.ApplyAllVariableReferences();
        StandardRuntimeRegistrations.Register();
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

    // Pins current behavior; see model doc, inconsistencies 4 and 5 (cases 2, 3, 6, 8, 10).
    // One test: the static registrations RegisterFromFileFormRuntimeDefaults makes do not survive a second
    // test of this class (the second build throws "single pixel texture is not set"), so every scenario runs here.
    [Fact]
    public void ForwardedAndLiveReads_OfAFormsOnlyProperty_AreWhatTheModelDocSays()
    {
        List<string> results = new List<string>();

        Built stored = Build(true, 12.0, "Width = Slider.Minimum");
        results.Add("1 firstApplyStoredExpr target=" + stored.Target.Width + " control=" + stored.SliderControl.Minimum);
        stored.SliderControl.Minimum = 5.0;
        results.Add("2 controlSetAfterLoad target=" + stored.Target.Width);
        stored.OwnerGue.RefreshReferences("Slider.Minimum");
        results.Add("3 controlSetThenRefreshReferences target=" + stored.Target.Width);
        stored.OwnerGue.SetProperty("Slider.Minimum", 7.0);
        results.Add("4 setPropertyOnOwner target=" + stored.Target.Width + " control=" + stored.SliderControl.Minimum);
        stored.Owner.DefaultState!.SetValue("Target.Width", 1f, "float");
        stored.Owner.ApplyVariableReferences(stored.Owner.DefaultState!);
        results.Add("5 toolApplyStored target=" + stored.Owner.DefaultState!.GetValueRecursive("Target.Width"));

        Built plain = Build(false, 12.0, "Width = Slider.Minimum");
        results.Add("6 firstApplyStoredNoExpr target=" + plain.Target.Width + " control=" + plain.SliderControl.Minimum);

        Built never = Build(true, null, "Width = Slider.Minimum");
        results.Add("7 neverStored target=" + never.Target.Width + " control=" + never.SliderControl.Minimum);
        never.SliderControl.Minimum = 5.0;
        never.OwnerGue.RefreshReferences("Slider.Minimum");
        results.Add("8 neverStoredControlSet target=" + never.Target.Width);

        Built visual = Build(true, 12.0, "Width = Slider.Width");
        visual.SliderGue.Width = 61;
        results.Add("9 visualPropertyLive target=" + visual.Target.Width);

        Built mixed = Build(true, 12.0, "Width = Slider.Minimum + Slider.Width");
        mixed.SliderControl.Minimum = 5.0;
        mixed.SliderGue.Width = 20;
        results.Add("10 controlSetThenVisualInput target=" + mixed.Target.Width);

        string.Join("; ", results).ShouldBe("1 firstApplyStoredExpr target=12 control=12; 2 controlSetAfterLoad target=12; 3 controlSetThenRefreshReferences target=12; 4 setPropertyOnOwner target=7 control=7; 5 toolApplyStored target=12; 6 firstApplyStoredNoExpr target=150 control=12; 7 neverStored target=150 control=0; 8 neverStoredControlSet target=150; 9 visualPropertyLive target=61; 10 controlSetThenVisualInput target=32");
    }
}
