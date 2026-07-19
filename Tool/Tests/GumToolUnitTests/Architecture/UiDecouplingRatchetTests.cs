using System.IO;
using System.Text.RegularExpressions;
using Shouldly;

namespace GumToolUnitTests.Architecture;

/// <summary>
/// Issue #3225 Phase 0: baseline counts + a ratchet for the Gum tool UI/logic decoupling effort, so
/// backsliding is caught mechanically instead of by manual audit (the issue's original hand-written
/// estimates had drifted far from reality by the time Phase 1 started).
///
/// Each test is a source-text scan over Gum/**/*.cs (the tool + its in-tree plugins) at the repo root
/// discovered from the test assembly's location, not a semantic analysis -- see the per-test comment
/// for exactly what is counted and what is deliberately excluded. Lower a baseline as items land;
/// never raise one to make a new violation pass.
/// </summary>
public class UiDecouplingRatchetTests
{
    private static readonly string ToolSourceRoot = Path.Combine(FindRepoRoot(), "Gum");

    [Fact]
    public void InlineDialogBypassSites_DoesNotExceedBaseline()
    {
        // Sites that pop a WPF Window/MessageBox directly instead of going through
        // IDialogService.Show<T>(). Excludes Gum/Services/Dialogs/** itself -- that's the
        // IDialogService implementation; it's the seam everything else should route through, so it's
        // allowed to talk to WPF directly.
        //
        // Not counted here: ElementTreeViewManager.RightClick.cs's direct `new AddInstanceDialogViewModel(...)`
        // + OnAffirmative() calls. Those construct a dialog ViewModel and drive it headlessly as a command
        // object for the favorited-component quick-add menu -- no WPF window is ever shown, so it isn't an
        // IDialogService bypass (audited 2026-07, see issue #3225 progress comments).
        const int Baseline = 2;

        var pattern = new Regex(@"MessageBox\.Show\(|\bnew\s+\w*Window\s*\(|\.ShowDialog\(");

        int count = SourceFiles()
            .Where(f => !NormalizePath(f).Contains("/Gum/Services/Dialogs/"))
            .Sum(f => File.ReadAllLines(f).Count(line => !line.TrimStart().StartsWith("//") && pattern.IsMatch(line)));

        count.ShouldBeLessThanOrEqualTo(Baseline);
    }

    [Fact]
    public void SelfStaticReferenceCount_DoesNotExceedBaseline()
    {
        // Dropped from 339 to 327: ElementAnimationsViewModel, SubAnimationSelectionDialogViewModel,
        // and their newly-relocated dependency interfaces (IAnimationCollectionViewModelManager,
        // IRenameManager, IAnimationFilePathService, NameValidator) moved to Gum.Presentation,
        // taking their .Self call sites out of the Gum/ scan (issue #3754). Dropped further, from
        // 327 to 323: BehaviorsViewModel, AlignmentViewModel, CodeWindowViewModel,
        // MainControlViewModel (VariableGrid and TextureCoordinateSelectionPlugin), and their
        // relocated dependency interfaces moved to Gum.Presentation. Dropped further, from 323 to
        // 293: VariableReferenceLogic, VariableInCategoryPropagationLogic, FileChangeReactionLogic,
        // and BehaviorToolOnlyReferencesApplier moved to Gum.Presentation (issue #3754 Round 5).
        const int Baseline = 293;

        var pattern = new Regex(@"\.Self\b");
        int count = SourceFiles().Sum(f => pattern.Matches(File.ReadAllText(f)).Count);

        count.ShouldBeLessThanOrEqualTo(Baseline);
    }

    [Fact]
    public void SystemWindowsUsageInViewModels_DoesNotExceedBaseline()
    {
        // Dropped from 46: AnimationViewModel/AnimatedKeyframeViewModel moved to headless
        // Gum.Presentation (their dead WPF BitmapFrame plumbing removed, and their remaining
        // Visibility/SolidColorBrush properties converted to bool per ADR-0004); ElementAnimationsViewModel
        // lost its own dead BitmapFrame/Visibility properties too. Dropped further, from 34 to 24:
        // ElementAnimationsViewModel itself (plus SubAnimationSelectionDialogViewModel,
        // AddStateKeyframeDialog, AddAnimationDialogViewModel) moved to Gum.Presentation once its
        // WPF MenuItem/DispatcherTimer coupling was replaced with framework-neutral
        // ContextMenuItemViewModel/IUiTimer seams (issue #3754). Dropped further, from 24 to 21:
        // CodeWindowViewModel moved to Gum.Presentation (its Visibility properties converted to
        // bool per ADR-0004) and SearchItemViewModel moved to Gum.Presentation (dropped a dead
        // System.Windows.Media.Imaging using); PerformanceViewModel stayed in Gum.csproj (it reads
        // the XNALIKE-only RenderingLibrary.Graphics.Renderer, not movable to the headless assembly)
        // but lost its own DispatcherTimer coupling via the same IUiTimer seam (issue #3754). Dropped
        // further, from 21 to 8: ThemingDialogViewModel, ImportFromGumxViewModel, ImportBaseDialogViewModel
        // (+ its 3 subclasses), MainControlViewModel (VariableGrid and TextureCoordinateSelectionPlugin)
        // moved to Gum.Presentation, each converting its Visibility/Color/Brush properties to neutral
        // types. The remaining 8 are ContextMenuItemViewModelExtensions.cs (a WPF-only MenuItem builder,
        // correctly still tool-side) plus MainWindowViewModel/MainPanelViewModel, which model WPF
        // window/panel chrome directly and are Phase 4b territory, not Phase 3 candidates.
        const int Baseline = 8;

        var pattern = new Regex(@"System\.Windows");
        int count = SourceFiles()
            .Where(f => Path.GetFileName(f).Contains("ViewModel", System.StringComparison.OrdinalIgnoreCase))
            .Sum(f => pattern.Matches(File.ReadAllText(f)).Count);

        count.ShouldBeLessThanOrEqualTo(Baseline);
    }

    [Fact]
    public void WinFormsOrWpfTypeLeaksInInterfaceSignatures_DoesNotExceedBaseline()
    {
        // Scope: System.Windows* namespaces (WPF's System.Windows tree, WinForms' System.Windows.Forms)
        // -- the UI-framework widget namespaces the tool is being decoupled from. System.Drawing
        // (Color, Point) is deliberately excluded: it's the general graphics-primitive namespace used
        // throughout the whole codebase, runtime included, not tool-UI-framework coupling -- see
        // ADR-0004 for that separate, broader neutral-types effort.
        //
        // Heuristic: a file that declares an interface and imports a System.Windows* namespace, with no
        // class declared in the same file. The class exclusion avoids false positives from files that
        // combine an interface with its WPF-heavy implementation (e.g. ExposeVariableService.cs,
        // ThemingDialogViewModel.cs), where the using is consumed by the class, not the interface
        // (audited 2026-07 -- both interfaces there only reference Gum/System.Drawing types). Known
        // blind spot: a leak added to an interface living in a combined interface+class file won't be
        // caught by this heuristic.
        //
        // ITabManager.AddControl and IBitmapLoader.LoadImage (the 2 sites baseline=2 was tracking)
        // were sealed to object/byte[] respectively -- see issue #3225.
        const int Baseline = 0;

        var interfacePattern = new Regex(@"\binterface\s+\w+");
        var classPattern = new Regex(@"\bclass\s+\w+");
        var usingPattern = new Regex(@"^\s*using\s+System\.Windows(\.\w+)*\s*;", RegexOptions.Multiline);

        int count = SourceFiles().Count(f =>
        {
            string text = File.ReadAllText(f);
            return interfacePattern.IsMatch(text) && !classPattern.IsMatch(text) && usingPattern.IsMatch(text);
        });

        count.ShouldBeLessThanOrEqualTo(Baseline);
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Gum")) && File.Exists(Path.Combine(current, "GumFull.sln")))
            {
                return current;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate repo root from " + AppContext.BaseDirectory);
    }

    private static string NormalizePath(string path) => path.Replace('\\', '/');

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(ToolSourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !NormalizePath(f).Contains("/obj/") && !NormalizePath(f).Contains("/bin/"));
}
