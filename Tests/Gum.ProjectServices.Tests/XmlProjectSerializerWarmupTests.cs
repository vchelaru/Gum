using System.Collections.Generic;
using System.Xml.Serialization;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.ProjectServices;
using Shouldly;
using ToolsUtilities;
using Xunit;

namespace Gum.ProjectServices.Tests;

public class XmlProjectSerializerWarmupTests
{
    [Fact]
    public void WarmUp_WarmsTheSerializersTheProjectLoadReads()
    {
        // The load asks these caches for its serializers, so a warmed one is reused, not rebuilt.
        XmlProjectSerializerWarmup warmup = new XmlProjectSerializerWarmup();

        IReadOnlyList<XmlSerializer> warmed = warmup.WarmUp();

        warmed.ShouldContain(FileManager.GetXmlSerializer(typeof(GumProjectSave)));
        warmed.ShouldContain(FileManager.GetXmlSerializer(typeof(ComponentSave)));
        warmed.ShouldContain(GumFileSerializer.GetGumProjectCompactSerializer());
        warmed.ShouldContain(GumFileSerializer.GetCompactSerializer(typeof(ScreenSave)));
        warmed.ShouldContain(GumFileSerializer.GetCompactSerializer(typeof(BehaviorSave)));
        warmed.Count.ShouldBe(10);
    }
}
