using System.Collections.Generic;

namespace Gum.Settings;

/// <summary>
/// Per-project user settings stored in .user.setj file next to .gumx
/// </summary>
public class UserProjectSettings
{
    public TreeViewState? TreeViewState { get; set; }
}

public class TreeViewState
{
    /// <summary>
    /// List of expanded node paths (e.g., "Components/Buttons", "Screens/MainMenu")
    /// </summary>
    public List<string> ExpandedNodes { get; set; } = new List<string>();
}
