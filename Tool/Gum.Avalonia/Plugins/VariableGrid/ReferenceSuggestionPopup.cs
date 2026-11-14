using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using AvaloniaDataUi.Controls;
using Gum.Avalonia.Themes;
using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.VariableGrid;

namespace Gum.Avalonia.Plugins.VariableGrid;

/// <summary>
/// The suggestion list under the caret of a VariableReferences editor. It never takes focus: the
/// editor applies its text when it loses focus, so keys are read from the editor and a click on an
/// entry does not move focus off it.
/// </summary>
public sealed class ReferenceSuggestionPopup
{
    private readonly StringListTextBoxDisplay _editor;
    private readonly IVariableReferenceSuggestions _suggestions;
    private readonly Popup _popup;
    private readonly ListBox _list;
    private Func<ElementSave?> _getOwner;
    private Func<string?> _getOwningInstanceName;
    private VariableReferenceSuggestionResult _current;
    private int _lineStartIndex;

    /// <summary>Attaches suggestions to <paramref name="editor"/>.</summary>
    public ReferenceSuggestionPopup(StringListTextBoxDisplay editor, IVariableReferenceSuggestions suggestions,
        Func<ElementSave?> getOwner, Func<string?> getOwningInstanceName)
    {
        _editor = editor;
        _suggestions = suggestions;
        _getOwner = getOwner;
        _getOwningInstanceName = getOwningInstanceName;
        _current = VariableReferenceSuggestionResult.None;

        _list = new ListBox { Focusable = false, MinWidth = 180, MaxHeight = 180 };
        _list.ItemTemplate = new global::Avalonia.Controls.Templates.FuncDataTemplate<VariableReferenceSuggestion>(
            (item, _) => new TextBlock { Text = item.Text, Focusable = false });
        _list.AddHandler(InputElement.PointerPressedEvent, HandleListPointerPressed, RoutingStrategies.Tunnel);

        Border frame = new Border
        {
            Child = _list,
            BorderThickness = new Thickness(1),
            Focusable = false,
        };
        frame.WithThemeResource(Border.BackgroundProperty, "Frb.Surface01");
        frame.WithThemeResource(Border.BorderBrushProperty, "Frb.Brushes.Border");

        _popup = new Popup
        {
            Child = frame,
            PlacementTarget = editor.EditorTextBox,
            Placement = PlacementMode.AnchorAndGravity,
            PlacementAnchor = PopupAnchor.BottomLeft,
            PlacementGravity = PopupGravity.BottomRight,
            IsLightDismissEnabled = false,
            Focusable = false,
        };
        if (editor.Content is Panel panel)
        {
            panel.Children.Add(_popup);
        }

        editor.EditorTextBox.TextChanged += (_, _) => Refresh();
        editor.EditorTextBox.LostFocus += (_, _) => Close();
        // Tunnel on the editor, an ancestor of its text box, so the popup sees Escape and Enter before the
        // editor's own revert and apply handling does.
        editor.AddHandler(InputElement.KeyDownEvent, HandleKeyDown, RoutingStrategies.Tunnel);
        editor.EditorTextBox.AddHandler(InputElement.KeyUpEvent, (_, e) =>
        {
            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Home || e.Key == Key.End)
            {
                Refresh();
            }
        }, RoutingStrategies.Bubble);
    }

    /// <summary>Re-points the popup at another row of the same editor.</summary>
    public void SetOwner(Func<ElementSave?> getOwner, Func<string?> getOwningInstanceName)
    {
        _getOwner = getOwner;
        _getOwningInstanceName = getOwningInstanceName;
        Close();
    }

    /// <summary>Whether the list is showing.</summary>
    public bool IsOpen => _popup.IsOpen;

    /// <summary>The names shown, in order.</summary>
    public IReadOnlyList<VariableReferenceSuggestion> Items => _current.Items;

    /// <summary>The highlighted entry, or null when the list is closed.</summary>
    public VariableReferenceSuggestion? Selected => IsOpen && _list.SelectedIndex >= 0 ? _current.Items[_list.SelectedIndex] : null;

    /// <summary>The list control, for tests and screenshots.</summary>
    public ListBox List => _list;


    private void Refresh()
    {
        TextBox box = _editor.EditorTextBox;
        ElementSave? owner = _getOwner();
        if (owner == null || !box.IsFocused)
        {
            Close();
            return;
        }

        string text = box.Text ?? string.Empty;
        int caret = Math.Clamp(box.CaretIndex, 0, text.Length);
        _lineStartIndex = caret == 0 ? 0 : text.LastIndexOf('\n', caret - 1) + 1;
        int lineEnd = text.IndexOf('\n', caret);
        string line = text.Substring(_lineStartIndex, (lineEnd < 0 ? text.Length : lineEnd) - _lineStartIndex).TrimEnd('\r');

        _current = _suggestions.GetSuggestions(owner, _getOwningInstanceName(), line, caret - _lineStartIndex);
        if (_current.Items.Count == 0)
        {
            Close();
            return;
        }

        _list.ItemsSource = _current.Items.ToList();
        _list.SelectedIndex = 0;
        _popup.PlacementRect = GetCaretRect(box);
        _popup.IsOpen = true;
    }

    private static Rect GetCaretRect(TextBox box)
    {
        TextPresenter? presenter = box.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();
        if (presenter == null)
        {
            return new Rect(0, 0, 1, box.Bounds.Height);
        }
        Rect inPresenter = presenter.TextLayout.HitTestTextPosition(box.CaretIndex);
        Point topLeft = presenter.TranslatePoint(inPresenter.TopLeft, box) ?? inPresenter.TopLeft;
        return new Rect(topLeft, inPresenter.Size);
    }

    private void Close()
    {
        _popup.IsOpen = false;
        _current = VariableReferenceSuggestionResult.None;
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsOpen)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Down:
                _list.SelectedIndex = Math.Min(_list.SelectedIndex + 1, _current.Items.Count - 1);
                _list.ScrollIntoView(_list.SelectedIndex);
                e.Handled = true;
                break;
            case Key.Up:
                _list.SelectedIndex = Math.Max(_list.SelectedIndex - 1, 0);
                _list.ScrollIntoView(_list.SelectedIndex);
                e.Handled = true;
                break;
            case Key.Tab:
                Accept();
                e.Handled = true;
                break;
            case Key.Enter when e.KeyModifiers == KeyModifiers.None:
                Accept();
                e.Handled = true;
                break;
            case Key.Escape:
                Close();
                e.Handled = true;
                break;
        }
    }

    private void HandleListPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Visual source && source.FindAncestorOfType<ListBoxItem>(true) is ListBoxItem item)
        {
            int index = _list.IndexFromContainer(item);
            if (index >= 0)
            {
                _list.SelectedIndex = index;
                Accept();
            }
        }
        // A press must not move focus off the editor.
        e.Handled = true;
    }

    private void Accept()
    {
        if (_list.SelectedIndex < 0 || _list.SelectedIndex >= _current.Items.Count)
        {
            return;
        }

        TextBox box = _editor.EditorTextBox;
        string text = box.Text ?? string.Empty;
        string insert = _current.Items[_list.SelectedIndex].InsertText;
        int start = _lineStartIndex + _current.ReplaceStart;
        int length = _current.ReplaceLength;
        Close();
        // TextChanged reopens the list for a name that continues with a dot.
        box.Text = text.Substring(0, start) + insert + text.Substring(start + length);
        box.CaretIndex = start + insert.Length;
    }
}
