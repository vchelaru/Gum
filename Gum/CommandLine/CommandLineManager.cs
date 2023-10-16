using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Linq;
using ToolsUtilities;

namespace Gum.CommandLine
{
    public class CommandLineManager : Singleton<CommandLineManager>
    {
        #region Fields/Properties

        public string GlueProjectToLoad
        {
            get;
            private set;
        }

        public bool ShouldExitImmediately { get; set; }

        public string ElementName
        {
            get;
            private set;
        }

        #endregion

        public void ReadCommandLine()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            GumCommands.Self.GuiCommands.PrintOutput(commandLineArgs.Length + " command line argument(s)...");

            for(int i = 0; i < commandLineArgs.Length; i++)
            {
                var arg = commandLineArgs[i];

                GumCommands.Self.GuiCommands.PrintOutput(arg);

                if (!string.IsNullOrEmpty(arg))
                {
                    if(arg?.ToLowerInvariant() == "--rebuildfonts")
                    {
                        HandleRebuildFontCommand(commandLineArgs, i);
                        ShouldExitImmediately = true;
                        break;
                    }
                    else
                    {
                        string argExtension = FileManager.GetExtension(arg);

                        if (argExtension == "gumx")
                        {
                            GlueProjectToLoad = arg;
                        }
                        else if(argExtension == GumProjectSave.ComponentExtension ||
                            argExtension == GumProjectSave.ScreenExtension ||
                            argExtension == GumProjectSave.StandardExtension)
                        {
                            ElementName = FileManager.RemovePath(FileManager.RemoveExtension(arg));

                            string gluxDirectory = FileManager.GetDirectory( FileManager.GetDirectory(arg));

                            GlueProjectToLoad = System.IO.Directory.GetFiles(gluxDirectory)
                                .FirstOrDefault(item => item.ToLowerInvariant().EndsWith(".gumx"));
                        }

                    }
                }


            }
        }

        private void HandleRebuildFontCommand(string[] commandLineArgs, int index)
        {
            // param 1 should be the .gumx

            var gumxFile = commandLineArgs[index + 1];

            // we want to:
            // 1. Load the file
            // 2. Initialize all references
            // 3. Tell the fonts to rebuild
            // 4. Exit

            // 1. and 2.
            GumCommands.Self.FileCommands.LoadProject(gumxFile);

            // 3.
            FontManager.Self.CreateAllMissingFontFiles(ProjectManager.Self.GumProjectSave);

            // 4.
            GumCommands.Self.FileCommands.Exit();
        }
    }
}
