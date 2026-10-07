using Gum.ProjectServices.CodeGeneration;

namespace Gum.Messages;

/// <summary>
/// Sent when a Code tab edit changed a setting that changes what custom code files must declare
/// (root namespace, append folder to namespace, inheritance location), so their namespace and class
/// header can be rewritten to match (#5855). Never sent for a settings file reloaded from disk.
/// </summary>
public class CustomCodeHeadersChangedMessage
{
    /// <summary>The settings after the edit.</summary>
    public CodeOutputProjectSettings Current { get; }

    /// <summary>What changed, e.g. "Root Namespace from Game to MyGame".</summary>
    public string Description { get; }

    public CustomCodeHeadersChangedMessage(CodeOutputProjectSettings current, string description)
    {
        Current = current;
        Description = description;
    }
}
