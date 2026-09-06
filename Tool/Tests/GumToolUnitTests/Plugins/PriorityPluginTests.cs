using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Shouldly;
using Xunit;

namespace GumToolUnitTests.Plugins;

public class PriorityPluginTests
{
    // FriendlyName is what the "Manage Plugins" dialog and the "Error in plugin ..." message boxes
    // show. No PriorityPlugin subclass overrides it, so this default is the name the user reads -
    // it has to be the plugin's own name, not a description of its dispatch order.
    [Fact]
    public void FriendlyName_IsTypeName_WithoutPriorityPrefix()
    {
        TestPriorityPlugin plugin = new();

        plugin.FriendlyName.ShouldBe("TestPriorityPlugin");
    }

    private sealed class TestPriorityPlugin : PriorityPlugin
    {
        public override void StartUp() { }
    }
}
