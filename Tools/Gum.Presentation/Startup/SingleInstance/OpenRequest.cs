using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gum.Startup.SingleInstance;

/// <summary>
/// A request to a running Gum to open a project and optionally select one of its elements.
/// </summary>
/// <param name="Project">Absolute path of the <c>.gumx</c>/<c>.gumj</c> file.</param>
/// <param name="Element">Gum element name such as <c>Main</c> or <c>Sub/Button</c>; null selects nothing.</param>
public sealed record OpenRequest(string Project, string? Element);

/// <summary>The reply to an <see cref="OpenRequest"/>.</summary>
public sealed record OpenResponse(bool Ok, string? Error);

/// <summary>Sent by the running Gum as soon as a client connects.</summary>
public sealed record InstanceHello(int Version, int Pid);

/// <summary>
/// The wire format of the single-instance handoff: one UTF-8 JSON object per line, so an external
/// tool can speak it without a Gum library. The client sends
/// <c>{"v":1,"command":"open","project":"...","element":"..."}</c>; the server first sends
/// <c>{"hello":1,"pid":123}</c> and answers with <c>{"ok":true}</c> or <c>{"ok":false,"error":"..."}</c>.
/// </summary>
public static class OpenRequestMessage
{
    /// <summary>The protocol version this build reads and writes.</summary>
    public const int Version = 1;

    private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed class Wire
    {
        [JsonPropertyName("v")] public int? V { get; set; }
        [JsonPropertyName("command")] public string? Command { get; set; }
        [JsonPropertyName("project")] public string? Project { get; set; }
        [JsonPropertyName("element")] public string? Element { get; set; }
    }

    /// <summary>The request as one line, without the trailing newline.</summary>
    public static string Serialize(OpenRequest request) =>
        JsonSerializer.Serialize(
            new Wire { V = Version, Command = "open", Project = request.Project, Element = request.Element },
            _options);

    /// <summary>Reads a request line. On failure <paramref name="error"/> says what is wrong.</summary>
    public static bool TryParse(string line, out OpenRequest? request, out string? error)
    {
        request = null;
        Wire? wire;
        try
        {
            wire = JsonSerializer.Deserialize<Wire>(line);
        }
        catch (JsonException)
        {
            error = "The request is not valid JSON.";
            return false;
        }

        if (wire == null)
        {
            error = "The request is empty.";
            return false;
        }

        if (wire.V != Version)
        {
            error = $"Unsupported protocol version {wire.V?.ToString() ?? "(none)"}; this Gum speaks {Version}.";
            return false;
        }

        if (wire.Command != "open")
        {
            error = $"Unknown command \"{wire.Command}\".";
            return false;
        }

        if (string.IsNullOrWhiteSpace(wire.Project))
        {
            error = "The request has no project.";
            return false;
        }

        request = new OpenRequest(wire.Project, string.IsNullOrWhiteSpace(wire.Element) ? null : wire.Element);
        error = null;
        return true;
    }

    /// <summary>The reply as one line.</summary>
    public static string Serialize(OpenResponse response) =>
        JsonSerializer.Serialize(new { ok = response.Ok, error = response.Error }, _options);

    /// <summary>Reads a reply line; false when it is not one.</summary>
    public static bool TryParse(string line, out OpenResponse? response)
    {
        response = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(line);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("ok", out JsonElement ok) ||
                ok.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            {
                return false;
            }

            string? error = document.RootElement.TryGetProperty("error", out JsonElement errorElement) &&
                errorElement.ValueKind == JsonValueKind.String ? errorElement.GetString() : null;
            response = new OpenResponse(ok.GetBoolean(), error);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>The hello as one line.</summary>
    public static string Serialize(InstanceHello hello) =>
        JsonSerializer.Serialize(new { hello = hello.Version, pid = hello.Pid });

    /// <summary>Reads a hello line; false when it is not one.</summary>
    public static bool TryParse(string line, out InstanceHello? hello)
    {
        hello = null;
        try
        {
            using JsonDocument document = JsonDocument.Parse(line);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("hello", out JsonElement version) && version.TryGetInt32(out int v) &&
                document.RootElement.TryGetProperty("pid", out JsonElement pid) && pid.TryGetInt32(out int p))
            {
                hello = new InstanceHello(v, p);
                return true;
            }
        }
        catch (JsonException)
        {
        }

        return false;
    }
}
