using System.Diagnostics;
using Avalonia.Threading;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>A renderable that takes the string-dispatched variables a runtime visual would (Text, Red...).</summary>
internal sealed class RecordingRenderable : InvisibleRenderable
{
    public string? Text { get; set; }
    public int Red { get; set; }
    public int Green { get; set; }
    public int Blue { get; set; }
}

/// <summary>
/// The running game's half of a hot reload test, in the same process as the tool: a screen built
/// from the project the tool saved, and the real <see cref="GumHotReloadManager"/> watching the
/// tool's project folder with a real <see cref="FileSystemWatcher"/>, so what the tool writes is what
/// reaches the runtime. The tool and the runtime share <c>ObjectFinder</c>, so the runtime's project
/// is swapped in only while the runtime runs. Dispose puts the tool's statics back.
/// </summary>
internal sealed class ToolSaveRuntimeWorld : IDisposable
{
    private readonly Func<string, ISystemManagers, IRenderable?>? _previousCreate;
    private readonly GumHotReloadManager _manager;
    private readonly TextWriter _previousError;
    private readonly StringWriter _errors = new StringWriter();
    private GumProjectSave _runtimeProject;

    public ToolSaveRuntimeWorld(string gumxPath, string screenName)
    {
        GumxPath = gumxPath;
        ScreenName = screenName;
        _previousCreate = ElementSaveExtensions.CustomCreateGraphicalComponentFunc;
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = (name, managers) => new RecordingRenderable();
        _previousError = Console.Error;
        Console.SetError(_errors);

        GumProjectSave loaded = GumProjectSave.Load(gumxPath);
        loaded.Initialize();
        _runtimeProject = loaded;
        InRuntime(() =>
        {
            Screen = loaded.Screens.Single(screen => screen.Name == screenName)
                .ToGraphicalUiElement(systemManagers: null!, addToManagers: false);
        });

        _manager = new GumHotReloadManager(_ => { }, (_, _) => 0, _ => { });
        _manager.ReloadCompleted += () => Reloads++;
        _manager.ReloadFinished += Results.Add;
        _manager.Start(gumxPath);
    }

    public string GumxPath { get; }
    public string ScreenName { get; }
    public GraphicalUiElement Screen { get; private set; } = null!;
    public int Reloads { get; private set; }

    /// <summary>The result of every reload attempt, in order.</summary>
    public List<HotReloadResult> Results { get; } = new List<HotReloadResult>();

    /// <summary>What the runtime wrote to the error console (<c>[Gum] ...</c> lines) since the world was built.</summary>
    public string ErrorOutput => _errors.ToString();

    public GraphicalUiElement? Find(string name) => Screen.GetGraphicalUiElementByName(name);

    public string? TextOf(string name) => ((RecordingRenderable)Find(name)!.RenderableComponent).Text;

    public int RedOf(string name) => ((RecordingRenderable)Find(name)!.RenderableComponent).Red;

    /// <summary>Runs <paramref name="action"/> with the runtime's project in <c>ObjectFinder</c>, then hands the tool its own back.</summary>
    private void InRuntime(Action action)
    {
        GumProjectSave? toolProject = ObjectFinder.Self.GumProjectSave;
        ObjectFinder.Self.GumProjectSave = _runtimeProject;
        try
        {
            action();
        }
        finally
        {
            _runtimeProject = ObjectFinder.Self.GumProjectSave ?? _runtimeProject;
            ObjectFinder.Self.GumProjectSave = toolProject;
        }
    }

    /// <summary>
    /// Runs the game's per-frame Update until a reload completes; false if none did in
    /// <paramref name="timeout"/>. The clock is real: the watcher's event, the debounce and the reload.
    /// </summary>
    public bool WaitForReload(TimeSpan? timeout = null)
    {
        int before = Reloads;
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < (timeout ?? TimeSpan.FromSeconds(3)))
        {
            InRuntime(() => _manager.Update(new[] { Screen }));
            if (Reloads != before)
            {
                return true;
            }
            Thread.Sleep(10);
            Dispatcher.UIThread.RunJobs();
        }
        return false;
    }

    /// <summary>Runs Update for <paramref name="duration"/> and returns how many reloads happened.</summary>
    public int ReloadsDuring(TimeSpan duration)
    {
        int before = Reloads;
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < duration)
        {
            InRuntime(() => _manager.Update(new[] { Screen }));
            Thread.Sleep(10);
        }
        return Reloads - before;
    }

    /// <summary>
    /// The design-time part of the running tree (instances from the project, not what the game added)
    /// as text: name, element, layout and the Text and Red the renderable holds, down the hierarchy.
    /// </summary>
    public static string Describe(GraphicalUiElement element, int depth = 0)
    {
        string indent = new string(' ', depth * 2);
        RecordingRenderable? renderable = element.RenderableComponent as RecordingRenderable;
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        text.Append(indent).Append(element.Name).Append(" <").Append(element.ElementSave?.Name).Append("> ")
            .Append(element.X).Append(',').Append(element.Y).Append(' ').Append(element.Width).Append('x').Append(element.Height)
            .Append(" text=").Append(renderable?.Text).Append(" red=").Append(renderable?.Red).AppendLine();
        foreach (GraphicalUiElement child in element.ContainedElements.Concat(element.Children).Distinct()
                     .Where(child => child.Tag is InstanceSave))
        {
            text.Append(Describe(child, depth + 1));
        }
        return text.ToString();
    }

    /// <summary>The tree a game that started now, from the files on disk, would show.</summary>
    public string DescribeAFreshLoad()
    {
        GumProjectSave fresh = GumProjectSave.Load(GumxPath);
        fresh.Initialize();
        GumProjectSave? toolProject = ObjectFinder.Self.GumProjectSave;
        ObjectFinder.Self.GumProjectSave = fresh;
        try
        {
            ScreenSave? screen = fresh.Screens.FirstOrDefault(candidate => candidate.Name == Screen.ElementSave!.Name);
            return screen == null
                ? "<no screen " + Screen.ElementSave!.Name + ">"
                : Describe(screen.ToGraphicalUiElement(systemManagers: null!, addToManagers: false));
        }
        finally
        {
            ObjectFinder.Self.GumProjectSave = toolProject;
        }
    }

    public void Dispose()
    {
        _manager.Stop();
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = _previousCreate;
        Console.SetError(_previousError);
    }
}
