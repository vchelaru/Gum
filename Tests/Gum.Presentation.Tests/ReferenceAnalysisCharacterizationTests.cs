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

namespace Gum.Presentation.Tests;

/// <summary>
/// How the places that analyze variable-reference rows order and find them
/// (<see cref="ElementSaveExtensions.ApplyAllVariableReferences"/>, <see cref="ObjectFinder.GetElementReferencesToThis"/>
/// and the tool's <see cref="VariableReferenceLogic.ApplyReferencesToElement"/>). They share one
/// analysis, so they agree on which rows read which elements and in what order rows run (#5920).
/// </summary>
public class ReferenceAnalysisCharacterizationTests : BaseTestClass
{
    private readonly GumProjectSave _project;

    public ReferenceAnalysisCharacterizationTests()
    {
        GumExpressionService.Initialize();
        _project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _project;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ElementSaveExtensions.VariableChangedThroughReference = null;
        base.Dispose();
    }

    #region ApplyAllVariableReferences

    [Fact]
    public void ApplyAll_ChainListedInReverse_AppliesSourcesFirst()
    {
        // Components are listed C, B, A though C reads B and B reads A.
        ComponentSave a = AddComponent("A", Vars(("V", 10f)));
        ComponentSave b = AddComponent("B", Vars(("V", 0f)), "V = Components/A.V * 2");
        ComponentSave c = AddComponent("C", Vars(("V", 0f)), "V = Components/B.V + 1");
        _project.Components.Reverse();

        _project.ApplyAllVariableReferences();

        b.DefaultState!.GetValue("V").ShouldBe(20f);
        c.DefaultState!.GetValue("V").ShouldBe(21f);
    }

    [Fact]
    public void ApplyAll_ScreenReadingComponentReadingStandard_AppliesStandardFirst()
    {
        AddStandard("Circle", Vars(("V", 5f)));
        ComponentSave part = AddComponent("Part", Vars(("V", 0f)), "V = Standards/Circle.V + 1");
        ScreenSave screen = new ScreenSave { Name = "Main" };
        StateSave screenState = AddState(screen, Vars(("V", 0f)), "V = Components/Part.V + 1");
        _project.Screens.Add(screen);

        _project.ApplyAllVariableReferences();

        part.DefaultState!.GetValue("V").ShouldBe(6f);
        screenState.GetValue("V").ShouldBe(7f);
    }

    [Fact]
    public void ApplyAll_RowsInOneElement_AreAppliedInDependencyOrder()
    {
        // Y reads Offset, which the next row sets, so the Offset row runs first.
        ComponentSave wave = AddComponent("Wave", Vars(("Progress", 3f), ("Offset", 0f), ("Y", 0f)),
            "Y = Offset + 1", "Offset = Progress * 10");

        _project.ApplyAllVariableReferences();

        wave.DefaultState!.GetValue("Offset").ShouldBe(30f);
        wave.DefaultState!.GetValue("Y").ShouldBe(31f);
    }

    [Fact]
    public void ApplyAll_RowReadingTwoElements_OrdersAfterEveryElementItReads()
    {
        // Z reads C and B, and B reads C, so B runs before Z.
        AddComponent("C", Vars(("V", 10f)));
        ComponentSave z = AddComponent("Z", Vars(("V", 0f)), "V = Components/C.V + Components/B.V");
        ComponentSave b = AddComponent("B", Vars(("V", 0f)), "V = Components/C.V * 2");

        _project.ApplyAllVariableReferences();

        b.DefaultState!.GetValue("V").ShouldBe(20f);
        z.DefaultState!.GetValue("V").ShouldBe(30f);
    }

    [Fact]
    public void ApplyAll_RowOnAnInstance_CountsAsADependency()
    {
        AddComponent("Source", Vars(("W", 40f)));
        ComponentSave host = new ComponentSave { Name = "Host" };
        host.Instances.Add(new InstanceSave { Name = "Box", BaseType = "Standards/Circle", ParentContainer = host });
        StateSave hostState = AddState(host, Vars(("Box.Width", 0f)));
        VariableListSave<string> rows = new VariableListSave<string> { Type = "string", Name = "Box.VariableReferences" };
        rows.Value.Add("Width = Components/Source.W");
        hostState.VariableLists.Add(rows);
        // Listed before Source, so it only gets the value if the instance row counts as a dependency.
        _project.Components.Insert(0, host);

        _project.ApplyAllVariableReferences();

        hostState.GetValue("Box.Width").ShouldBe(40f);
    }

    [Fact]
    public void ApplyAll_TwoElementsReadingEachOther_AreAppliedInProjectOrder()
    {
        ComponentSave a = AddComponent("A", Vars(("V", 0f)), "V = Components/B.V + 1");
        ComponentSave b = AddComponent("B", Vars(("V", 0f)), "V = Components/A.V + 1");

        _project.ApplyAllVariableReferences();

        a.DefaultState!.GetValue("V").ShouldBe(1f);
        b.DefaultState!.GetValue("V").ShouldBe(2f);
    }

    [Fact]
    public void ApplyAll_RowReadingItsOwnElementThroughTheQualifiedName_IsNotACrossElementDependency()
    {
        ComponentSave a = AddComponent("A", Vars(("V", 4f), ("W", 0f)), "W = Components/A.V * 2");

        _project.ApplyAllVariableReferences();

        a.DefaultState!.GetValue("W").ShouldBe(8f);
    }

    #endregion

    #region GetElementReferencesToThis

    [Fact]
    public void GetElementReferencesToThis_RowStartingWithTheElementName_IsAReference()
    {
        ComponentSave target = AddComponent("Styles/Colors", Vars(("Red", 1f)));
        ComponentSave reader = AddComponent("Button", Vars(("Red", 0f)), "Red = Components/Styles/Colors.Red");

        ReferencingOwners(target).ShouldBe(new[] { reader });
    }

    [Fact]
    public void GetElementReferencesToThis_ElementReadAfterAnOperator_IsAReference()
    {
        ComponentSave target = AddComponent("Styles/Colors", Vars(("Red", 1f)));
        ComponentSave reader = AddComponent("Button", Vars(("Red", 0f)), "Red = 1 + Components/Styles/Colors.Red");

        ReferencingOwners(target).ShouldBe(new[] { reader });
    }

    [Fact]
    public void GetElementReferencesToThis_ElementWhoseNameStartsWithTheTargetName_IsNotAReference()
    {
        // "Components/ButtonGroup.X" reads ButtonGroup, not Button.
        ComponentSave target = AddComponent("Button", Vars(("X", 1f)));
        AddComponent("ButtonGroup", Vars(("X", 2f)));
        AddComponent("Reader", Vars(("X", 0f)), "X = Components/ButtonGroup.X");

        ReferencingOwners(target).ShouldBeEmpty();
    }

    [Fact]
    public void GetElementReferencesToThis_RowOnACategoryState_IsAReference()
    {
        ComponentSave target = AddComponent("Styles/Colors", Vars(("Red", 1f)));
        ComponentSave reader = AddComponent("Button", Vars(("Red", 0f)));
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        StateSave hover = new StateSave { Name = "Hover", ParentContainer = reader };
        VariableListSave<string> rows = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        rows.Value.Add("Red = Components/Styles/Colors.Red");
        hover.VariableLists.Add(rows);
        category.States.Add(hover);
        reader.Categories.Add(category);

        ReferencingOwners(target).ShouldBe(new[] { reader });
    }

    #endregion

    #region Tool propagation

    [Fact]
    public void ApplyReferencesToElement_ChangedElement_UpdatesEveryDownstreamReaderInDependencyOrder()
    {
        // C reads B and B reads A. After A changes, one call updates B and then C.
        ComponentSave a = AddComponent("A", Vars(("V", 10f)));
        ComponentSave b = AddComponent("B", Vars(("V", 0f)), "V = Components/A.V * 2");
        ComponentSave c = AddComponent("C", Vars(("V", 0f)), "V = Components/B.V + 1");
        VariableReferenceLogic sut = CreateLogic();

        sut.ApplyReferencesToElement(a, trySave: false);

        b.DefaultState!.GetValue("V").ShouldBe(20f);
        c.DefaultState!.GetValue("V").ShouldBe(21f);
    }

    [Fact]
    public void ApplyReferencesToElement_ReaderOfTwoElementsThatBothChange_SeesBothNewValues()
    {
        // D reads A and B, and B reads A. D has to run after B.
        ComponentSave a = AddComponent("A", Vars(("V", 10f)));
        ComponentSave d = AddComponent("D", Vars(("V", 0f)), "V = Components/A.V + Components/B.V");
        AddComponent("B", Vars(("V", 0f)), "V = Components/A.V * 2");
        VariableReferenceLogic sut = CreateLogic();

        sut.ApplyReferencesToElement(a, trySave: false);

        d.DefaultState!.GetValue("V").ShouldBe(30f);
    }

    [Fact]
    public void ApplyReferencesToElement_ElementsReadingEachOther_Finishes()
    {
        ComponentSave a = AddComponent("A", Vars(("V", 1f)), "V = Components/B.V + 1");
        AddComponent("B", Vars(("V", 0f)), "V = Components/A.V + 1");
        VariableReferenceLogic sut = CreateLogic();

        Should.NotThrow(() => sut.ApplyReferencesToElement(a, trySave: false));
    }

    [Fact]
    public void ApplyReferencesToElement_ChangedValue_NotifiesOncePerReaderVariable()
    {
        ComponentSave a = AddComponent("A", Vars(("V", 10f)));
        ComponentSave b = AddComponent("B", Vars(("V", 0f)), "V = Components/A.V");
        List<(ElementSave Element, string Variable)> notifications = new List<(ElementSave, string)>();
        ElementSaveExtensions.VariableChangedThroughReference = (element, _, name, _, _) =>
            notifications.Add((element, name));
        VariableReferenceLogic sut = CreateLogic();

        sut.ApplyReferencesToElement(a, trySave: false);

        notifications.ShouldBe(new[] { ((ElementSave)b, "V") });
    }

    #endregion

    #region Helpers

    private static VariableReferenceLogic CreateLogic()
    {
        Mock<ICompositeMemberRegistry> registry = new Mock<ICompositeMemberRegistry>();
        registry.Setup(x => x.Descriptors).Returns(new List<CompositeMemberDescriptor>());
        return new VariableReferenceLogic(
            new Mock<IGuiCommands>().Object,
            new Mock<IWireframeCommands>().Object,
            new Mock<IDialogService>().Object,
            new Mock<IFileCommands>().Object,
            registry.Object,
            new Mock<IDispatcher>().Object,
            new Mock<IWireframeObjectManager>().Object);
    }

    private static (string Name, object Value)[] Vars(params (string Name, object Value)[] variables) => variables;

    private List<ComponentSave> ReferencingOwners(ComponentSave target)
    {
        return ObjectFinder.Self.GetElementReferencesToThis(target)
            .Where(item => item.ReferenceType == ReferenceType.VariableReference)
            .Select(item => (ComponentSave)item.OwnerOfReferencingObject!)
            .Distinct()
            .ToList();
    }

    private ComponentSave AddComponent(string name, (string Name, object Value)[] variables, params string[] rows)
    {
        ComponentSave component = new ComponentSave { Name = name };
        AddState(component, variables, rows);
        _project.Components.Add(component);
        return component;
    }

    private void AddStandard(string name, (string Name, object Value)[] variables)
    {
        StandardElementSave standard = new StandardElementSave { Name = name };
        AddState(standard, variables);
        _project.StandardElements.Add(standard);
    }

    private static StateSave AddState(ElementSave element, (string Name, object Value)[] variables, params string[] rows)
    {
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        foreach ((string name, object value) in variables)
        {
            state.Variables.Add(new VariableSave { Name = name, Value = value, Type = "float", SetsValue = true });
        }
        if (rows.Length > 0)
        {
            VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
            list.Value.AddRange(rows);
            state.VariableLists.Add(list);
        }
        return state;
    }

    #endregion
}
