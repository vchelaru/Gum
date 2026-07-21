using System.Collections.Generic;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Headless stand-in for a WpfDataUi <c>MemberCategory</c> made up of <see cref="SyntheticVariableRow"/>
/// rows rather than real <see cref="VariableGridEntry"/> rows (see that class's remarks). A WPF-side
/// mapper materializes each into a real <c>MemberCategory</c>.
/// </summary>
public class SyntheticCategoryDescriptor
{
    public string Name { get; }

    public List<SyntheticVariableRow> Members { get; } = new();

    public SyntheticCategoryDescriptor(string name)
    {
        Name = name;
    }
}
