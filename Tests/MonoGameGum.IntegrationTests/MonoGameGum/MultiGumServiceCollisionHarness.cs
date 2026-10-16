using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

// Harness for MultiGumServiceCollisionSpikeTests (#6066 spike). Kept separate from
// MultiGumServiceStaticSnapshot.cs so the two spike branches do not conflict.

/// <summary>
/// <see cref=StaticSnapshot/> plus the element-type table. The table used to be one process-wide
/// dictionary read here through reflection; it is now owned by each service's GumContext, so there is
/// nothing left to capture or restore for it and the table members below do nothing. They stay so the
/// tests that exercised the old per-service-table spike keep their shape.
/// </summary>
internal sealed class CollisionSnapshot
{
    /// <summary>
    /// State found by the collision spike that neither <see cref="StaticSnapshot"/> nor this class restores
    /// (in addition to <see cref="StaticSnapshot.UncapturedState"/>).
    /// </summary>
    public static readonly string[] AdditionalUncapturedState =
    {
        "Renderer.TextureFilter (public static; ApplyProjectTextureFilter overwrites it for every project load)",
        "Sprite.InvalidTexture (in the snapshot, but UninitializePlatform disposes whatever instance is static at the time)",
        "CustomSetPropertyOnRenderable statics: FontService, InMemoryFontCreator, LocalizationService, _knownUnresolvableFontKeys, the BBCode font stacks",
        "ColoredRectangleRuntime.DefaultWidth/DefaultHeight (obsolete statics written from the loaded project's Standards)",
        "BitmapFont._reportedPageLoadFailures (static dedupe set)",
        "ObjectFinder.Self is read lazily after construction (BehaviorFormsPropertyApplier, TextRuntime font cache names, CustomSetPropertyOnRenderable.ResolveFontFilePath), as is FileManager.RelativeDirectory (font and SourceFile reloads on property change)",
    };

    public StaticSnapshot Statics { get; private set; } = null!;

    private Gum.GumService _service = null!;

    /// <summary>
    /// Captures the statics as seen from <paramref name="service"/>. The ones that moved into the
    /// service's context (project lookup, relative directory, content loader) are read through its own
    /// scope, so the snapshot holds that service's values and not the first service's.
    /// </summary>
    public static CollisionSnapshot Capture(Gum.GumService service)
    {
        global::RenderingLibrary.IGumService.Default = service;
        {
            return new CollisionSnapshot { Statics = StaticSnapshot.Capture(service), _service = service };
        }
    }

    /// <summary>
    /// Restores every static the snapshot covers, in the owning service's scope so the per-service ones
    /// are written back to that service. <paramref name="includeElementTypeTable"/> is ignored: the table
    /// is per service now.
    /// </summary>
    public void Restore(bool includeElementTypeTable = false)
    {
        global::RenderingLibrary.IGumService.Default = _service;
        {
            Statics.Restore();
        }
    }

    /// <summary>No longer needed: each service registers into its own element-type table.</summary>
    public static void ClearElementTypeTable()
    {
    }
}

/// <summary>A game holding two <see cref="Gum.GumService"/> instances, each initialized on demand.</summary>
internal sealed class CollisionGame : Game
{
    private readonly GraphicsDeviceManager _graphics;

    public Gum.GumService ServiceA { get; }
    public Gum.GumService ServiceB { get; }
    public GumProjectSave? ProjectA { get; private set; }
    public GumProjectSave? ProjectB { get; private set; }
    public CollisionSnapshot? StateA { get; private set; }
    public CollisionSnapshot? StateB { get; private set; }

    /// <summary>Free-form tag a test sets to say which service is mid-Update, for logging.</summary>
    public string? Updating { get; set; }

    public CollisionGame()
    {
        LoaderManager.Self?.DisposeAndClear();
        _graphics = new GraphicsDeviceManager(this);
        ServiceA = new Gum.GumService();
        ServiceB = new Gum.GumService();
    }

    /// <summary>
    /// Initializes service A. With <paramref name="freshElementTypeTable"/> the process-wide element-type
    /// table is emptied first, so only this service's registrations land in it (spike of a per-service table).
    /// </summary>
    public GumProjectSave InitializeA(string gumxPath, bool freshElementTypeTable = false)
    {
        if (freshElementTypeTable) { CollisionSnapshot.ClearElementTypeTable(); }
        ProjectA = ServiceA.Initialize(this, gumxPath)!;
        StateA = CollisionSnapshot.Capture(ServiceA);
        return ProjectA;
    }

    public GumProjectSave InitializeB(string gumxPath, bool freshElementTypeTable = false)
    {
        if (freshElementTypeTable) { CollisionSnapshot.ClearElementTypeTable(); }
        ProjectB = ServiceB.Initialize(this, gumxPath)!;
        StateB = CollisionSnapshot.Capture(ServiceB);
        return ProjectB;
    }

    /// <summary>Re-initializes a service after Uninitialize and captures a new snapshot for it.</summary>
    public GumProjectSave ReinitializeA(string gumxPath, bool freshElementTypeTable = false) =>
        InitializeA(gumxPath, freshElementTypeTable);

    public GumProjectSave ReinitializeB(string gumxPath, bool freshElementTypeTable = false) =>
        InitializeB(gumxPath, freshElementTypeTable);

    protected override void Update(GameTime gameTime) { }
    protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(XnaColor.CornflowerBlue);

    protected override void Dispose(bool disposing)
    {
        // Each service tears down against its own statics; failures here are teardown after a red test.
        if (ServiceB.IsInitialized)
        {
            try { StateB?.Restore(); ServiceB.Uninitialize(); } catch { }
        }
        if (ServiceA.IsInitialized)
        {
            try { StateA?.Restore(); ServiceA.Uninitialize(); } catch { }
        }
        try { LoaderManager.Self?.DisposeAndClear(); } catch { }

        CollisionSnapshot.ClearElementTypeTable();
        SystemManagers.Default = null!;
        ISystemManagers.Default = null!;
        IGumService.Default = null;
        FrameworkElement.PopupRoot = null!;
        FrameworkElement.ModalRoot = null!;
        Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle = null!;
        FileManager_ResetRelativeDirectory();
        base.Dispose(disposing);
    }

    private static void FileManager_ResetRelativeDirectory() =>
        ToolsUtilities.FileManager.RelativeDirectory = "Content/";
}

/// <summary>One variable in a hand-written element or state.</summary>
internal readonly record struct GumVar(string Type, string Name, string Value, string XsiType)
{
    public static GumVar Float(string name, float value) =>
        new("float", name, value.ToString(System.Globalization.CultureInfo.InvariantCulture), "float");
    public static GumVar Int(string name, int value) => new("int", name, value.ToString(), "int");
    public static GumVar String(string name, string value) => new("string", name, value, "string");
    public static GumVar Bool(string name, bool value) => new("bool", name, value ? "true" : "false", "boolean");
    /// <summary>A DimensionUnitType value (0 Absolute, 1 PercentageOfParent, 2 RelativeToParent, 4 RelativeToChildren).</summary>
    public static GumVar Units(string name, int value) => new("DimensionUnitType", name, value.ToString(), "int");

    public string ToXml() =>
        $"    <Variable><Type>{Type}</Type><Name>{Name}</Name><Value xsi:type=\"xsd:{XsiType}\">{Value}</Value><SetsValue>true</SetsValue></Variable>";
}

internal sealed class GumInstance
{
    public string Name { get; }
    public string BaseType { get; }
    /// <summary>True for an instance a derived component repeats from its base component (the tool writes these).</summary>
    public bool DefinedByBase { get; }
    public GumInstance(string name, string baseType, bool definedByBase = false) { Name = name; BaseType = baseType; DefinedByBase = definedByBase; }
}

/// <summary>A component or screen written to disk.</summary>
internal sealed class GumElement
{
    public string Name { get; }
    public bool IsScreen { get; }
    public string? BaseType { get; set; }
    public List<GumVar> Variables { get; } = new();
    public List<GumInstance> Instances { get; } = new();
    public List<string> Behaviors { get; } = new();
    /// <summary>Category names written with the standard Forms state names, for Forms behavior components.</summary>
    public List<(string Category, string[] States)> Categories { get; } = new();

    public GumElement(string name, bool isScreen = false, string? baseType = null)
    {
        Name = name;
        IsScreen = isScreen;
        BaseType = isScreen ? null : (baseType ?? "Container");
    }

    public GumElement Var(GumVar variable) { Variables.Add(variable); return this; }
    public GumElement Child(string name, string baseType, bool definedByBase = false) { Instances.Add(new GumInstance(name, baseType, definedByBase)); return this; }
    public GumElement Behavior(string behaviorName) { Behaviors.Add(behaviorName); return this; }
    public GumElement Category(string name, params string[] states) { Categories.Add((name, states)); return this; }
}

/// <summary>Writes a small Gum project (real sample Standards plus hand-written elements) under a folder.</summary>
internal sealed class GumProjectFolder
{
    public string Directory { get; }
    public string GumxPath => Path.Combine(Directory, "Project.gumx");

    /// <summary>The project's texture filter setting ("Linear" or "Point"), applied to the renderer at load.</summary>
    public string TextureFilter { get; set; } = "Point";

    private readonly List<GumElement> _elements = new();
    private readonly List<string> _behaviorNames = new();

    public GumProjectFolder(string directory)
    {
        Directory = directory;
        System.IO.Directory.CreateDirectory(directory);
    }

    public GumProjectFolder Add(GumElement element) { _elements.Add(element); return this; }

    /// <summary>Writes <c>Behaviors/&lt;name&gt;.behx</c> with one bool Forms property and the standard category.</summary>
    public GumProjectFolder AddBehavior(string name, string? formsPropertyName = null, bool formsPropertyDefault = true,
        string? categoryName = null, params string[] categoryStates)
    {
        _behaviorNames.Add(name);
        string formsProperty = formsPropertyName == null ? "" :
            $"  <FormsProperty Type=\"bool\" Name=\"{formsPropertyName}\">\n    <Value xsi:type=\"xsd:boolean\">{(formsPropertyDefault ? "true" : "false")}</Value>\n  </FormsProperty>\n";
        string category = categoryName == null ? "" :
            $"  <Category>\n    <Name>{categoryName}</Name>\n{string.Concat(categoryStates.Select(s => $"    <State>\n      <Name>{s}</Name>\n    </State>\n"))}  </Category>\n";
        string xml = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <BehaviorSave xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
              <Name>{name}</Name>
              <RequiredVariables />
            {formsProperty}{category}  <RequiredInstances />
              <RequiredAnimations />
            </BehaviorSave>
            """;
        System.IO.Directory.CreateDirectory(Path.Combine(Directory, "Behaviors"));
        File.WriteAllText(Path.Combine(Directory, "Behaviors", name + ".behx"), xml);
        return this;
    }

    /// <summary>
    /// Copies the sample Standards and optionally rewrites the default value of some variables
    /// (standard name, variable name, new value), so two projects define the same Standard differently.
    /// </summary>
    public GumProjectFolder WriteStandards(params (string Standard, string Variable, string Value)[] overrides)
    {
        string standardsDirectory = Path.Combine(Directory, "Standards");
        System.IO.Directory.CreateDirectory(standardsDirectory);
        string sample = FindSampleStandardsDirectory();
        foreach (string file in System.IO.Directory.GetFiles(sample, "*.gutx"))
        {
            string text = File.ReadAllText(file);
            string standard = Path.GetFileNameWithoutExtension(file);
            foreach (var item in overrides.Where(o => o.Standard == standard))
            {
                Regex pattern = new Regex(
                    $@"(<Name>{Regex.Escape(item.Variable)}</Name>\s*<Value xsi:type=""xsd:\w+"">)[^<]*(</Value>)");
                if (!pattern.IsMatch(text))
                {
                    throw new InvalidOperationException($"{standard}.gutx has no default value for {item.Variable}");
                }
                text = pattern.Replace(text, "${1}" + item.Value + "${2}", 1);
            }
            File.WriteAllText(Path.Combine(standardsDirectory, Path.GetFileName(file)), text);
        }
        return this;
    }

    /// <summary>Writes every element and the .gumx, and returns the .gumx path.</summary>
    public string Write()
    {
        if (!System.IO.Directory.Exists(Path.Combine(Directory, "Standards")))
        {
            WriteStandards();
        }
        string[] standardNames = System.IO.Directory.GetFiles(Path.Combine(Directory, "Standards"), "*.gutx")
            .Select(item => Path.GetFileNameWithoutExtension(item)!).ToArray();

        foreach (GumElement element in _elements)
        {
            WriteElement(element);
        }

        string references = string.Concat(_elements.Select(e => e.IsScreen
            ? $"  <ScreenReference Name=\"{e.Name}\" />{Environment.NewLine}"
            : $"  <ComponentReference Name=\"{e.Name}\" />{Environment.NewLine}"));
        string behaviors = string.Concat(_behaviorNames.Select(n => $"  <BehaviorReference Name=\"{n}\" />{Environment.NewLine}"));
        string standards = string.Concat(standardNames.Select(name => $"  <StandardElementReference Name=\"{name}\" />{Environment.NewLine}"));

        File.WriteAllText(GumxPath, $"""
            <?xml version="1.0" encoding="utf-8"?>
            <GumProjectSave xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
              <DefaultCanvasWidth>800</DefaultCanvasWidth>
              <DefaultCanvasHeight>480</DefaultCanvasHeight>
              <TextureFilter>{TextureFilter}</TextureFilter>
            {references}{standards}{behaviors}</GumProjectSave>
            """);
        return GumxPath;
    }

    private void WriteElement(GumElement element)
    {
        string subfolder = element.IsScreen ? "Screens" : "Components";
        string extension = element.IsScreen ? ".gusx" : ".gucx";
        string root = element.IsScreen ? "ScreenSave" : "ComponentSave";
        System.IO.Directory.CreateDirectory(Path.Combine(Directory, subfolder));

        StringBuilder xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml.AppendLine($"<{root} xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">");
        xml.AppendLine($"  <Name>{element.Name}</Name>");
        if (element.BaseType != null)
        {
            xml.AppendLine($"  <BaseType>{element.BaseType}</BaseType>");
        }
        xml.AppendLine("  <State>");
        xml.AppendLine("    <Name>Default</Name>");
        foreach (GumVar variable in element.Variables) { xml.AppendLine(variable.ToXml()); }
        xml.AppendLine("  </State>");
        foreach ((string category, string[] states) in element.Categories)
        {
            xml.AppendLine("  <Category>");
            xml.AppendLine($"    <Name>{category}</Name>");
            foreach (string state in states)
            {
                xml.AppendLine($"    <State><Name>{state}</Name></State>");
            }
            xml.AppendLine("  </Category>");
        }
        foreach (GumInstance instance in element.Instances)
        {
            xml.AppendLine("  <Instance>");
            xml.AppendLine($"    <Name>{instance.Name}</Name>");
            xml.AppendLine($"    <BaseType>{instance.BaseType}</BaseType>");
            xml.AppendLine($"    <DefinedByBase>{(instance.DefinedByBase ? "true" : "false")}</DefinedByBase>");
            xml.AppendLine("  </Instance>");
        }
        xml.AppendLine("  <Behaviors>");
        foreach (string behavior in element.Behaviors)
        {
            xml.AppendLine($"    <ElementBehaviorReference><BehaviorName>{behavior}</BehaviorName></ElementBehaviorReference>");
        }
        xml.AppendLine("  </Behaviors>");
        xml.AppendLine($"</{root}>");
        File.WriteAllText(Path.Combine(Directory, subfolder, element.Name + extension), xml.ToString());
    }

    private static string FindSampleStandardsDirectory()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(current, "Samples", "MonoGameGumFromFile", "MonoGameGumFromFile", "Content", "Standards");
            if (System.IO.Directory.Exists(candidate))
            {
                return candidate;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate sample Standards from " + AppContext.BaseDirectory);
    }
}

/// <summary>Writes the same-named image and bitmap-font files with different content into a project folder.</summary>
internal static class CollisionAssets
{
    /// <summary>Writes a solid-color square PNG (through the game's device) at <c>directory/relativePath</c>.</summary>
    public static void WritePng(GraphicsDevice device, string directory, string relativePath, XnaColor color, int size = 16)
    {
        string path = Path.Combine(directory, relativePath);
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using Texture2D texture = new Texture2D(device, size, size);
        texture.SetData(Enumerable.Repeat(color, size * size).ToArray());
        using FileStream stream = File.Create(path);
        texture.SaveAsPng(stream, size, size);
    }

    /// <summary>
    /// Writes a one-glyph BMFont: 'A' is an 8x8 block cut from the top-left of a solid-color page and
    /// advances <paramref name="xAdvance"/> pixels, so two fonts at the same path differ in glyph color
    /// (the page) and in text width (the advance).
    /// </summary>
    public static void WriteBitmapFont(GraphicsDevice device, string directory, string fntRelativePath,
        XnaColor pageColor, int xAdvance)
    {
        string fntPath = Path.Combine(directory, fntRelativePath);
        string fntDirectory = Path.GetDirectoryName(fntPath)!;
        System.IO.Directory.CreateDirectory(fntDirectory);
        string pageName = Path.GetFileNameWithoutExtension(fntPath) + "_0.png";
        WritePng(device, fntDirectory, pageName, pageColor);

        string text =
            "info face=\"TestFont\" size=20 bold=0 italic=0 charset=\"\" unicode=1 stretchH=100 smooth=1 aa=1 padding=0,0,0,0 spacing=1,1 outline=0\n" +
            "common lineHeight=20 base=16 scaleW=16 scaleH=16 pages=1 packed=0\n" +
            $"page id=0 file=\"{pageName}\"\n" +
            "chars count=2\n" +
            "char id=32 x=0 y=0 width=0 height=0 xoffset=0 yoffset=0 xadvance=8 page=0 chnl=15\n" +
            $"char id=65 x=0 y=0 width=8 height=8 xoffset=0 yoffset=2 xadvance={xAdvance} page=0 chnl=15\n";
        File.WriteAllText(fntPath, text);
    }
}

/// <summary>Renders services into one render target and reads pixels back.</summary>
internal sealed class CollisionCapture : IDisposable
{
    public const int Width = 400;
    public const int Height = 300;

    private readonly GraphicsDevice _device;
    private readonly RenderTarget2D _target;

    public CollisionCapture(GraphicsDevice device)
    {
        _device = device;
        _target = new RenderTarget2D(device, Width, Height, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);
        _device.SetRenderTarget(_target);
        _device.Clear(XnaColor.CornflowerBlue);
    }

    /// <summary>
    /// Draws one service's tree. <paramref name="staticsToRestoreFirst"/> is applied right before the draw
    /// (null means draw with whatever statics are active, which is what a bare Draw does today).
    /// </summary>
    public void Draw(Gum.GumService service, CollisionSnapshot? staticsToRestoreFirst, bool includeElementTypeTable = false)
    {
        // Renderer.Draw leaves no render target bound, so rebind before each service's pass.
        _device.SetRenderTarget(_target);
        staticsToRestoreFirst?.Restore(includeElementTypeTable);
        service.SystemManagers.Renderer.Draw(service.SystemManagers);
    }

    public XnaColor[] Read()
    {
        _device.SetRenderTarget(null);
        XnaColor[] pixels = new XnaColor[Width * Height];
        _target.GetData(pixels);
        return pixels;
    }

    public void Dispose()
    {
        _device.SetRenderTarget(null);
        _target.Dispose();
    }

    public static XnaColor At(XnaColor[] pixels, int x, int y) => pixels[(y * Width) + x];

    /// <summary>Counts pixels in a rectangle matching the predicate.</summary>
    public static int Count(XnaColor[] pixels, Rectangle area, Func<XnaColor, bool> predicate)
    {
        int count = 0;
        for (int y = area.Top; y < area.Bottom; y++)
        {
            for (int x = area.Left; x < area.Right; x++)
            {
                if (predicate(At(pixels, x, y))) { count++; }
            }
        }
        return count;
    }

    public static bool IsRed(XnaColor c) => c.R > 200 && c.G < 60 && c.B < 60;
    public static bool IsBlue(XnaColor c) => c.B > 200 && c.R < 60 && c.G < 60;
    public static bool IsYellow(XnaColor c) => c.R > 200 && c.G > 200 && c.B < 60;
    public static bool IsGreen(XnaColor c) => c.G > 200 && c.R < 60 && c.B < 60;
    public static string Describe(XnaColor c) => $"({c.R},{c.G},{c.B})";
}
