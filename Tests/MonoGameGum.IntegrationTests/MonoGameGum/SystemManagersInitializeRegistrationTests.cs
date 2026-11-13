using Gum.DataTypes;
using Gum.Forms;
using Gum.GueDeriving;
using GumRuntime;
using Shouldly;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// A repeat <see cref="global::RenderingLibrary.SystemManagers.Initialize"/> fills only the standard
/// runtime types missing from the registry, matching Skia (#7995).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class SystemManagersInitializeRegistrationTests : BaseTestClass
{
    [Fact]
    public void Initialize_AfterRegistryReset_RegistersStandardRuntimesAgain()
    {
        using ServiceTestGame game = new();
        game.Start();
        global::Gum.GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);
        ElementSaveExtensions.Reset();

        service.SystemManagers.Initialize(game.GraphicsDevice, fullInstantiation: true);

        ElementSaveExtensions.CreateGueForElement(new StandardElementSave { Name = "Text" })
            .ShouldBeAssignableTo<TextRuntime>();
    }

    [Fact]
    public void Initialize_Again_KeepsAHostRegistrationForAStandardType()
    {
        using ServiceTestGame game = new();
        game.Start();
        global::Gum.GumService service = game.CreateService();
        service.Initialize(game, DefaultVisualsVersion.V3);
        ElementSaveExtensions.RegisterGueInstantiation("Text", () => new HostTextRuntime());

        service.SystemManagers.Initialize(game.GraphicsDevice, fullInstantiation: true);

        ElementSaveExtensions.CreateGueForElement(new StandardElementSave { Name = "Text" })
            .ShouldBeOfType<HostTextRuntime>();
    }

    private class HostTextRuntime : TextRuntime
    {
    }
}
