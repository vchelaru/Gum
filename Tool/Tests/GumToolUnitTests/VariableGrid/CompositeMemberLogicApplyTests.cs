using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
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
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<INameVerifier> _nameVerifier;
    private readonly Mock<IClipboardService> _clipboardService;
    private readonly UndoManager _realUndoManager;
    private readonly CompositeMemberLogic _logic;

    public CompositeMemberLogicApplyTests()
    {
        _project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = _project;

        _selectedState = new Mock<ISelectedState>();
        _exposeVariableService = new Mock<IExposeVariableService>();
        _guiCommands = new Mock<IGuiCommands>();
        _dialogService = new Mock<IDialogService>();
        _nameVerifier = new Mock<INameVerifier>();
        _clipboardService = new Mock<IClipboardService>();

        _realUndoManager = new UndoManager(null!, null!, null!, null!, null!, null!, null!);
        _undoManager = new Mock<IUndoManager>();
        _undoManager.Setup(x => x.RequestLock()).Returns(() => _realUndoManager.RequestLock());

        _logic = new CompositeMemberLogic(
            _selectedState.Object,
            _exposeVariableService.Object,
            _undoManager.Object,
            _guiCommands.Object,
            ObjectFinder.Self,
            new CompositeMemberRegistry(),
            _dialogService.Object,
            _nameVerifier.Object,
            _clipboardService.Object);
    }

    private ComponentSave MakeColorComponent(string prefix = "")
    {
        ComponentSave component = new() { Name = "Colored" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.DefaultState.Variables.Add(new VariableSave { Name = prefix + "Red", Type = "int", Value = 0 });
        component.DefaultState.Variables.Add(new VariableSave { Name = prefix + "Green", Type = "int", Value = 0 });
        component.DefaultState.Variables.Add(new VariableSave { Name = prefix + "Blue", Type = "int", Value = 0 });
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
    public void Apply_ShouldStillCollapse_WhenAChannelIsExposed()
    {
        // Exposure no longer suppresses the swatch; the triple still collapses (a partially-exposed color too).
        ComponentSave component = MakeColorComponent();
        component.DefaultState.GetVariableSave("Red")!.ExposedAsName = "MyColorRed";

        MemberCategory category = CategoryWith(
            new InstanceMember("Red", null!),
            new InstanceMember("Green", null!),
            new InstanceMember("Blue", null!));
        List<MemberCategory> categories = new() { category };

        _logic.Apply(categories, component, instance: null);

        category.Members.Count.ShouldBe(1);
        category.Members[0].ShouldBeOfType<CompositeInstanceMember>();
    }

    [Fact]
    public void Apply_ShouldSetExposedSubtext_ListingExposedNames_WhenChannelsExposed()
    {
        ComponentSave component = MakeColorComponent();
        component.DefaultState.GetVariableSave("Red")!.ExposedAsName = "MyColorRed";
        component.DefaultState.GetVariableSave("Green")!.ExposedAsName = "MyColorGreen";
        component.DefaultState.GetVariableSave("Blue")!.ExposedAsName = "MyColorBlue";

        MemberCategory category = CategoryWith(
            new InstanceMember("Red", null!),
            new InstanceMember("Green", null!),
            new InstanceMember("Blue", null!));
        List<MemberCategory> categories = new() { category };

        _logic.Apply(categories, component, instance: null);
        CompositeInstanceMember composite = (CompositeInstanceMember)category.Members.Single();

        composite.DetailText.ShouldContain("MyColorRed");
        composite.DetailText.ShouldContain("MyColorGreen");
        composite.DetailText.ShouldContain("MyColorBlue");
    }

    [Fact]
    public void Apply_ShouldNotSetExposedSubtext_WhenNotExposed()
    {
        ComponentSave component = MakeColorComponent();
        MemberCategory category = CategoryWith(
            new InstanceMember("Red", null!),
            new InstanceMember("Green", null!),
            new InstanceMember("Blue", null!));
        List<MemberCategory> categories = new() { category };

        _logic.Apply(categories, component, instance: null);
        CompositeInstanceMember composite = (CompositeInstanceMember)category.Members.Single();

        composite.DetailText.ShouldBeNullOrEmpty();
    }

    [Fact]
    public void Apply_ShouldCombineChannelDetailText_WhenChannelsAreDisabled()
    {
        // A color driven by a variable reference disables (read-only) each Red/Green/Blue channel and
        // gives each one a subtext describing its assignment. Once collapsed into the single swatch the
        // per-channel rows are gone, so the swatch must surface the combined per-channel messages -
        // otherwise the swatch is disabled with no explanation of why (issue #3058).
        ComponentSave component = MakeColorComponent();
        MemberCategory category = CategoryWith(
            DisabledChannel("Red", "=A.Red"),
            DisabledChannel("Green", "=A.Green"),
            DisabledChannel("Blue", "=A.Blue"));
        List<MemberCategory> categories = new() { category };

        _logic.Apply(categories, component, instance: null);
        CompositeInstanceMember composite = (CompositeInstanceMember)category.Members.Single();

        composite.DetailText.ShouldContain("Red=A.Red");
        composite.DetailText.ShouldContain("Green=A.Green");
        composite.DetailText.ShouldContain("Blue=A.Blue");
    }

    private static InstanceMember DisabledChannel(string name, string detailText)
    {
        return new InstanceMember(name, null!) { IsReadOnly = true, DetailText = detailText };
    }

    [Fact]
    public void CopyQualifiedVariableName_ShouldSetClipboardText_ToTheCompositeQualifiedName()
    {
        ComponentSave component = MakeColorComponent();
        MemberCategory category = CategoryWith(
            new InstanceMember("Red", null!),
            new InstanceMember("Green", null!),
            new InstanceMember("Blue", null!));
        List<MemberCategory> categories = new() { category };

        _logic.Apply(categories, component, instance: null);
        CompositeInstanceMember composite = (CompositeInstanceMember)category.Members.Single();

        string copyKey = composite.ContextMenuEvents.Keys.Single(k => k == "Copy Qualified Variable Name");
        composite.ContextMenuEvents[copyKey].Invoke(composite, null!);

        _clipboardService.Verify(x => x.SetText("Components/Colored.Color"), Times.Once);
    }

    [Fact]
    public void Apply_ShouldOfferUnexposeNotExpose_WhenChannelsExposed()
    {
        InstanceSave instance = SetUpInstanceForExpose(out ComponentSave container);
        ExposeChannelsOnInstance(container, instance);
        CompositeInstanceMember composite = BuildInstanceComposite(container, instance);

        composite.ContextMenuEvents.Keys.ShouldContain(k => k.StartsWith("Un-expose"));
        composite.ContextMenuEvents.Keys.ShouldNotContain(k => k.StartsWith("Expose"));
    }

    [Fact]
    public void Unexpose_ShouldUnexposeEveryExposedChannel_UnderASingleUndoLock()
    {
        InstanceSave instance = SetUpInstanceForExpose(out ComponentSave container);
        ExposeChannelsOnInstance(container, instance);
        CompositeInstanceMember composite = BuildInstanceComposite(container, instance);

        string unexposeKey = composite.ContextMenuEvents.Keys.Single(k => k.StartsWith("Un-expose"));
        composite.ContextMenuEvents[unexposeKey].Invoke(composite, null!);

        _exposeVariableService.Verify(
            x => x.HandleUnexposeVariableClick(It.IsAny<VariableSave>(), container), Times.Exactly(3));
        _undoManager.Verify(x => x.RequestLock(), Times.Once);
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

        SetUpPrompt(baseName: "MyColor");
        _exposeVariableService
            .Setup(x => x.ExposeVariable(instance, It.IsAny<string>(), It.IsAny<string>()))
            .Returns((InstanceSave _, string _, string exposed) => Attempted(succeeded: true, new VariableSave { Name = exposed }));

        InvokeExpose(composite);

        _exposeVariableService.Verify(x => x.ExposeVariable(instance, "Red", "MyColorRed"), Times.Once);
        _exposeVariableService.Verify(x => x.ExposeVariable(instance, "Green", "MyColorGreen"), Times.Once);
        _exposeVariableService.Verify(x => x.ExposeVariable(instance, "Blue", "MyColorBlue"), Times.Once);
        _exposeVariableService.Verify(
            x => x.HandleUnexposeVariableClick(It.IsAny<VariableSave>(), It.IsAny<ElementSave>()), Times.Never);
    }

    [Fact]
    public void Expose_ShouldPromptExactlyOnce_ForAllChannels()
    {
        InstanceSave instance = SetUpInstanceForExpose(out ComponentSave container);
        CompositeInstanceMember composite = BuildInstanceComposite(container, instance);

        SetUpPrompt(baseName: "MyColor");
        _exposeVariableService
            .Setup(x => x.ExposeVariable(instance, It.IsAny<string>(), It.IsAny<string>()))
            .Returns((InstanceSave _, string _, string exposed) => Attempted(succeeded: true, new VariableSave { Name = exposed }));

        InvokeExpose(composite);

        _dialogService.Verify(x => x.Show(It.IsAny<ExposeColorDialogViewModel>()), Times.Once);
    }

    [Fact]
    public void Expose_ShouldSuffixFullChannelRootName_ForAffixedColors()
    {
        InstanceSave instance = SetUpInstanceForExpose(out ComponentSave container, prefix: "Stroke");
        CompositeInstanceMember composite = BuildInstanceComposite(container, instance, prefix: "Stroke");

        SetUpPrompt(baseName: "MyShape");
        _exposeVariableService
            .Setup(x => x.ExposeVariable(instance, It.IsAny<string>(), It.IsAny<string>()))
            .Returns((InstanceSave _, string _, string exposed) => Attempted(succeeded: true, new VariableSave { Name = exposed }));

        InvokeExpose(composite);

        _exposeVariableService.Verify(x => x.ExposeVariable(instance, "StrokeRed", "MyShapeStrokeRed"), Times.Once);
        _exposeVariableService.Verify(x => x.ExposeVariable(instance, "StrokeGreen", "MyShapeStrokeGreen"), Times.Once);
        _exposeVariableService.Verify(x => x.ExposeVariable(instance, "StrokeBlue", "MyShapeStrokeBlue"), Times.Once);
    }

    [Fact]
    public void Expose_ShouldDoNothing_WhenPromptCancelled()
    {
        InstanceSave instance = SetUpInstanceForExpose(out ComponentSave container);
        CompositeInstanceMember composite = BuildInstanceComposite(container, instance);

        _dialogService
            .Setup(x => x.Show(It.IsAny<ExposeColorDialogViewModel>()))
            .Returns(false);

        InvokeExpose(composite);

        _exposeVariableService.Verify(
            x => x.ExposeVariable(It.IsAny<InstanceSave>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Expose_ShouldRollBackAlreadyExposedChannels_WhenAChannelFails()
    {
        InstanceSave instance = SetUpInstanceForExpose(out ComponentSave container);
        CompositeInstanceMember composite = BuildInstanceComposite(container, instance);

        SetUpPrompt(baseName: "MyColor");
        VariableSave exposedRed = new() { Name = "MyColorRed" };
        _exposeVariableService
            .Setup(x => x.ExposeVariable(instance, "Red", "MyColorRed"))
            .Returns(Attempted(succeeded: true, exposedRed));
        _exposeVariableService
            .Setup(x => x.ExposeVariable(instance, "Green", "MyColorGreen"))
            .Returns(Attempted(succeeded: false, null));

        InvokeExpose(composite);

        _exposeVariableService.Verify(x => x.ExposeVariable(instance, "Blue", "MyColorBlue"), Times.Never);
        _exposeVariableService.Verify(x => x.HandleUnexposeVariableClick(exposedRed, container), Times.Once);
    }

    private void SetUpPrompt(string baseName)
    {
        // Simulate the user entering a base name and clicking OK: set BaseName on the dialog VM the logic
        // constructs, then report an affirmative result.
        _dialogService
            .Setup(x => x.Show(It.IsAny<ExposeColorDialogViewModel>()))
            .Callback((ExposeColorDialogViewModel vm) => vm.BaseName = baseName)
            .Returns(true);
    }

    private InstanceSave SetUpInstanceForExpose(out ComponentSave container, string prefix = "")
    {
        ComponentSave baseComponent = MakeColorComponent(prefix);

        container = new ComponentSave { Name = "Container" };
        container.States.Add(new StateSave { Name = "Default", ParentContainer = container });
        InstanceSave instance = new() { Name = "Shape", BaseType = baseComponent.Name };
        container.Instances.Add(instance);
        instance.ParentContainer = container;
        _project.Components.Add(container);

        _selectedState.Setup(x => x.SelectedElement).Returns(container);
        return instance;
    }

    private static void ExposeChannelsOnInstance(ComponentSave container, InstanceSave instance, string prefix = "")
    {
        foreach (string token in new[] { "Red", "Green", "Blue" })
        {
            container.DefaultState.Variables.Add(new VariableSave
            {
                Name = $"{instance.Name}.{prefix}{token}",
                Type = "int",
                ExposedAsName = $"My{prefix}{token}",
            });
        }
    }

    private CompositeInstanceMember BuildInstanceComposite(ComponentSave container, InstanceSave instance, string prefix = "")
    {
        MemberCategory category = CategoryWith(
            new InstanceMember(prefix + "Red", null!),
            new InstanceMember(prefix + "Green", null!),
            new InstanceMember(prefix + "Blue", null!));
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
