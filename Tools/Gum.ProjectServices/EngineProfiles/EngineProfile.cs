using System.Collections.Generic;

namespace Gum.Managers;

/// <summary>
/// Whether a <see cref="VariableRule"/> shows or hides the variables its pattern matches.
/// </summary>
public enum VariableRuleAction
{
    Show,
    Hide
}

/// <summary>
/// One entry of <see cref="EngineProfile.VariableRules"/>. The pattern is a variable name
/// (<c>X</c>) or a standard type and variable name (<c>Sprite.X</c>); <c>*</c> matches any run of characters.
/// </summary>
public class VariableRule
{
    private string _pattern = string.Empty;

    public string Pattern
    {
        get => _pattern;
        set => _pattern = value ?? string.Empty;
    }

    public VariableRuleAction Action { get; set; }
}

/// <summary>
/// What a target engine supports, read from a <c>.gumengine</c> JSON file that projects reference through
/// <c>GumProjectSave.EngineProfile</c>. A default instance is unrestricted.
/// </summary>
public class EngineProfile
{
    private List<string> _supportedStandardTypes = new List<string>();
    private List<VariableRule> _variableRules = new List<VariableRule>();

    /// <summary>
    /// The standard element types the engine supports. Empty means every type. The tool offers
    /// only these when adding an instance; instances of other types already in a file still load.
    /// </summary>
    public List<string> SupportedStandardTypes
    {
        get => _supportedStandardTypes;
        set => _supportedStandardTypes = value ?? new List<string>();
    }

    /// <summary>
    /// When true, a standard variable no rule shows is hidden in the Variables tab. Hidden variables keep
    /// their stored values.
    /// </summary>
    public bool HideVariablesByDefault { get; set; }

    /// <summary>
    /// Ordered show/hide rules for standard variables; the last rule matching a variable wins,
    /// and <see cref="HideVariablesByDefault"/> decides when none matches.
    /// </summary>
    public List<VariableRule> VariableRules
    {
        get => _variableRules;
        set => _variableRules = value ?? new List<VariableRule>();
    }

    /// <summary>
    /// When true, the tool hides Components: the tree folder and every way to add one. Components already
    /// in a file still load.
    /// </summary>
    public bool DisableComponents { get; set; }

    /// <summary>
    /// When true, the tool hides Behaviors the same way <see cref="DisableComponents"/> hides Components.
    /// </summary>
    public bool DisableBehaviors { get; set; }
}
