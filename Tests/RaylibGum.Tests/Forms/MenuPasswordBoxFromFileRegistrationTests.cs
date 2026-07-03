using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Forms;
using Gum.Managers;
using GumRuntime;
using Shouldly;

namespace RaylibGum.Tests.Forms;

/// <summary>
/// Guards the from-file registration path for the three controls enabled on raylib in #3174.
/// <c>FormsUtilities.RegisterFromFileFormRuntimeDefaults</c> maps a project component carrying a
/// Menu / MenuItem / PasswordBox behavior to its <c>DefaultFromFile*Runtime</c> wrapper. Those
/// registration blocks (and the wrapper classes themselves) were left gated <c>#if XNALIKE || FRB</c>
/// when #3174 added the controls to raylib's code-only path, so on raylib a project-authored
/// Menu/MenuItem/PasswordBox previously got no from-file registration and fell back to a plain
/// <see cref="Gum.Wireframe.GraphicalUiElement"/>.
///
/// The wrapper types are <c>#if</c>-gated, so they can't be referenced by name here (they don't
/// exist in the assembly before the fix) — the assertion compares the produced GUE's type name
/// instead, which compiles in both states.
/// </summary>
public class MenuPasswordBoxFromFileRegistrationTests : BaseTestClass
{
    [Fact]
    public void Menu_Component_RegistersDefaultFromFileMenuRuntime()
    {
        RegisteredRuntimeTypeNameFor(StandardFormsBehaviorNames.MenuBehaviorName, "RaylibFromFileMenu")
            .ShouldBe("DefaultFromFileMenuRuntime");
    }

    [Fact]
    public void MenuItem_Component_RegistersDefaultFromFileMenuItemRuntime()
    {
        RegisteredRuntimeTypeNameFor(StandardFormsBehaviorNames.MenuItemBehaviorName, "RaylibFromFileMenuItem")
            .ShouldBe("DefaultFromFileMenuItemRuntime");
    }

    [Fact]
    public void PasswordBox_Component_RegistersDefaultFromFilePasswordBoxRuntime()
    {
        RegisteredRuntimeTypeNameFor(StandardFormsBehaviorNames.PasswordBoxBehaviorName, "RaylibFromFilePasswordBox")
            .ShouldBe("DefaultFromFilePasswordBoxRuntime");
    }

    /// <summary>
    /// Builds a single-component project whose component carries <paramref name="behaviorName"/>,
    /// runs the from-file registration pass, and returns the type name of the GUE that
    /// <see cref="ElementSaveExtensions.CreateGueForElement"/> produces for that component.
    /// </summary>
    private static string RegisteredRuntimeTypeNameFor(string behaviorName, string componentName)
    {
        var component = new ComponentSave { Name = componentName };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });

        var project = new GumProjectSave();
        project.Components.Add(component);

        var previousProject = ObjectFinder.Self.GumProjectSave;
        ObjectFinder.Self.GumProjectSave = project;
        try
        {
            FormsUtilities.RegisterFromFileFormRuntimeDefaults();
            return ElementSaveExtensions.CreateGueForElement(component).GetType().Name;
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = previousProject;
        }
    }
}
