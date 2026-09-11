using System;

namespace Gum.Plugins;

/// <summary>
/// A head's Code tab view as the shared Code Output plugin drives it: the settings and preview
/// (<see cref="ICodeOutputTabView"/>), the control the tab hosts, and the view's buttons.
/// </summary>
public interface ICodeOutputTabHost : ICodeOutputTabView
{
    /// <summary>The control added to the Code tab.</summary>
    object Control { get; }

    /// <summary>Raised when a project or element setting changed through the settings grid.</summary>
    event EventHandler? CodeOutputSettingsPropertyChanged;

    /// <summary>Raised by the Generate button.</summary>
    event EventHandler? GenerateCodeClicked;

    /// <summary>Raised by a Generate All button, in a head that shows one.</summary>
    event EventHandler? GenerateAllCodeClicked;
}
