using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

public class ReferencePropagationServiceTests : IDisposable
{
    private readonly ReferencePropagationService _sut;

    public ReferencePropagationServiceTests()
    {
        StandardElementsManager.Self.Initialize();
        _sut = new ReferencePropagationService();
    }

    public void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }

    // Use variable-to-variable RHS so the test doesn't need Gum.Expressions wired —
    // RecursiveVariableFinder (the fallback evaluator) resolves dot-paths natively.
    private static ComponentSave BuildComponentWithUnpropagatedReference(string componentName)
    {
        ComponentSave component = new ComponentSave { Name = componentName };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);

        // The RHS source variable lives on the same state.
        state.Variables.Add(new VariableSave
        {
            Name = "SourceX",
            Type = "float",
            Value = 100f,
            SetsValue = true
        });

        VariableListSave<string> refs = new VariableListSave<string>
        {
            Name = "VariableReferences",
            Type = "string"
        };
        refs.Value.Add("X = SourceX");
        state.VariableLists.Add(refs);

        return component;
    }

    private static ComponentSave BuildCleanComponent(string componentName)
    {
        ComponentSave component = BuildComponentWithUnpropagatedReference(componentName);
        // Add the materialized scalar that ApplyVariableReferences would have written.
        component.DefaultState.Variables.Add(new VariableSave
        {
            Name = "X",
            Type = "float",
            Value = 100f,
            SetsValue = true
        });
        return component;
    }

    // The type Red/Green/Blue-composite components derive from. Its own DefaultState is what
    // tells the composite-channel guard the channels are real - a real ColoredRectangle-derived
    // component normally doesn't redeclare inherited scalars on its own state unless overridden,
    // so the schema (channel existence) and the materialized value live on different states.
    private static ComponentSave BuildColorTypeComponent()
    {
        ComponentSave colorType = new ComponentSave { Name = "ColorType" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = colorType };
        state.Variables.Add(new VariableSave { Name = "Red", Type = "int", Value = 0, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Green", Type = "int", Value = 0, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Blue", Type = "int", Value = 0, SetsValue = true });
        colorType.States.Add(state);
        return colorType;
    }

    // "Color = Source.Color" is a collapsed composite reference - Red/Green/Blue are the real
    // materialized scalars. materializeChannels controls whether this component's OWN state
    // already records them (simulating post-propagation) or not (pre-propagation); either way
    // BuildColorTypeComponent (added separately via BaseType) is what makes the channels real.
    private static ComponentSave BuildComponentWithCollapsedColorReference(string componentName, bool materializeChannels)
    {
        ComponentSave component = new ComponentSave { Name = componentName, BaseType = "ColorType" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);

        if (materializeChannels)
        {
            state.Variables.Add(new VariableSave { Name = "Red", Type = "int", Value = 10, SetsValue = true });
            state.Variables.Add(new VariableSave { Name = "Green", Type = "int", Value = 20, SetsValue = true });
            state.Variables.Add(new VariableSave { Name = "Blue", Type = "int", Value = 30, SetsValue = true });
        }

        state.Variables.Add(new VariableSave { Name = "Source.Red", Type = "int", Value = 10, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Source.Green", Type = "int", Value = 20, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Source.Blue", Type = "int", Value = 30, SetsValue = true });

        VariableListSave<string> refs = new VariableListSave<string>
        {
            Name = "VariableReferences",
            Type = "string"
        };
        refs.Value.Add("Color = Source.Color");
        state.VariableLists.Add(refs);

        return component;
    }

    [Fact]
    public void Detect_ProjectWithUnpropagatedComponent_ReportsThatComponent()
    {
        GumProjectSave project = new GumProjectSave();
        ComponentSave bad = BuildComponentWithUnpropagatedReference("BadComponent");
        project.Components.Add(bad);
        ObjectFinder.Self.GumProjectSave = project;

        DetectUnpropagatedReferencesResult result = _sut.Detect(project);

        result.HasUnpropagatedReferences.ShouldBeTrue();
        result.Elements.Count.ShouldBe(1);
        result.Elements[0].Element.ShouldBe(bad);
        result.Elements[0].States.Count.ShouldBe(1);
    }

    [Fact]
    public void Detect_ProjectWithOnlyCleanElements_ReportsNothing()
    {
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(BuildCleanComponent("CleanComponent"));
        ObjectFinder.Self.GumProjectSave = project;

        DetectUnpropagatedReferencesResult result = _sut.Detect(project);

        result.HasUnpropagatedReferences.ShouldBeFalse();
        result.Elements.ShouldBeEmpty();
    }

    [Fact]
    public void Detect_MixedProject_ReportsOnlyTheBadOnes()
    {
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(BuildCleanComponent("Clean1"));
        ComponentSave bad = BuildComponentWithUnpropagatedReference("Bad1");
        project.Components.Add(bad);
        project.Components.Add(BuildCleanComponent("Clean2"));
        ObjectFinder.Self.GumProjectSave = project;

        DetectUnpropagatedReferencesResult result = _sut.Detect(project);

        result.Elements.Count.ShouldBe(1);
        result.Elements[0].Element.Name.ShouldBe("Bad1");
    }

    [Fact]
    public void PropagateReferences_FillsMissingScalarsAndReturnsModifiedElement()
    {
        GumProjectSave project = new GumProjectSave();
        ComponentSave bad = BuildComponentWithUnpropagatedReference("BadComponent");
        project.Components.Add(bad);
        ObjectFinder.Self.GumProjectSave = project;

        IReadOnlyList<ElementSave> modified = _sut.PropagateReferences(project);

        modified.Count.ShouldBe(1);
        modified[0].ShouldBe(bad);

        // After propagation the scalar should be materialized so a re-detect is clean.
        bad.DefaultState.GetValue("X").ShouldBe(100f);
        _sut.Detect(project).HasUnpropagatedReferences.ShouldBeFalse();
    }

    [Fact]
    public void PropagateReferences_CleanProject_ReturnsEmptyList()
    {
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(BuildCleanComponent("CleanComponent"));
        ObjectFinder.Self.GumProjectSave = project;

        IReadOnlyList<ElementSave> modified = _sut.PropagateReferences(project);

        modified.ShouldBeEmpty();
    }

    [Fact]
    public void Detect_ComponentWithMaterializedCollapsedColorReference_ReportsClean()
    {
        // Red/Green/Blue are already materialized (10/20/30) - the collapsed "Color = ..."
        // row must not be treated as unpropagated just because no literal "Color" scalar exists.
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(BuildColorTypeComponent());
        ComponentSave component = BuildComponentWithCollapsedColorReference("ColorComponent", materializeChannels: true);
        project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = project;

        DetectUnpropagatedReferencesResult result = _sut.Detect(project);

        result.HasUnpropagatedReferences.ShouldBeFalse();
    }

    [Fact]
    public void PropagateReferences_CollapsedColorReference_MaterializesAllThreeChannels()
    {
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(BuildColorTypeComponent());
        ComponentSave component = BuildComponentWithCollapsedColorReference("ColorComponent", materializeChannels: false);
        project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = project;

        IReadOnlyList<ElementSave> modified = _sut.PropagateReferences(project);

        modified.Count.ShouldBe(1);
        component.DefaultState.GetValue("Red").ShouldBe(10);
        component.DefaultState.GetValue("Green").ShouldBe(20);
        component.DefaultState.GetValue("Blue").ShouldBe(30);
    }
}
