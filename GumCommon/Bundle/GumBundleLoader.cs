using System;
using System.IO;
using ToolsUtilities;

namespace Gum.Bundle;

/// <summary>
/// Resolves whether a Gum project should load from loose `.gumx` + sibling files on disk
/// or from a sibling `.gumpkg` bundle, and installs the <see cref="FileManager.CustomGetStreamFromFile"/>
/// hook to serve bundle entries when needed. Per the bundle plan §4: loose wins when both exist
/// (dev convenience / hot reload). Production publishes only the bundle.
/// </summary>
public static class GumBundleLoader
{
    /// <summary>
    /// Inspects the directory containing <paramref name="gumxPath"/> for a sibling `.gumpkg`
    /// and returns a <see cref="BundleResolution"/> describing which source the loader should use.
    /// When bundle mode is selected, installs <see cref="FileManager.CustomGetStreamFromFile"/>
    /// to serve bundle entries (composing with any pre-existing user hook as a fallback).
    /// </summary>
    public static BundleResolution Resolve(string gumxPath)
    {
        if (string.IsNullOrEmpty(gumxPath))
        {
            throw new ArgumentException("Gumx path must be non-empty.", nameof(gumxPath));
        }

        // Normalize through FileManager so a relative input ("GumProject/GumProject.gumx")
        // resolves against FileManager.RelativeDirectory the same way GumProjectSave.Load
        // will resolve it. Without this, a desktop app launching from its bin/ folder
        // would probe ./GumProject/... while the deployed files live at ./Content/GumProject/...
        // and the loader would fall through to loose-mode and fail with "not found".
        string resolvedGumxPath = FileManager.IsRelative(gumxPath)
            ? FileManager.MakeAbsolute(gumxPath)
            : gumxPath;

        // Loose wins when both exist (plan §4.1 / §4.2). File.Exists returns false on
        // platforms without a real filesystem (Blazor WASM); on those targets the
        // convention is "ship the bundle" so falling through to bundle mode is correct.
        // Probing through CustomGetStreamFromFile would be unreliable: a host hook may
        // succeed for any input (e.g. a CDN catch-all) and falsely claim loose exists.
        if (File.Exists(resolvedGumxPath))
        {
            return new BundleResolution(usedBundle: false, resolvedGumxPath: resolvedGumxPath, previousHook: null);
        }

        string? directory = Path.GetDirectoryName(resolvedGumxPath);
        string nameWithoutExtension = Path.GetFileNameWithoutExtension(resolvedGumxPath);
        string bundlePath = string.IsNullOrEmpty(directory)
            ? nameWithoutExtension + ".gumpkg"
            : Path.Combine(directory!, nameWithoutExtension + ".gumpkg");

#if !NET7_0_OR_GREATER
        // .gumpkg loading requires System.Formats.Tar (net7+). On older targets fall back to
        // loose-file resolution; downstream Load will surface a clear "not found" for the .gumx.
        return new BundleResolution(usedBundle: false, resolvedGumxPath: resolvedGumxPath, previousHook: null);
#else
        // Read the bundle through the same file seam as the rest of the loader. On desktop
        // this is File.OpenRead; on TitleContainer-backed platforms (Blazor WASM / Android /
        // iOS) the host-installed CustomGetStreamFromFile hook routes the read through
        // TitleContainer.OpenStream. If neither resolves the file, we silently return loose
        // mode so the downstream "not found" path produces a familiar error.
        Stream? bundleStream = TryOpenBundle(bundlePath);
        if (bundleStream == null)
        {
            return new BundleResolution(usedBundle: false, resolvedGumxPath: resolvedGumxPath, previousHook: null);
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

        return new BundleResolution(usedBundle: true, resolvedGumxPath: resolvedGumxPath, previousHook: previousHook);
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

        string normalizedIncoming = incomingPath.Replace('\\', '/');
        string normalizedRoot = projectRoot.Replace('\\', '/').TrimEnd('/');

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
}

/// <summary>
/// Result of <see cref="GumBundleLoader.Resolve"/>: whether the loader should treat the
/// project as bundle-backed, the path to pass to <c>GumProjectSave.Load</c>, and the previous
/// <see cref="FileManager.CustomGetStreamFromFile"/> hook (so callers can restore it on dispose
/// if they need to fully unwind the install).
/// </summary>
public class BundleResolution
{
    /// <summary>True if a `.gumpkg` was found and the bundle hook was installed.</summary>
    public bool UsedBundle { get; }

    /// <summary>The path to pass to <c>GumProjectSave.Load</c>. Always the original `.gumx` path.</summary>
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
