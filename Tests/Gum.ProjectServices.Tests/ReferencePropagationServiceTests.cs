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
}
