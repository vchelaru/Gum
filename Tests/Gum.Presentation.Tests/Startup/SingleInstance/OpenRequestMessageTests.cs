using Gum.Startup.SingleInstance;
using Shouldly;

namespace Gum.Presentation.Tests.Startup.SingleInstance;

public class OpenRequestMessageTests
{
    [Fact]
    public void Serialize_ThenTryParse_RoundTripsPathWithSpacesAndQuotes()
    {
        OpenRequest request = new OpenRequest("C:\\My \"Games\"\\Ui\\Ui.gumx", "Sub/Main");

        string line = OpenRequestMessage.Serialize(request);

        line.ShouldNotContain("\n");
        OpenRequestMessage.TryParse(line, out OpenRequest? parsed, out string? error).ShouldBeTrue(error);
        parsed.ShouldBe(request);
    }

    [Fact]
    public void Serialize_WithoutElement_OmitsItAndParsesToNull()
    {
        string line = OpenRequestMessage.Serialize(new OpenRequest("/g/Ui.gumx", null));

        line.ShouldNotContain("element");
        OpenRequestMessage.TryParse(line, out OpenRequest? parsed, out _).ShouldBeTrue();
        parsed!.Element.ShouldBeNull();
    }

    [Fact]
    public void TryParse_AcceptsHandWrittenMessageFromExternalTool()
    {
        OpenRequestMessage.TryParse("{\"v\":1,\"command\":\"open\",\"project\":\"/g/Ui.gumx\",\"element\":\"Main\"}",
            out OpenRequest? parsed, out _).ShouldBeTrue();

        parsed.ShouldBe(new OpenRequest("/g/Ui.gumx", "Main"));
    }

    [Theory]
    [InlineData("not json", "not valid JSON")]
    [InlineData("null", "empty")]
    [InlineData("{\"command\":\"open\",\"project\":\"/g/Ui.gumx\"}", "version")]
    [InlineData("{\"v\":2,\"command\":\"open\",\"project\":\"/g/Ui.gumx\"}", "version 2")]
    [InlineData("{\"v\":1,\"command\":\"close\",\"project\":\"/g/Ui.gumx\"}", "Unknown command")]
    [InlineData("{\"v\":1,\"command\":\"open\"}", "no project")]
    [InlineData("{\"v\":1,\"command\":\"open\",\"project\":\"  \"}", "no project")]
    public void TryParse_RejectsBadRequestsWithAReason(string line, string expectedFragment)
    {
        OpenRequestMessage.TryParse(line, out OpenRequest? parsed, out string? error).ShouldBeFalse();

        parsed.ShouldBeNull();
        error!.ShouldContain(expectedFragment);
    }

    [Fact]
    public void Response_RoundTrips()
    {
        OpenRequestMessage.TryParse(OpenRequestMessage.Serialize(new OpenResponse(false, "nope")), out OpenResponse? response).ShouldBeTrue();
        response.ShouldBe(new OpenResponse(false, "nope"));

        OpenRequestMessage.TryParse(OpenRequestMessage.Serialize(new OpenResponse(true, null)), out response).ShouldBeTrue();
        response.ShouldBe(new OpenResponse(true, null));
    }

    [Fact]
    public void Response_RejectsGarbage()
    {
        OpenRequestMessage.TryParse("{\"x\":1}", out OpenResponse? response).ShouldBeFalse();
        OpenRequestMessage.TryParse("", out response).ShouldBeFalse();
        response.ShouldBeNull();
    }

    [Fact]
    public void Hello_RoundTrips()
    {
        OpenRequestMessage.TryParse(OpenRequestMessage.Serialize(new InstanceHello(1, 4242)), out InstanceHello? hello).ShouldBeTrue();

        hello.ShouldBe(new InstanceHello(1, 4242));
    }
}
