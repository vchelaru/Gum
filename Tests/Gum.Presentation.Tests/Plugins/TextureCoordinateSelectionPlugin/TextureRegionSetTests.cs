using System.Collections.Generic;
using System.Linq;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Moq;
using Shouldly;
using TextureCoordinateSelectionPlugin.Logic;
using ToolsUtilities;

namespace Gum.Presentation.Tests.Plugins.TextureCoordinateSelectionPlugin;

public class TextureRegionSetTests
{
    private static TextureRegionSet Set(params TextureRegion[] regions) => new TextureRegionSet(regions, 256, 256);

    [Fact]
    public void HitTest_InsideARegion_ReturnsIt()
    {
        TextureRegionSet set = Set(new TextureRegion("A", "A", 10, 10, 20, 20));

        set.HitTest(15, 15)!.Value.Name.ShouldBe("A");
    }

    [Fact]
    public void HitTest_OwnsLeftAndTopEdges_ButNotRightAndBottom()
    {
        TextureRegionSet set = Set(new TextureRegion("A", "A", 10, 10, 20, 20));

        set.HitTest(10, 10).ShouldNotBeNull();
        set.HitTest(29.9f, 29.9f).ShouldNotBeNull();
        set.HitTest(30, 15).ShouldBeNull();
        set.HitTest(15, 30).ShouldBeNull();
        set.HitTest(9.9f, 15).ShouldBeNull();
    }

    [Fact]
    public void HitTest_NeighborsSharingAnEdge_NeverBothMatch()
    {
        TextureRegionSet set = Set(new TextureRegion("L", "L", 0, 0, 16, 16), new TextureRegion("R", "R", 16, 0, 16, 16));

        set.HitTest(15.9f, 5)!.Value.Name.ShouldBe("L");
        set.HitTest(16, 5)!.Value.Name.ShouldBe("R");
    }

    [Fact]
    public void HitTest_OverlappingRegions_SmallestAreaWins_AndTiesGoToTheFirstReported()
    {
        TextureRegionSet set = Set(
            new TextureRegion("Panel", "Panel", 0, 0, 200, 150),
            new TextureRegion("Icon", "Icon", 60, 60, 20, 20),
            new TextureRegion("Twin", "Twin", 60, 60, 20, 20));

        set.HitTest(70, 70)!.Value.Name.ShouldBe("Icon");
        set.HitTest(150, 100)!.Value.Name.ShouldBe("Panel");
    }

    [Fact]
    public void HitTest_OutsideEveryRegion_ReturnsNull()
    {
        Set(new TextureRegion("A", "A", 10, 10, 20, 20)).HitTest(100, 100).ShouldBeNull();
        TextureRegionSet.Empty.HitTest(0, 0).ShouldBeNull();
    }

    [Fact]
    public void ATinyRegion_IsStillHittable()
    {
        Set(new TextureRegion("Dot", "Dot", 40, 40, 1, 1)).HitTest(40.5f, 40.5f)!.Value.Name.ShouldBe("Dot");
    }

    [Fact]
    public void RegionsWithNoArea_AreDropped()
    {
        TextureRegionSet set = Set(new TextureRegion("W0", "W0", 0, 0, 0, 10), new TextureRegion("H0", "H0", 0, 0, 10, 0), new TextureRegion("Neg", "Neg", 0, 0, -5, 5));

        set.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void ARegionSticking_OutOfTheTexture_IsClampedToIt()
    {
        TextureRegionSet set = Set(new TextureRegion("Edge", "Edge", 240, -8, 40, 24));

        set.Regions.Single().ShouldBe(new TextureRegion("Edge", "Edge", 240, 0, 16, 16));
    }

    [Fact]
    public void TwoRegionsWithTheSameName_AreBothKept_AndHitTestReturnsTheOneUnderThePoint()
    {
        TextureRegionSet set = Set(
            new TextureRegion("hero-1", "Hero", 0, 0, 16, 16),
            new TextureRegion("hero-2", "Hero", 16, 0, 16, 16));

        set.Regions.Count.ShouldBe(2);
        set.HitTest(4, 4)!.Value.Key.ShouldBe("hero-1");
        set.HitTest(20, 4)!.Value.Key.ShouldBe("hero-2");
    }

    [Fact]
    public void Clamping_KeepsTheKeyAndName()
    {
        TextureRegionSet set = Set(new TextureRegion("k", "Edge", 240, -8, 40, 24));

        set.Regions.Single().Key.ShouldBe("k");
        set.Regions.Single().Name.ShouldBe("Edge");
    }

    [Fact]
    public void ARegionEntirelyOutsideTheTexture_IsIgnored()
    {
        Set(new TextureRegion("Gone", "Gone", 300, 300, 10, 10), new TextureRegion("Left", "Left", -20, 0, 10, 10)).IsEmpty.ShouldBeTrue();
    }
}

public class PluginManagerTextureRegionsTests
{
    private static PluginManager CreateManager(params PluginBase[] plugins)
    {
        PluginManager manager = new PluginManager(
            new Mock<IPluginEnablementStore>().Object,
            new Mock<IPluginHostConfiguration>().Object);
        manager.Plugins = plugins.ToList();
        foreach (PluginBase plugin in plugins)
        {
            PluginManager.StartupPlugin(plugin, manager);
        }
        return manager;
    }

    [Fact]
    public void GetTextureRegions_ConcatenatesWhatEveryPluginReports()
    {
        PluginManager manager = CreateManager(
            new RegionPlugin(new TextureRegion("A", "A", 0, 0, 8, 8)),
            new RegionPlugin(new TextureRegion("B", "B", 8, 0, 8, 8)));

        manager.GetTextureRegions(new FilePath("Sheet.png")).Select(region => region.Name).ShouldBe(new[] { "A", "B" });
    }

    [Fact]
    public void GetTextureRegions_WithNoPluginThatKnowsTheFile_IsEmpty()
    {
        PluginManager manager = CreateManager(new RegionPlugin(), new PlainPlugin());

        manager.GetTextureRegions(new FilePath("Sheet.png")).ShouldBeEmpty();
    }

    [Fact]
    public void GetTextureRegions_APluginThatThrows_DoesNotStopTheOthers()
    {
        PluginManager manager = CreateManager(
            new RegionPlugin(throws: true),
            new RegionPlugin(new TextureRegion("Good", "Good", 0, 0, 8, 8)));

        manager.GetTextureRegions(new FilePath("Sheet.png")).Select(region => region.Name).ShouldBe(new[] { "Good" });
    }

    private sealed class PlainPlugin : PluginBase
    {
        public override string FriendlyName => "Plain";
        public override void StartUp() { }
        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }

    private sealed class RegionPlugin : PluginBase
    {
        private readonly TextureRegion[] _regions;
        private readonly bool _throws;

        public RegionPlugin(params TextureRegion[] regions) => _regions = regions;

        public RegionPlugin(bool throws) : this() => _throws = throws;

        public override string FriendlyName => "Regions";

        public override void StartUp() => GetTextureRegionsForFile += _ =>
            _throws ? throw new System.InvalidOperationException("buggy plugin") : _regions.Length == 0 ? null : _regions;

        public override bool ShutDown(PluginShutDownReason shutDownReason) => true;
    }
}
