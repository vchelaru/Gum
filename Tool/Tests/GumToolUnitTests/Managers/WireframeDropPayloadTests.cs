using Gum.Managers;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace GumToolUnitTests.Managers;

public class WireframeDropPayloadTests
{
    [Fact]
    public void ResolveAction_ChipAndNodesAndFilesPresent_ChipWins()
    {
        WireframeDropPayload payload = new WireframeDropPayload(
            "Button",
            new List<object> { "NodeTag" },
            new[] { "C:\\file.png" });

        WireframeDropAction action = payload.ResolveAction();

        action.ShouldBeOfType<WireframeDropAction.StandardChip>()
            .StandardElementTypeName.ShouldBe("Button");
    }

    [Fact]
    public void ResolveAction_FilesOnly_ReturnsFileDrop()
    {
        string[] files = { "C:\\file.png" };
        WireframeDropPayload payload = new WireframeDropPayload(null, null, files);

        WireframeDropAction action = payload.ResolveAction();

        action.ShouldBeOfType<WireframeDropAction.FileDrop>()
            .Files.ShouldBe(files);
    }

    [Fact]
    public void ResolveAction_NoData_ReturnsNone()
    {
        WireframeDropPayload payload = new WireframeDropPayload(null, null, null);

        WireframeDropAction action = payload.ResolveAction();

        action.ShouldBeOfType<WireframeDropAction.None>();
    }

    [Fact]
    public void ResolveAction_NodesAndFilesPresent_NodesWinOverFiles()
    {
        List<object> tags = new List<object> { "NodeTag" };
        WireframeDropPayload payload = new WireframeDropPayload(null, tags, new[] { "C:\\file.png" });

        WireframeDropAction action = payload.ResolveAction();

        action.ShouldBeOfType<WireframeDropAction.Nodes>()
            .Tags.ShouldBe(tags);
    }
}
