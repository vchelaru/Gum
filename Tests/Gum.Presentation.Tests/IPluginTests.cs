using Gum.Plugins;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Pins <see cref="IPlugin"/>'s contract after its relocation to Gum.Presentation (#3940) -- the
/// interface + <see cref="PluginShutDownReason"/> enum had zero WPF/WinForms surface, so the move
/// is a plain file relocation with no behavior change. <c>PluginBase</c>/<c>PluginContainer</c>/
/// <c>PluginManager</c> (Gum.csproj) still implement/consume it via the existing project reference
/// to Gum.Presentation -- see <c>AllPluginsCompositionTests</c> for that side of the contract.
/// </summary>
public class IPluginTests
{
    private class FakePlugin : IPlugin
    {
        public string FriendlyName => "Fake Plugin";
        public string UniqueId { get; set; } = "";
        public Version Version => new Version(1, 2, 3);
        public bool StartUpCalled;
        public PluginShutDownReason? LastShutDownReason;
        public bool ShutDownReturnValue = true;

        public void StartUp() => StartUpCalled = true;

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {
            LastShutDownReason = shutDownReason;
            return ShutDownReturnValue;
        }
    }

    [Fact]
    public void FriendlyNameVersionAndUniqueId_ExposeImplementationValues()
    {
        var plugin = new FakePlugin { UniqueId = "abc-123" };

        plugin.FriendlyName.ShouldBe("Fake Plugin");
        plugin.UniqueId.ShouldBe("abc-123");
        plugin.Version.ShouldBe(new Version(1, 2, 3));
    }

    [Fact]
    public void StartUp_InvokesImplementation()
    {
        var plugin = new FakePlugin();

        plugin.StartUp();

        plugin.StartUpCalled.ShouldBeTrue();
    }

    [Theory]
    [InlineData(PluginShutDownReason.UserDisabled)]
    [InlineData(PluginShutDownReason.PluginException)]
    [InlineData(PluginShutDownReason.PluginInitiated)]
    [InlineData(PluginShutDownReason.GumxUnload)]
    [InlineData(PluginShutDownReason.GumShutDown)]
    public void ShutDown_ForwardsReasonAndReturnsImplementationResult(PluginShutDownReason reason)
    {
        var plugin = new FakePlugin { ShutDownReturnValue = false };

        var result = plugin.ShutDown(reason);

        result.ShouldBeFalse();
        plugin.LastShutDownReason.ShouldBe(reason);
    }
}
