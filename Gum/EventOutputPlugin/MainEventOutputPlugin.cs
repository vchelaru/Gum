using EventOutputPlugin.Managers;
using EventOutputPlugin.Models;
using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace EventOutputPlugin
{
    public enum GumEventTypes
    {
        ElementAdded,
        ElementDeleted,
        ElementRenamed,
        StateRenamed,
        StateCategoryRenamed,
        InstanceAdded,
        InstanceDeleted,
        InstanceRenamed,
    }

    [Export(typeof(PluginBase))]
    public class MainEventOutputPlugin : PluginBase
    {
        public override string FriendlyName => "Event Output Plugin";

        public override Version Version => new Version(1,0,0);

        private readonly IExportEventFileManager _exportEventFileManager;
        private readonly IDispatcher _dispatcher;
        private readonly IProjectState _projectState;
        private readonly IPluginManager _pluginManager;

        [ImportingConstructor]
        public MainEventOutputPlugin(IProjectState projectState, IFileCommands fileCommands, IRetryService retryService,
            IDispatcher dispatcher, IPluginManager pluginManager)
        {
            _exportEventFileManager = new ExportEventFileManager(projectState, fileCommands, retryService);
            _dispatcher = dispatcher;
            _projectState = projectState;
            _pluginManager = pluginManager;
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AssignEvents();
        }

        private void AssignEvents()
        {
            // Nothing on screen depends on the pruned file, so it waits until the project is shown.
            this.ProjectLoad += (project) =>
            {
                string? loadedDirectory = _projectState.ProjectDirectory;
                _dispatcher.Post(() => _pluginManager.CallGuarded(this, () =>
                {
                    // A later load posted its own prune; this one would prune that project twice.
                    if (_projectState.ProjectDirectory == loadedDirectory)
                    {
                        _exportEventFileManager.DeleteOldEventFiles();
                    }
                }, nameof(ProjectLoad)));
            };

            this.ElementAdd += (newElement) =>
                _exportEventFileManager.ExportEvent(newElement.Name, null, GumEventTypes.ElementAdded, GetElementPrefix(newElement) );
            this.ElementDelete += (deletedElement) =>
                _exportEventFileManager.ExportEvent(null, deletedElement.Name, GumEventTypes.ElementDeleted, GetElementPrefix(deletedElement));
            this.ElementRename += (renamedElement, oldName) =>
                _exportEventFileManager.ExportEvent(renamedElement.Name, oldName, GumEventTypes.ElementRenamed, GetElementPrefix(renamedElement));
            this.StateRename += (renamedState, oldName) =>
                _exportEventFileManager.ExportEvent(renamedState.Name, oldName, GumEventTypes.StateRenamed, GetElementPrefix(renamedState.ParentContainer));
            this.CategoryRename += (renamedCategory, oldName) =>
                _exportEventFileManager.ExportEvent(renamedCategory.Name, oldName, GumEventTypes.StateCategoryRenamed, GetElementPrefix(ObjectFinder.Self.GetContainerOf(renamedCategory)));
            this.InstanceAdd += (element, newInstance) =>
                _exportEventFileManager.ExportEvent(element + "." + newInstance.Name, null, GumEventTypes.InstanceAdded, GetElementPrefix(element));
            this.InstanceRename += (element, instance, oldName) =>
                _exportEventFileManager.ExportEvent(element + "." + instance.Name, element + "." + oldName, GumEventTypes.InstanceRenamed, GetElementPrefix(element));
            this.InstanceDelete += (element, instance) =>
                _exportEventFileManager.ExportEvent(null,  element + "." + instance, GumEventTypes.InstanceDeleted, GetElementPrefix(element));


        }

        private string? GetElementPrefix(ElementSave? element)
        {
            if (element is ScreenSave)
            {
                return ElementReference.ScreenSubfolder;
            }
            else if(element is ComponentSave)
            {
                return ElementReference.ComponentSubfolder;
            }
            else if(element is StandardElementSave)
            {
                return ElementReference.StandardSubfolder;
            }
            else
            {
                return null;
            }
        }
    }
}
