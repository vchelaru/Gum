using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq.AutoMock;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.VariableGrid;

public class PropertyGridManagerFontSourceToggleTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();

    [Fact]
    public void FontSourceToggle_FontRowAssignedByReference_IsReadOnlyAndIgnoresEdits()
    {
        // A variable set through a variable reference gets a read-only row with no setter,
        // so the toggle must not try to write the row's value.
        ComponentSave component = new ComponentSave { Name = "FontToggleComponent", BaseType = "Text" };
        component.States.Add(new StateSave());
        component.DefaultState.ParentContainer = component;
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(component);
        component.DefaultState.SetValue("Font", "Fonts/Some.ttf");

        StateReferencingInstanceMember fontMember = new StateReferencingInstanceMember(
            Array.Empty<Attribute>(), null, typeof(string),
            isReadOnly: true, isAssignedByReference: true, isVariable: true,
            component.DefaultState, null, "Font", null, component,
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
        MemberCategory category = new MemberCategory("Font");
        category.Members.Add(fontMember);
        List<MemberCategory> categories = new() { category };
        PropertyGridManager sut = _mocker.CreateInstance<PropertyGridManager>();

        sut.AdjustFontSourceToggle(categories, component.DefaultState, null);
        InstanceMember toggle = category.Members.First(member => member.Name == "Font Source");
        toggle.Value = "System Font";

        toggle.IsReadOnly.ShouldBeTrue();
        component.DefaultState.GetValueRecursive("Font").ShouldBe("Fonts/Some.ttf");
    }
}
