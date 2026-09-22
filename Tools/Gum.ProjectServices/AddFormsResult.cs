using System.Collections.Generic;

namespace Gum.ProjectServices;

/// <summary>
/// Result of <see cref="IAddFormsToProjectService.AddFormsTo"/>.
/// </summary>
public class AddFormsResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public List<string> AddedComponents { get; } = new();
    public List<string> AddedStandards { get; } = new();
    public List<string> AddedBehaviors { get; } = new();
}
