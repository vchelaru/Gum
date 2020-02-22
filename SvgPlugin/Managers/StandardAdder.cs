using Gum;
using Gum.DataTypes;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace SkiaPlugin.Managers
{
    static class StandardAdder
    {
        public static void AddAllStandards()
        {
            AddStandard("Svg");
            AddStandard("ColoredCircle");
            AddStandard("RoundedRectangle");
        }

        private static void AddStandard(string standardName)
        {
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
                    loaded.Initialize(DefaultStateManager.GetSvgState());
                }
                GumCommands.Self.FileCommands.TryAutoSaveProject();
            }
        }
    }
}
