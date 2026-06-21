namespace ImportFromGumxPlugin.ViewModels;

/// <summary>
/// One row of variable- or category-level diff detail rendered under a flagged Standard
/// in the import dialog (#2779). Passive display record — no behavior, no service.
/// </summary>
public class StandardDiffRowViewModel
{
    /// <summary>
    /// Short kind label shown to the left of the row, e.g. <c>"Changed"</c>,
    /// <c>"Added"</c>, <c>"Removed"</c>, <c>"Category added"</c>, <c>"Category removed"</c>.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// Human-readable summary of the diff, e.g.
    /// <c>"Rotation · SetsValue: True → False"</c> or <c>"ColorCategory"</c>.
    /// </summary>
    public string Summary { get; }

    public StandardDiffRowViewModel(string kind, string summary)
    {
        Kind = kind;
        Summary = summary;
    }
}
