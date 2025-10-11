using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Gum.Settings;

public sealed class WritableOptions<T> : IWritableOptions<T> where T : class, new()
{
    private readonly IOptionsMonitor<T> _monitor;
    private readonly IConfigurationRoot _configRoot;
    private readonly string _sectionName;
    private readonly string _filePath;
    private readonly object _writeLock = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new ColorJsonConverter() }
    };

    public WritableOptions(
        IOptionsMonitor<T> monitor,
        IConfigurationRoot configRoot,
        string sectionName,
        string filePath)
    {
        _monitor = monitor;
        _configRoot = configRoot;
        _sectionName = sectionName;
        _filePath = filePath;
    }

    // IOptionsMonitor<T>
    public T CurrentValue => _monitor.CurrentValue;
    public T Get(string? name) => _monitor.Get(name);
    public IDisposable? OnChange(Action<T, string?> listener) => _monitor.OnChange(listener);

    public void Update(Action<T> applyChanges)
    {
        lock (_writeLock)
        {
            // Load file (or create an empty root if missing)
            JsonObject root;
            if (File.Exists(_filePath))
            {
                var text = File.ReadAllText(_filePath);
                root = JsonNode.Parse(text) as JsonObject ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            // Get or create this section node
            var sectionNode = root[_sectionName] as JsonObject ?? new JsonObject();

            // Deserialize current section to T, apply changes
            var model = sectionNode.Deserialize<T>(JsonOpts) ?? new T();
            applyChanges(model);

            // Write section back
            root[_sectionName] = JsonSerializer.SerializeToNode(model, JsonOpts) as JsonNode;

            // Persist to disk atomically
            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(root, JsonOpts));
            File.Copy(tempPath, _filePath, overwrite: true);
            File.Delete(tempPath);

            // Notify config that file changed
            _configRoot.Reload();
        }
    }
}

public class ColorJsonConverter : System.Text.Json.Serialization.JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // You can support multiple formats — here we support "#RRGGBB" or ARGB objects.
        if (reader.TokenType == JsonTokenType.String)
        {
            string? hex = reader.GetString();
            if (string.IsNullOrWhiteSpace(hex))
                return Color.Empty;

            return ColorTranslator.FromHtml(hex);
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            int a = 255, r = 0, g = 0, b = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                string propertyName = reader.GetString()!;
                reader.Read();

                switch (propertyName)
                {
                    case "A": a = reader.GetInt32(); break;
                    case "R": r = reader.GetInt32(); break;
                    case "G": g = reader.GetInt32(); break;
                    case "B": b = reader.GetInt32(); break;
                }
            }

            return Color.FromArgb(a, r, g, b);
        }

        throw new System.Text.Json.JsonException($"Unexpected token parsing color: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        // Choose one format — hex or object. Here we use hex like "#RRGGBB" (omit alpha if 255).
        string hex = value.A == 255
            ? $"#{value.R:X2}{value.G:X2}{value.B:X2}"
            : $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";

        writer.WriteStringValue(hex);
    }
}

public static class WritableOptionsServiceCollectionExtensions
{
    /// <summary>
    /// Binds T to the given section and registers IWritableOptions&lt;T&gt; that
    /// writes back only that section to the provided file path.
    /// </summary>
    public static IServiceCollection ConfigureWritable<T>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName,
        string filePath)
        where T : class, new()
    {
        var section = configuration.GetSection(sectionName);
        services.Configure<T>(section);

        services.AddSingleton<IWritableOptions<T>>(sp =>
        {
            var monitor = sp.GetRequiredService<IOptionsMonitor<T>>();
            var configRoot = (IConfigurationRoot)sp.GetRequiredService<IConfiguration>();
            return new WritableOptions<T>(monitor, configRoot, sectionName, filePath);
        });

        return services;
    }
}