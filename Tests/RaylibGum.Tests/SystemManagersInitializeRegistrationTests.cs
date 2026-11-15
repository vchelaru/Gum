using Gum.DataTypes;
using Gum.GueDeriving;
using GumRuntime;
using RenderingLibrary;
using Shouldly;

namespace RaylibGum.Tests;

/// <summary>
/// A repeat <see cref="SystemManagers.Initialize"/> fills only the standard runtime types missing from
/// the registry, matching Skia (#7995).
/// </summary>
public class SystemManagersInitializeRegistrationTests
{
    [Fact]
    public void Initialize_AfterRegistryReset_RegistersStandardRuntimesAgain()
    {
        ElementSaveExtensions.Reset();

        SystemManagers.Default.Initialize();

        ElementSaveExtensions.CreateGueForElement(new StandardElementSave { Name = "Text" })
            .ShouldBeAssignableTo<TextRuntime>();
    }

    [Fact]
    public void Initialize_Again_KeepsAHostRegistrationForAStandardType()
    {
        ElementSaveExtensions.RegisterGueInstantiation("Text", () => new HostTextRuntime());

        try
        {
            SystemManagers.Default.Initialize();

            ElementSaveExtensions.CreateGueForElement(new StandardElementSave { Name = "Text" })
                .ShouldBeOfType<HostTextRuntime>();
        }
        finally
        {
            SystemManagers systemManagers = SystemManagers.Default;
            ElementSaveExtensions.RegisterGueInstantiation("Text", () => new TextRuntime(systemManagers: systemManagers));
        }
    }

    private class HostTextRuntime : TextRuntime
    {
    }
}
