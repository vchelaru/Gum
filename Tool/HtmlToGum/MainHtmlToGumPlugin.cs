using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;

namespace HtmlToGumPlugin;

/// <summary>
/// Content → Import HTML… — runs converter/convert.ts (via tsx) into a staging folder, copies
/// Images/Fonts/FontCache into the open Gum project, then IImportLogic.ImportScreen.
/// </summary>
[Export(typeof(PluginBase))]
public class MainHtmlToGumPlugin : WpfPluginBase
{
    public override string FriendlyName => "HTML to Gum";
    public override Version Version => new(0, 3, 0);

    public override bool ShutDown(PluginShutDownReason shutDownReason) => true;

    public override void StartUp()
    {
        AddMenuItemTo("Import HTML…", HandleImportHtml, "Content");
    }

    private async void HandleImportHtml(object? sender, System.Windows.RoutedEventArgs e)
    {
        var projectState = Locator.GetRequiredService<IProjectState>();
        var importLogic = Locator.GetRequiredService<IImportLogic>();
        var fileCommands = Locator.GetRequiredService<IFileCommands>();
        var selectedState = Locator.GetRequiredService<ISelectedState>();

        if (projectState.NeedsToSaveProject)
        {
            _dialogService.ShowMessage("Save the Gum project before importing HTML.");
            return;
        }

        var projectDir = projectState.ProjectDirectory;
        if (string.IsNullOrEmpty(projectDir))
        {
            _dialogService.ShowMessage("No project directory — save the project first.");
            return;
        }

        var prefs = ImportPrefs.Load();
        var defaults = new ImportOptions
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
        if (!ImportOptionsForm.ShowDialog(defaults, out var opts) || opts is null) return;

        prefs.Selector = opts.Selector;
        prefs.Width = opts.Width;
        prefs.Height = opts.Height;
        prefs.NoResponsive = opts.NoResponsive;
        prefs.LastSource = opts.HtmlPath;
        prefs.LastIsUrl = opts.IsUrl;
        prefs.DestinationSubfolder = opts.DestinationSubfolder;
        prefs.Save();

        var subfolder = string.IsNullOrWhiteSpace(opts.DestinationSubfolder) ? null : opts.DestinationSubfolder.Trim();

        var converterDir = ResolveConverterDir();
        var convertTs = Path.Combine(converterDir, "convert.ts");
        var convertMjs = Path.Combine(converterDir, "convert.mjs");
        var useTs = File.Exists(convertTs);
        var convertEntry = useTs ? convertTs : convertMjs;
        if (!File.Exists(convertEntry))
        {
            _dialogService.ShowMessage(
                "Converter not found.\n\n" +
                $"Looked for:\n{convertTs}\n{convertMjs}\n\n" +
                "Fix: run `cd Tool/HtmlToGum/converter && npm install`, or set the\n" +
                "HTMLTOGUM_CONVERTER environment variable to the converter folder, then restart Gum.");
            return;
        }

        if (!TryFindNode(out var nodePath, out var nodeHint))
        {
            _dialogService.ShowMessage(
                "Node.js was not found on PATH.\n\n" +
                "Install Node.js LTS and ensure `node` works in a terminal, then restart Gum.\n\n" +
                nodeHint);
            return;
        }

        var tsxCli = Path.Combine(converterDir, "node_modules", "tsx", "dist", "cli.mjs");
        if (useTs && !File.Exists(tsxCli))
        {
            _dialogService.ShowMessage(
                "tsx is required to run convert.ts but was not found.\n\n" +
                $"Looked for:\n{tsxCli}\n\n" +
                "Fix: cd Tool/HtmlToGum/converter && npm install, then restart Gum.");
            return;
        }

        // Unique screen name if one already exists in the project (scoped to the destination subfolder, if any).
        var screenName = HtmlImportNaming.ResolveUniqueScreenName(
            opts.ScreenName, subfolder, name => ObjectFinder.Self.GetElementSave(name) != null);
        if (screenName != opts.ScreenName)
        {
            var existingQualifiedName = HtmlImportNaming.QualifyScreenName(opts.ScreenName, subfolder);
            var newQualifiedName = HtmlImportNaming.QualifyScreenName(screenName, subfolder);
            if (!_dialogService.ShowYesNoMessage(
                    $"Screen \"{existingQualifiedName}\" already exists. Import as \"{newQualifiedName}\"?",
                    FriendlyName))
            {
                return;
            }
        }

        var stageDir = Path.Combine(Path.GetTempPath(), "html-to-gum-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stageDir);

        using var progress = new ImportProgressForm();
        progress.Show();
        progress.SetStatus("Running converter…");

        try
        {
            var flagArgs = opts.NoResponsive ? " --no-responsive" : "";
            var scriptArgs =
                $"\"{opts.HtmlPath}\" \"{opts.Selector}\" {screenName} " +
                $"{opts.Width} {opts.Height} --out=\"{stageDir}\" --tag=plugin{flagArgs}";
            // Prefer local tsx (same as `npx tsx convert.ts`); fall back to legacy convert.mjs.
            var args = useTs
                ? $"\"{tsxCli}\" \"{convertTs}\" {scriptArgs}"
                : $"\"{convertMjs}\" {scriptArgs}";

            progress.SetStatus($"{(useTs ? "tsx convert.ts" : "node convert.mjs")} → {screenName}");
            var (exitCode, stdout, stderr) = await RunProcessAsync(nodePath, args, converterDir, progress)
                .ConfigureAwait(true);

            if (exitCode != 0)
            {
                progress.Hide();
                _dialogService.ShowMessage(FormatConverterFailure(exitCode, stdout, stderr, nodePath, converterDir));
                return;
            }

            var gusx = Path.Combine(stageDir, "Screens", screenName + ".gusx");
            if (!File.Exists(gusx))
            {
                progress.Hide();
                _dialogService.ShowMessage(
                    $"Converter finished but {screenName}.gusx was not found in staging.\n\n" +
                    SummarizeConverterLog(stdout));
                return;
            }

            progress.SetStatus("Copying Images / Fonts / FontCache…");
            CopyAssetTree(Path.Combine(stageDir, "Images"), Path.Combine(projectDir, "Images"));
            CopyAssetTree(Path.Combine(stageDir, "Fonts"), Path.Combine(projectDir, "Fonts"));
            CopyAssetTree(Path.Combine(stageDir, "FontCache"), Path.Combine(projectDir, "FontCache"));

            progress.SetStatus("Importing screen into project…");
            var screenSave = ElementReference.DeserializeElement<ScreenSave>(gusx, GumProjectSave.NativeVersion);
            if (subfolder != null)
            {
                screenSave.Name = HtmlImportNaming.QualifyScreenName(screenSave.Name, subfolder);
            }
            var qualifiedScreenName = screenSave.Name;
            var imported = importLogic.ImportScreen(screenSave, saveProject: false);
            if (imported is null)
            {
                progress.Hide();
                _dialogService.ShowMessage("ImportScreen returned null — check for name conflicts.");
                return;
            }

            fileCommands.TryAutoSaveProject();
            var gumxPath = projectState.GumProjectSave?.FullFileName;
            if (!string.IsNullOrEmpty(gumxPath))
            {
                fileCommands.LoadProject(gumxPath);
            }

            // Re-resolve after reload so selection points at the live ElementSave.
            var live = ObjectFinder.Self.GetElementSave(qualifiedScreenName);
            if (live != null)
            {
                selectedState.SelectedElement = live;
            }

            progress.Hide();
            _dialogService.ShowMessage(
                $"Imported and selected screen \"{qualifiedScreenName}\".\n\n{SummarizeConverterLog(stdout)}");
        }
        catch (Exception ex)
        {
            progress.Hide();
            _dialogService.ShowMessage($"Import failed:\n{ex.Message}");
        }
        finally
        {
            try { Directory.Delete(stageDir, recursive: true); } catch { /* temp cleanup best-effort */ }
        }
    }

    private static string FormatConverterFailure(
        int exitCode, string stdout, string stderr, string nodePath, string converterDir)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Converter failed (exit {exitCode}).");
        sb.AppendLine();
        sb.AppendLine($"node: {nodePath}");
        sb.AppendLine($"cwd:  {converterDir}");
        sb.AppendLine();
        var err = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
        if (string.IsNullOrWhiteSpace(err))
        {
            sb.AppendLine("(no stdout/stderr — is Playwright Chromium installed?");
            sb.AppendLine("Run: cd Tool/HtmlToGum/converter && npx playwright-core install chromium");
        }
        else
        {
            // Keep the dialog readable; full log is usually the last ~40 lines.
            var lines = err.Replace("\r\n", "\n").Split('\n');
            var tail = lines.Length <= 40 ? lines : lines.Skip(lines.Length - 40);
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
            var psi = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = "-v",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using var proc = Process.Start(psi);
            if (proc is null)
            {
                hint = "Process.Start returned null.";
                return false;
            }
            var output = proc.StandardOutput.ReadToEnd().Trim();
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
        string fileName, string arguments, string workingDirectory, ImportProgressForm progress)
    {
        return Task.Run(() =>
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start node.");

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            proc.OutputDataReceived += (_, ev) =>
            {
                if (ev.Data is null) return;
                stdout.AppendLine(ev.Data);
                var line = ev.Data.Trim();
                if (line.Length > 0 && line.Length < 120)
                {
                    try { progress.BeginInvoke(new Action(() => progress.SetStatus(line))); }
                    catch { /* form may be closing */ }
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

    private static string SummarizeConverterLog(string stdout)
    {
        var lines = stdout.Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => l.Length > 0)
            .Where(l => !l.StartsWith('>'))
            .TakeLast(12);
        return string.Join("\n", lines);
    }

    private static void CopyAssetTree(string sourceDir, string destDir)
    {
        if (!Directory.Exists(sourceDir)) return;
        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sourceDir, file);
            var dest = Path.Combine(destDir, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, overwrite: true);
        }
    }

    private static string ResolveConverterDir()
    {
        var env = Environment.GetEnvironmentVariable("HTMLTOGUM_CONVERTER");
        if (!string.IsNullOrWhiteSpace(env) && Directory.Exists(env))
        {
            return Path.GetFullPath(env);
        }

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string[] candidates =
        [
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "Tool", "HtmlToGum", "converter")),
            Path.GetFullPath(Path.Combine(baseDir, "converter")),
            // Legacy sibling html-to-gum repository layouts.
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "html-to-gum", "converter")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "html-to-gum", "converter")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "html-to-gum", "converter")),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "Repos", "html-to-gum", "converter")),
        ];
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

    internal static string SanitizeScreenName(string? raw)
    {
        var name = Regex.Replace(raw ?? "ImportedScreen", @"[^A-Za-z0-9_]", "_");
        if (string.IsNullOrEmpty(name)) name = "ImportedScreen";
        if (char.IsDigit(name[0])) name = "S_" + name;
        return name;
    }

    private static string DeriveDefaultScreenName(string source, bool isUrl)
    {
        if (string.IsNullOrWhiteSpace(source)) return "ImportedScreen";
        if (isUrl)
        {
            return Uri.TryCreate(source, UriKind.Absolute, out var uri)
                ? SanitizeScreenName(uri.Host)
                : "ImportedScreen";
        }
        return SanitizeScreenName(Path.GetFileNameWithoutExtension(source));
    }
}

internal sealed class ImportOptions
{
    public string HtmlPath { get; set; } = "";
    public bool IsUrl { get; set; }
    public string Selector { get; set; } = "body";
    public string ScreenName { get; set; } = "ImportedScreen";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public bool NoResponsive { get; set; }
    public string DestinationSubfolder { get; set; } = "";
}

internal sealed class ImportPrefs
{
    public string LastSource { get; set; } = "";
    public bool LastIsUrl { get; set; }
    public string Selector { get; set; } = "body";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
    public bool NoResponsive { get; set; }
    public string DestinationSubfolder { get; set; } = "";

    private static string PrefsPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HtmlToGumPlugin", "import-prefs.json");

    public static ImportPrefs Load()
    {
        try
        {
            var path = PrefsPath;
            if (!File.Exists(path)) return new ImportPrefs();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ImportPrefs>(json) ?? new ImportPrefs();
        }
        catch
        {
            return new ImportPrefs();
        }
    }

    public void Save()
    {
        try
        {
            var path = PrefsPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // prefs are best-effort
        }
    }
}

/// <summary>Non-modal progress while convert.ts runs (can take 10–30s).</summary>
internal sealed class ImportProgressForm : Form
{
    private readonly Label _status;

    public ImportProgressForm()
    {
        Text = "Import HTML";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        ControlBox = false;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(420, 72);
        Font = new Font("Segoe UI", 9f);
        TopMost = true;

        _status = new Label
        {
            Text = "Starting…",
            Left = 16,
            Top = 24,
            Width = 388,
            Height = 28,
            AutoEllipsis = true,
        };
        Controls.Add(_status);
    }

    public void SetStatus(string text)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => SetStatus(text)));
            return;
        }
        _status.Text = text;
    }
}

/// <summary>WinForms dialog for HTML source (local file / URL) / selector / screen name / viewport / responsive flag / destination subfolder.</summary>
internal static class ImportOptionsForm
{
    public static bool ShowDialog(ImportOptions defaults, out ImportOptions? result)
    {
        result = null;
        using var form = new Form
        {
            Text = "Import HTML options",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ClientSize = new Size(460, 316),
            Font = new Font("Segoe UI", 9f),
        };

        var radioLocal = new RadioButton { Text = "Local file", Left = 12, Top = 12, Width = 100, Checked = !defaults.IsUrl };
        var radioUrl = new RadioButton { Text = "URL", Left = 116, Top = 12, Width = 80, Checked = defaults.IsUrl };
        var txtSource = new TextBox { Text = defaults.HtmlPath, Left = 12, Top = 36, Width = 356 };
        var btnBrowse = new Button { Text = "Browse…", Left = 372, Top = 34, Width = 76 };

        var lblSel = new Label { Text = "CSS root selector", Left = 12, Top = 72, Width = 140 };
        var txtSel = new TextBox { Text = defaults.Selector, Left = 160, Top = 68, Width = 280 };
        var lblName = new Label { Text = "Screen name", Left = 12, Top = 104, Width = 140 };
        var txtName = new TextBox { Text = defaults.ScreenName, Left = 160, Top = 100, Width = 280 };
        var lblSize = new Label { Text = "Viewport W×H", Left = 12, Top = 136, Width = 140 };
        var txtW = new TextBox { Text = defaults.Width.ToString(), Left = 160, Top = 132, Width = 80 };
        var txtH = new TextBox { Text = defaults.Height.ToString(), Left = 250, Top = 132, Width = 80 };
        var chkNoResp = new CheckBox
        {
            Text = "Disable responsive units (--no-responsive)",
            Left = 160,
            Top = 168,
            Width = 280,
            Checked = defaults.NoResponsive,
        };
        var lblSubfolder = new Label { Text = "Destination subfolder", Left = 12, Top = 200, Width = 140 };
        var txtSubfolder = new TextBox { Text = defaults.DestinationSubfolder, Left = 160, Top = 196, Width = 280 };
        var hint = new Label
        {
            Text = "Optional — avoids name conflicts by importing under Screens/<subfolder>/.",
            Left = 12,
            Top = 228,
            Width = 436,
            ForeColor = SystemColors.GrayText,
        };
        var btnOk = new Button { Text = "Import", Left = 264, Top = 272, Width = 88 };
        var btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Left = 360, Top = 272, Width = 88 };
        form.Controls.AddRange(
        [
            radioLocal, radioUrl, txtSource, btnBrowse,
            lblSel, txtSel, lblName, txtName, lblSize, txtW, txtH,
            chkNoResp, lblSubfolder, txtSubfolder, hint, btnOk, btnCancel,
        ]);
        form.AcceptButton = btnOk;
        form.CancelButton = btnCancel;

        void UpdateBrowseEnabled() => btnBrowse.Enabled = radioLocal.Checked;
        radioLocal.CheckedChanged += (_, _) => UpdateBrowseEnabled();
        radioUrl.CheckedChanged += (_, _) => UpdateBrowseEnabled();
        UpdateBrowseEnabled();

        btnBrowse.Click += (_, _) =>
        {
            using var fileDlg = new OpenFileDialog
            {
                Filter = "HTML (*.html;*.htm)|*.html;*.htm|All files (*.*)|*.*",
                Title = "Choose local HTML file",
            };
            if (fileDlg.ShowDialog(form) != DialogResult.OK) return;
            txtSource.Text = fileDlg.FileName;
            if (string.IsNullOrWhiteSpace(txtName.Text) || txtName.Text == "ImportedScreen")
            {
                txtName.Text = MainHtmlToGumPlugin.SanitizeScreenName(Path.GetFileNameWithoutExtension(fileDlg.FileName));
            }
        };

        btnOk.Click += (_, _) =>
        {
            var source = txtSource.Text.Trim();
            if (string.IsNullOrWhiteSpace(source))
            {
                MessageBox.Show(form, "Enter a local HTML file path or a URL.", "Import HTML",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (radioUrl.Checked)
            {
                if (!HtmlImportNaming.IsUrl(source))
                {
                    MessageBox.Show(form, "URL must start with http:// or https://.", "Import HTML",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (!File.Exists(source))
            {
                MessageBox.Show(form, $"File not found:\n{source}", "Import HTML",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            form.DialogResult = DialogResult.OK;
            form.Close();
        };

        if (form.ShowDialog() != DialogResult.OK) return false;

        if (!int.TryParse(txtW.Text.Trim(), out var w) || w < 1) w = defaults.Width;
        if (!int.TryParse(txtH.Text.Trim(), out var h) || h < 1) h = defaults.Height;
        var name = string.IsNullOrWhiteSpace(txtName.Text) ? defaults.ScreenName : txtName.Text.Trim();
        name = MainHtmlToGumPlugin.SanitizeScreenName(name);

        result = new ImportOptions
        {
            HtmlPath = txtSource.Text.Trim(),
            IsUrl = radioUrl.Checked,
            Selector = string.IsNullOrWhiteSpace(txtSel.Text) ? "body" : txtSel.Text.Trim(),
            ScreenName = name,
            Width = w,
            Height = h,
            NoResponsive = chkNoResp.Checked,
            DestinationSubfolder = txtSubfolder.Text.Trim(),
        };
        return true;
    }
}
