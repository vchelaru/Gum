using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System.IO;
using System.Text.Json;

namespace GumRuntime;

/// <summary>
/// Exports the computed/absolute layout of a Gum UI tree as JSON.
/// Intended for AI and diagnostic consumption — all values are resolved pixels,
/// not Gum-unit values.
/// </summary>
public static class LayoutExporter
{
    /// <summary>
    /// Returns a JSON string representing the absolute layout of the given element and all its descendants.
    /// </summary>
    public static string ToLayoutJson(this GraphicalUiElement root)
    {
        using var stream = new MemoryStream();
        WriteLayoutJson(root, stream);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Writes the absolute layout JSON of the given element and all its descendants to a file.
    /// </summary>
    public static void ExportLayoutJson(this GraphicalUiElement root, string filePath)
    {
        using var stream = File.Create(filePath);
        WriteLayoutJson(root, stream);
    }

    private static void WriteLayoutJson(GraphicalUiElement root, Stream stream)
    {
        var options = new JsonWriterOptions { Indented = true };
        using var writer = new Utf8JsonWriter(stream, options);
        WriteElement(writer, root);
        writer.Flush();
    }

    private static void WriteElement(Utf8JsonWriter writer, GraphicalUiElement element)
    {
        writer.WriteStartObject();

        var typeName = element.RenderableComponent?.GetType().Name ?? element.GetType().Name;
        writer.WriteString("type", typeName);

        if (!string.IsNullOrEmpty(element.Name))
        {
            writer.WriteString("name", element.Name);
        }

        writer.WriteNumber("x", element.AbsoluteX);
        writer.WriteNumber("y", element.AbsoluteY);
        writer.WriteNumber("width", element.GetAbsoluteWidth());
        writer.WriteNumber("height", element.GetAbsoluteHeight());
        writer.WriteBoolean("visible", element.AbsoluteVisible);

        if (element.RenderableComponent is IText text && text.RawText != null)
        {
            writer.WriteString("text", text.RawText);
        }

        if (element.Children.Count > 0)
        {
            writer.WriteStartArray("children");
            foreach (var child in element.Children)
            {
                WriteElement(writer, child);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }
}
