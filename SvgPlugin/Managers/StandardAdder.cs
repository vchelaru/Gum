using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.Services;
using ToolsUtilities;

namespace SkiaPlugin.Managers
{
    static class StandardAdder
    {
        private static readonly IFileCommands _fileCommands = Locator.GetRequiredService<IFileCommands>();
        public static void AddAllStandards()
        {
            AddStandard("Arc", DefaultStateManager.GetArcState());
            AddStandard("Canvas", DefaultStateManager.GetCanvasState());
            AddStandard("ColoredCircle", DefaultStateManager.GetColoredCircleState());
            AddStandard("LottieAnimation", DefaultStateManager.GetLottieAnimationState());
            AddStandard("RoundedRectangle", DefaultStateManager.GetRoundedRectangleState());
            AddStandard("Svg", DefaultStateManager.GetSvgState());
        }


        private static StandardElementSave AddStandard(string standardName, StateSave defaultState)
        {
            StandardElementSave toReturn = null;

            var targetFile = ProjectState.Self.ProjectDirectory + $"Standards/{standardName}.gutx";
            FileManager.SaveEmbeddedResource(
                typeof(StandardAdder).Assembly,
                $"SkiaPlugin.Embedded.{standardName}.gutx",
                targetFile);

            var gumProject = ProjectState.Self.GumProjectSave;
            var hasStandard = gumProject.StandardElementReferences.Any(item => item.Name == standardName);
            if (!hasStandard)
            {
                var newReference = new ElementReference();
                newReference.ElementType = ElementType.Standard;
                newReference.Name = standardName;
                gumProject.StandardElementReferences.Add(newReference);

                if (gumProject.StandardElements.Any(item => item.Name == standardName) == false)
                {
                    GumLoadResult result = new GumLoadResult();
                    var loaded = newReference.ToElementSave<StandardElementSave>(
                        ProjectState.Self.ProjectDirectory,
                        "gutx",
                        result);

                    // load it:
                    gumProject.StandardElements.Add(loaded);
                    loaded.Initialize(defaultState);
                    StandardElementsManagerGumTool.Self.FixCustomTypeConverters(loaded);
                    _fileCommands.TryAutoSaveElement(loaded);
                    toReturn = loaded;
                }
                _fileCommands.TryAutoSaveProject();
            }

            return toReturn;
        }
    }
}
