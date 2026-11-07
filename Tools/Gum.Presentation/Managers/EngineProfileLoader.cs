using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ToolsUtilities;

namespace Gum.Managers;

/// <inheritdoc cref="IEngineProfileLoader"/>
public class EngineProfileLoader : IEngineProfileLoader
{
    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <inheritdoc/>
    public EngineProfileLoadResult Load(string? projectFileName, string profileReference)
    {
        if (string.IsNullOrEmpty(profileReference) || string.IsNullOrEmpty(projectFileName))
        {
            return new EngineProfileLoadResult(new EngineProfile(), null, null);
        }

        FilePath filePath;
        try
        {
            string projectFolder = Path.GetDirectoryName(projectFileName) ?? string.Empty;
            filePath = new FilePath(Path.Combine(projectFolder, profileReference.Replace('\\', '/')));
        }
        catch (Exception exception)
        {
            return Unrestricted(null, $"Engine profile \"{profileReference}\" is not a valid path ({exception.Message}), so the project is unrestricted.");
        }

        if (!File.Exists(filePath.FullPath))
        {
            return Unrestricted(filePath, $"Engine profile \"{filePath.FullPath}\" was not found, so the project is unrestricted.");
        }

        try
        {
            string json = File.ReadAllText(filePath.FullPath);
            EngineProfile profile = JsonSerializer.Deserialize<EngineProfile>(json, Options) ?? new EngineProfile();
            return new EngineProfileLoadResult(profile, filePath, null);
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            string problem = exception.Message.Split('\n')[0].Trim();
            return Unrestricted(filePath, $"Engine profile \"{filePath.FullPath}\" could not be read ({problem}), so the project is unrestricted.");
        }
    }

    private static EngineProfileLoadResult Unrestricted(FilePath? filePath, string warning) =>
        new EngineProfileLoadResult(new EngineProfile(), filePath, warning);
}
