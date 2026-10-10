using System.Collections.Generic;
using Gum.DataTypes;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Reports a component whose base type cannot become a Forms class: it inherits a standard other than
/// Container (Sprite, NineSlice, Text, ...) and has no Forms behavior. Without this the generated file
/// declares an error sentence as the base class and only fails later, in the game project's compile.
/// Codegen-specific (it depends on the project's output library), so it is passed to the error checker
/// by <c>gumcli codegen</c> and does not run in <c>gumcli check</c>.
/// </summary>
public class FormsCodegenBaseTypeErrorSource : IAdditionalErrorSource
{
    private readonly CodeOutputProjectSettings _projectSettings;

    public FormsCodegenBaseTypeErrorSource(CodeOutputProjectSettings projectSettings)
    {
        _projectSettings = projectSettings;
    }

    /// <inheritdoc/>
    public IEnumerable<ErrorResult> GetErrors(ElementSave element, GumProjectSave project)
    {
        string? message = CodeGenerator.GetUnsupportedFormsBaseTypeError(element, _projectSettings);
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
