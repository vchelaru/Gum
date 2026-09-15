using Avalonia.Input;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Controls;
using Gum.Avalonia.Services;
using Gum.Input;
using Gum.Managers;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The window boundary maps the platform's command modifier (Cmd on macOS, Ctrl elsewhere) to
/// Gum's neutral "Ctrl" flag, so the Ctrl-based hotkey defaults fire from Cmd on a Mac.
/// </summary>
public class AvaloniaKeyMappingTests
{
    [Fact]
    public void ToGumKeyEventArgs_MetaIsTheCommandModifier_MapsMetaToCtrl()
    {
        KeyEventArgs meta = new KeyEventArgs { Key = Key.Z, KeyModifiers = KeyModifiers.Meta };
        KeyEventArgs control = new KeyEventArgs { Key = Key.Z, KeyModifiers = KeyModifiers.Control };

        GumKeyEventArgs fromMeta = meta.ToGumKeyEventArgs(KeyModifiers.Meta);
        GumKeyEventArgs fromControl = control.ToGumKeyEventArgs(KeyModifiers.Meta);

        fromMeta.Key.ShouldBe(GumKey.Z);
        fromMeta.IsCtrlDown.ShouldBeTrue();
        fromControl.IsCtrlDown.ShouldBeFalse();
    }

    [Fact]
    public void ToGumKeyEventArgs_ControlIsTheCommandModifier_MapsControlToCtrl()
    {
        KeyEventArgs meta = new KeyEventArgs { Key = Key.Z, KeyModifiers = KeyModifiers.Meta };
        KeyEventArgs control = new KeyEventArgs { Key = Key.Z, KeyModifiers = KeyModifiers.Control | KeyModifiers.Shift };

        GumKeyEventArgs fromMeta = meta.ToGumKeyEventArgs(KeyModifiers.Control);
        GumKeyEventArgs fromControl = control.ToGumKeyEventArgs(KeyModifiers.Control);

        fromMeta.IsCtrlDown.ShouldBeFalse();
        fromControl.IsCtrlDown.ShouldBeTrue();
        fromControl.IsShiftDown.ShouldBeTrue();
    }

    [Fact]
    public void ToKeyGesture_BuildsTheGestureWithThePlatformCommandModifier()
    {
        KeyCombination combo = new KeyCombination { Key = GumKey.Z, IsCtrlDown = true, IsShiftDown = true };

        KeyGesture? mac = combo.ToKeyGesture(KeyModifiers.Meta);
        KeyGesture? windows = combo.ToKeyGesture(KeyModifiers.Control);

        mac.ShouldNotBeNull();
        mac.Key.ShouldBe(Key.Z);
        mac.KeyModifiers.ShouldBe(KeyModifiers.Meta | KeyModifiers.Shift);
        windows.ShouldNotBeNull();
        windows.KeyModifiers.ShouldBe(KeyModifiers.Control | KeyModifiers.Shift);
    }

    [Fact]
    public void ToKeyGesture_ModifierOnlyCombination_HasNoGesture()
    {
        KeyCombination.Shift().ToKeyGesture(KeyModifiers.Control).ShouldBeNull();
    }

    [Fact]
    public void ModifierKeyState_ReportsTheCommandModifierAsCtrl()
    {
        AvaloniaModifierKeyState state = new AvaloniaModifierKeyState(KeyModifiers.Meta);

        state.Current = KeyModifiers.Meta;
        state.IsCtrlDown.ShouldBeTrue();

        state.Current = KeyModifiers.Control;
        state.IsCtrlDown.ShouldBeFalse();
    }

    [Fact]
    public void ToTreeModifiers_CommandModifierIsControl_AndNotAlsoWindows()
    {
        // TreeViewPlugin.Core compares against TreeModifierKeys.Control exactly, so Cmd on macOS
        // must come through as Control alone.
        AvaloniaGumTreeView.ToTreeModifiers(KeyModifiers.Meta, KeyModifiers.Meta).ShouldBe(TreeModifierKeys.Control);
        AvaloniaGumTreeView.ToTreeModifiers(KeyModifiers.Control, KeyModifiers.Meta).ShouldBe(TreeModifierKeys.None);
        AvaloniaGumTreeView.ToTreeModifiers(KeyModifiers.Meta, KeyModifiers.Control).ShouldBe(TreeModifierKeys.Windows);
        AvaloniaGumTreeView.ToTreeModifiers(KeyModifiers.Control | KeyModifiers.Shift, KeyModifiers.Control)
            .ShouldBe(TreeModifierKeys.Control | TreeModifierKeys.Shift);
    }
}
