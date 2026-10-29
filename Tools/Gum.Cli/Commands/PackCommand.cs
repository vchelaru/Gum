using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.Bundle;
using Gum.Expressions;
using Gum.Managers;
using Gum.ProjectServices;

namespace Gum.Cli.Commands;

/// <summary>
/// Defines the <c>gumcli pack</c> command which loads a project, walks its dependencies,
/// and writes a `.gumpkg` bundle containing the requested file categories.
/// </summary>
public static class PackCommand
{
    /// <summary>
    /// Creates the <c>pack</c> command definition.
    /// </summary>
    public static Command Create()
    {
        var projectArgument = new Argument<string>(
            "project",
            "Path to the .gumx project file.");

        var outputOption = new Option<string?>(
            aliases: new[] { "--output", "-o" },
            getDefaultValue: () => null,
            description: "Output path for the .gumpkg bundle. Defaults to <ProjectName>.gumpkg next to the .gumx.");

        var includeOption = new Option<string>(
            aliases: new[] { "--include" },
            getDefaultValue: () => "core,fontcache,external",
            description: "Comma-separated categories to include. Valid values: core, fontcache, external.");

        var premultipliedAlphaOption = new Option<bool>(
            aliases: new[] { "--premultiplied-alpha" },
            description: "Store PNG textures with premultiplied alpha, for games that set Renderer.UsePremultipliedAlpha " +
                "to true and for SkiaGum. Off by default (straight alpha).");

        var shakeOption = new Option<bool>(
            aliases: new[] { "--shake" },
            description: "Leave out every Screen and Component that no Screen or root reaches, with the files only they use.");

        var rootsOption = new Option<string[]>(
            aliases: new[] { "--roots" },
            getDefaultValue: () => Array.Empty<string>(),
            description: "With --shake: a text file of element names to keep (one per line, # starts a comment), such as elements created only from code.");

        var keepOption = new Option<string[]>(
            aliases: new[] { "--keep" },
            getDefaultValue: () => Array.Empty<string>(),
            description: "With --shake: element names to keep, comma-separated or repeated.");

        var rootsAssemblyOption = new Option<string[]>(
            aliases: new[] { "--roots-assembly" },
            getDefaultValue: () => Array.Empty<string>(),
            description: "With --shake: a built game assembly whose [assembly: GumElementRoot] attributes (written by the Gum.Analyzers generator) name elements to keep.");

        var command = new Command("pack", "Pack a Gum project into a .gumpkg bundle.")
        {
            projectArgument,
            outputOption,
            includeOption,
            premultipliedAlphaOption,
            shakeOption,
            rootsOption,
            keepOption,
            rootsAssemblyOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            string? outputPath = context.ParseResult.GetValueForOption(outputOption);
            string includeFlags = context.ParseResult.GetValueForOption(includeOption) ?? "core,fontcache,external";
            bool premultipliedAlpha = context.ParseResult.GetValueForOption(premultipliedAlphaOption);
            ShakeOptions shake = new ShakeOptions(
                context.ParseResult.GetValueForOption(shakeOption),
                context.ParseResult.GetValueForOption(rootsOption) ?? Array.Empty<string>(),
                context.ParseResult.GetValueForOption(keepOption) ?? Array.Empty<string>(),
                context.ParseResult.GetValueForOption(rootsAssemblyOption) ?? Array.Empty<string>());
            context.ExitCode = Execute(projectPath, outputPath, includeFlags, premultipliedAlpha, shake);
        });

        return command;
    }

    private sealed record ShakeOptions(bool Enabled, string[] RootFiles, string[] Keep, string[] RootAssemblies);

    private static int Execute(string projectPath, string? outputPath, string includeFlags, bool premultipliedAlpha,
        ShakeOptions shake)
    {
        string fullPath = Path.GetFullPath(projectPath);

        IProjectLoader loader = new ProjectLoader();
        ProjectLoadResult loadResult = loader.Load(fullPath);

        if (!loadResult.Success)
        {
            Console.Error.WriteLine(loadResult.ErrorMessage);
            return 2;
        }

        if (!TryParseInclusion(includeFlags, out GumBundleInclusion inclusion, out string? parseError))
        {
            Console.Error.WriteLine(parseError);
            return 2;
        }

        if (!shake.Enabled && (shake.RootFiles.Length > 0 || shake.Keep.Length > 0 || shake.RootAssemblies.Length > 0))
        {
            Console.Error.WriteLine("--roots, --keep and --roots-assembly only apply with --shake.");
            return 2;
        }

        string projectDirectory = Path.GetDirectoryName(fullPath) ?? "";

        // FontReferenceCollector (used by the walker for FontCache enumeration) resolves
        // component-instance BaseTypes via ObjectFinder.Self. Wire the loaded project in
        // before walking so inherited font variables (e.g. Strong.Font from Standards/Text)
        // resolve correctly.
        ObjectFinder.Self.GumProjectSave = loadResult.Project;

        // Wires Roslyn-based evaluation of VariableReferences so conditional (ternary) Font
        // expressions resolve (and every branch gets pregenerated for FontCache - see #4042)
        // instead of silently failing to resolve at all (mirrors CheckReferencesCommand).
        GumExpressionService.Initialize();

        string resolvedOutputPath = outputPath != null
            ? Path.GetFullPath(outputPath)
            : Path.Combine(projectDirectory, Path.GetFileNameWithoutExtension(fullPath) + ".gumpkg");

        GumProjectSave project = loadResult.Project!;
        GumElementReachabilityResult? reachability = null;
        if (shake.Enabled)
        {
            if (!TryReadRoots(shake, out List<string> roots, out string? rootsError))
            {
                Console.Error.WriteLine(rootsError);
                return 2;
            }
            reachability = new GumElementReachability().Compute(project, roots);
            if (reachability.UnknownRoots.Count > 0)
            {
                foreach (string unknownRoot in reachability.UnknownRoots)
                {
                    Console.Error.WriteLine($"Root '{unknownRoot}' names no Screen or Component in the project.");
                }
                return 2;
            }
        }

        GumProjectDependencyWalker walker = new GumProjectDependencyWalker();
        WalkResult walkResult = walker.Walk(project, projectDirectory, inclusion,
            reachability == null ? null : new HashSet<string>(reachability.KeptElementNames, StringComparer.OrdinalIgnoreCase));

        if (walkResult.MissingFiles.Count > 0)
        {
            foreach (DependencyWarning warning in walkResult.MissingFiles)
            {
                if (string.IsNullOrEmpty(warning.ReferencedFromElementName))
                {
                    Console.Error.WriteLine($"missing: {warning.ReferencedPath}");
                }
                else
                {
                    Console.Error.WriteLine($"missing: {warning.ReferencedPath} (referenced from: {warning.ReferencedFromElementName})");
                }
            }
            return 1;
        }

        LooseFileGumFileProvider provider = new LooseFileGumFileProvider(projectDirectory);
        List<(string path, byte[] content)> entries = new List<(string, byte[])>();
        long uncompressedBytes = 0;

        foreach (string relativePath in walkResult.AllIncludedFiles)
        {
            byte[] bytes;
            try
            {
                using Stream stream = provider.OpenRead(relativePath);
                using MemoryStream memory = new MemoryStream();
                stream.CopyTo(memory);
                bytes = memory.ToArray();
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine($"missing: {relativePath}");
                return 1;
            }
            if (reachability != null && reachability.TrimmedElementNames.Count > 0
                && string.Equals(relativePath, Path.GetFileName(fullPath), StringComparison.Ordinal))
            {
                bytes = WriteProjectWithoutTrimmedElements(project, fullPath, reachability.TrimmedElementNames);
            }

            if (IsPng(relativePath))
            {
                try
                {
                    bytes = ToRawTexture(bytes, premultipliedAlpha);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"error: could not decode PNG {relativePath}: {ex.Message}");
                    return 1;
                }
            }
            entries.Add((relativePath, bytes));
            uncompressedBytes += bytes.Length;
        }

        using (FileStream output = File.Create(resolvedOutputPath))
        {
            GumBundleWriter.Write(output, entries);
        }

        long compressedBytes = new FileInfo(resolvedOutputPath).Length;
        double ratio = uncompressedBytes == 0 ? 0.0 : (double)compressedBytes / uncompressedBytes * 100.0;

        if (reachability != null)
        {
            int total = project.Screens.Count + project.Components.Count;
            int trimmedCount = reachability.TrimmedElementNames.Count;
            string trimmedList = trimmedCount == 0 ? "" : ": " + string.Join(", ", reachability.TrimmedElementNames);
            Console.WriteLine($"Kept {total - trimmedCount} of {total} Screens and Components; trimmed {trimmedCount}{trimmedList}");
        }
        Console.WriteLine($"Packed {entries.Count} files into {resolvedOutputPath}");
        Console.WriteLine($"  Core:          {walkResult.CoreFiles.Count}");
        Console.WriteLine($"  FontCache:     {walkResult.FontCacheFiles.Count}");
        Console.WriteLine($"  External:      {walkResult.ExternalFiles.Count}");
        Console.WriteLine($"Uncompressed:    {uncompressedBytes} bytes");
        Console.WriteLine($"Compressed:      {compressedBytes} bytes");
        Console.WriteLine($"Ratio:           {ratio:F1}%");

        return 0;
    }

    private static bool IsPng(string relativePath) =>
        string.Equals(Path.GetExtension(relativePath), ".png", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Decodes a PNG to straight RGBA with StbImageSharp (the decoder behind MonoGame's and KNI's
    /// Texture2D.FromStream, so the pixels match a loose-file load) and stores it as a raw texture,
    /// premultiplied or with the transparent-edge bleed baked, so the runtime uploads it as-is.
    /// </summary>
    private static byte[] ToRawTexture(byte[] png, bool premultiply)
    {
        StbImageSharp.ImageResult image =
            StbImageSharp.ImageResult.FromMemory(png, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
        byte[] rgba = image.Data;
        if (premultiply)
        {
            global::RenderingLibrary.Content.RgbaPixels.Premultiply(rgba);
        }
        else
        {
            global::RenderingLibrary.Content.RgbaPixels.BleedTransparentEdges(rgba, image.Width, image.Height);
        }
        return GumRawTexture.Encode(image.Width, image.Height, rgba, premultiply);
    }

    private static bool TryReadRoots(ShakeOptions shake, out List<string> roots, out string? error)
    {
        roots = new List<string>();
        error = null;
        foreach (string rootFile in shake.RootFiles)
        {
            if (!File.Exists(rootFile))
            {
                error = $"Roots file '{rootFile}' was not found.";
                return false;
            }
            foreach (string line in File.ReadAllLines(rootFile))
            {
                string trimmed = line.Trim();
                if (trimmed.Length > 0 && !trimmed.StartsWith("#", StringComparison.Ordinal))
                {
                    roots.Add(trimmed);
                }
            }
        }
        foreach (string rootAssembly in shake.RootAssemblies)
        {
            if (!File.Exists(rootAssembly))
            {
                error = $"Roots assembly '{rootAssembly}' was not found.";
                return false;
            }
            roots.AddRange(GumElementRootAssemblyReader.Read(rootAssembly));
        }
        foreach (string keep in shake.Keep)
        {
            roots.AddRange(keep.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }
        return true;
    }

    // The bundled project file lists only the kept elements, since GumProjectSave.Load fails on a
    // reference whose file is missing, and records the trimmed names so the runtime can say an
    // element was trimmed rather than misspelled.
    private static byte[] WriteProjectWithoutTrimmedElements(GumProjectSave project, string projectFilePath,
        IReadOnlyList<string> trimmedElementNames)
    {
        HashSet<string> trimmed = new HashSet<string>(trimmedElementNames, StringComparer.OrdinalIgnoreCase);
        project.ScreenReferences.RemoveAll(reference => trimmed.Contains(reference.Name));
        project.ComponentReferences.RemoveAll(reference => trimmed.Contains(reference.Name));
        project.TrimmedElements = trimmedElementNames.ToList();

        string tempPath = Path.Combine(Path.GetTempPath(),
            "gumcli-pack-" + Path.GetRandomFileName() + Path.GetExtension(projectFilePath));
        try
        {
            project.Save(tempPath, saveElements: false);
            return File.ReadAllBytes(tempPath);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private static bool TryParseInclusion(string includeFlags, out GumBundleInclusion inclusion, out string? error)
    {
        inclusion = 0;
        error = null;

        if (string.IsNullOrWhiteSpace(includeFlags))
        {
            error = "--include must specify at least one category. Valid values: core, fontcache, external.";
            return false;
        }

        string[] tokens = includeFlags.Split(',');
        foreach (string raw in tokens)
        {
            string token = raw.Trim().ToLowerInvariant();
            if (token.Length == 0)
            {
                continue;
            }
            switch (token)
            {
                case "core":
                    inclusion |= GumBundleInclusion.Core;
                    break;
                case "fontcache":
                    inclusion |= GumBundleInclusion.FontCache;
                    break;
                case "external":
                    inclusion |= GumBundleInclusion.ExternalFiles;
                    break;
                default:
                    error = $"Unknown --include value '{token}'. Valid values: core, fontcache, external.";
                    return false;
            }
        }

        if (inclusion == 0)
        {
            error = "--include must specify at least one category. Valid values: core, fontcache, external.";
            return false;
        }

        return true;
    }
}
