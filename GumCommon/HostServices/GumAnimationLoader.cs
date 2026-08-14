using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Gum.Bundle;
using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.SaveClasses;
using ToolsUtilities;

// Companion to Gum.GumService / GumServiceSkiaBase (issue #4452 / #4460): the *Animations.ganx/.ganj
// loading logic both host families need. Lives in GumCommon so GumService (MonoGame/Raylib/Silk-partial
// family) and GumServiceSkiaBase (WPF/MAUI/standalone/Silk-on-Skia) share one implementation instead of
// duplicating it. Stateless orchestration over caller-supplied inputs (an IGumFileProvider, a bundle
// flag) rather than an instance you construct and hold, mirroring GumBundleLoader's shape -- unlike
// GumHotReloadManager/WindowFitController, nothing here varies per host at construction time.
namespace Gum;

public static class GumAnimationLoader
{
    /// <summary>
    /// Loads animations for all elements in the currently-loaded project (<see cref="ObjectFinder.GumProjectSave"/>)
    /// by enumerating <c>*Animations.ganx</c> and <c>*Animations.ganj</c> files through <paramref name="fileProvider"/>.
    /// </summary>
    /// <param name="fileProvider">
    /// The file provider produced when the project was loaded (e.g. <c>ProjectResolution.FileProvider</c>),
    /// or <c>null</c> if the project wasn't loaded through a provider-producing path.
    /// </param>
    /// <param name="usedBundle">
    /// Whether the project was loaded from a bundle (<c>.gumpkg</c>). Suppresses the "no animations found"
    /// warning, since bundle mode's in-memory entry list is the manifest and an empty result is unambiguous
    /// there; loose mode can't distinguish "genuinely no animations" from "can't enumerate on this platform"
    /// (streaming platforms like Blazor WASM), so it warns instead.
    /// </param>
    /// <remarks>
    /// This enumerates once instead of probing <see cref="FileManager.FileExists"/> per element. In bundle
    /// mode the enumeration is an in-memory dictionary scan; in loose mode on a real filesystem it is a
    /// single directory walk, but loose mode on a streaming platform cannot enumerate a directory over
    /// HTTP, so no animations load there — package the project as a <c>.gumpkg</c> to ship animations to
    /// those platforms.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a Gum project hasn't been loaded first, or if no project file provider is available
    /// (e.g. the project was injected directly rather than loaded via <c>Initialize(...gumProjectFile...)</c>).
    /// </exception>
    public static void LoadAnimations(IGumFileProvider? fileProvider, bool usedBundle = false)
    {
        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;

        if (project == null)
        {
            throw new InvalidOperationException(
                "You must first load a project before attempting to load its animations. " +
                "Did you call GumUI.Initialize with a valid .gumx first?");
        }

        if (fileProvider == null)
        {
            throw new InvalidOperationException(
                "No project file provider is available. LoadAnimations enumerates animation files " +
                "through the provider produced when the project is loaded — load the project via " +
                "Initialize(...gumProjectFile...) before calling LoadAnimations.");
        }

        int loaded = LoadAnimationsFromProvider(project, fileProvider);

        // Loose mode relies on directory enumeration, which streaming platforms (Blazor WASM)
        // can't do over HTTP — there the enumeration silently returns nothing. Surface that once
        // so a developer who expected animations isn't left guessing. Bundle mode never has this
        // problem (the in-memory entry list is the manifest), so it stays quiet.
        if (!usedBundle && loaded == 0)
        {
            Console.WriteLine(
                "[Gum] No animation (*Animations.ganx/*Animations.ganj) files were found for this loosely-loaded project. " +
                "Loose-mode animation loading enumerates the project directory, which is unavailable on " +
                "browser/streaming platforms (e.g. Blazor WASM) — package the project as a .gumpkg to " +
                "load animations on those platforms.");
        }
    }

    /// <summary>
    /// Enumerates <c>*Animations.ganx</c> and <c>*Animations.ganj</c> files from <paramref name="provider"/>,
    /// deserializes each, and adds the result to <paramref name="project"/>'s
    /// <see cref="GumProjectSave.ElementAnimations"/>. The element name is derived from the file's path,
    /// not from the (stale) value serialized inside the file. Returns the number of animation files loaded.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Deserializes ElementAnimationsSave, which GumCommon's ILLink.Descriptors.xml preserves in full (preserve=\"all\") under Gum.StateAnimation.SaveClasses.*.")]
    public static int LoadAnimationsFromProvider(GumProjectSave project, IGumFileProvider provider)
    {
        // Filename-only pattern (no '/'): GlobMatcher matches it against the file name regardless of
        // directory depth, so nested component folders (Components/Buttons/MyButtonAnimations.ganx)
        // are found. A "**/*Animations.ganx" pattern would NOT work — GlobMatcher has no recursive
        // '**' support and would only match files exactly one folder deep.
        int loaded = 0;
        foreach (string path in provider.EnumerateFiles("*Animations.ganx"))
        {
            using Stream stream = provider.OpenRead(path);
            ElementAnimationsSave animation = FileManager.XmlDeserializeFromStream<ElementAnimationsSave>(stream);
            animation.ElementName = ElementNameFromPath(path);
            project.ElementAnimations.Add(animation);
            loaded++;
        }
        foreach (string path in provider.EnumerateFiles("*Animations.ganj"))
        {
            using Stream stream = provider.OpenRead(path);
            using StreamReader reader = new StreamReader(stream);
            string json = reader.ReadToEnd();
            ElementAnimationsSave animation = Gum.DataTypes.Serialization.Json.GumAnimationJsonFileSerializer.DeserializeElementAnimations(json);
            animation.ElementName = ElementNameFromPath(path);
            project.ElementAnimations.Add(animation);
            loaded++;
        }
        return loaded;
    }

    /// <summary>
    /// Maps an animation file path back to its element name — the inverse of the
    /// <c>{categoryFolder}/{element.Name}Animations.ganx</c> (or <c>.ganj</c>) convention. Strips the
    /// <c>Animations.ganx</c>/<c>Animations.ganj</c> suffix and any leading category folder so a nested
    /// component path like <c>Components/Buttons/MyButtonAnimations.ganx</c> resolves to
    /// <c>Buttons/MyButton</c>.
    /// </summary>
    internal static string ElementNameFromPath(string path)
    {
        string normalized = path.Replace('\\', '/');

        string withoutSuffix = normalized;
        foreach (string suffix in AnimationFileSuffixes)
        {
            if (normalized.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                withoutSuffix = normalized.Substring(0, normalized.Length - suffix.Length);
                break;
            }
        }

        foreach (string categoryFolder in AnimationCategoryFolders)
        {
            if (withoutSuffix.StartsWith(categoryFolder, StringComparison.OrdinalIgnoreCase))
            {
                return withoutSuffix.Substring(categoryFolder.Length);
            }
        }

        return withoutSuffix;
    }

    private static readonly string[] AnimationFileSuffixes =
        { "Animations.ganx", "Animations.ganj" };

    private static readonly string[] AnimationCategoryFolders =
        { "Screens/", "Components/", "StandardElements/" };
}
