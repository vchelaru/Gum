using System;
using System.Collections.Generic;
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

    /// <summary>
    /// The full Skia shape bundle in add order. <see cref="IsLegacyShape"/> marks the two shapes
    /// (ColoredCircle, RoundedRectangle) that were superseded by the v3
    /// (<see cref="GumProjectSave.GumxVersions.ShapeVariableExpansion"/>) Circle / Rectangle
    /// variable surface and so are no longer added on V3+ projects.
    /// </summary>
    private static readonly (string Name, Func<StateSave> GetState, bool IsLegacyShape)[] AllStandards =
    {
        ("Arc", StandardElementsManager.GetArcState, false),
        ("Canvas", StandardElementsManager.GetCanvasState, false),
        ("ColoredCircle", StandardElementsManager.GetColoredCircleState, true),
        ("Line", StandardElementsManager.GetLineState, false),
        ("LottieAnimation", StandardElementsManager.GetLottieAnimationState, false),
        ("RoundedRectangle", StandardElementsManager.GetRoundedRectangleState, true),
        ("Svg", StandardElementsManager.GetSvgState, false),
    };

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

    /// <summary>
    /// The Skia standard element names to add for a project at <paramref name="projectVersion"/>.
    /// On V3 (<see cref="GumProjectSave.GumxVersions.ShapeVariableExpansion"/>) or later the legacy
    /// ColoredCircle / RoundedRectangle are omitted — the plain Circle / Rectangle absorbed their
    /// fill / gradient / dropshadow / corner-radius surface, so adding them would only clutter the
    /// project with superseded shapes. Pure decision logic (no project state) so it can be unit tested.
    /// </summary>
    internal static IReadOnlyList<string> GetStandardNamesToAdd(int projectVersion)
    {
        bool isV3OrLater = projectVersion >= (int)GumProjectSave.GumxVersions.ShapeVariableExpansion;
        return AllStandards
            .Where(standard => !isV3OrLater || !standard.IsLegacyShape)
            .Select(standard => standard.Name)
            .ToList();
    }

    public void AddAllStandards()
    {
        int projectVersion = _projectState.GumProjectSave?.Version ?? 0;
        HashSet<string> namesToAdd = GetStandardNamesToAdd(projectVersion).ToHashSet();

        foreach ((string name, Func<StateSave> getState, _) in AllStandards)
        {
            if (namesToAdd.Contains(name))
            {
                AddStandard(name, getState());
            }
        }
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
