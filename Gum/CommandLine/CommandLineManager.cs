using Gum.DataTypes;
using Gum.Managers;
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
    /// <inheritdoc cref="ICommandLineManager"/>
    public class CommandLineManager : ICommandLineManager
    {
        private readonly IFontManager _fontManager;
        private readonly IGuiCommands _guiCommands;
        private readonly IFileCommands _fileCommands;
        private readonly IMessenger _messenger;
        private readonly IProjectManager _projectManager;

        #region Fields/Properties

        /// <inheritdoc/>
        public string GlueProjectToLoad
        {
            get;
            private set;
        }

        /// <inheritdoc/>
        public bool ShouldExitImmediately { get; private set; }

        /// <inheritdoc/>
        public bool ShouldCodeGenAll { get; private set; }

        /// <inheritdoc/>
        public string ElementName
        {
            get;
            private set;
        }

        #endregion

        public CommandLineManager(
            IFontManager fontManager,
            IGuiCommands guiCommands,
            IFileCommands fileCommands,
            IMessenger messenger,
            IProjectManager projectManager)
        {
            _fontManager = fontManager;
            _guiCommands = guiCommands;
            _fileCommands = fileCommands;
            _messenger = messenger;
            _projectManager = projectManager;
        }

        /// <inheritdoc/>
        public Task ReadCommandLine() => ReadCommandLine(Environment.GetCommandLineArgs());

        /// <summary>
        /// Parses the supplied command-line arguments and populates this manager's properties.
        /// Prefer the parameterless <see cref="ReadCommandLine()"/> in production; this overload
        /// exists so the parsing logic can be exercised with explicit arguments in tests.
        /// </summary>
        public async Task ReadCommandLine(string[] commandLineArgs)
        {
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
            await _fontManager.CreateAllMissingFontFiles(_projectManager.GumProjectSave);

            // 4.
            _messenger.Send<CloseMainWindowMessage>();
        }
    }
}
