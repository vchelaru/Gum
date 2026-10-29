#pragma warning disable CS0618
using Gum.DataTypes;
using Shouldly;
using Xunit;
using static MonoGameGum.Tests.Hot.PreviewAccuracyWorld;

namespace MonoGameGum.Tests.Hot;

/// <summary>Renaming a base component that another derives from: the tree equals a restart, so the result must too, now and on every later reload.</summary>
public partial class PreviewAccuracyTests
{
    [Fact]
    public void Component_Renamed_WithItsDerivedAndInstancesUpdated() => ShouldBeCorrect(Run(p =>
    {
        ComponentSave widget = Component(p, "Widgets/Widget");
        widget.Name = "Widgets/Control";
        Component(p, "Widgets/Gadget").BaseType = "Widgets/Control";
        MainScreen(p).GetInstance("Widget1")!.BaseType = "Widgets/Control";
    }));

    // ---- A rename that leaves the tree right must not keep asking for a restart ----

    [Fact]
    public void Component_RenamedUnderADerivedOne_DoesNotMakeEveryLaterReloadAskForARestart()
    {
        PreviewAccuracyWorld world = new PreviewAccuracyWorld();
        _worlds.Add(world);
        world.Boot();
        world.Save(p =>
        {
            Component(p, "Widgets/Widget").Name = "Widgets/Control";
            Component(p, "Widgets/Gadget").BaseType = "Widgets/Control";
            MainScreen(p).GetInstance("Widget1")!.BaseType = "Widgets/Control";
        });

        PreviewVerdict later = world.Save(p =>
        {
            Component(p, "Widgets/Widget").Name = "Widgets/Control";
            Component(p, "Widgets/Gadget").BaseType = "Widgets/Control";
            MainScreen(p).GetInstance("Widget1")!.BaseType = "Widgets/Control";
            Std(MainScreen(p), "Label", "Text", "Later");
        });
        ShouldBeCorrect(later);
    }
}
