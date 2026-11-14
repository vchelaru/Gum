using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// A reference whose right side has no value right now (<c>100 / Source.Width</c> with a width of 0) keeps the
/// variable's last value and is reported as a note on its row (#7512).
/// </summary>
public class VariableReferenceStatusLogicTests : BaseTestClass
{
    private readonly VariableReferenceLogic _logic;
    private readonly VariableReferenceStatusLogic _status = new VariableReferenceStatusLogic();
    private readonly ComponentSave _source;
    private readonly ComponentSave _reader;

    public VariableReferenceStatusLogicTests()
    {
        GumExpressionService.Initialize();
        StandardElementsManager.Self.Initialize();
        Mock<ICompositeMemberRegistry> registry = new Mock<ICompositeMemberRegistry>();
        registry.Setup(x => x.Descriptors).Returns(new List<CompositeMemberDescriptor>());
        _logic = new VariableReferenceLogic(
            new Mock<IGuiCommands>().Object,
            new Mock<IWireframeCommands>().Object,
            new Mock<IDialogService>().Object,
            new Mock<IFileCommands>().Object,
            registry.Object,
            new Mock<IDispatcher>().Object,
            new Mock<IWireframeObjectManager>().Object);

        GumProjectSave project = new GumProjectSave();
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container")!.Clone());
        container.DefaultState!.ParentContainer = container;
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;

        _source = AddComponent(project, "Source", 10f);
        _reader = AddComponent(project, "Reader", 1f);
        _reader.Instances.Add(new InstanceSave { Name = "Inner", BaseType = "Container", ParentContainer = _reader });
        _reader.DefaultState!.Variables.Add(new VariableSave { Name = "Inner.Width", Type = "float", Value = 1f, SetsValue = true });
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ElementSaveExtensions.VariableChangedThroughReference = null;
        base.Dispose();
    }

    private static ComponentSave AddComponent(GumProjectSave project, string name, float width)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.DefaultState!.Variables.Add(new VariableSave { Name = "Width", Type = "float", Value = width, SetsValue = true });
        project.Components.Add(component);
        return component;
    }

    private static void SetRows(StateSave state, string? instanceName, params string[] rows)
    {
        VariableListSave<string> list = new VariableListSave<string>
        {
            Type = "string",
            Name = string.IsNullOrEmpty(instanceName) ? "VariableReferences" : instanceName + ".VariableReferences"
        };
        list.Value.AddRange(rows);
        state.VariableLists.Add(list);
    }

    private static void SetSourceWidth(ComponentSave source, float width) =>
        source.DefaultState!.Variables.First(v => v.Name == "Width").Value = width;

    [Fact]
    public void ApplyReferencesToElement_SourceInAnotherComponentChangedWhileReaderIsNotOpen_ReReadsTheReader()
    {
        SetRows(_reader.DefaultState!, null, "Width = 100 / Components/Source.Width");
        SetSourceWidth(_source, 4f);
        _logic.ApplyReferencesToElement(_source, false);

        _reader.DefaultState!.GetVariableSave("Width")!.Value.ShouldBe(25f);
    }

    [Fact]
    public void ApplyReferencesToElement_SourceBecomesZero_ReaderKeepsItsLastValue()
    {
        SetRows(_reader.DefaultState!, null, "Width = 100 / Components/Source.Width");
        SetSourceWidth(_source, 4f);
        _logic.ApplyReferencesToElement(_source, false);

        SetSourceWidth(_source, 0f);
        _logic.ApplyReferencesToElement(_source, false);

        _reader.DefaultState!.GetVariableSave("Width")!.Value.ShouldBe(25f);
    }

    [Fact]
    public void GetNote_SourceInAnotherComponentIsZero_NamesTheDivisor()
    {
        SetRows(_reader.DefaultState!, null, "Width = 100 / Components/Source.Width");
        SetSourceWidth(_source, 0f);

        _status.GetNote("Width", null, _reader, _reader.DefaultState!)
            .ShouldBe("Not updating: Components/Source.Width is 0, so the reference can't divide by it.");
    }

    [Fact]
    public void GetNote_SourceLaterBecomesValid_ClearsTheNote()
    {
        SetRows(_reader.DefaultState!, null, "Width = 100 / Components/Source.Width");
        SetSourceWidth(_source, 0f);
        _status.GetNote("Width", null, _reader, _reader.DefaultState!).ShouldNotBeNull();

        SetSourceWidth(_source, 5f);

        _status.GetNote("Width", null, _reader, _reader.DefaultState!).ShouldBeNull();
    }

    [Fact]
    public void GetNote_InstanceRowReadsASiblingThatIsZero_NamesTheSibling()
    {
        _reader.Instances.Add(new InstanceSave { Name = "Other", BaseType = "Container", ParentContainer = _reader });
        _reader.DefaultState!.Variables.Add(new VariableSave { Name = "Other.Width", Type = "float", Value = 0f, SetsValue = true });
        SetRows(_reader.DefaultState!, "Inner", "Width = 100 / Other.Width");

        _status.GetNote("Width", _reader.GetInstance("Inner"), _reader, _reader.DefaultState!)
            .ShouldBe("Not updating: Other.Width is 0, so the reference can't divide by it.");
    }

    [Fact]
    public void GetNote_OtherVariableOnTheSameStateHasNoNote()
    {
        SetRows(_reader.DefaultState!, null, "Width = 100 / Components/Source.Width");
        SetSourceWidth(_source, 0f);

        _status.GetNote("Height", null, _reader, _reader.DefaultState!).ShouldBeNull();
    }

    [Fact]
    public void GetNote_ReferenceEvaluates_IsNull()
    {
        SetRows(_reader.DefaultState!, null, "Width = 100 / Components/Source.Width");

        _status.GetNote("Width", null, _reader, _reader.DefaultState!).ShouldBeNull();
    }

    [Fact]
    public void GetNote_CommentedOutLine_IsNull()
    {
        SetRows(_reader.DefaultState!, null, "//Width = 100 / Components/Source.Width");
        SetSourceWidth(_source, 0f);

        _status.GetNote("Width", null, _reader, _reader.DefaultState!).ShouldBeNull();
    }

    [Fact]
    public void GetNote_RightSideReadsAnUnknownInstance_NamesThePath()
    {
        SetRows(_reader.DefaultState!, null, "Width = 100 / Ghost.Width");

        _status.GetNote("Width", null, _reader, _reader.DefaultState!)
            .ShouldBe("Not updating: it reads \"Ghost.Width\", which Reader does not have.");
    }

    [Fact]
    public void GetNote_RightSideReadsAnUnknownVariableOfAnInstance_NamesThePath()
    {
        SetRows(_reader.DefaultState!, null, "Width = 100 / Inner.Nope");

        _status.GetNote("Width", null, _reader, _reader.DefaultState!)
            .ShouldBe("Not updating: it reads \"Inner.Nope\", which Reader does not have.");
    }
}