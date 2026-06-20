using Shouldly;
using Xunit;

namespace GumToolUnitTests.Coupling;

/// <summary>
/// Pins the deterministic counting rules of <see cref="CouplingScanner"/> against small
/// inline source samples, so the rules are proven without depending on the live tree.
/// These are the executable specification of each Phase 0 coupling metric.
/// </summary>
public class CouplingScannerTests
{
    private readonly CouplingScanner _scanner;

    public CouplingScannerTests()
    {
        _scanner = new CouplingScanner();
    }

    [Fact]
    public void CountInlineDialogSites_CountsMessageBoxShowAndNamedWindowConstructions()
    {
        string source = @"
class Foo
{
    void Bar()
    {
        MessageBox.Show(""hi"");
        var w = new DeleteOptionsWindow(arg);
        var f = new FileListWindow();
        var p = new Window();
    }
}";

        // 1 MessageBox.Show + 2 named *Window( + 1 bare Window( = 4
        _scanner.CountInlineDialogSites(source).ShouldBe(4);
    }

    [Fact]
    public void CountInlineDialogSites_DoesNotMatchUnrelatedWindowIdentifiers()
    {
        string source = @"
class Foo
{
    void Bar()
    {
        var helper = new WindowHelper();
        ActiveWindow.Focus();
        ShowWindow();
    }
}";

        _scanner.CountInlineDialogSites(source).ShouldBe(0);
    }

    [Fact]
    public void CountInterfaceTypeLeaks_CountsQualifiedAndUnqualifiedTypesInsideInterfaceBodies()
    {
        string source = @"
using System.Windows;
using System.Windows.Forms;

public interface ITabManager
{
    void AddControl(System.Windows.Forms.Control control, string title);
    void AddElement(FrameworkElement element);
    void RemoveTab(PluginTab plugin);
}";

        // System.Windows.Forms.Control (qualified) + FrameworkElement (unqualified) = 2
        _scanner.CountInterfaceTypeLeaks(source).ShouldBe(2);
    }

    [Fact]
    public void CountInterfaceTypeLeaks_CountsTwoQualifiedTypesOnASingleMember()
    {
        string source = @"
public interface IHotkeyManager
{
    bool ProcessCmdKey(ref System.Windows.Forms.Message msg, System.Windows.Forms.Keys keyData);
}";

        // Two distinct qualified System.Windows.Forms.* references on one member = 2
        _scanner.CountInterfaceTypeLeaks(source).ShouldBe(2);
    }

    [Fact]
    public void CountInterfaceTypeLeaks_DoesNotCountWindowsTypesOutsideInterfaceBodies()
    {
        // The using directive and the class member both reference System.Windows.Forms,
        // but neither is inside an interface body, so the leak count is 0. This mirrors
        // ISelectedState.cs, which has a stale `using System.Windows.Forms;` but a clean body.
        string source = @"
using System.Windows.Forms;

public interface ICleanInterface
{
    string Name { get; }
}

public class SomeClass
{
    public Control MyControl;
    public System.Windows.Forms.Keys Pressed;
}";

        _scanner.CountInterfaceTypeLeaks(source).ShouldBe(0);
    }

    [Fact]
    public void CountInterfaceTypeLeaks_HandlesDefaultMemberBodiesWithNestedBraces()
    {
        // Default interface members have bodies with nested braces; the body brace-matcher
        // must still find the interface's closing brace and count the leak inside it.
        string source = @"
public interface IWithDefault
{
    string Name { get; }
    public string Describe()
    {
        if (Name != null) { return Name; }
        return string.Empty;
    }
    void Handle(System.Windows.Forms.KeyEventArgs e);
}";

        _scanner.CountInterfaceTypeLeaks(source).ShouldBe(1);
    }

    [Fact]
    public void CountInterfaceTypeLeaks_UnderCountsLeakAfterDefaultBodyWithBraceBearingInterpolatedString()
    {
        // KNOWN LIMITATION pinned here (this is documented current behavior, NOT desired behavior):
        // ExtractInterfaceBodies treats an interpolated string as opaque "..." segments. The nested
        // string literal "a}" carries a '}' that the matcher mistakes for code, prematurely closing
        // the interface body. The System.Windows.Forms leak declared AFTER this default-method body
        // is therefore outside the captured body and is MISSED -- the scanner reports 0 where a
        // perfect parser would report 1 (under-count, the dangerous direction).
        // If anyone later hardens the parser, this assertion flips and flags the behavior change.
        string source = @"
public interface IPathological
{
    public string Describe()
    {
        var label = $""{(Name != null ? ""a}"" : ""b"")}"";
        return label;
    }
    void Handle(System.Windows.Forms.KeyEventArgs e);
}";

        _scanner.CountInterfaceTypeLeaks(source).ShouldBe(0);
    }

    [Fact]
    public void CountSelfReferences_CountsTotalAndObjectFinderSubcount()
    {
        string source = @"
class Foo
{
    void Bar()
    {
        ObjectFinder.Self.GetElement();
        PluginManager.Self.Do();
        var x = Gum.ObjectFinder.Self.GumProjectSave;
        StandardElementsManager.Self.Initialize();
    }
}";

        SelfReferenceCount count = _scanner.CountSelfReferences(source);

        count.Total.ShouldBe(4);
        count.ObjectFinder.ShouldBe(2);
        count.ExcludingObjectFinder.ShouldBe(2);
    }

    [Fact]
    public void CountSelfReferences_DoesNotMatchSelfFollowedByMoreLetters()
    {
        string source = @"
class Foo
{
    SelfManager _manager;
    void Bar()
    {
        _manager.SelfManager.Do();
        this.SelfDescribing = true;
    }
}";

        SelfReferenceCount count = _scanner.CountSelfReferences(source);

        count.Total.ShouldBe(0);
        count.ObjectFinder.ShouldBe(0);
    }

    [Fact]
    public void CountWindowsCouplingInViewModel_CountsUsingDirectivesAndAllowListTypes()
    {
        string source = @"
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;

public class FooViewModel
{
    public Visibility ButtonVisibility { get; set; }
    public SolidColorBrush Accent { get; set; }
    private Brush _brush;
}";

        // 2 `using System.Windows*` lines + Visibility(1) + SolidColorBrush(1) + Brush(1) = 5.
        // Note: the property NAME `ButtonVisibility` must NOT add a count (whole-word only).
        _scanner.CountWindowsCouplingInViewModel(source).ShouldBe(5);
    }

    [Fact]
    public void CountWindowsCouplingInViewModel_DoesNotCountAmbiguousNonWpfTypes()
    {
        string source = @"
using System.Collections.Generic;

public class FooViewModel
{
    public Color? AccentColor { get; set; }
    public Point Location { get; set; }
    public Keys Pressed { get; set; }
}";

        // Color / Point / Keys are deliberately excluded (ambiguous with XNA/Gum/WinForms types).
        _scanner.CountWindowsCouplingInViewModel(source).ShouldBe(0);
    }

    [Fact]
    public void IsDialogInfrastructure_IsTrueOnlyForServicesDialogsFolder()
    {
        _scanner.IsDialogInfrastructure(@"Services\Dialogs\DialogService.cs").ShouldBeTrue();
        _scanner.IsDialogInfrastructure("Services/Dialogs/MessageDialogViewModel.cs").ShouldBeTrue();
        _scanner.IsDialogInfrastructure("Managers/DeleteLogic.cs").ShouldBeFalse();
    }

    [Fact]
    public void IsViewModelFile_MatchesPlainAndPartialViewModelFiles()
    {
        _scanner.IsViewModelFile("AddFormsViewModel.cs").ShouldBeTrue();
        _scanner.IsViewModelFile("AddFormsViewModel.Designer.cs").ShouldBeTrue();
        _scanner.IsViewModelFile("ViewModel.cs").ShouldBeTrue();
    }

    [Fact]
    public void IsViewModelFile_RejectsNonViewModelFiles()
    {
        _scanner.IsViewModelFile("ViewModelHelper.cs").ShouldBeFalse();
        _scanner.IsViewModelFile("MyViewModelThing.cs").ShouldBeFalse();
        _scanner.IsViewModelFile("DeleteLogic.cs").ShouldBeFalse();
    }
}
