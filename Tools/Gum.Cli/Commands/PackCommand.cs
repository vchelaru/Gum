using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using Gum.Bundle;
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

        var command = new Command("pack", "Pack a Gum project into a .gumpkg bundle.")
        {
            projectArgument,
            outputOption,
            includeOption
        };

        command.SetHandler((InvocationContext context) =>
        {
            string projectPath = context.ParseResult.GetValueForArgument(projectArgument);
            string? outputPath = context.ParseResult.GetValueForOption(outputOption);
            string includeFlags = context.ParseResult.GetValueForOption(includeOption) ?? "core,fontcache,external";
            context.ExitCode = Execute(projectPath, outputPath, includeFlags);
        });

        return command;
    }

    private static int Execute(string projectPath, string? outputPath, string includeFlags)
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

        string projectDirectory = Path.GetDirectoryName(fullPath) ?? "";

        string resolvedOutputPath = outputPath != null
            ? Path.GetFullPath(outputPath)
            : Path.Combine(projectDirectory, Path.GetFileNameWithoutExtension(fullPath) + ".gumpkg");

        GumProjectDependencyWalker walker = new GumProjectDependencyWalker();
        WalkResult walkResult = walker.Walk(loadResult.Project!, projectDirectory, inclusion);

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
            entries.Add((relativePath, bytes));
            uncompressedBytes += bytes.Length;
        }

        using (FileStream output = File.Create(resolvedOutputPath))
        {
            GumBundleWriter.Write(output, entries);
        }

        long compressedBytes = new FileInfo(resolvedOutputPath).Length;
        double ratio = uncompressedBytes == 0 ? 0.0 : (double)compressedBytes / uncompressedBytes * 100.0;

        Console.WriteLine($"Packed {entries.Count} files into {resolvedOutputPath}");
        Console.WriteLine($"  Core:          {walkResult.CoreFiles.Count}");
        Console.WriteLine($"  FontCache:     {walkResult.FontCacheFiles.Count}");
        Console.WriteLine($"  External:      {walkResult.ExternalFiles.Count}");
        Console.WriteLine($"Uncompressed:    {uncompressedBytes} bytes");
        Console.WriteLine($"Compressed:      {compressedBytes} bytes");
        Console.WriteLine($"Ratio:           {ratio:F1}%");

        return 0;
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
