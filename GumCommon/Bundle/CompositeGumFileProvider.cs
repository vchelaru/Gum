using System;
using System.Collections.Generic;
using System.IO;

namespace Gum.Bundle;

/// <summary>
/// <see cref="IGumFileProvider"/> that chains several providers in order: the first provider that
/// has a file wins for <see cref="Exists"/>/<see cref="OpenRead"/>, and <see cref="EnumerateFiles"/>
/// returns the deduped union (first occurrence wins). Gum's default loading never builds one of
/// these — it is the explicit opt-in for advanced hosts that want mod / DLC / hot-reload overlays
/// layered over the project's own content (e.g.
/// <c>new CompositeGumFileProvider(resolution.FileProvider, new LooseFileGumFileProvider("./Mods"))</c>).
/// </summary>
public class CompositeGumFileProvider : IGumFileProvider
{
    private readonly IGumFileProvider[] _providers;

    /// <summary>
    /// Initializes a new <see cref="CompositeGumFileProvider"/> over <paramref name="providers"/>,
    /// in precedence order — earlier providers shadow later ones.
    /// </summary>
    public CompositeGumFileProvider(params IGumFileProvider[] providers)
    {
        if (providers == null)
        {
            throw new ArgumentNullException(nameof(providers));
        }
        _providers = providers;
    }

    /// <inheritdoc/>
    public bool Exists(string relativePath)
    {
        foreach (IGumFileProvider provider in _providers)
        {
            if (provider.Exists(relativePath))
            {
                return true;
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public Stream OpenRead(string relativePath)
    {
        foreach (IGumFileProvider provider in _providers)
        {
            if (provider.Exists(relativePath))
            {
                return provider.OpenRead(relativePath);
            }
        }
        throw new FileNotFoundException(
            $"File '{relativePath}' was not found in any of the composed file providers.",
            relativePath);
    }

    /// <inheritdoc/>
    public IEnumerable<string> EnumerateFiles(string searchPattern)
    {
        HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (IGumFileProvider provider in _providers)
        {
            foreach (string path in provider.EnumerateFiles(searchPattern))
            {
                if (seen.Add(path))
                {
                    yield return path;
                }
            }
        }
    }
}
