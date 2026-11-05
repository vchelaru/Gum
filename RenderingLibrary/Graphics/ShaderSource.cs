using System;
using System.IO;

namespace Gum;

/// <summary>
/// A shader a render-target container's <c>SourceShaderFile</c> names: its logical name plus a way to open
/// the bytes. A resolver reads <see cref="Open"/> or <see cref="ReadAllText"/> instead of assuming
/// <see cref="Name"/> is a file on disk, so a shader served from a stream or a <c>.gumpkg</c> bundle loads the
/// same as a loose file. A resolver decides what a source is from its content, not from <see cref="Name"/>.
/// </summary>
public sealed class ShaderSource
{
    private readonly Func<Stream> _open;

    /// <param name="name">The logical name, used for diagnostics and to resolve a relative <c>#include</c>.</param>
    /// <param name="open">Opens a new readable stream over the shader's bytes. Called once per read.</param>
    public ShaderSource(string name, Func<Stream> open)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _open = open ?? throw new ArgumentNullException(nameof(open));
    }

    /// <summary>
    /// The logical name: the file path the container named, made absolute against the project when it exists
    /// there. It is not guaranteed to be a file on disk (a bundled or in-memory shader has none).
    /// </summary>
    public string Name { get; }

    /// <summary>Opens a new stream over the shader's bytes. The caller disposes it.</summary>
    public Stream Open() => _open();

    /// <summary>The shader's text, decoded as UTF-8 (a byte order mark is honored).</summary>
    public string ReadAllText()
    {
        using Stream stream = Open();
        using StreamReader reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// A source for <paramref name="path"/> read through <c>FileManager.GetStreamForFile</c>, so a file the
    /// host serves from a bundle or other hook is found as well as a loose one.
    /// </summary>
    public static ShaderSource FromFile(string path) =>
        new ShaderSource(path, () => ToolsUtilities.FileManager.GetStreamForFile(path));

    /// <inheritdoc/>
    public override string ToString() => Name;
}

/// <summary>
/// Holds a render-target shader resolver in whichever shape it was registered: <c>Func&lt;string, T&gt;</c> (the
/// original, path only) or <c>Func&lt;ShaderSource, T&gt;</c>. The last one assigned wins and the other shape
/// reads back as an adapter, so code that chains to the previous resolver keeps working whichever shape
/// either side uses.
/// </summary>
/// <typeparam name="T">The effect type the resolver produces.</typeparam>
public sealed class ShaderResolverSlot<T> where T : class
{
    private Func<string, T?>? _pathResolver;
    private Func<ShaderSource, T?>? _sourceResolver;

    /// <summary>The resolver as a path function. Assigning replaces any source resolver.</summary>
    public Func<string, T?>? PathResolver
    {
        get
        {
            if (_pathResolver != null || _sourceResolver == null)
            {
                return _pathResolver;
            }
            Func<ShaderSource, T?> source = _sourceResolver;
            return path => source(ShaderSource.FromFile(path));
        }
        set
        {
            _pathResolver = value;
            _sourceResolver = null;
        }
    }

    /// <summary>The resolver as a source function. Assigning replaces any path resolver.</summary>
    public Func<ShaderSource, T?>? SourceResolver
    {
        get
        {
            if (_sourceResolver != null || _pathResolver == null)
            {
                return _sourceResolver;
            }
            Func<string, T?> path = _pathResolver;
            return source => path(source.Name);
        }
        set
        {
            _sourceResolver = value;
            _pathResolver = null;
        }
    }

    /// <summary>Whether a resolver of either shape is registered.</summary>
    public bool HasResolver => _pathResolver != null || _sourceResolver != null;

    /// <summary>Runs the registered resolver on <paramref name="source"/>; null when none is registered or it declined.</summary>
    public T? Resolve(ShaderSource source)
    {
        if (_sourceResolver != null)
        {
            return _sourceResolver(source);
        }
        return _pathResolver?.Invoke(source.Name);
    }
}
