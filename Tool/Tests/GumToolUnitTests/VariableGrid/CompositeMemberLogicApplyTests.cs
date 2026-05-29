using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;
using ToolsUtilities;
using WpfDataUi.DataTypes;
using Xunit;

namespace GumToolUnitTests.VariableGrid;

public class CompositeMemberLogicApplyTests : BaseTestClass
{
    private readonly GumProjectSave _project;
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IExposeVariableService> _exposeVariableService;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IUndoManager> _undoManager;
    private readonly UndoManager _realUndoManager;
    private readonly CompositeMemberLogic _logic;

    public CompositeMemberLogicApplyTests()
    {
        _project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _project;

        _selectedState = new Mock<ISelectedState>();
        _exposeVariableService = new Mock<IExposeVariableService>();
        _guiCommands = new Mock<IGuiCommands>();

        _realUndoManager = new UndoManager(null!, null!, null!, null!, null!, null!);
        _undoManager = new Mock<IUndoManager>();
        _undoManager.Setup(x => x.RequestLock()).Returns(() => _realUndoManager.RequestLock());

        _logic = new CompositeMemberLogic(
            _selectedState.Object,
            _exposeVariableService.Object,
            _undoManager.Object,
            _guiCommands.Object,
            ObjectFinder.Self,
            new CompositeMemberRegistry());
    }

    private ComponentSave MakeColorComponent()
    {
        ComponentSave component = new() { Name = "Colored" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.DefaultState.Variables.Add(new VariableSave { Name = "Red", Type = "int", Value = 0 });
        component.DefaultState.Variables.Add(new VariableSave { Name = "Green", Type = "int", Value = 0 });
        component.DefaultState.Variables.Add(new VariableSave { Name = "Blue", Type = "int", Value = 0 });
        _project.Components.Add(component);
        return component;
    }

    private static MemberCategory CategoryWith(params InstanceMember[] members)
    {
        MemberCategory category = new() { Name = "Rendering" };
        foreach (InstanceMember member in members)
        {
            category.Members.Add(member);
        }
        return category;
    }

    [Fact]
    public void Apply_ShouldCollapsePlainColor_RemovingChannelsAndAddingComposite()
    {
        ComponentSave component = MakeColorComponent();
        MemberCategory category = CategoryWith(
            new InstanceMember("Red", null!),
            new InstanceMember("Green", null!),
            new InstanceMember("Blue", null!));
        List<MemberCategory> categories = new() { category };

        _logic.Apply(categories, component, instance: null);

        category.Members.Count.ShouldBe(1);
        category.Members[0].ShouldBeOfType<CompositeInstanceMember>();
        category.Members[0].DisplayName.ShouldBe("Color");
    }

    [Fact]
    public void Apply_ShouldNotCollapse_WhenAChannelIsExposed()
    {
        ComponentSave component = MakeColorComponent();
        component.DefaultState.GetVariableSave("Red").ExposedAsName = "MyColorRed";

        MemberCategory category = CategoryWith(
            new InstanceMember("Red", null!),
            new InstanceMember("Green", null!),
            new InstanceMember("Blue", null!));
        List<MemberCategory> categories = new() { category };

        _logic.Apply(categories, component, instance: null);

        category.Members.Count.ShouldBe(3);
        category.Members.ShouldNotContain(m => m is CompositeInstanceMember);
    }

    [Fact]
    public void MakeDefault_ShouldResetAllChannels_UnderASingleUndoLock()
    {
        ComponentSave component = MakeColorComponent();
        FakeChannelMember red = new("Red");
        FakeChannelMember green = new("Green");
        FakeChannelMember blue = new("Blue");
        MemberCategory category = CategoryWith(red, green, blue);
        List<MemberCategory> categories = new() { category };

        _logic.Apply(categories, component, instance: null);
        CompositeInstanceMember composite = (CompositeInstanceMember)category.Members.Single();

        composite.ContextMenuEvents["Make Default"].Invoke(this, null!);

        red.MadeDefault.ShouldBeTrue();
        green.MadeDefault.ShouldBeTrue();
        blue.MadeDefault.ShouldBeTrue();
        _undoManager.Verify(x => x.RequestLock(), Times.Once);
    }

    [Fact]
    public void Expose_ShouldExposeEveryChannel_WhenAllSucceed()
    {
        InstanceSave instance = SetUpInstanceForExpose(out ComponentSave container);
        CompositeInstanceMember composite = BuildInstanceComposite(container, instance);

        _exposeVariableService
            .Setup(x => x.HandleExposeVariableClick(instance, It.IsAny<string>()))
            .Returns((InstanceSave _, string root) => Attempted(succeeded: true, new VariableSave { Name = root }));

        InvokeExpose(composite);

        _exposeVariableService.Verify(x => x.HandleExposeVariableClick(instance, "Red"), Times.Once);
        _exposeVariableService.Verify(x => x.HandleExposeVariableClick(instance, "Green"), Times.Once);
        _exposeVariableService.Verify(x => x.HandleExposeVariableClick(instance, "Blue"), Times.Once);
        _exposeVariableService.Verify(
            x => x.HandleUnexposeVariableClick(It.IsAny<VariableSave>(), It.IsAny<ElementSave>()), Times.Never);
    }

    [Fact]
    public void Expose_ShouldRollBackAlreadyExposedChannels_WhenAChannelFails()
    {
        InstanceSave instance = SetUpInstanceForExpose(out ComponentSave container);
        CompositeInstanceMember composite = BuildInstanceComposite(container, instance);

        VariableSave exposedRed = new() { Name = "Red" };
        _exposeVariableService
            .Setup(x => x.HandleExposeVariableClick(instance, "Red"))
            .Returns(Attempted(succeeded: true, exposedRed));
        _exposeVariableService
            .Setup(x => x.HandleExposeVariableClick(instance, "Green"))
            .Returns(Attempted(succeeded: false, null));

        InvokeExpose(composite);

        _exposeVariableService.Verify(x => x.HandleExposeVariableClick(instance, "Blue"), Times.Never);
        _exposeVariableService.Verify(x => x.HandleUnexposeVariableClick(exposedRed, container), Times.Once);
    }

    private InstanceSave SetUpInstanceForExpose(out ComponentSave container)
    {
        ComponentSave baseComponent = MakeColorComponent();

        container = new ComponentSave { Name = "Container" };
        container.States.Add(new StateSave { Name = "Default", ParentContainer = container });
        InstanceSave instance = new() { Name = "Shape", BaseType = baseComponent.Name };
        container.Instances.Add(instance);
        instance.ParentContainer = container;
        _project.Components.Add(container);

        _selectedState.Setup(x => x.SelectedElement).Returns(container);
        return instance;
    }

    private CompositeInstanceMember BuildInstanceComposite(ComponentSave container, InstanceSave instance)
    {
        MemberCategory category = CategoryWith(
            new InstanceMember("Red", null!),
            new InstanceMember("Green", null!),
            new InstanceMember("Blue", null!));
        List<MemberCategory> categories = new() { category };

        _logic.Apply(categories, container, instance);

        return (CompositeInstanceMember)category.Members.Single();
    }

    private static void InvokeExpose(CompositeInstanceMember composite)
    {
        string exposeKey = composite.ContextMenuEvents.Keys.Single(k => k.StartsWith("Expose"));
        composite.ContextMenuEvents[exposeKey].Invoke(composite, null!);
    }

    private static OptionallyAttemptedGeneralResponse<VariableSave> Attempted(bool succeeded, VariableSave? data)
    {
        return new OptionallyAttemptedGeneralResponse<VariableSave>
        {
            DidAttempt = true,
            Succeeded = succeeded,
            Data = data,
        };
    }

    private class FakeChannelMember : InstanceMember
    {
        public bool MadeDefault { get; private set; }

        public override bool IsDefault
        {
            get => MadeDefault;
            set => MadeDefault = value;
        }

        public FakeChannelMember(string name) : base(name, null!)
        {
            CustomGetEvent += _ => 0;
            CustomGetTypeEvent += _ => typeof(int);
            CustomSetPropertyEvent += (_, _) => { };
        }
    }
}
