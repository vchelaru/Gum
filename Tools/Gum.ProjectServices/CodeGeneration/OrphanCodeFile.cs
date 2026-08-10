using ToolsUtilities;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// The kind of file an <see cref="OrphanCodeFile"/> refers to. Only
/// <see cref="Generated"/> is derived data that can be removed losslessly.
/// </summary>
public enum OrphanCodeFileKind
{
    /// <summary>
    /// A <c>.Generated.cs</c> file. A pure function of its element, so removing it is lossless.
    /// </summary>
    Generated,

    /// <summary>
    /// A user-editable custom code <c>.cs</c> file. Unrecoverable through Gum, so it must never be
    /// removed without consent, and never with a plain delete.
    /// </summary>
    CustomCode,

    /// <summary>
    /// A per-element <c>.codsj</c> code settings file living alongside the element XML.
    /// </summary>
    ElementSettings
}

/// <summary>
/// A file on disk that code generation wrote (or that belongs to an element) but which no longer
/// has a matching element in the project.
/// </summary>
public class OrphanCodeFile
{
    /// <summary>
    /// The full path of the orphaned file.
    /// </summary>
    public FilePath FilePath { get; }

    /// <inheritdoc cref="OrphanCodeFileKind"/>
    public OrphanCodeFileKind Kind { get; }

    /// <summary>
    /// The element name this file appears to belong to, read from the generated file's
    /// <c>//Code for</c> header or the settings file name. Informational only — orphan
    /// detection matches on paths, not on this value.
    /// </summary>
    public string? ElementName { get; }

    public OrphanCodeFile(FilePath filePath, OrphanCodeFileKind kind, string? elementName)
    {
        FilePath = filePath;
        Kind = kind;
        ElementName = elementName;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Kind}: {FilePath}";
}
