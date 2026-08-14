using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.PropertyGridHelpers.Converters;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq.AutoMock;
using Shouldly;
using WpfDataUi.DataTypes;
using Xunit;

namespace GumToolUnitTests.VariableGrid;

public class VariableCategoryRowAdapterTests : BaseTestClass
{
    private readonly VariableCategoryRowAdapter _adapter = new();
    private readonly AutoMocker _mocker = new();

    /// <summary>
    /// Builds a grid row backed by a local value rather than by reflection over an instance, so a test can
    /// read and write it without standing up a real <c>StateReferencingInstanceMember</c>.
    /// </summary>
    private static InstanceMember CreateMember(string name, object? initialValue)
    {
        object? backingValue = initialValue;
        InstanceMember member = new InstanceMember(name, null!);
        member.CustomGetEvent += (_) => backingValue;
        member.CustomGetTypeEvent += (_) => typeof(object);
        member.CustomSetPropertyEvent += (_, args) => backingValue = args.Value;
        return member;
    }

    private static CompositeInstanceMember CreateColorComposite(int initialValue)
    {
        InstanceMember red = CreateMember("Red", initialValue);
        InstanceMember green = CreateMember("Green", initialValue);
        return new CompositeInstanceMember(
            "Color",
            new List<InstanceMember> { red, green },
            typeof(int),
            channelValues => (int)channelValues[0]!,
            compositeValue => new object?[] { compositeValue, compositeValue });
    }

    private StateReferencingInstanceMember CreateStateSelectionRow(string variableName)
    {
        ComponentSave component = new ComponentSave { Name = "AdapterTestComponent" };
        component.States.Add(new StateSave());
        component.DefaultState.ParentContainer = component;
        ObjectFinder.Self.GumProjectSave ??= new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(component);

        return new StateReferencingInstanceMember(
            Array.Empty<Attribute>(),
            new AvailableStatesConverter(category: "", _mocker.Get<ISelectedState>()),
            typeof(string),
            false,
            false,
            true,
            component.DefaultState,
            null,
            variableName,
            null,
            component,
            _mocker.Get<ISelectedState>(),
            _mocker.Get<IUndoManager>(),
            _mocker.Get<IGuiCommands>(),
            _mocker.Get<IFileCommands>(),
            _mocker.Get<ISetVariableLogic>(),
            _mocker.Get<IWireframeObjectManager>(),
            _mocker.Get<IPluginManager>(),
            _mocker.Get<IHotkeyManager>(),
            _mocker.Get<IDeleteVariableService>(),
            _mocker.Get<IExposeVariableService>(),
            _mocker.Get<IEditVariableService>(),
            _mocker.Get<ITypeManager>(),
            _mocker.Get<IClipboardService>());
    }

    /// <summary>
    /// A composite must be matched by the same name whether one object or several are selected. The
    /// multi-select wrapper sits above the composite, so expanding composites into their channels would
    /// name them Red/Green with one object selected but Color with two, and a copy made in one mode would
    /// silently not apply in the other.
    /// </summary>
    [Fact]
    public void CreateRows_ShouldNameACompositeRowTheSameInSingleAndMultiSelect()
    {
        CompositeInstanceMember singleSelectComposite = CreateColorComposite(10);
        MultiSelectInstanceMember multiSelectComposite = new MultiSelectInstanceMember
        {
            Name = "Color",
            InstanceMembers = new List<InstanceMember> { CreateColorComposite(10), CreateColorComposite(20) }
        };

        List<IVariableCategoryRow> singleSelectRows =
            _adapter.CreateRows(new List<InstanceMember> { singleSelectComposite });
        List<IVariableCategoryRow> multiSelectRows =
            _adapter.CreateRows(new List<InstanceMember> { multiSelectComposite });

        singleSelectRows.Single().RootVariableName.ShouldBe("Color");
        multiSelectRows.Single().RootVariableName.ShouldBe("Color");
    }

    /// <summary>
    /// State names are only meaningful on the element that declares them. A pasted state name the target
    /// does not offer must be rejected, or a dangling state name gets saved into the project.
    /// </summary>
    [Fact]
    public void TrySetValue_ShouldRejectAStateNameTheTargetDoesNotHave()
    {
        StateReferencingInstanceMember stateRow = CreateStateSelectionRow("StyleCategoryState");

        List<IVariableCategoryRow> rows = _adapter.CreateRows(new List<InstanceMember> { stateRow });

        rows.Single().TrySetValue("StateTheTargetDoesNotHave").ShouldBeFalse();
    }

    [Fact]
    public void TrySetValue_ShouldWriteThroughACompositeRowToEveryChannel()
    {
        CompositeInstanceMember composite = CreateColorComposite(10);

        List<IVariableCategoryRow> rows = _adapter.CreateRows(new List<InstanceMember> { composite });

        rows.Single().Value.ShouldBe(10);
        rows.Single().TrySetValue(30).ShouldBeTrue();

        composite.ChannelMembers[0].Value.ShouldBe(30);
        composite.ChannelMembers[1].Value.ShouldBe(30);
    }

    [Fact]
    public void TrySetValue_ShouldWriteThroughAMultiSelectRowToEveryWrappedMember()
    {
        InstanceMember firstInstanceFontSize = CreateMember("FontSize", 12);
        InstanceMember secondInstanceFontSize = CreateMember("FontSize", 14);
        MultiSelectInstanceMember multiSelect = new MultiSelectInstanceMember
        {
            Name = "FontSize",
            InstanceMembers = new List<InstanceMember> { firstInstanceFontSize, secondInstanceFontSize }
        };

        List<IVariableCategoryRow> rows = _adapter.CreateRows(new List<InstanceMember> { multiSelect });

        rows.Single().RootVariableName.ShouldBe("FontSize");
        rows.Single().TrySetValue(36).ShouldBeTrue();

        firstInstanceFontSize.Value.ShouldBe(36);
        secondInstanceFontSize.Value.ShouldBe(36);
    }
}
