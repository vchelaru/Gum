using System.Linq;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using ToolsUtilities;

namespace Gum.Logic;

/// <inheritdoc/>
public class SkiaShapeStandardsLogic : ISkiaShapeStandardsLogic
{
    private const string EmbeddedResourcePrefix = "Gum.Embedded.SkiaShapes.";

    private readonly IFileCommands _fileCommands;
    private readonly StandardElementsManagerGumTool _standardElementsManagerGumTool;
    private readonly IProjectState _projectState;

    public SkiaShapeStandardsLogic(
        IFileCommands fileCommands,
        StandardElementsManagerGumTool standardElementsManagerGumTool,
        IProjectState projectState)
    {
        _fileCommands = fileCommands;
        _standardElementsManagerGumTool = standardElementsManagerGumTool;
        _projectState = projectState;
    }

    public void AddAllStandards()
    {
        AddStandard("Arc", StandardElementsManager.GetArcState());
        AddStandard("Canvas", StandardElementsManager.GetCanvasState());
        AddStandard("ColoredCircle", StandardElementsManager.GetColoredCircleState());
        AddStandard("Line", StandardElementsManager.GetLineState());
        AddStandard("LottieAnimation", StandardElementsManager.GetLottieAnimationState());
        AddStandard("RoundedRectangle", StandardElementsManager.GetRoundedRectangleState());
        AddStandard("Svg", StandardElementsManager.GetSvgState());
    }

    private void AddStandard(string standardName, StateSave defaultState)
    {
        var targetFile = _projectState.ProjectDirectory + $"Standards/{standardName}.gutx";
        FileManager.SaveEmbeddedResource(
            typeof(SkiaShapeStandardsLogic).Assembly,
            EmbeddedResourcePrefix + standardName + ".gutx",
            targetFile);

        var gumProject = _projectState.GumProjectSave;
        var hasStandard = gumProject.StandardElementReferences.Any(item => item.Name == standardName);
        if (!hasStandard)
        {
            var newReference = new ElementReference
            {
                ElementType = ElementType.Standard,
                Name = standardName,
            };
            gumProject.StandardElementReferences.Add(newReference);

            if (gumProject.StandardElements.Any(item => item.Name == standardName) == false)
            {
                var result = new GumLoadResult();
                var loaded = newReference.ToElementSave<StandardElementSave>(
                    _projectState.ProjectDirectory,
                    "gutx",
                    result);

                gumProject.StandardElements.Add(loaded);
                loaded.Initialize(defaultState);
                _standardElementsManagerGumTool.FixCustomTypeConverters(loaded);
                _fileCommands.TryAutoSaveElement(loaded);
            }
            _fileCommands.TryAutoSaveProject();
        }
    }
}
