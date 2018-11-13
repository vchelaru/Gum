using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.SaveClasses;
using Gum.Managers;
using ToolsUtilities;
using GlueElement = FlatRedBall.Glue.SaveClasses.IElement;

namespace GluePlugin.Logic
{
    public class CodeCreationLogic : Singleton<CodeCreationLogic>
    {
        #region Fields
        string entityFileContentsToFormat =
@"
namespace {0}
{{
	public partial class {1}
	{{
        /// <summary>
        /// Initialization logic which is execute only one time for this Entity (unless the Entity is pooled).
        /// This method is called when the Entity is added to managers. Entities which are instantiated but not
        /// added to managers will not have this method called.
        /// </summary>
        private void CustomInitialize()
        {{


        }}

        private void CustomActivity()
        {{


        }}

        private void CustomDestroy()
        {{


        }}

        private static void CustomLoadStaticContent(string contentManagerName)
        {{


        }}
    }}
}}
";


        string screenFileContentsToFormat =
@"
namespace {0}
{{
	public partial class {1}
	{{
        private void CustomInitialize()
        {{


        }}

        private void CustomActivity(bool firstTimeCalled)
        {{


        }}

        private void CustomDestroy()
        {{


        }}

        private static void CustomLoadStaticContent(string contentManagerName)
        {{


        }}
    }}
}}
";

        #endregion


        public void TrySaveCustomCodeFileFor(GlueElement glueElement)
        {
            var glueProject = GluePluginState.Self.GlueProject;

            ///////////////////////Early Out///////////////////////
            if (glueProject == null || GluePluginState.Self.InitializationState != InitializationState.Initialized)
            {
                return;
            }
            ////////////////////End Early Out/////////////////////

            var location = GetCustomCodeFileLocationFor(glueElement);
            // if it already exists, don't overwrite it:
            if(location.Exists() == false)
            {
                var fileContents = GetFileContentsFor(glueElement);

                System.IO.File.WriteAllText(location.StandardizedCaseSensitive, fileContents);
            }

            // todo - do we have to add the file to the .csproj or will
            // Glue handle that for us?
        }

        private string GetFileContentsFor(GlueElement glueElement)
        {

            var glueProject = GluePluginState.Self.GlueProject;
            var projectFileName = GluePluginState.Self.GlueProjectFilePath;

            string screensOrEntities = glueElement is ScreenSave ? "Screens" : "Entities";

            // Assume it's the same as the project name. Glue
            // uses the namespace in the .csproj which is more
            // accurate, but we don't (currently) have the csproj
            // loaded.
            // Does not support subfolders (yet)
            string entityNamespace = $"{GluePluginState.Self.ProjectRootNamespace}.{screensOrEntities}";

            string className = new FilePath(glueElement.Name).CaseSensitiveNoPathNoExtension;

            string fileContents = null;
            if(glueElement is FlatRedBall.Glue.SaveClasses.ScreenSave)
            {
                fileContents = string.Format(screenFileContentsToFormat, entityNamespace, className);
            }
            else
            {
                fileContents = string.Format(entityFileContentsToFormat, entityNamespace, className);
            }

            return fileContents;
        }

        public FilePath GetCustomCodeFileLocationFor(GlueElement glueElement, bool absolute = true)
        {
            var projectFileName = GluePluginState.Self.GlueProjectFilePath;

            string screensOrEntities = glueElement is ScreenSave ? "Screens" : "Entities";
            string className = className = new FilePath(glueElement.Name).CaseSensitiveNoPathNoExtension;

            string customCodeLocation = null;

            customCodeLocation = $"{screensOrEntities}\\{className}.cs";

            if(absolute)
            {
                customCodeLocation = $"{projectFileName.GetDirectoryContainingThis()}{customCodeLocation}";
            }

            return customCodeLocation;
        }
    }
}
