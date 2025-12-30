using Gum.DataTypes;
using Gum.Managers;
using Gum.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using ToolsUtilities;
using Gum.Messages;
using Gum.Extensions;
using Gum.Services.Fonts;

namespace Gum.CommandLine
{
    public class CommandLineManager : Singleton<CommandLineManager>
    {
        private readonly FontManager _fontManager;
        private readonly IGuiCommands _guiCommands;
        private readonly IFileCommands _fileCommands;
        private readonly IMessenger _messenger;
        
        #region Fields/Properties

        public string GlueProjectToLoad
        {
            get;
            private set;
        }

        public bool ShouldExitImmediately { get; set; }
        public bool ShouldCodeGenAll { get; private set; }

        public string ElementName
        {
            get;
            private set;
        }

        #endregion

        public CommandLineManager()
        {
            _fontManager = Locator.GetRequiredService<FontManager>();
            _guiCommands = Locator.GetRequiredService<IGuiCommands>();
            _fileCommands = Locator.GetRequiredService<IFileCommands>();
            _messenger = Locator.GetRequiredService<IMessenger>();
        }

        public async Task ReadCommandLine()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            _guiCommands.PrintOutput(commandLineArgs.Length + " command line argument(s)...");

            for(int i = 0; i < commandLineArgs.Length; i++)
            {
                var arg = commandLineArgs[i];

                _guiCommands.PrintOutput(arg);

                if (!string.IsNullOrEmpty(arg))
                {
                    if(arg?.ToLowerInvariant() == "--rebuildfonts")
                    {
                        await HandleRebuildFontCommand(commandLineArgs, i);
                        ShouldExitImmediately = true;
                    }
                    else if(arg?.ToLowerInvariant() == "--generatecode")
                    {
                        ShouldCodeGenAll = true;
                        ShouldExitImmediately = true;
                    }
                    else
                    {
                        string argExtension = FileManager.GetExtension(arg);

                        if (argExtension == "gumx")
                        {
                            GlueProjectToLoad = arg;
                        }
                        else if (argExtension == GumProjectSave.ComponentExtension ||
                            argExtension == GumProjectSave.ScreenExtension ||
                            argExtension == GumProjectSave.StandardExtension)
                        {
                            ElementName = FileManager.RemovePath(FileManager.RemoveExtension(arg));

                            string gluxDirectory = FileManager.GetDirectory(FileManager.GetDirectory(arg));

                            GlueProjectToLoad = System.IO.Directory.GetFiles(gluxDirectory)
                                .FirstOrDefault(item => item.ToLowerInvariant().EndsWith(".gumx"));
                        }

                    }
                }


            }
        }

        private async Task HandleRebuildFontCommand(string[] commandLineArgs, int index)
        {
            // param 1 should be the .gumx

            var gumxFile = commandLineArgs[index + 1];

            // we want to:
            // 1. Load the file
            // 2. Initialize all references
            // 3. Tell the fonts to rebuild
            // 4. Exit

            // 1. and 2.
            _fileCommands.LoadProject(gumxFile);

            // 3.
            await _fontManager.CreateAllMissingFontFiles(ProjectManager.Self.GumProjectSave);

            // 4.
            _messenger.Send<CloseMainWindowMessage>();
        }
    }
}
