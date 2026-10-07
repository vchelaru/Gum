using CodeOutputPlugin.Manager;
using Gum.DataTypes;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;

namespace OrphanCodeFilePlugin;

/// <summary>Regenerates one element's code files.</summary>
public interface IElementCodeRegenerator
{
    /// <summary>Regenerates <paramref name="elementName"/>'s code without prompting; does nothing if the project has no such element.</summary>
    void Regenerate(string elementName, CodeOutputProjectSettings projectSettings);
}

/// <inheritdoc cref="IElementCodeRegenerator"/>
public class ElementCodeRegenerator : IElementCodeRegenerator
{
    private readonly CodeGenerationService _codeGenerationService;
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;

    public ElementCodeRegenerator(CodeGenerationService codeGenerationService, CodeOutputElementSettingsManager elementSettingsManager)
    {
        _codeGenerationService = codeGenerationService;
        _elementSettingsManager = elementSettingsManager;
    }

    /// <inheritdoc/>
    public void Regenerate(string elementName, CodeOutputProjectSettings projectSettings)
    {
        ElementSave? element = ObjectFinder.Self.GetElementSave(elementName);
        if (element != null)
        {
            _codeGenerationService.GenerateCodeForElement(element, _elementSettingsManager.LoadOrCreateSettingsFor(element),
                projectSettings, showPopups: false);
        }
    }
}
