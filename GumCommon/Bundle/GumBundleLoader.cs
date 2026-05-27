using System;
using System.IO;
using System.Text;
using ToolsUtilities;

namespace Gum.Bundle;

/// <summary>
/// Resolves a Gum project path to either loose-file or bundle-backed loading based on the
/// extension of <c>projectPath</c>: <c>.gumx</c> means loose, <c>.gumpkg</c> means bundle.
/// No sibling probing happens — the caller's chosen extension is the single source of truth,
/// which keeps behavior identical across desktop and streaming-only platforms (Blazor WASM,
/// Android, iOS) where a probe would otherwise issue a guaranteed-404 HTTP request.
/// When bundle mode is selected, installs <see cref="FileManager.CustomGetStreamFromFile"/>
/// to serve bundle entries (composing with any pre-existing user hook as a fallback).
/// </summary>
public static class GumBundleLoader
{
    /// <summary>
    /// Returns a <see cref="BundleResolution"/> describing how to load <paramref name="projectPath"/>.
    /// The extension picks the mode: <c>.gumx</c> = loose, <c>.gumpkg</c> = bundle. Any other
    /// extension throws.
    /// </summary>
    public static BundleResolution Resolve(string projectPath)
    {
        if (string.IsNullOrEmpty(projectPath))
        {
            throw new ArgumentException("Project path must be non-empty.", nameof(projectPath));
        }

        // Normalize through FileManager so a relative input ("GumProject/GumProject.gumx")
        // resolves against FileManager.RelativeDirectory the same way GumProjectSave.Load
        // will resolve it.
        string resolvedPath = FileManager.IsRelative(projectPath)
            ? FileManager.MakeAbsolute(projectPath)
            : projectPath;

        string extension = Path.GetExtension(resolvedPath);

        if (string.Equals(extension, ".gumx", StringComparison.OrdinalIgnoreCase))
        {
            // Loose mode: hand the path straight to GumProjectSave.Load. No probing for a
            // sibling .gumpkg — if the caller wanted a bundle they would have said so.
            return new BundleResolution(usedBundle: false, resolvedGumxPath: resolvedPath, previousHook: null);
        }

        if (!string.Equals(extension, ".gumpkg", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Project path '{projectPath}' must end with '.gumx' (loose) or '.gumpkg' (bundle).",
                nameof(projectPath));
        }

#if !NET7_0_OR_GREATER
        // .gumpkg loading requires System.Formats.Tar (net7+).
        throw new NotSupportedException(".gumpkg loading requires net7.0 or greater.");
#else
        // The bundle stores its own .gumx by name; the path we hand to GumProjectSave.Load is
        // the sibling .gumx path, and the hook below intercepts that read so it's served from
        // the bundle stream. Callers never need to know the .gumx name themselves.
        string? directory = Path.GetDirectoryName(resolvedPath);
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(resolvedPath);
        string gumxPath = string.IsNullOrEmpty(directory)
            ? nameWithoutExtension + ".gumx"
            : Path.Combine(directory!, nameWithoutExtension + ".gumx");

        // Read the bundle through the same file seam as the rest of the loader. On desktop
        // this is File.OpenRead; on TitleContainer-backed platforms (Blazor WASM / Android /
        // iOS) the host-installed CustomGetStreamFromFile hook routes the read through
        // TitleContainer.OpenStream.
        Stream? bundleStream = TryOpenBundle(resolvedPath);
        if (bundleStream == null)
        {
            throw new FileNotFoundException(
                $"The Gum bundle '{resolvedPath}' was not found.",
                resolvedPath);
        }

        GumBundle bundle;
        using (bundleStream)
        {
            bundle = GumBundleReader.Read(bundleStream);
        }

        BundleGumFileProvider provider = new BundleGumFileProvider(bundle);
        string projectRoot = string.IsNullOrEmpty(directory) ? "." : directory!;
        Func<string, Stream>? previousHook = FileManager.CustomGetStreamFromFile;

        // Compose: bundle entries first, fall back to any pre-existing user hook on miss.
        // Don't clobber state set by the host (e.g. their own asset zip — see SystemManagers.cs:141).
        FileManager.CustomGetStreamFromFile = incomingPath =>
        {
            string? relative = TryMakeRelative(incomingPath, projectRoot);
            if (relative != null && provider.Exists(relative))
            {
                return provider.OpenRead(relative);
            }

            if (previousHook != null)
            {
                return previousHook(incomingPath);
            }

            // No user fallback — surface a meaningful FileNotFoundException so callers
            // can distinguish "asked for a path the bundle doesn't have" from generic IO errors.
            throw new FileNotFoundException(
                $"File '{incomingPath}' was not found in the Gum bundle and no fallback file provider is installed.",
                incomingPath);
        };

        return new BundleResolution(usedBundle: true, resolvedGumxPath: gumxPath, previousHook: previousHook);
#endif
    }

    private static Stream? TryOpenBundle(string bundlePath)
    {
        if (File.Exists(bundlePath))
        {
            return File.OpenRead(bundlePath);
        }

        if (FileManager.CustomGetStreamFromFile == null)
        {
            return null;
        }

        try
        {
            return FileManager.CustomGetStreamFromFile(bundlePath);
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
    }

    private static string? TryMakeRelative(string incomingPath, string projectRoot)
    {
        if (string.IsNullOrEmpty(incomingPath))
        {
            return null;
        }

        // Normalize separators AND collapse runs of slashes — on Blazor WASM, Gum's
        // FileManager.MakeAbsolute can occasionally produce paths with doubled slashes
        // (e.g. "//Content/foo") when concatenating against a RelativeDirectory that
        // already starts with one. Without collapsing, the literal prefix-match below
        // would fail and the bundled asset would silently fall through to the host hook.
        string normalizedIncoming = CollapseSlashes(incomingPath.Replace('\\', '/'));
        string normalizedRoot = CollapseSlashes(projectRoot.Replace('\\', '/')).TrimEnd('/');

        if (normalizedRoot.Length == 0 || normalizedRoot == ".")
        {
            return normalizedIncoming;
        }

        // Case-insensitive prefix match on the directory portion: Windows file systems are
        // case-insensitive and FileManager.MakeAbsolute may differ in case from the original
        // root we captured. The bundle key lookup itself stays case-sensitive (see
        // BundleGumFileProvider) — this only relaxes the directory-prefix strip.
        if (normalizedIncoming.Length > normalizedRoot.Length
            && normalizedIncoming.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
            && normalizedIncoming[normalizedRoot.Length] == '/')
        {
            return normalizedIncoming.Substring(normalizedRoot.Length + 1);
        }

        return null;
    }

    private static string CollapseSlashes(string path)
    {
        if (string.IsNullOrEmpty(path) || path.IndexOf("//", StringComparison.Ordinal) < 0)
        {
            return path;
        }
        StringBuilder sb = new StringBuilder(path.Length);
        bool prevSlash = false;
        foreach (char c in path)
        {
            if (c == '/')
            {
                if (!prevSlash)
                {
                    sb.Append('/');
                }
                prevSlash = true;
            }
            else
            {
                sb.Append(c);
                prevSlash = false;
            }
        }
        return sb.ToString();
    }
}

/// <summary>
/// Result of <see cref="GumBundleLoader.Resolve"/>: whether the loader should treat the
/// project as bundle-backed, the path to pass to <c>GumProjectSave.Load</c>, and the previous
/// <see cref="FileManager.CustomGetStreamFromFile"/> hook (so callers can restore it on dispose
/// if they need to fully unwind the install).
/// </summary>
public class BundleResolution
{
    /// <summary>True if the caller passed a `.gumpkg` and the bundle hook was installed.</summary>
    public bool UsedBundle { get; }

    /// <summary>
    /// The path to pass to <c>GumProjectSave.Load</c>. For loose mode this is the original
    /// `.gumx` path; for bundle mode this is the synthetic `.gumx` path inside the bundle that
    /// the installed hook will intercept.
    /// </summary>
    public string ResolvedGumxPath { get; }

    /// <summary>
    /// The <see cref="FileManager.CustomGetStreamFromFile"/> hook value at the time
    /// <see cref="GumBundleLoader.Resolve"/> ran, or <c>null</c> if none was set or bundle mode
    /// was not selected. Callers that wish to fully uninstall the bundle hook should restore
    /// this value rather than null'ing the field.
    /// </summary>
    public Func<string, Stream>? PreviousHook { get; }

    /// <summary>Initializes a new <see cref="BundleResolution"/>.</summary>
    public BundleResolution(bool usedBundle, string resolvedGumxPath, Func<string, Stream>? previousHook)
    {
        UsedBundle = usedBundle;
        ResolvedGumxPath = resolvedGumxPath;
        PreviousHook = previousHook;
    }
}
