using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Plugins.ImportPlugin.Services;

public class GumxSourceService
{
    private static readonly HttpClient _httpClient = new HttpClient();

    /// <summary>
    /// Loads a GumProjectSave from a local file path or a URL.
    /// For local paths, uses GumProjectSave.Load directly.
    /// For URLs, fetches the .gumx and all referenced element files over HTTP.
    /// </summary>
    public async Task<GumProjectSave?> LoadProjectAsync(string pathOrUrl)
    {
        if (IsUrl(pathOrUrl))
        {
            pathOrUrl = NormalizeGitHubUrl(pathOrUrl);
            return await LoadProjectFromUrlAsync(pathOrUrl);
        }
        else
        {
            return GumProjectSave.Load(pathOrUrl, out _);
        }
    }

    /// <summary>
    /// Fetches the text of a single element file (.gucx, .gusx, .behx, .gutx)
    /// relative to the source base (which may be a local directory path or a base URL).
    /// </summary>
    public async Task<string?> FetchElementTextAsync(string relativeElementPath, string sourceBase)
    {
        if (IsUrl(sourceBase))
        {
            var url = sourceBase.TrimEnd('/') + "/" + relativeElementPath.Replace('\\', '/');
            try
            {
                return await _httpClient.GetStringAsync(url);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
        else
        {
            var path = Path.Combine(sourceBase, relativeElementPath);
            return File.Exists(path) ? await File.ReadAllTextAsync(path) : null;
        }
    }

    /// <summary>
    /// Converts a GitHub blob URL to its raw.githubusercontent.com equivalent.
    /// e.g. https://github.com/user/repo/blob/branch/path/file.gumx
    ///   -> https://raw.githubusercontent.com/user/repo/branch/path/file.gumx
    /// </summary>
    public string NormalizeGitHubUrl(string url)
    {
        // Only transform github.com blob URLs
        if (url.Contains("github.com") && url.Contains("/blob/"))
        {
            url = url.Replace("https://github.com/", "https://raw.githubusercontent.com/");
            url = url.Replace("/blob/", "/");
        }
        return url;
    }

    /// <summary>
    /// Returns the base path or URL for resolving element files relative to the .gumx source.
    /// For local paths, returns the directory containing the .gumx file.
    /// For URLs, returns the directory portion of the URL.
    /// </summary>
    public string GetSourceBase(string pathOrUrl)
    {
        if (IsUrl(pathOrUrl))
        {
            int lastSlash = pathOrUrl.LastIndexOf('/');
            return lastSlash >= 0 ? pathOrUrl[..(lastSlash + 1)] : pathOrUrl;
        }
        else
        {
            return FileManager.GetDirectory(pathOrUrl);
        }
    }

    /// <summary>
    /// Fetches the raw bytes of an asset file (e.g., .png) relative to the source base.
    /// Returns null if the file cannot be found or downloaded.
    /// </summary>
    public async Task<byte[]?> FetchBinaryAsync(string relativePath, string sourceBase)
    {
        if (IsUrl(sourceBase))
        {
            var url = sourceBase.TrimEnd('/') + "/" + relativePath.Replace('\\', '/');
            try
            {
                return await _httpClient.GetByteArrayAsync(url);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }
        else
        {
            var path = Path.Combine(sourceBase, relativePath);
            return File.Exists(path) ? await File.ReadAllBytesAsync(path) : null;
        }
    }

    private static bool IsUrl(string pathOrUrl)
    {
        return pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<GumProjectSave?> LoadProjectFromUrlAsync(string url)
    {
        string content;
        try
        {
            content = await _httpClient.GetStringAsync(url);
        }
        catch (HttpRequestException)
        {
            return null;
        }

        // Deserialize just the project save (references only, not element files)
        GumProjectSave gps;
        try
        {
            bool isCompact = content.Contains("Reference Name=");
            var deserializer = isCompact
                ? VariableSaveSerializer.GetGumProjectCompactSerializer()
                : FileManager.GetXmlSerializer(typeof(GumProjectSave));
            gps = (GumProjectSave)deserializer.Deserialize(new StringReader(content));
        }
        catch (Exception)
        {
            return null;
        }

        gps.FullFileName = url;

        // Compute base URL (directory of the .gumx URL)
        int lastSlash = url.LastIndexOf('/');
        string baseUrl = lastSlash >= 0 ? url[..(lastSlash + 1)] : url;

        // Populate element lists from references by fetching element files over HTTP
        await PopulateScreensFromReferencesAsync(gps, baseUrl);
        await PopulateComponentsFromReferencesAsync(gps, baseUrl);
        await PopulateStandardsFromReferencesAsync(gps, baseUrl);
        await PopulateBehaviorsFromReferencesAsync(gps, baseUrl);

        return gps;
    }

    private async Task PopulateScreensFromReferencesAsync(GumProjectSave gps, string baseUrl)
    {
        gps.Screens.Clear();
        foreach (var reference in gps.ScreenReferences)
        {
            reference.ElementType = ElementType.Screen;
            string relativePath = $"Screens/{reference.Name}.{GumProjectSave.ScreenExtension}";
            var elementText = await FetchElementTextAsync(relativePath, baseUrl);
            if (elementText != null)
            {
                var screen = DeserializeElementFromText<ScreenSave>(elementText, gps.Version);
                if (screen != null)
                {
                    screen.Name = reference.Name;
                    gps.Screens.Add(screen);
                }
            }
        }
    }

    private async Task PopulateComponentsFromReferencesAsync(GumProjectSave gps, string baseUrl)
    {
        gps.Components.Clear();
        foreach (var reference in gps.ComponentReferences)
        {
            reference.ElementType = ElementType.Component;
            string relativePath = $"Components/{reference.Name}.{GumProjectSave.ComponentExtension}";
            var elementText = await FetchElementTextAsync(relativePath, baseUrl);
            if (elementText != null)
            {
                var component = DeserializeElementFromText<ComponentSave>(elementText, gps.Version);
                if (component != null)
                {
                    component.Name = reference.Name;
                    gps.Components.Add(component);
                }
            }
        }
    }

    private async Task PopulateStandardsFromReferencesAsync(GumProjectSave gps, string baseUrl)
    {
        gps.StandardElements.Clear();
        foreach (var reference in gps.StandardElementReferences)
        {
            reference.ElementType = ElementType.Standard;
            string relativePath = $"Standards/{reference.Name}.{GumProjectSave.StandardExtension}";
            var elementText = await FetchElementTextAsync(relativePath, baseUrl);
            if (elementText != null)
            {
                var standard = DeserializeElementFromText<StandardElementSave>(elementText, gps.Version);
                if (standard != null)
                {
                    standard.Name = reference.Name;
                    gps.StandardElements.Add(standard);
                }
            }
        }
    }

    private async Task PopulateBehaviorsFromReferencesAsync(GumProjectSave gps, string baseUrl)
    {
        gps.Behaviors.Clear();
        if (gps.BehaviorReferences == null) return;
        foreach (var reference in gps.BehaviorReferences)
        {
            string relativePath = $"Behaviors/{reference.Name}.{BehaviorReference.Extension}";
            var elementText = await FetchElementTextAsync(relativePath, baseUrl);
            if (elementText != null)
            {
                var behavior = DeserializeBehaviorFromText(elementText, gps.Version);
                if (behavior != null)
                {
                    behavior.Name = reference.Name;
                    gps.Behaviors.Add(behavior);
                }
            }
        }
    }

    private static T? DeserializeElementFromText<T>(string content, int projectVersion) where T : ElementSave, new()
    {
        try
        {
            if (projectVersion >= (int)GumProjectSave.GumxVersions.AttributeVersion)
            {
                bool isV1 = content.Contains("<Variable>");
                bool isCompact = !isV1;
                if (isCompact)
                {
                    bool hasLegacyInstances = content.Contains("<Instance>");
                    var serializer = hasLegacyInstances
                        ? VariableSaveSerializer.GetLegacyInstancesCompactSerializer(typeof(T))
                        : VariableSaveSerializer.GetCompactSerializer(typeof(T));
                    using var reader = new StringReader(content);
                    return (T)serializer.Deserialize(reader);
                }
            }

            return FileManager.XmlDeserializeFromStream<T>(
                new MemoryStream(Encoding.UTF8.GetBytes(content)));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static BehaviorSave? DeserializeBehaviorFromText(string content, int projectVersion)
    {
        try
        {
            if (projectVersion >= (int)GumProjectSave.GumxVersions.AttributeVersion)
            {
                bool isV1 = content.Contains("<Variable>");
                bool isCompact = !isV1;
                if (isCompact)
                {
                    var compactSerializer = VariableSaveSerializer.GetCompactSerializer(typeof(BehaviorSave));
                    using var reader = new StringReader(content);
                    return (BehaviorSave)compactSerializer.Deserialize(reader);
                }
            }

            return FileManager.XmlDeserializeFromStream<BehaviorSave>(
                new MemoryStream(Encoding.UTF8.GetBytes(content)));
        }
        catch (Exception)
        {
            return null;
        }
    }
}
