using System;
using System.Collections.Generic;

namespace Gum.ViewModels;

public class ContextMenuItemViewModel
{
    public string Text { get; set; } = string.Empty;
    public Action? Action { get; set; }
    public List<ContextMenuItemViewModel> Children { get; set; } = new();
}
