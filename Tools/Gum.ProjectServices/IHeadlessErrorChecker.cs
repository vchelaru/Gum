using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.ProjectServices;

/// <summary>
/// Checks a Gum project for errors without requiring the Gum tool UI.
/// </summary>
public interface IHeadlessErrorChecker
{
    /// <summary>
    /// Returns all errors for a single element within the given project.
    /// </summary>
    IReadOnlyList<ErrorResult> GetErrorsFor(ElementSave element, GumProjectSave project);

    /// <summary>
    /// Returns all errors across all elements in the given project.
    /// </summary>
    IReadOnlyList<ErrorResult> GetAllErrors(GumProjectSave project);

    /// <summary>
    /// Returns the errors that belong to the project rather than to an element: the part of
    /// <see cref="GetAllErrors"/> that no element's <see cref="GetErrorsFor"/> reports. Walks the
    /// whole project, so a caller should not run it on every selection change.
    /// </summary>
    IReadOnlyList<ErrorResult> GetProjectErrors(GumProjectSave project);
}
