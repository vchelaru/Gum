// ColoredRectangle, Text, Sprite, NineSlice and Container are the standards the project files in this sweep instantiate.
#pragma warning disable CS0618
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Localization;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using ToolsUtilities;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// The disk side of the Preview accuracy sweep, round 3. Unlike round 2 the folder is not rebuilt from a
/// baseline for every edit: the project is loaded from disk, edited in memory and saved back, the way the
/// tool works, so edits accumulate and a test can save several times. The oracle is the round 2 dump
/// (<see cref="PreviewAccuracy2World.Describe"/>) of the running tree against a fresh load of a copy of the folder.
/// </summary>
internal sealed class PreviewAccuracy3World : IDisposable
{
    private static int _freshCounter;

    // The folder name carries the round 2 prefix so the round 2 dump normalizes the paths it prints.
    public string Dir { get; } = Path.Combine(Path.GetTempPath(), "PreviewAccuracy2_" + Path.GetRandomFileName());
    public string Gumx => Path.Combine(Dir, "Proj.gumx");

    public DateTime Now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public GumHotReloadManager Manager = null!;
    public GraphicalUiElement Screen = null!;
    public readonly List<HotReloadResult> Results = new List<HotReloadResult>();
    public readonly SizedTextureLoader Loader = new SizedTextureLoader();
    public readonly List<string> Errors = new List<string>();
    public string ShownElement = "MainScreen";

    /// <summary>The state the tool selected, applied the way Preview applies it: after every build and after every hot reload.</summary>
    public PreviewSelectionMessage? Selection;

    /// <summary>Layout of elements nobody sees (AbsoluteVisible false) is computed lazily, so it is left out of the comparison when set.</summary>
    public bool IgnoreLayoutOfInvisible;

    private readonly ILocalizationService? _previousLocalization = CustomSetPropertyOnRenderable.LocalizationService;
    private readonly bool _previousCacheTextures = LoaderManager.Self.CacheTextures;
    private readonly string _previousRelativeDirectory = FileManager.RelativeDirectory;

    public PreviewAccuracy3World()
    {
        Directory.CreateDirectory(Dir);
    }

    /// <summary>Writes <paramref name="project"/> into the folder (with the images and fonts <paramref name="assets"/> adds), builds the shown element and starts the real manager.</summary>
    public void Boot(GumProjectSave project, string shown = "MainScreen", Action<string>? assets = null)
    {
        ShownElement = shown;
        LoaderManager.Self.CacheTextures = true;
        LoaderManager.Self.ContentLoader = Loader;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Errors.Add;
        PreviewAccuracy2World.WriteBaselineAssets(Dir);
        assets?.Invoke(Dir);
        PreviewAccuracy2World.SaveAll(project, Gumx);
        FileManager.RelativeDirectory = Dir + "/";
        Screen = LoadAndBuild(Gumx, shown);
        Screen.AddToRoot();
        ApplySelectedState(Screen);

        Manager = new GumHotReloadManager(
            GumService.ApplyProjectTextureFilter,
            GumAnimationLoader.LoadAnimationsFromProvider,
            path => LoaderManager.Self.Dispose(path),
            () => Now);
        Manager.ReloadFinished += Results.Add;
        // Preview re-applies the selected state when a reload completes (Game1: HotReloadCompleted += ApplySelectedState).
        Manager.ReloadCompleted += () => ApplySelectedState(Screen);
        Manager.Start(Gumx);
        Manager.Stop();
    }

    private void ApplySelectedState(GraphicalUiElement root)
    {
        StateSave? state = Selection?.FindState(root.ElementSave!);
        if (state != null)
        {
            root.ApplyState(state);
        }
    }

    /// <summary>The saved project, loaded from the folder as the tool loads it.</summary>
    public GumProjectSave LoadSaved()
    {
        GumProjectSave project = GumProjectSave.Load(Gumx);
        project.Initialize();
        return project;
    }

    /// <summary>Loads the folder's project, applies <paramref name="edit"/>, saves it as the tool does and notifies the files whose bytes changed.</summary>
    public PreviewVerdict2 Save(Action<GumProjectSave> edit, Action<string>? assets = null, Func<string, bool>? notify = null)
    {
        Dictionary<string, string> before = Snapshot();
        assets?.Invoke(Dir);
        GumProjectSave project = LoadSaved();
        edit(project);
        foreach (ElementSave element in project.AllElements)
        {
            element.Instances.RemoveAll(instance => instance.DefinedByBase);
        }
        project.Initialize();
        PreviewAccuracy2World.SaveAll(project, Gumx);
        return Reload(ChangedSince(before).Where(file => notify?.Invoke(file) ?? true).ToArray());
    }

    public Dictionary<string, string> Snapshot() => Directory.EnumerateFiles(Dir, "*.*", SearchOption.AllDirectories)
        .ToDictionary(file => file, file => Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(File.ReadAllBytes(file))));

    public string[] ChangedSince(Dictionary<string, string> before) => Snapshot()
        .Where(pair => !before.TryGetValue(pair.Key, out string? hash) || hash != pair.Value).Select(pair => pair.Key).ToArray();

    /// <summary>Notifies every Gum file on disk (a save that rewrote them all) and runs the reload.</summary>
    public PreviewVerdict2 Touch() => Reload(Directory.EnumerateFiles(Dir, "*.*", SearchOption.AllDirectories)
        .Where(file => Path.GetExtension(file).ToLowerInvariant() is ".gumx" or ".gusx" or ".gucx" or ".gutx" or ".behx" or ".ganx").ToArray());

    /// <summary>Notifies exactly these files, waits out the debounce and runs the game's frames; the verdict is for the reload it ran, if any.</summary>
    public PreviewVerdict2 Reload(params string[] files)
    {
        foreach (string file in files)
        {
            Manager.NotifyFileChanged(file);
        }
        return Settle();
    }

    public PreviewVerdict2 Settle()
    {
        int before = Results.Count;
        Now = Now.AddMilliseconds(300);
        Manager.Update(new[] { Screen });
        HotReloadResult? result = Results.Count > before ? Results[Results.Count - 1] : null;
        return Verdict(result);
    }

    /// <summary>Whether both trees are laid out before they are dumped: a game lays its screen out in its first frame.</summary>
    public bool LayOutBothBeforeDumping = true;

    public PreviewVerdict2 Verdict(HotReloadResult? result)
    {
        if (LayOutBothBeforeDumping)
        {
            GumService.Default.Root.UpdateLayout();
        }
        return new PreviewVerdict2(Mask(PreviewAccuracy2World.Describe(Screen) + Animations()), Mask(DescribeAFreshLoad()), result);
    }

    private static readonly HashSet<string> _layoutTokens = new HashSet<string>
    {
        "AbsoluteBottom", "AbsoluteHeight", "AbsoluteLeft", "AbsoluteRight", "AbsoluteTop", "AbsoluteWidth", "abs",
        "Height", "Width", "X", "Y", "r.Height", "r.Width", "r.X", "r.Y", "r.EffectiveHeight", "r.EffectiveWidth", "r.Rotation", "Rotation",
    };

    private string Mask(string dump)
    {
        if (!IgnoreLayoutOfInvisible)
        {
            return dump;
        }
        return string.Join("\n", dump.Split('\n').Select(line => !line.Contains("AbsoluteVisible=False")
            ? line
            : string.Join(' ', line.Split(' ').Where(token => !_layoutTokens.Contains(token.Split('=')[0])))));
    }

    /// <summary>The state animations the project holds, one line per element animation set.</summary>
    private static string Animations()
    {
        GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
        return "animations: " + string.Join(" / ", (project?.ElementAnimations ?? new List<Gum.StateAnimation.SaveClasses.ElementAnimationsSave>())
            .OrderBy(set => set.ElementName, StringComparer.Ordinal)
            .Select(set => set.ElementName + "=" + string.Join(";", set.Animations.Select(animation =>
                animation.Name + (animation.Loops ? "(loop)" : "") + ":" + string.Join(",", animation.States.Select(state => state.StateName + "@" + state.Time + "/" + state.InterpolationType + "/" + state.Easing))
                + "|" + string.Join(",", animation.Events.Select(item => item.Name + "@" + item.Time)))))) + "\n";
    }

    private static GraphicalUiElement LoadAndBuild(string gumx, string shown)
    {
        GumProjectSave loaded = GumProjectSave.Load(gumx);
        loaded.Initialize();
        ObjectFinder.Self.GumProjectSave = loaded;
        GumAnimationLoader.LoadAnimationsFromProvider(loaded, new Gum.Bundle.LooseFileGumFileProvider(Path.GetDirectoryName(gumx)!));
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        ElementSaveExtensions.RegisterGueInstantiation("ColoredRectangle", () => new ColoredRectangleRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Text", () => new TextRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Container", () => new ContainerRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("Sprite", () => new SpriteRuntime());
        ElementSaveExtensions.RegisterGueInstantiation("NineSlice", () => new NineSliceRuntime());
        return ObjectFinder.Self.GetElementSave(shown)!.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
    }

    /// <summary>A restart: a copy of the saved folder loaded into empty caches.</summary>
    public string DescribeAFreshLoad()
    {
        string copy = Dir + "_fresh" + (++_freshCounter);
        CopyFolder(Dir, copy);
        GumProjectSave? running = ObjectFinder.Self.GumProjectSave;
        string runningDirectory = FileManager.RelativeDirectory;
        try
        {
            FileManager.RelativeDirectory = copy + "/";
            GumProjectSave probe = GumProjectSave.Load(Path.Combine(copy, "Proj.gumx"));
            if (!probe.AllElements.Any(candidate => candidate.Name == Screen.ElementSave!.Name))
            {
                return "<gone>";
            }
            GraphicalUiElement fresh = LoadAndBuild(Path.Combine(copy, "Proj.gumx"), Screen.ElementSave!.Name);
            // A game adds the screen to the root and lays it out in its first frame.
            fresh.AddToRoot();
            if (LayOutBothBeforeDumping)
            {
                GumService.Default.Root.UpdateLayout();
            }
            ApplySelectedState(fresh);
            string dump = PreviewAccuracy2World.Describe(fresh) + Animations();
            fresh.RemoveFromRoot();
            return dump;
        }
        finally
        {
            FileManager.RelativeDirectory = runningDirectory;
            ObjectFinder.Self.GumProjectSave = running;
            try { Directory.Delete(copy, recursive: true); } catch { /* best-effort */ }
        }
    }

    public static void CopyFolder(string from, string to)
    {
        foreach (string file in Directory.EnumerateFiles(from, "*.*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    public GraphicalUiElement? Find(string name) => Screen.GetGraphicalUiElementByName(name);

    public void Dispose()
    {
        Manager?.Stop();
        Screen?.RemoveFromRoot();
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= Errors.Add;
        CustomSetPropertyOnRenderable.LocalizationService = _previousLocalization;
        LoaderManager.Self.CacheTextures = _previousCacheTextures;
        FileManager.RelativeDirectory = _previousRelativeDirectory;
        try { Directory.Delete(Dir, recursive: true); } catch { /* best-effort */ }
    }
}
