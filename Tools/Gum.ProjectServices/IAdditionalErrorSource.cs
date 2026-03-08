using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.ProjectServices;

/// <summary>
/// Provides additional error checking beyond the built-in checks.
/// Implement this interface to contribute plugin-specific errors
/// to the headless error checker.
/// </summary>
public interface IAdditionalErrorSource
{
    IEnumerable<ErrorResult> GetErrors(ElementSave element, GumProjectSave project);
}
