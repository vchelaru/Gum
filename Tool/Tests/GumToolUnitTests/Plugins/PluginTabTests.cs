using CommunityToolkit.Mvvm.Messaging;
using Gum;
using Gum.Plugins;
using Moq;
using Shouldly;
using System.Windows.Controls;
using Xunit;
using Xunit.Sdk;

namespace GumToolUnitTests.Plugins;

// Pins the IPluginTab seam (issue #3950): PluginBase/ITabManager's consumers reach PluginTab only
// through this headless interface now, so its members must actually forward to the same PluginTab
// instance's real state, not just satisfy the interface's shape at compile time.
public class PluginTabTests
{
    private static PluginTab CreateTab() => new PluginTab(new UserControl(), new Mock<IMessenger>().Object);

    [StaFact]
    public void Location_SetThroughInterface_ChangesConcreteInstance()
    {
        PluginTab tab = CreateTab();
        IPluginTab asInterface = tab;

        asInterface.Location = TabLocation.Left;

        tab.Location.ShouldBe(TabLocation.Left);
    }

    [StaFact]
    public void Hide_CalledThroughInterface_SetsIsVisibleFalseAndRaisesTabHidden()
    {
        PluginTab tab = CreateTab();
        IPluginTab asInterface = tab;
        bool tabHiddenRaised = false;
        tab.TabHidden += () => tabHiddenRaised = true;

        asInterface.Hide();

        tab.IsVisible.ShouldBeFalse();
        tabHiddenRaised.ShouldBeTrue();
    }

    [StaFact]
    public void Show_CalledThroughInterface_SetsIsVisibleTrueAndRaisesTabShown()
    {
        PluginTab tab = CreateTab();
        tab.Hide();
        IPluginTab asInterface = tab;
        bool tabShownRaised = false;
        tab.TabShown += () => tabShownRaised = true;

        asInterface.Show();

        tab.IsVisible.ShouldBeTrue();
        tabShownRaised.ShouldBeTrue();
    }
}
