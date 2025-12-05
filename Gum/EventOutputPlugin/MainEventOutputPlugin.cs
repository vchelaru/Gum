using EventOutputPlugin.Managers;
using EventOutputPlugin.Models;
using Gum;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
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
            this.ProjectLoad += (project) =>
                ExportEventFileManager.DeleteOldEventFiles();

            this.ElementAdd += (newElement) =>
                ExportEventFileManager.ExportEvent(newElement.Name, null, GumEventTypes.ElementAdded, GetElementPrefix(newElement) );
            this.ElementDelete += (deletedElement) =>
                ExportEventFileManager.ExportEvent(null, deletedElement.Name, GumEventTypes.ElementDeleted, GetElementPrefix(deletedElement));
            this.ElementRename += (renamedElement, oldName) =>
                ExportEventFileManager.ExportEvent(renamedElement.Name, oldName, GumEventTypes.ElementRenamed, GetElementPrefix(renamedElement));
            this.StateRename += (renamedState, oldName) =>
                ExportEventFileManager.ExportEvent(renamedState.Name, oldName, GumEventTypes.StateRenamed, GetElementPrefix(renamedState.ParentContainer));
            this.CategoryRename += (renamedCategory, oldName) =>
                ExportEventFileManager.ExportEvent(renamedCategory.Name, oldName, GumEventTypes.StateCategoryRenamed, GetElementPrefix(ObjectFinder.Self.GetContainerOf(renamedCategory)));
            this.InstanceAdd += (element, newInstance) =>
                ExportEventFileManager.ExportEvent(element + "." + newInstance.Name, null, GumEventTypes.InstanceAdded, GetElementPrefix(element));
            this.InstanceRename += (element, instance, oldName) =>
                ExportEventFileManager.ExportEvent(element + "." + instance.Name, element + "." + oldName, GumEventTypes.InstanceRenamed, GetElementPrefix(element));
            this.InstanceDelete += (element, instance) =>
                ExportEventFileManager.ExportEvent(null,  element + "." + instance, GumEventTypes.InstanceDeleted, GetElementPrefix(element));


        }

        private string GetElementPrefix(ElementSave element)
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
