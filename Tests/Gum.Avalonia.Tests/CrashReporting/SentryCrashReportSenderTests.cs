using System.Text;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Gum.Avalonia.Diagnostics.CrashReporting;
using Gum.Diagnostics;
using Moq;
using Sentry;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;
using Shouldly;

namespace Gum.Avalonia.Tests.CrashReporting;

public class SentryCrashReportSenderTests
{
    // Never contacted: the fake transport takes every envelope.
    private const string FakeDsn = "https://public@example.invalid/1";

    private static SentryOptions CreateOptions(RecordingTransport transport, CrashReportScrubber scrubber)
    {
        SentryOptions options = SentryCrashReportSender.CreateOptions(FakeDsn, "1.2.3", Path.GetTempPath(), scrubber);
        // The cache would hand the envelope to a background worker that writes it to disk first.
        options.CacheDirectoryPath = null;
        options.Transport = transport;
        return options;
    }

    [Fact]
    public void CreateOptions_SetsReleaseCacheAndPrivacy()
    {
        SentryOptions options = SentryCrashReportSender.CreateOptions(FakeDsn, "1.2.3", "cache-dir", new CrashReportScrubber(new PathScrubber("sam", "/home/sam"), () => null));

        options.Dsn.ShouldBe(FakeDsn);
        options.Release.ShouldBe("1.2.3");
        options.SendDefaultPii.ShouldBeFalse();
        options.CacheDirectoryPath.ShouldBe("cache-dir");
    }

    [Fact]
    public async Task CapturedException_IsSentWithUserAndProjectPathsScrubbed()
    {
        RecordingTransport transport = new RecordingTransport();
        CrashReportScrubber scrubber = new CrashReportScrubber(new PathScrubber("sam", @"C:\Users\sam"), () => @"C:\Users\sam\Games\MyGame");
        using (SentryClient client = new SentryClient(CreateOptions(transport, scrubber)))
        {
            client.CaptureEvent(new SentryEvent(new FileNotFoundException(
                @"Could not find C:\Users\sam\Games\MyGame\a.png or C:\Users\sam\notes.txt")));
            await client.FlushAsync(TimeSpan.FromSeconds(5));
        }

        transport.SentStrings.ShouldContain(@"Could not find <project>\a.png or %USER%\notes.txt");
        transport.SentStrings.ShouldContain("1.2.3");
        transport.SentStrings.ShouldNotContain(value => value.Contains("sam", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ThrownException_IsSentWithoutMachineNameAndWithFramesAndMessageScrubbed()
    {
        // This file's folder stands in for the project, so the thrown exception's own frames point into it.
        string projectFolder = Path.GetDirectoryName(ThisFilePath())!;
        RecordingTransport transport = new RecordingTransport();
        CrashReportScrubber scrubber = new CrashReportScrubber(
            new PathScrubber(Environment.UserName, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
            () => projectFolder);
        Exception thrown = Throw("Could not find " + Path.Combine(projectFolder, "a.png"));
        using (SentryClient client = new SentryClient(CreateOptions(transport, scrubber)))
        {
            client.CaptureEvent(new SentryEvent(thrown)
            {
                ServerName = Environment.MachineName,
                Message = new SentryMessage { Message = "Opened " + Path.Combine(projectFolder, "b.gumx") + " for " + Environment.UserName },
            });
            await client.FlushAsync(TimeSpan.FromSeconds(5));
        }

        transport.PropertyNames.ShouldNotContain("server_name");
        transport.SentStrings.ShouldContain("Could not find " + Path.Combine("<project>", "a.png"));
        transport.SentStrings.ShouldContain("Opened " + Path.Combine("<project>", "b.gumx") + " for " + PathScrubber.Placeholder);
        transport.SentStrings.ShouldContain(Path.Combine("<project>", "SentryCrashReportSenderTests.cs"), "a stack frame's path");
        transport.SentStrings.ShouldNotContain(value => value.Contains(projectFolder, StringComparison.OrdinalIgnoreCase));
        transport.SentStrings.ShouldNotContain(value => value.Contains(Environment.MachineName, StringComparison.OrdinalIgnoreCase));
        transport.SentStrings.ShouldNotContain(value => value.Contains(Environment.UserName, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void Send_FlushesOnlyAFatalReport(bool isFatal, int expectedFlushes)
    {
        Mock<IHub> hub = new Mock<IHub>();
        hub.Setup(x => x.FlushAsync(It.IsAny<TimeSpan>())).Returns(Task.CompletedTask);
        InvalidOperationException exception = new InvalidOperationException("boom");

        new SentryCrashReportSender(hub.Object).Send(exception, "domain", isFatal);

        hub.Verify(x => x.CaptureEvent(It.Is<SentryEvent>(e => e.Exception == exception), It.IsAny<Action<Scope>>()), Times.Once);
        hub.Verify(x => x.FlushAsync(It.IsAny<TimeSpan>()), Times.Exactly(expectedFlushes));
    }

    private static string ThisFilePath([CallerFilePath] string path = "") => path;

    private static Exception Throw(string message)
    {
        try
        {
            throw new FileNotFoundException(message);
        }
        catch (FileNotFoundException exception)
        {
            return exception;
        }
    }

    // Every string value and property name in the envelope's JSON lines, unescaped.
    private sealed class RecordingTransport : ITransport
    {
        public List<string> SentStrings { get; } = new List<string>();
        public List<string> PropertyNames { get; } = new List<string>();

        public async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        {
            using MemoryStream stream = new MemoryStream();
            await envelope.SerializeAsync(stream, null, cancellationToken);
            foreach (string line in Encoding.UTF8.GetString(stream.ToArray()).Split('\n'))
            {
                if (line.StartsWith('{'))
                {
                    using JsonDocument document = JsonDocument.Parse(line);
                    Collect(document.RootElement);
                }
            }
        }

        private void Collect(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    SentStrings.Add(element.GetString()!);
                    break;
                case JsonValueKind.Object:
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        PropertyNames.Add(property.Name);
                        Collect(property.Value);
                    }
                    break;
                case JsonValueKind.Array:
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        Collect(item);
                    }
                    break;
            }
        }
    }
}
