using Gum.ProjectServices.CodeGeneration;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Shared setup for the <c>NewProject*</c> service tests: a temp folder (with a space in its name, like a real
/// user's), a service wired to recording fakes, and request and file helpers.
/// </summary>
public abstract class NewProjectServiceTestBase : IDisposable
{
    protected readonly string _folder;
    protected readonly string _checkout;
    protected readonly List<string> _calls = new();
    internal readonly RecordingCodegen _codegen;

    protected NewProjectServiceTestBase()
    {
        // The space is deliberate: the user's real projects live under "Vic Personal".
        _folder = Path.Combine(Path.GetTempPath(), "Gum NewProject " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _checkout = GumSourceLocator.Find(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("These tests run from a Gum checkout.");
        _codegen = new RecordingCodegen(_calls);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    protected string ThemesRoot => Path.Combine(_checkout, "Tools", "Gum.ProjectServices", "Templates", "FormsThemes");

    protected static string[] ThemeNames(string themesRoot) =>
        Directory.GetDirectories(themesRoot).Select(Path.GetFileName).Select(name => name!).OrderBy(name => name).ToArray();

    protected NewProjectService CreateService(IRestoreRunner? restore = null, IPackageAddRunner? add = null) =>
        new NewProjectService(
            new FormsThemeLocator(new[] { ThemesRoot }),
            restore ?? new RecordingRestore(_calls),
            add ?? new RecordingAdd(_calls),
            new[] { _checkout },
            _codegen);

    protected NewProjectRequest Request(
        string? platform, string name = "Game", string template = "forms", string? theme = null, bool sourceLinked = false,
        bool restore = false, bool packages = false, bool generate = false, bool xml = false) =>
        new NewProjectRequest
        {
            Path = Path.Combine(_folder, name),
            Platform = platform,
            Template = template,
            Theme = theme,
            SourceLinked = sourceLinked,
            Restore = restore,
            IncludeRecommendedPackages = packages,
            GenerateCode = generate,
            UseXmlFormat = xml,
        };

    protected Task<NewProjectResult> Run(NewProjectRequest request, CancellationToken token = default) =>
        CreateService().ExecuteAsync(request, null, token);

    protected string Game(string name = "Game") => Path.Combine(_folder, name);

    protected static IEnumerable<(string Name, HostPlatform Platform)> Platforms => NewProjectPlatforms.MainNames;

    public static IEnumerable<object[]> PlatformNames() => NewProjectPlatforms.MainNames.Select(entry => new object[] { entry.Name });

    protected static string[] RelativeFiles(string root) =>
        Directory.GetFiles(root, "*", SearchOption.AllDirectories).Select(file => Path.GetRelativePath(root, file)).Order().ToArray();
}


internal class RecordingRestore : IRestoreRunner
{
    private readonly List<string> _calls;

    public RecordingRestore(List<string> calls) => _calls = calls;

    public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken)
    {
        _calls.Add("restore " + Path.GetFileName(csprojPath));
        return Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
    }
}

internal class FailingRestore : IRestoreRunner
{
    public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken) =>
        Task.FromResult(new RestoreResult { Status = RestoreStatus.Failed, ExitCode = 1, OutputTail = "offline" });
}

internal class CancelingRestore : IRestoreRunner
{
    private readonly CancellationTokenSource _source;

    public CancelingRestore(CancellationTokenSource source) => _source = source;

    public Task<RestoreResult> RestoreAsync(string csprojPath, Action<string>? onOutputLine, CancellationToken cancellationToken)
    {
        _source.Cancel();
        return Task.FromResult(new RestoreResult { Status = RestoreStatus.Canceled });
    }
}

internal class RecordingAdd : IPackageAddRunner
{
    private readonly List<string> _calls;

    public RecordingAdd(List<string> calls) => _calls = calls;

    public Task<RestoreResult> AddAsync(string csprojPath, string packageId, Action<string>? onOutputLine, CancellationToken cancellationToken)
    {
        _calls.Add("add " + packageId);
        return Task.FromResult(new RestoreResult { Status = RestoreStatus.Succeeded, ExitCode = 0 });
    }
}

internal class CancelingAdd : IPackageAddRunner
{
    private readonly CancellationTokenSource _source;

    public CancelingAdd(CancellationTokenSource source) => _source = source;

    public Task<RestoreResult> AddAsync(string csprojPath, string packageId, Action<string>? onOutputLine, CancellationToken cancellationToken)
    {
        _source.Cancel();
        return Task.FromResult(new RestoreResult { Status = RestoreStatus.Canceled });
    }
}

internal class RecordingCodegen : IHeadlessCodegenRunner
{
    private readonly List<string> _calls;

    public RecordingCodegen(List<string> calls) => _calls = calls;

    public bool Fail { get; set; }

    public bool Throw { get; set; }

    public int Run(string projectPath, IReadOnlyList<string>? elementNames, bool prune, TextWriter output, TextWriter error)
    {
        _calls.Add("codegen " + Path.GetFileName(projectPath));
        if (Throw)
        {
            throw new InvalidOperationException("boom");
        }
        if (Fail)
        {
            error.WriteLine("boom");
            return 1;
        }
        return 0;
    }
}
