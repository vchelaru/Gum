using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace ImportFromGumxPlugin.Services;

/// <summary>
/// Where the asset files a source project names from outside its folder (a <c>..</c> path or an absolute path)
/// land in the destination: an <c>external</c> folder at the destination project's root (#7392). Built before
/// anything is written, from reads only, so the elements can be compared and written with their final paths.
/// A file with the same content already there is reused; a different file with the same name gets
/// <c>name-2.ext</c>; an animation chain keeps its textures' layout relative to it.
/// </summary>
internal sealed class ExternalAssetPlan
{
    public const string DefaultFolderName = "external";

    public static ExternalAssetPlan Empty { get; } = new ExternalAssetPlan();

    private readonly Dictionary<string, string> _destinationBySource = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, string>> _chainTextureKeys = new(StringComparer.OrdinalIgnoreCase);

    private ExternalAssetPlan() { }

    public bool HasEntries => _destinationBySource.Count > 0;

    public bool Contains(string sourcePath) => _destinationBySource.ContainsKey(sourcePath);

    /// <summary>The destination path (relative to the destination project, forward slashes) for a source path.</summary>
    public bool TryGetDestination(string sourcePath, out string destination) =>
        _destinationBySource.TryGetValue(sourcePath, out destination!);

    /// <summary>The key a texture of an outside chain is fetched and copied by, or false for a chain that is not planned.</summary>
    public bool TryGetChainTexture(string chainPath, string textureName, out string key)
    {
        key = "";
        return _chainTextureKeys.TryGetValue(chainPath, out Dictionary<string, string>? names)
            && names.TryGetValue(textureName, out key!);
    }

    public bool HasChain(string chainPath) => _chainTextureKeys.ContainsKey(chainPath);

    /// <summary>Points every variable of the element, in every state and category, that names a planned file at its copy.</summary>
    public void Rewrite(ElementSave element)
    {
        if (_destinationBySource.Count == 0) return;
        foreach (StateSave state in element.AllStates)
        {
            foreach (VariableSave variable in state.Variables) Rewrite(variable);
        }
    }

    /// <summary>Every variable a behavior carries: its category states, required variables and forms properties.</summary>
    public static IEnumerable<VariableSave> GetVariables(BehaviorSave behavior) =>
        behavior.AllStates.SelectMany(state => state.Variables)
            .Concat(behavior.RequiredVariables.Variables)
            .Concat(behavior.FormsProperties);

    /// <summary>Points the behavior's variables that name a planned file at its copy.</summary>
    public void Rewrite(BehaviorSave behavior)
    {
        foreach (VariableSave variable in GetVariables(behavior)) Rewrite(variable);
    }

    /// <summary>Points one variable at the copy of the file it names, if that file was planned. Returns the variable.</summary>
    public VariableSave Rewrite(VariableSave variable)
    {
        if (variable.Value is string text && _destinationBySource.TryGetValue(text, out string? destination))
            variable.Value = destination;
        return variable;
    }

    /// <param name="assetPaths">Every asset path the imported elements name, as written.</param>
    /// <param name="readChainTextures">Reads the texture names and FileRelativeTextures of the chain file at a full path, or null if unreadable.</param>
    public static ExternalAssetPlan Build(
        IEnumerable<string> assetPaths,
        string sourceBase,
        string projectDir,
        Func<string, (List<string> Names, bool FileRelative)?> readChainTextures)
    {
        if (sourceBase.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || sourceBase.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return Empty;
        }

        string root = Path.GetFullPath(sourceBase);
        List<(string Written, string Full)> outside = new();
        foreach (string written in assetPaths)
        {
            if (written.Contains("://")) continue;
            bool isOutside = Path.IsPathRooted(written)
                || SourceProjectContainment.GetRefusal(sourceBase, written) == SourceProjectContainment.LeavesProjectReason;
            if (!isOutside) continue;

            string full = Path.GetFullPath(Path.IsPathRooted(written)
                ? written
                : Path.Combine(root, written.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)));
            if (File.Exists(full)) outside.Add((written, full));
        }
        if (outside.Count == 0) return Empty;

        ExternalAssetPlan plan = new();
        Planner planner = new(projectDir);
        Dictionary<string, string> destinationByFull = new(StringComparer.OrdinalIgnoreCase);

        foreach (var group in outside.GroupBy(item => item.Full, StringComparer.OrdinalIgnoreCase))
        {
            string full = group.Key;
            if (!destinationByFull.TryGetValue(full, out string? destination))
            {
                bool isChain = full.EndsWith(".achx", StringComparison.OrdinalIgnoreCase)
                    || full.EndsWith(".achj", StringComparison.OrdinalIgnoreCase);
                (List<string> Names, bool FileRelative)? chain = isChain ? readChainTextures(full) : null;
                if (chain == null)
                {
                    destination = planner.PlaceSingle(full);
                    destinationByFull[full] = destination;
                }
                else
                {
                    destination = PlaceChain(plan, planner, destinationByFull, full, chain.Value, root, group.First().Written);
                }
            }
            foreach (var item in group) plan._destinationBySource[item.Written] = destination;
        }
        return plan;
    }

    private static string PlaceChain(
        ExternalAssetPlan plan, Planner planner, Dictionary<string, string> destinationByFull,
        string chainFull, (List<string> Names, bool FileRelative) chain, string root, string chainWritten)
    {
        string chainDir = Path.GetDirectoryName(chainFull)!;
        Dictionary<string, string> textureFull = new(StringComparer.OrdinalIgnoreCase);
        foreach (string name in chain.Names)
        {
            if (Path.IsPathRooted(name) || name.Contains("://")) continue;
            textureFull[name] = Path.GetFullPath(Path.Combine(
                chain.FileRelative ? chainDir : root,
                name.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)));
        }

        List<string> members = new() { chainFull };
        members.AddRange(textureFull.Values.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(path => !path.Equals(chainFull, StringComparison.OrdinalIgnoreCase)));
        string commonDir = CommonDirectory(members.Select(path => Path.GetDirectoryName(path)!));
        Dictionary<string, string> placed = planner.PlaceGroup(members, commonDir);

        foreach (var pair in placed) destinationByFull[pair.Key] = pair.Value;
        Dictionary<string, string> keys = new(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in textureFull)
        {
            keys[pair.Key] = pair.Value;
            if (placed.TryGetValue(pair.Value, out string? textureDestination))
                plan._destinationBySource[pair.Value] = textureDestination;
        }
        plan._chainTextureKeys[chainWritten] = keys;
        return placed[chainFull];
    }

    private static string CommonDirectory(IEnumerable<string> directories)
    {
        string[] common = Array.Empty<string>();
        bool first = true;
        foreach (string directory in directories)
        {
            string[] segments = directory.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            if (first) { common = segments; first = false; continue; }
            int length = 0;
            while (length < common.Length && length < segments.Length
                && common[length].Equals(segments[length], StringComparison.OrdinalIgnoreCase)) length++;
            common = common.Take(length).ToArray();
        }
        return string.Join(Path.DirectorySeparatorChar, common);
    }

    /// <summary>Picks free or identical destination paths under the external folder and remembers what this import already took.</summary>
    private sealed class Planner
    {
        private readonly string _projectDir;
        private readonly string _folder;
        private readonly Dictionary<string, string> _existing = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _claimed = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _hashes = new(StringComparer.OrdinalIgnoreCase);

        public Planner(string projectDir)
        {
            _projectDir = projectDir;
            // A folder the user already has under this name (any casing) is reused.
            string? existingFolder = Directory.Exists(projectDir)
                ? Directory.EnumerateDirectories(projectDir)
                    .Select(Path.GetFileName)
                    .FirstOrDefault(name => string.Equals(name, DefaultFolderName, StringComparison.OrdinalIgnoreCase))
                : null;
            _folder = existingFolder ?? DefaultFolderName;

            string folderPath = Path.Combine(projectDir, _folder);
            if (Directory.Exists(folderPath))
            {
                foreach (string file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    string relative = _folder + "/" + Path.GetRelativePath(folderPath, file).Replace(Path.DirectorySeparatorChar, '/');
                    _existing[relative] = file;
                }
            }
        }

        public string PlaceSingle(string full)
        {
            string name = Path.GetFileName(full);
            string stem = Path.GetFileNameWithoutExtension(name);
            string extension = Path.GetExtension(name);
            for (int attempt = 1; ; attempt++)
            {
                string candidate = _folder + "/" + (attempt == 1 ? name : $"{stem}-{attempt}{extension}");
                if (TryClaim(candidate, full, out string destination)) return destination;
            }
        }

        public Dictionary<string, string> PlaceGroup(List<string> files, string commonDir)
        {
            string leaf = Path.GetFileName(commonDir);
            if (leaf.Length == 0) leaf = "files";
            for (int attempt = 1; ; attempt++)
            {
                string prefix = attempt == 1 ? "" : $"{leaf}-{attempt}/";
                Dictionary<string, string> candidates = files.ToDictionary(
                    file => file,
                    file => _folder + "/" + prefix + Path.GetRelativePath(commonDir, file).Replace(Path.DirectorySeparatorChar, '/'),
                    StringComparer.OrdinalIgnoreCase);
                if (!candidates.All(pair => Fits(pair.Value, pair.Key))) continue;

                Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in candidates)
                {
                    TryClaim(pair.Value, pair.Key, out string destination);
                    result[pair.Key] = destination;
                }
                return result;
            }
        }

        private bool Fits(string candidate, string full)
        {
            if (_claimed.TryGetValue(candidate, out string? claimedBy)) return SameContent(claimedBy, full);
            if (_existing.TryGetValue(candidate, out string? onDisk)) return SameContent(onDisk, full);
            return !File.Exists(Path.Combine(_projectDir, candidate)) && !Directory.Exists(Path.Combine(_projectDir, candidate));
        }

        private bool TryClaim(string candidate, string full, out string destination)
        {
            destination = candidate;
            if (!Fits(candidate, full)) return false;
            // An identical file already there is reused under the spelling it has.
            if (_existing.TryGetValue(candidate, out string? onDisk))
                destination = _folder + "/" + Path.GetRelativePath(Path.Combine(_projectDir, _folder), onDisk).Replace(Path.DirectorySeparatorChar, '/');
            _claimed.TryAdd(candidate, full);
            return true;
        }

        private bool SameContent(string first, string second) =>
            first.Equals(second, StringComparison.OrdinalIgnoreCase) || Hash(first) == Hash(second);

        private string Hash(string path)
        {
            if (!_hashes.TryGetValue(path, out string? hash))
            {
                hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
                _hashes[path] = hash;
            }
            return hash;
        }
    }
}
