using Gum.Managers;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace Gum.Presentation.Tests.Managers;

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

    [Theory]
    [InlineData("C:\\Game\\Game.gumx")]
    [InlineData("C:\\Game\\Game.gumj")]
    public void ProjectFileAmongDroppedFiles_IsNotAFileDropForTheCanvas(string projectFile)
    {
        // The main window opens a dropped project; the canvas neither accepts nor handles it.
        WireframeDropPayload payload = new WireframeDropPayload(null, null, new[] { "C:\\file.png", projectFile });

        payload.HasFileDrop.ShouldBeFalse();
        payload.ResolveAction().ShouldBeOfType<WireframeDropAction.None>();
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
