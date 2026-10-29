// ColoredRectangle, Text, Sprite, NineSlice and Container are the standards the project files in this sweep instantiate.
#pragma warning disable CS0618
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework.Graphics;
using Gum.Localization;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ToolsUtilities;

namespace MonoGameGum.Tests.Hot;

/// <summary>What one saved edit did to a running tree, measured against a fresh load of the same files.</summary>
internal sealed record PreviewVerdict2(string Hot, string Fresh, HotReloadResult? Result)
{
    public bool Differs => Hot != Fresh;

    /// <summary>Whether the reload finished at all; false when nothing the manager watches changed.</summary>
    public bool Reloaded => Result != null;

    public bool MatchesRestart => Result?.MatchesRestart ?? true;

    public string Kinds => Result == null ? "none" : string.Join(",", Result.Issues.Select(issue => issue.Kind).Distinct());

    /// <summary>The tree differs from a restart and the result says nothing: the Preview or game stays stale unnoticed.</summary>
    public bool IsFalseNegative => Differs && MatchesRestart;

    /// <summary>The tree equals a restart and the result asks for one anyway.</summary>
    public bool IsFalsePositive => !Differs && !MatchesRestart;

    public string Explain() => "differs=" + Differs + " reloaded=" + Reloaded + " matchesRestart=" + MatchesRestart + " issues=[" + Kinds + "]\n"
        + string.Join("", (Result?.Issues ?? Array.Empty<HotReloadIssue>()).Select(issue => "  " + issue.Kind + ": " + issue.Message.Replace('\n', ' ') + "\n")) + Difference();

    // Per node, the tokens that differ; nodes only one side has are listed whole by name.
    private string Difference()
    {
        List<string> hot = Hot.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        List<string> fresh = Fresh.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        StringBuilder text = new StringBuilder();
        if (hot.Select(Key).SequenceEqual(fresh.Select(Key)))
        {
            for (int i = 0; i < hot.Count; i++)
            {
                HashSet<string> freshTokens = fresh[i].Split(' ').ToHashSet();
                HashSet<string> hotTokens = hot[i].Split(' ').ToHashSet();
                string[] changed = hot[i].Split(' ').Where(token => !freshTokens.Contains(token)).ToArray();
                if (changed.Length > 0)
                {
                    text.Append("  ").Append(Key(hot[i])).Append(": hot ").Append(string.Join(' ', changed))
                        .Append(" | fresh ").Append(string.Join(' ', fresh[i].Split(' ').Where(token => !hotTokens.Contains(token)))).AppendLine();
                }
            }
        }
        else
        {
            text.AppendLine("  nodes (in order) differ:");
            text.AppendLine("  hot:   " + string.Join(" / ", hot.Select(Key)));
            text.AppendLine("  fresh: " + string.Join(" / ", fresh.Select(Key)));
        }
        return text.ToString();
    }

    private static string Key(string line)
    {
        int end = line.IndexOf('>');
        int runtime = line.IndexOf(' ', end + 2);
        return line.Substring(0, runtime < 0 ? line.Length : runtime);
    }
}

/// <summary>
/// Hands out a texture of the size its PNG header names, without a graphics device: the test host has
/// none. Caches like the real loader (by absolute path, until <c>LoaderManager.Dispose</c> evicts it),
/// and returns null for a missing file as the desktop loader does.
/// </summary>
internal sealed class SizedTextureLoader : IContentLoader
{
    private static readonly FieldInfo _width = typeof(Texture2D).GetField("width", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo _height = typeof(Texture2D).GetField("height", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public int Loads;

    public T? LoadContent<T>(string contentName)
    {
        if (typeof(T) != typeof(Texture2D))
        {
            throw new NotImplementedException(contentName);
        }
        return (T?)(object?)LoadTexture(contentName);
    }

    public T? TryLoadContent<T>(string contentName)
    {
        try
        {
            return LoadContent<T>(contentName);
        }
        catch (Exception)
        {
            return default;
        }
    }

    private Texture2D? LoadTexture(string contentName)
    {
        string path = FileManager.Standardize(contentName, preserveCase: true, makeAbsolute: false);
        if (FileManager.IsRelative(path))
        {
            path = FileManager.RemoveDotDotSlash(FileManager.RelativeDirectory + path);
        }
        string key = FileManager.Standardize(path, preserveCase: true, makeAbsolute: true);
        if (LoaderManager.Self.CacheTextures && LoaderManager.Self.GetDisposable(key) is Texture2D cached)
        {
            return cached;
        }
        if (!File.Exists(path))
        {
            return null;
        }
        byte[] header = new byte[24];
        using (FileStream stream = File.OpenRead(path))
        {
            stream.ReadExactly(header, 0, 24);
        }
        int width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
        int height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
        Texture2D texture = (Texture2D)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Texture2D));
        _width.SetValue(texture, width);
        _height.SetValue(texture, height);
        texture.Name = key;
        Loads++;
        if (LoaderManager.Self.CacheTextures)
        {
            LoaderManager.Self.AddDisposable(key, texture);
        }
        return texture;
    }
}

/// <summary>
/// The disk side of the Preview accuracy sweep, round 2: fonts, textures and animation files on top of
/// the project files. A baseline the tool saved, one edit applied and saved the way the tool saves,
/// and the real <see cref="GumHotReloadManager"/> notify, debounce and Update flow with an injected
/// clock. The hot tree is compared with a fresh load of a copy of the same folder through a dump that
/// includes layout results, resolved font files and texture sizes.
/// </summary>
internal sealed class PreviewAccuracy2World : IDisposable
{
    private static int _freshCounter;

    public string Dir { get; } = Path.Combine(Path.GetTempPath(), "PreviewAccuracy2_" + Path.GetRandomFileName());
    public string Gumx => Path.Combine(Dir, "Proj.gumx");

    /// <summary>The folder the game runs from. The same as <see cref="Dir"/> in Preview; a copy in a game that runs from its build output.</summary>
    public string Bin { get; private set; } = "";

    public DateTime Now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    public GumHotReloadManager Manager = null!;
    public GraphicalUiElement Screen = null!;
    public readonly List<HotReloadResult> Results = new List<HotReloadResult>();
    public readonly SizedTextureLoader Loader = new SizedTextureLoader();
    public readonly List<string> Errors = new List<string>();
    public string ShownElement = "MainScreen";

    /// <summary>Set before <see cref="Boot"/>: the project lists Content/loc.csv and the game loads it at startup, as GumService does.</summary>
    public bool UseLocalization;

    private Action<GumProjectSave>? _baselineEdit;

    private readonly ILocalizationService? _previousLocalization = CustomSetPropertyOnRenderable.LocalizationService;
    private readonly bool _previousCacheTextures = LoaderManager.Self.CacheTextures;
    private readonly string _previousRelativeDirectory = FileManager.RelativeDirectory;

    public PreviewAccuracy2World()
    {
        Directory.CreateDirectory(Dir);
    }

    // ---- Baseline project ----

    internal static GumProjectSave Baseline()
    {
        StandardElementsManager.Self.Initialize();
        GumProjectSave project = new GumProjectSave();
        foreach (string standard in new[] { "Container", "ColoredRectangle", "Text", "Sprite", "NineSlice" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!.Clone());
            standardElement.DefaultState!.ParentContainer = standardElement;
            project.StandardElements.Add(standardElement);
        }

        ComponentSave badge = NewComponent("Widgets/Badge");
        AddInstance(badge, "Pic", "Sprite");
        Std(badge, "Pic", "SourceFile", "Content/red.png");
        AddInstance(badge, "Name", "Text");
        Std(badge, "Name", "Text", "Badge");
        Std(badge, "Name", "Font", "Pixel");
        Std(badge, "Name", "FontSize", 20);
        Std(badge, "Name", "WidthUnits", "RelativeToChildren");
        Std(badge, "Name", "HeightUnits", "RelativeToChildren");
        project.Components.Add(badge);

        ScreenSave screen = new ScreenSave { Name = "MainScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        AddInstance(screen, "Caption", "Text");
        Std(screen, "Caption", "Text", "Hello World");
        Std(screen, "Caption", "Font", "Pixel");
        Std(screen, "Caption", "FontSize", 20);
        Std(screen, "Caption", "WidthUnits", "RelativeToChildren");
        Std(screen, "Caption", "HeightUnits", "RelativeToChildren");
        AddInstance(screen, "Pic", "Sprite");
        Std(screen, "Pic", "SourceFile", "Content/red.png");
        AddInstance(screen, "Frame", "NineSlice");
        Std(screen, "Frame", "SourceFile", "Content/frame.png");
        AddInstance(screen, "Wrap", "Container");
        Std(screen, "Wrap", "HeightUnits", "RelativeToChildren");
        Std(screen, "Wrap", "WidthUnits", "RelativeToChildren");
        AddInstance(screen, "Inner", "Text");
        Set(screen, "Inner.Parent", "string", "Wrap");
        Std(screen, "Inner", "Text", "Wrapped");
        Std(screen, "Inner", "Font", "Pixel");
        Std(screen, "Inner", "FontSize", 20);
        Std(screen, "Inner", "WidthUnits", "RelativeToChildren");
        Std(screen, "Inner", "HeightUnits", "RelativeToChildren");
        AddInstance(screen, "Badge1", "Widgets/Badge");
        AddInstance(screen, "Box", "ColoredRectangle");
        Std(screen, "Box", "Red", 10);
        AddInstance(screen, "Swatch", "ColoredRectangle");
        Std(screen, "Swatch", "Red", 20);
        AddInstance(screen, "Follower", "ColoredRectangle");
        Std(screen, "Follower", "Red", 30);
        project.Screens.Add(screen);

        ScreenSave second = new ScreenSave { Name = "OtherScreen" };
        second.States.Add(new StateSave { Name = "Default", ParentContainer = second });
        AddInstance(second, "Solo", "ColoredRectangle");
        project.Screens.Add(second);

        project.Initialize();
        return project;
    }

    /// <summary>Files the baseline project loads: a bitmap font and three images, written the way the tool leaves them.</summary>
    internal static void WriteBaselineAssets(string dir)
    {
        WriteFont(dir, "Font20Pixel", advance: 10, lineHeight: 24);
        WritePng(Path.Combine(dir, "Content", "red.png"), 16, 16);
        WritePng(Path.Combine(dir, "Content", "frame.png"), 30, 30);
        WritePng(Path.Combine(dir, "Content", "other.png"), 40, 20);
    }

    // ---- Editing helpers the tests use ----

    internal static ComponentSave NewComponent(string name, string baseType = "Container")
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = baseType };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    internal static void AddInstance(ElementSave owner, string name, string baseType)
    {
        owner.Instances.Add(new InstanceSave { Name = name, BaseType = baseType, ParentContainer = owner });
    }

    internal static void Set(ElementSave owner, string name, string type, object? value)
    {
        Set(owner.DefaultState!, name, type, value);
    }

    internal static void Set(StateSave state, string name, string type, object? value)
    {
        state.Variables.RemoveAll(variable => variable.Name == name);
        state.Variables.Add(new VariableSave { Name = name, Type = type, Value = value, SetsValue = true });
    }

    /// <summary>Sets <c>instance.variable</c> typed as the standard element declares it. A string for an enum variable is parsed.</summary>
    internal static void Std(ElementSave owner, string instanceName, string variable, object value)
    {
        string baseType = owner.GetInstance(instanceName)!.BaseType!;
        VariableSave declared = StandardElementsManager.Self.GetDefaultStateFor(baseType)!.Variables.First(candidate => candidate.Name == variable);
        object converted = declared.Value != null && declared.Value.GetType().IsEnum && value is string text
            ? Enum.Parse(declared.Value.GetType(), text)
            : value;
        Set(owner, instanceName + "." + variable, declared.Type, converted);
    }

    internal static ScreenSave MainScreen(GumProjectSave project) => project.Screens.Single(screen => screen.Name == "MainScreen");

    internal static ComponentSave Component(GumProjectSave project, string name) => project.Components.Single(component => component.Name == name);

    // ---- Assets, as the tool and the user leave them ----

    /// <summary>A text-format .fnt with every printable ASCII character, <paramref name="advance"/> pixels apart. Returns its path.</summary>
    internal static string WriteFont(string dir, string baseName, int advance, int lineHeight, string folder = "FontCache")
    {
        string fontCache = Path.Combine(dir, folder);
        Directory.CreateDirectory(fontCache);
        string pageName = baseName + ".png";
        StringBuilder fnt = new StringBuilder();
        fnt.AppendLine("info face=\"Pixel\" size=-" + lineHeight + " bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0");
        fnt.AppendLine("common lineHeight=" + lineHeight + " base=" + (lineHeight - 4) + " scaleW=256 scaleH=256 pages=1 packed=0 alphaChnl=0 redChnl=4 greenChnl=4 blueChnl=4");
        fnt.AppendLine("page id=0 file=\"" + pageName + "\"");
        fnt.AppendLine("chars count=95");
        for (int id = 32; id <= 126; id++)
        {
            fnt.AppendLine("char id=" + id + " x=" + (id - 32) * 2 + " y=0 width=" + (advance - 1) + " height=" + (lineHeight - 2)
                + " xoffset=0 yoffset=1 xadvance=" + advance + " page=0 chnl=15");
        }
        string path = Path.Combine(fontCache, baseName + ".fnt");
        File.WriteAllText(path, fnt.ToString());
        WritePng(Path.Combine(fontCache, Path.GetFileNameWithoutExtension(pageName) + ".png"), 256, 256);
        return path;
    }

    internal static void WriteShader(string dir, string name)
    {
        Directory.CreateDirectory(Path.Combine(dir, "Content"));
        File.WriteAllText(Path.Combine(dir, "Content", name), "// shader " + name);
    }

    /// <summary>Stands for a compiled effect: the file it was built from.</summary>
    internal sealed class ResolvedEffect : IDisposable
    {
        public readonly string Path;
        public ResolvedEffect(string path) { Path = path; }
        public void Dispose() { }
        public override string ToString() => Normalize(Path);
    }

    /// <summary>A PNG signature and header naming the size. The test loader only reads the size.</summary>
    internal static void WritePng(string path, int width, int height)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        byte[] bytes = new byte[33];
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 13, 0x49, 0x48, 0x44, 0x52 }.CopyTo(bytes, 0);
        bytes[16] = (byte)(width >> 24); bytes[17] = (byte)(width >> 16); bytes[18] = (byte)(width >> 8); bytes[19] = (byte)width;
        bytes[20] = (byte)(height >> 24); bytes[21] = (byte)(height >> 16); bytes[22] = (byte)(height >> 8); bytes[23] = (byte)height;
        bytes[24] = 8; bytes[25] = 6;
        File.WriteAllBytes(path, bytes);
    }

    // ---- Writing, as the tool does ----

    internal static void SaveAll(GumProjectSave project, string gumx)
    {
        project.ScreenReferences.Clear();
        project.ComponentReferences.Clear();
        project.StandardElementReferences.Clear();
        project.BehaviorReferences.Clear();
        foreach (ScreenSave screen in project.Screens)
        {
            project.ScreenReferences.Add(new ElementReference { Name = screen.Name, ElementType = ElementType.Screen });
        }
        foreach (ComponentSave component in project.Components)
        {
            project.ComponentReferences.Add(new ElementReference { Name = component.Name, ElementType = ElementType.Component });
        }
        foreach (StandardElementSave standard in project.StandardElements)
        {
            project.StandardElementReferences.Add(new ElementReference { Name = standard.Name, ElementType = ElementType.Standard });
        }
        foreach (BehaviorSave behavior in project.Behaviors)
        {
            project.BehaviorReferences.Add(new BehaviorReference { Name = behavior.Name });
        }
        project.Save(gumx, saveElements: true);
        foreach (BehaviorSave behavior in project.Behaviors)
        {
            string path = Path.Combine(Path.GetDirectoryName(gumx)!, "Behaviors", behavior.Name + ".behx");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            behavior.Save(path);
        }
    }

    // ---- Running side ----

    /// <summary>
    /// Saves the baseline and its assets, builds the running screen from them, and starts the real manager
    /// (the OS watcher is replaced by Notify). <paramref name="separateBin"/> runs the game from a copy of
    /// the folder, as a game does that runs from its build output.
    /// </summary>
    public void Boot(string shown = "MainScreen", bool separateBin = false, Action<GumProjectSave>? editBaseline = null, Action<string>? beforeBoot = null)
    {
        if (UseLocalization)
        {
            editBaseline = Chain(editBaseline, project => Std(MainScreen(project), "Caption", "Text", "T_Hello"));
        }
        _baselineEdit = editBaseline;
        ShownElement = shown;
        LoaderManager.Self.CacheTextures = true;
        LoaderManager.Self.ContentLoader = Loader;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += Errors.Add;
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = path => File.Exists(FileManager.RelativeDirectory + path) || File.Exists(path) ? new ResolvedEffect(path) : null;

        WriteBaselineAssets(Dir);
        WriteShader(Dir, "Outline.fx");
        GumProjectSave baseline = Baseline();
        if (UseLocalization)
        {
            baseline.LocalizationFiles.Add("Content/loc.csv");
            WriteLocalization(Dir, "Hello");
        }
        editBaseline?.Invoke(baseline);
        SaveAll(baseline, Gumx);
        beforeBoot?.Invoke(Dir);

        if (separateBin)
        {
            Bin = Dir + "_bin";
            CopyFolder(Dir, Bin);
        }
        else
        {
            Bin = Dir;
        }
        FileManager.RelativeDirectory = Bin + "/";

        Screen = LoadAndBuild(Path.Combine(separateBin ? Bin : Dir, "Proj.gumx"), shown, UseLocalization);

        Manager = new GumHotReloadManager(
            GumService.ApplyProjectTextureFilter,
            GumAnimationLoader.LoadAnimationsFromProvider,
            path => LoaderManager.Self.Dispose(path),
            () => Now)
        {
            LocalizationServiceProvider = () => CustomSetPropertyOnRenderable.LocalizationService
        };
        Manager.ReloadFinished += Results.Add;
        Manager.Start(Gumx);
        Manager.Stop();
    }

    internal static void WriteLocalization(string dir, string english)
    {
        Directory.CreateDirectory(Path.Combine(dir, "Content"));
        File.WriteAllText(Path.Combine(dir, "Content", "loc.csv"), "StringId,English,Spanish\nT_Hello," + english + ",Hola\n");
    }

    private static Action<GumProjectSave> Chain(Action<GumProjectSave>? first, Action<GumProjectSave> second) =>
        project => { first?.Invoke(project); second(project); };

    private static GraphicalUiElement LoadAndBuild(string gumx, string shown, bool localize = false)
    {
        GumProjectSave loaded = GumProjectSave.Load(gumx);
        loaded.Initialize();
        if (localize)
        {
            LocalizationService service = new LocalizationService();
            CustomSetPropertyOnRenderable.LocalizationService = service;
            ProjectLocalizationLoader.Load(loaded, service, null, new List<string>());
            // Language 0 is the id column; the first language is 1.
            service.CurrentLanguage = 1;
        }
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

    private static void CopyFolder(string from, string to)
    {
        foreach (string file in Directory.EnumerateFiles(from, "*.*", SearchOption.AllDirectories))
        {
            string target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }

    /// <summary>Edits the baseline and saves it as the tool would; <paramref name="assets"/> runs first, to write the fonts and images the edit refers to.</summary>
    public PreviewVerdict2 Save(Action<GumProjectSave> edit, Action<string>? assets = null, bool notifyEverything = true)
    {
        Dictionary<string, string> before = Snapshot();
        assets?.Invoke(Dir);
        List<string> changedAssets = Snapshot().Where(pair => !before.TryGetValue(pair.Key, out string? hash) || hash != pair.Value).Select(pair => pair.Key).ToList();
        GumProjectSave edited = Baseline();
        if (UseLocalization)
        {
            edited.LocalizationFiles.Add("Content/loc.csv");
        }
        _baselineEdit?.Invoke(edited);
        edit(edited);
        // The tool refreshes the instances a derived component inherits before it saves.
        foreach (ElementSave element in edited.AllElements)
        {
            element.Instances.RemoveAll(instance => instance.DefinedByBase);
        }
        edited.Initialize();
        SaveAll(edited, Gumx);
        if (!notifyEverything)
        {
            return Settle();
        }
        // The watcher reports the files the save wrote and the assets the edit changed, not the ones nobody touched.
        return Reload(Directory.EnumerateFiles(Dir, "*.*", SearchOption.AllDirectories)
            .Where(file => IsGumFile(file) || changedAssets.Contains(file)).ToArray());
    }

    private static bool IsGumFile(string file) => Path.GetExtension(file).ToLowerInvariant() is ".gumx" or ".gusx" or ".gucx" or ".gutx" or ".behx" or ".ganx";

    private Dictionary<string, string> Snapshot() => Directory.EnumerateFiles(Dir, "*.*", SearchOption.AllDirectories)
        .ToDictionary(file => file, file => Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(File.ReadAllBytes(file))));

    /// <summary>Notifies every file on disk, waits out the debounce and runs Update.</summary>
    public PreviewVerdict2 Reload()
    {
        foreach (string file in Directory.EnumerateFiles(Dir, "*.*", SearchOption.AllDirectories))
        {
            Manager.NotifyFileChanged(file);
        }
        return Settle();
    }

    /// <summary>Notifies exactly these files, waits out the debounce and runs Update.</summary>
    public PreviewVerdict2 Reload(params string[] files)
    {
        foreach (string file in files)
        {
            Manager.NotifyFileChanged(file);
        }
        return Settle();
    }

    /// <summary>Waits out the debounce and runs the game's frame; the verdict is for the reload it ran, if any.</summary>
    public PreviewVerdict2 Settle()
    {
        int before = Results.Count;
        Now = Now.AddMilliseconds(300);
        Manager.Update(new[] { Screen });
        HotReloadResult? result = Results.Count > before ? Results[Results.Count - 1] : null;
        return Verdict(result);
    }

    public PreviewVerdict2 Verdict(HotReloadResult? result) => new PreviewVerdict2(Describe(Screen), DescribeAFreshLoad(), result);

    // ---- The oracle ----

    private static readonly HashSet<string> _skipped = new HashSet<string>
    {
        "Name", "Tag", "AbsoluteX", "AbsoluteY", "WorldX", "WorldY", "ChildrenCount", "ContainedElementsCount", "EffectiveParentCount", "Z", "NameInHierarchy",
        "RenderedPositionX", "RenderedPositionY",
    };

    private static bool IsSimple(Type type) =>
        type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
        || type == typeof(Microsoft.Xna.Framework.Color) || type == typeof(Microsoft.Xna.Framework.Vector2)
        || type == typeof(Microsoft.Xna.Framework.Rectangle) || type == typeof(Microsoft.Xna.Framework.Rectangle?);

    private static readonly Regex _folder = new Regex(@"\S*PreviewAccuracy2_[A-Za-z0-9]+(?:\.[A-Za-z0-9]+)?(?:_bin|_fresh\d+)?", RegexOptions.Compiled);

    private static string Normalize(string text) => _folder.Replace(text, "<root>");

    /// <summary>
    /// The design-time part of a tree and what it measured: every simple property of the visual and of its
    /// renderable, the layout result, the resolved font file, the text's measured size and the textures'
    /// sizes, with the children in draw order. Instances nothing parents sort by name.
    /// </summary>
    public static string Describe(GraphicalUiElement element, int depth = 0)
    {
        StringBuilder text = new StringBuilder();
        text.Append(' ', depth * 2).Append(element.Name).Append(" <").Append(element.ElementSave?.Name).Append("> ").Append(element.GetType().Name);
        AppendProperties(text, element, string.Empty);
        text.Append(" abs=").Append(Math.Round(element.AbsoluteLeft, 2)).Append(',').Append(Math.Round(element.AbsoluteTop, 2))
            .Append(',').Append(Math.Round(element.GetAbsoluteWidth(), 2)).Append(',').Append(Math.Round(element.GetAbsoluteHeight(), 2));
        object? renderable = element.RenderableComponent;
        if (renderable != null)
        {
            AppendProperties(text, renderable, "r.");
            if (renderable is Text label)
            {
                text.Append(" r.font=").Append(label.BitmapFont?.FontFile == null ? "<none>" : Normalize(label.BitmapFont.FontFile))
                    .Append(" r.page=").Append(label.BitmapFont?.TextureName == null ? "<none>" : Normalize(label.BitmapFont.TextureName))
                    .Append(" r.lineH=").Append(label.BitmapFont?.LineHeightInPixels)
                    .Append(" r.measured=").Append(Math.Round(label.WrappedTextWidth, 2)).Append('x').Append(Math.Round(label.WrappedTextHeight, 2));
            }
            else if (renderable is Sprite sprite)
            {
                text.Append(" r.tex=").Append(Describe(sprite.Texture));
                if (sprite.AnimationChains != null)
                {
                    text.Append(" r.chains=").Append(string.Join('|', sprite.AnimationChains.Select(chain =>
                        chain.Name + ":" + string.Join(';', chain.Select(frame => Normalize(frame.TextureName ?? "<null>") + "@" + Describe(frame.Texture) + "@" + frame.FrameLength + "@" + frame.LeftCoordinate + "," + frame.TopCoordinate + "," + frame.RightCoordinate + "," + frame.BottomCoordinate)))));
                }
            }
            else if (renderable is IRenderTargetRenderable target)
            {
                text.Append(" r.effect=").Append(target.RenderTargetEffect?.ToString() ?? "<none>");
                FieldInfo? values = target.ShaderParameters?.GetType().GetField("_values", BindingFlags.Instance | BindingFlags.NonPublic);
                if (values?.GetValue(target.ShaderParameters) is System.Collections.IDictionary { Count: > 0 } dictionary)
                {
                    text.Append(" r.shader=").Append(string.Join('|', dictionary.Keys.Cast<object>().Select(key => key + ":" + dictionary[key]).OrderBy(item => item, StringComparer.Ordinal)));
                }
            }
            else if (renderable is NineSlice nineSlice)
            {
                text.Append(" r.tex=").Append(Describe(nineSlice.TopLeftTexture));
            }
        }
        if (element is InteractiveGue interactive && interactive.FormsControlAsObject != null)
        {
            text.Append(" forms=").Append(interactive.FormsControlAsObject.GetType().Name);
        }
        text.AppendLine();

        List<GraphicalUiElement> inOrder = element.Children.Where(child => child.Tag is InstanceSave).ToList();
        IEnumerable<GraphicalUiElement> rest = element.ContainedElements.Where(child => child.Tag is InstanceSave && !inOrder.Contains(child))
            .OrderBy(child => child.Name, StringComparer.Ordinal);
        foreach (GraphicalUiElement child in inOrder.Concat(rest))
        {
            text.Append(Describe(child, depth + 1));
        }
        return text.ToString();
    }

    private static string Describe(Texture2D? texture) => texture == null ? "<null>" : texture.Width + "x" + texture.Height;

    private static void AppendProperties(StringBuilder text, object target, string prefix)
    {
        foreach (PropertyInfo property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(candidate => candidate.GetIndexParameters().Length == 0 && candidate.CanRead && IsSimple(candidate.PropertyType)
                         && !_skipped.Contains(candidate.Name))
                     .OrderBy(candidate => candidate.Name, StringComparer.Ordinal))
        {
            object? value;
            try
            {
                value = property.GetValue(target);
            }
            catch (Exception)
            {
                continue;
            }
            text.Append(' ').Append(prefix).Append(property.Name).Append('=').Append(Normalize(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "").Replace(' ', '_'));
        }
    }

    /// <summary>A restart: a copy of the saved folder (what a build copies to the output), loaded into empty caches.</summary>
    public string DescribeAFreshLoad()
    {
        string copy = Dir + "_fresh" + (++_freshCounter);
        CopyFolder(Dir, copy);
        GumProjectSave? running = ObjectFinder.Self.GumProjectSave;
        string runningDirectory = FileManager.RelativeDirectory;
        ILocalizationService? runningLocalization = CustomSetPropertyOnRenderable.LocalizationService;
        try
        {
            FileManager.RelativeDirectory = copy + "/";
            GraphicalUiElement fresh = LoadAndBuildIfPresent(Path.Combine(copy, "Proj.gumx"));
            return fresh == null ? "<gone>" : Describe(fresh);
        }
        finally
        {
            CustomSetPropertyOnRenderable.LocalizationService = runningLocalization;
            FileManager.RelativeDirectory = runningDirectory;
            ObjectFinder.Self.GumProjectSave = running;
            try { Directory.Delete(copy, recursive: true); } catch { /* best-effort */ }
        }
    }

    private GraphicalUiElement LoadAndBuildIfPresent(string gumx)
    {
        GumProjectSave fresh = GumProjectSave.Load(gumx);
        if (!fresh.AllElements.Any(candidate => candidate.Name == Screen.ElementSave!.Name))
        {
            return null!;
        }
        return LoadAndBuild(gumx, Screen.ElementSave!.Name, UseLocalization);
    }

    public GraphicalUiElement? Find(string name) => Screen.GetGraphicalUiElementByName(name);

    public void Dispose()
    {
        CustomSetPropertyOnRenderable.PropertyAssignmentError -= Errors.Add;
        CustomSetPropertyOnRenderable.LocalizationService = _previousLocalization;
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
        LoaderManager.Self.CacheTextures = _previousCacheTextures;
        FileManager.RelativeDirectory = _previousRelativeDirectory;
        try { Directory.Delete(Dir, recursive: true); } catch { /* best-effort */ }
        try { Directory.Delete(Dir + "_bin", recursive: true); } catch { /* best-effort */ }
    }
}
