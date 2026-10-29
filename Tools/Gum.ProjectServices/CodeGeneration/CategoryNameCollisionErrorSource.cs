using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Reports an element whose category names clash with the names codegen gives its enums, state properties and
/// class (<see cref="CodeGenerator.GetCategoryNameCollisionError"/>). Whether a name clashes depends on the
/// project's output library, so it is passed to the error checker by <c>gumcli codegen</c> and does not run in
/// <c>gumcli check</c>.
/// </summary>
public class CategoryNameCollisionErrorSource : IAdditionalErrorSource
{
    private readonly CodeGenerator _codeGenerator;
    private readonly CodeOutputProjectSettings _projectSettings;

    public CategoryNameCollisionErrorSource(CodeGenerator codeGenerator, CodeOutputProjectSettings projectSettings)
    {
        _codeGenerator = codeGenerator;
        _projectSettings = projectSettings;
    }

    /// <inheritdoc/>
    public IEnumerable<ErrorResult> GetErrors(ElementSave element, GumProjectSave project)
    {
        string? message = _codeGenerator.GetCategoryNameCollisionError(element, _projectSettings);
        if (message != null)
        {
            yield return new ErrorResult
            {
                ElementName = element.Name,
                Message = message,
                Severity = ErrorSeverity.Error
            };
        }
    }
}
