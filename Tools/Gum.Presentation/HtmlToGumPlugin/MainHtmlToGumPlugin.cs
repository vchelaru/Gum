using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.ProjectServices;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;

namespace HtmlToGumPlugin;

/// <summary>
/// Content > Import > HTML: runs converter/convert.ts (via tsx) into a staging folder, copies
/// Images/Fonts/FontCache into the open Gum project, then IImportLogic.ImportScreen. Shared by
/// both heads: the options and result dialogs are view models each head gives a view, progress
/// goes to the Output tab under the tool's spinner.
/// </summary>
[Export(typeof(PluginBase))]
public class MainHtmlToGumPlugin : PluginBase
{
    /// <summary>The screen name used when the source gives none.</summary>
    public const string DefaultScreenName = "ImportedScreen";

    private readonly IProjectState _projectState;
    private readonly IImportLogic _importLogic;
    private readonly IFileCommands _fileCommands;
    private readonly ISelectedState _selectedState;
    private readonly IGuiCommands _guiCommands;

    [ImportingConstructor]
    public MainHtmlToGumPlugin(
        IProjectState projectState,
        IImportLogic importLogic,
        IFileCommands fileCommands,
        ISelectedState selectedState,
        IDialogService dialogService,
        IGuiCommands guiCommands)
    {
        _projectState = projectState;
        _importLogic = importLogic;
        _fileCommands = fileCommands;
        _selectedState = selectedState;
        _dialogService = dialogService;
        _guiCommands = guiCommands;
    }

    public override string FriendlyName => "HTML to Gum";
    public override Version Version => new(0, 3, 0);

    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    public override void StartUp()
    {
        AddMenuEntry(HandleImportHtml, "Content", "Import", "HTML…");
    }

    private async void HandleImportHtml()
    {
        if (_projectState.NeedsToSaveProject)
        {
            _dialogService.ShowMessage("Save the Gum project before importing HTML.");
            return;
        }

        string projectDir = _projectState.ProjectDirectory;
        if (string.IsNullOrEmpty(projectDir))
        {
            _dialogService.ShowMessage("No project directory: save the project first.");
            return;
        }

        ImportPrefs prefs = ImportPrefs.Load();
        ImportOptions defaults = new ImportOptions
        {
            HtmlPath = prefs.LastSource,
            IsUrl = prefs.LastIsUrl,
            Selector = string.IsNullOrWhiteSpace(prefs.Selector) ? "body" : prefs.Selector,
            ScreenName = DeriveDefaultScreenName(prefs.LastSource, prefs.LastIsUrl),
            Width = prefs.Width > 0 ? prefs.Width : 800,
            Height = prefs.Height > 0 ? prefs.Height : 600,
            NoResponsive = prefs.NoResponsive,
            DestinationSubfolder = prefs.DestinationSubfolder,
        };
        ImportHtmlOptionsViewModel optionsDialog = new ImportHtmlOptionsViewModel(_dialogService, defaults);
        if (!_dialogService.Show(optionsDialog) || optionsDialog.Result is not { } opts)
        {
            return;
        }

        prefs.Selector = opts.Selector;
        prefs.Width = opts.Width;
        prefs.Height = opts.Height;
        prefs.NoResponsive = opts.NoResponsive;
        prefs.LastSource = opts.HtmlPath;
        prefs.LastIsUrl = opts.IsUrl;
        prefs.DestinationSubfolder = opts.DestinationSubfolder;
        prefs.Save();

        string? subfolder = string.IsNullOrWhiteSpace(opts.DestinationSubfolder) ? null : opts.DestinationSubfolder.Trim();

        string converterDir = ResolveConverterDir();
        string convertTs = Path.Combine(converterDir, "convert.ts");
        string convertMjs = Path.Combine(converterDir, "convert.mjs");
        bool useTs = File.Exists(convertTs);
        string convertEntry = useTs ? convertTs : convertMjs;
        if (!File.Exists(convertEntry))
        {
            _dialogService.ShowMessage(
                "Converter not found.\n\n" +
                $"Looked for:\n{convertTs}\n{convertMjs}\n\n" +
                "Fix: set the HTMLTOGUM_CONVERTER environment variable to the converter folder, then restart Gum.");
            return;
        }

        if (!TryFindNode(out string nodePath, out string nodeHint))
        {
            _dialogService.ShowMessage(
                "Node.js was not found on PATH.\n\n" +
                "Install Node.js LTS and ensure `node` works in a terminal, then restart Gum.\n\n" +
                nodeHint);
            return;
        }

        // Unique screen name if one already exists in the project (scoped to the destination subfolder, if any).
        string screenName = HtmlImportNaming.ResolveUniqueScreenName(
            opts.ScreenName, subfolder, name => ObjectFinder.Self.GetElementSave(name) != null);
        if (screenName != opts.ScreenName)
        {
            string existingQualifiedName = HtmlImportNaming.QualifyScreenName(opts.ScreenName, subfolder);
            string newQualifiedName = HtmlImportNaming.QualifyScreenName(screenName, subfolder);
            if (!_dialogService.ShowYesNoMessage(
                    $"Screen \"{existingQualifiedName}\" already exists. Import as \"{newQualifiedName}\"?",
                    FriendlyName))
            {
                return;
            }
        }

        string stageDir = Path.Combine(Path.GetTempPath(), "html-to-gum-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stageDir);

        ImportPhaseRecorder recorder = new ImportPhaseRecorder();

        // Progress: the converter can take 10 to 30 s, so the tool's spinner shows while its
        // status lines go to the Output tab. The Progress instance created here reports on the UI
        // thread, which the process reader threads are not on.
        ISpinner spinner = _guiCommands.ShowSpinner();
        spinner.SetTotal(1);
        Progress<string> progress = new Progress<string>(ReportStatus);
        ReportStatus("Running converter…");

        try
        {
            string tsxCli = Path.Combine(converterDir, "node_modules", "tsx", "dist", "cli.mjs");
            if (useTs && !File.Exists(tsxCli))
            {
                ReportStatus("Installing converter dependencies (first run only)…");
                (string shell, string shellArguments) = ShellCommand.Build("npm install");
                (int installExitCode, string installStdout, string installStderr) = await recorder
                    .MeasureAsync("npm install", () => RunProcessAsync(
                        shell, shellArguments, converterDir, progress))
                    .ConfigureAwait(true);

                if (installExitCode != 0 || !File.Exists(tsxCli))
                {
                    spinner.Hide();
                    _dialogService.ShowMessage(
                        FormatNpmInstallFailure(installExitCode, installStdout, installStderr, converterDir));
                    return;
                }
            }

            string flagArgs = opts.NoResponsive ? " --no-responsive" : "";
            string scriptArgs =
                $"\"{opts.HtmlPath}\" \"{opts.Selector}\" {screenName} " +
                $"{opts.Width} {opts.Height} --out=\"{stageDir}\" --tag=plugin{flagArgs}";
            // Prefer local tsx (same as `npx tsx convert.ts`); fall back to legacy convert.mjs.
            string args = useTs
                ? $"\"{tsxCli}\" \"{convertTs}\" {scriptArgs}"
                : $"\"{convertMjs}\" {scriptArgs}";

            ReportStatus($"{(useTs ? "tsx convert.ts" : "node convert.mjs")} → {screenName}");
            (int exitCode, string stdout, string stderr) = await recorder
                .MeasureAsync("converter process", () => RunProcessAsync(nodePath, args, converterDir, progress))
                .ConfigureAwait(true);

            if (exitCode != 0)
            {
                spinner.Hide();
                _dialogService.ShowMessage(FormatConverterFailure(exitCode, stdout, stderr, nodePath, converterDir));
                return;
            }

            string gusx = Path.Combine(stageDir, "Screens", screenName + ".gusx");
            if (!File.Exists(gusx))
            {
                spinner.Hide();
                _dialogService.ShowMessage(
                    $"Converter finished but {screenName}.gusx was not found in staging.\n\n" +
                    SummarizeConverterLog(stdout));
                return;
            }

            ReportStatus("Copying Images / Fonts / FontCache…");
            recorder.Measure("asset copy", () => AssetTreeCopier.CopyStagedAssets(stageDir, projectDir));

            ReportStatus("Importing screen into project…");
            ScreenSave screenSave = recorder.Measure(
                "deserialize screen",
                () => ElementReference.DeserializeElement<ScreenSave>(gusx, GumProjectSave.NativeVersion));
            if (subfolder != null)
            {
                screenSave.Name = HtmlImportNaming.QualifyScreenName(screenSave.Name, subfolder);
            }
            string qualifiedScreenName = screenSave.Name;
            ScreenSave? imported = recorder.Measure(
                "ImportScreen", () => _importLogic.ImportScreen(screenSave, saveProject: false));
            if (imported is null)
            {
                spinner.Hide();
                _dialogService.ShowMessage("ImportScreen returned null: check for name conflicts.");
                return;
            }

            // The remaining phases run on the UI thread, which is why the result dialog can come
            // up unresponsive; keep them separately timed rather than lumped into one number.
            recorder.Measure("TryAutoSaveProject", () => { _fileCommands.TryAutoSaveProject(); });
            string? gumxPath = _projectState.GumProjectSave?.FullFileName;
            if (!string.IsNullOrEmpty(gumxPath))
            {
                recorder.Measure("LoadProject", () => { _fileCommands.LoadProject(gumxPath); });
            }

            recorder.Measure("select screen", () =>
            {
                // Re-resolve after reload so selection points at the live ElementSave.
                ElementSave? live = ObjectFinder.Self.GetElementSave(qualifiedScreenName);
                if (live != null)
                {
                    _selectedState.SelectedElement = live;
                }
            });

            spinner.Hide();
            _dialogService.Show(new ImportHtmlResultViewModel(
                $"Imported and selected screen \"{qualifiedScreenName}\".",
                $"Timing log: {HtmlImportTimingLog.LogPath}\n\n" + SummarizeConverterLog(stdout, maxLines: 300)));
        }
        catch (Exception ex)
        {
            spinner.Hide();
            _dialogService.ShowMessage($"Import failed:\n{ex.Message}");
        }
        finally
        {
            spinner.Hide();
            HtmlImportTimingLog.Append(HtmlImportTimingLog.LogPath, HtmlImportTimingLog.Format(new ImportTimingRun
            {
                TimestampUtc = DateTime.UtcNow,
                Source = opts.HtmlPath,
                ScreenName = screenName,
                ViewportWidth = opts.Width,
                ViewportHeight = opts.Height,
                Responsive = !opts.NoResponsive,
                PluginPhases = recorder.Phases.ToList(),
                PluginTotalMilliseconds = recorder.Total,
                ConverterTimings = HtmlImportTimingLog.TryReadConverterTimings(stageDir),
            }));
            try { Directory.Delete(stageDir, recursive: true); } catch { /* temp cleanup best-effort */ }
        }
    }

    private void ReportStatus(string status) => _guiCommands.PrintOutput("Import HTML: " + status);

    private static string FormatConverterFailure(
        int exitCode, string stdout, string stderr, string nodePath, string converterDir) =>
        FormatProcessFailure(
            "Converter failed", exitCode, stdout, stderr,
            [$"node: {nodePath}", $"cwd:  {converterDir}"],
            "(no stdout/stderr; is Playwright Chromium installed?\n" +
            "Run: cd Tool/HtmlToGum/converter && npx playwright-core install chromium)");

    private static string FormatNpmInstallFailure(int exitCode, string stdout, string stderr, string converterDir) =>
        FormatProcessFailure(
            "Automatic npm install failed", exitCode, stdout, stderr,
            [$"cwd: {converterDir}"],
            "(no output; is npm on PATH? It ships with Node.js; run `npm -v` in a terminal to check.)") +
        "\nFix: run `cd Tool/HtmlToGum/converter && npm install` manually, then retry.";

    /// <summary>Formats a failed child-process run for display: exit code, context lines, then the last ~40 lines of its stderr/stdout.</summary>
    private static string FormatProcessFailure(
        string title, int exitCode, string stdout, string stderr, string[] contextLines, string noOutputHint)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{title} (exit {exitCode}).");
        sb.AppendLine();
        foreach (string line in contextLines)
        {
            sb.AppendLine(line);
        }
        sb.AppendLine();
        string err = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
        if (string.IsNullOrWhiteSpace(err))
        {
            sb.AppendLine(noOutputHint);
        }
        else
        {
            // Keep the dialog readable; full log is usually the last ~40 lines.
            string[] lines = err.Replace("\r\n", "\n").Split('\n');
            System.Collections.Generic.IEnumerable<string> tail = lines.Length <= 40 ? lines : lines.Skip(lines.Length - 40);
            sb.AppendLine(string.Join("\n", tail));
        }
        return sb.ToString();
    }

    private static bool TryFindNode(out string nodePath, out string hint)
    {
        nodePath = "node";
        hint = "";
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = "-v",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using Process? proc = Process.Start(psi);
            if (proc is null)
            {
                hint = "Process.Start returned null.";
                return false;
            }
            string output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(5000);
            if (proc.ExitCode != 0)
            {
                hint = $"node -v exited {proc.ExitCode}.";
                return false;
            }
            hint = $"Found {output}";
            return true;
        }
        catch (Exception ex)
        {
            hint = ex.Message;
            return false;
        }
    }

    private static Task<(int exitCode, string stdout, string stderr)> RunProcessAsync(
        string fileName, string arguments, string workingDirectory, IProgress<string> progress)
    {
        return Task.Run(() =>
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using Process proc = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start node.");

            StringBuilder stdout = new StringBuilder();
            StringBuilder stderr = new StringBuilder();
            proc.OutputDataReceived += (_, ev) =>
            {
                if (ev.Data is null) return;
                stdout.AppendLine(ev.Data);
                string line = ev.Data.Trim();
                if (line.Length > 0 && line.Length < 120)
                {
                    progress.Report(line);
                }
            };
            proc.ErrorDataReceived += (_, ev) =>
            {
                if (ev.Data is null) return;
                stderr.AppendLine(ev.Data);
            };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();
            proc.WaitForExit();
            return (proc.ExitCode, stdout.ToString(), stderr.ToString());
        });
    }

    private static string SummarizeConverterLog(string stdout, int maxLines = 12)
    {
        System.Collections.Generic.IEnumerable<string> lines = stdout.Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => l.Length > 0)
            .Where(l => !l.StartsWith('>'))
            .TakeLast(maxLines);
        return string.Join("\n", lines);
    }

    /// <summary>
    /// Candidate converter directories, in lookup order, given the head's base directory
    /// (<c>AppDomain.CurrentDomain.BaseDirectory</c>). The WPF head's <c>Gum.csproj</c> sets
    /// <c>AppendTargetFrameworkToOutputPath=false</c>, so its base is <c>&lt;repo&gt;/Gum/bin/&lt;Config&gt;/</c>
    /// and three levels up reaches the repo (or worktree) root; the Avalonia head runs from
    /// <c>Tool/Gum.Avalonia/bin/&lt;Config&gt;/&lt;tfm&gt;/</c>, four levels under <c>Tool/</c>.
    /// </summary>
    public static string[] GetConverterDirCandidates(string baseDir) =>
    [
        Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "Tool", "HtmlToGum", "converter")),
        Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "HtmlToGum", "converter")),
        Path.GetFullPath(Path.Combine(baseDir, "converter")),
        // Legacy sibling html-to-gum repository layouts.
        Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "html-to-gum", "converter")),
        Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "html-to-gum", "converter")),
        Path.GetFullPath(Path.Combine(baseDir, "..", "..", "html-to-gum", "converter")),
        Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "Repos", "html-to-gum", "converter")),
    ];

    private static string ResolveConverterDir()
    {
        string? env = Environment.GetEnvironmentVariable("HTMLTOGUM_CONVERTER");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
        {
            return Path.GetFullPath(env);
        }

        string[] candidates = GetConverterDirCandidates(AppDomain.CurrentDomain.BaseDirectory);
        foreach (string candidate in candidates)
        {
            if (File.Exists(Path.Combine(candidate, "convert.ts")) ||
                File.Exists(Path.Combine(candidate, "convert.mjs")))
            {
                return candidate;
            }
        }
        return candidates[0];
    }

    /// <summary>Turns <paramref name="raw"/> into a valid screen name: word characters only, not starting with a digit.</summary>
    public static string SanitizeScreenName(string? raw)
    {
        string name = Regex.Replace(raw ?? DefaultScreenName, @"[^A-Za-z0-9_]", "_");
        if (string.IsNullOrEmpty(name)) name = DefaultScreenName;
        if (char.IsDigit(name[0])) name = "S_" + name;
        return name;
    }

    private static string DeriveDefaultScreenName(string source, bool isUrl)
    {
        if (string.IsNullOrWhiteSpace(source)) return DefaultScreenName;
        if (isUrl)
        {
            return Uri.TryCreate(source, UriKind.Absolute, out Uri? uri)
                ? SanitizeScreenName(uri.Host)
                : DefaultScreenName;
        }
        return SanitizeScreenName(Path.GetFileNameWithoutExtension(source));
    }
}
