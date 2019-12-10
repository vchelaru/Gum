using EventOutputPlugin.Managers;
using EventOutputPlugin.Models;
using Gum;
using Gum.DataTypes;
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
                ExportEventFileManager.ExportEvent(GetElementPrefix(newElement) + newElement.Name, null, GumEventTypes.ElementAdded);
            this.ElementDelete += (deletedElement) =>
                ExportEventFileManager.ExportEvent(null, GetElementPrefix(deletedElement) + deletedElement.Name, GumEventTypes.ElementDeleted);
            this.ElementRename += (renamedElement, oldName) =>
                ExportEventFileManager.ExportEvent(GetElementPrefix(renamedElement) + renamedElement.Name, oldName, GumEventTypes.ElementRenamed);
            this.StateRename += (renamedState, oldName) =>
                ExportEventFileManager.ExportEvent(renamedState.Name, oldName, GumEventTypes.StateRenamed);
            this.CategoryRename += (renamedCategory, oldName) =>
                ExportEventFileManager.ExportEvent(renamedCategory.Name, oldName, GumEventTypes.StateCategoryRenamed);
            this.InstanceAdd += (element, newInstance) =>
                ExportEventFileManager.ExportEvent(GetElementPrefix(element) + element + "." + newInstance.Name, null, GumEventTypes.InstanceAdded);
            this.InstanceRename += (element, instance, oldName) =>
                ExportEventFileManager.ExportEvent(GetElementPrefix(element) + element + "." + instance.Name, element + "." + oldName, GumEventTypes.InstanceRenamed);
            this.InstanceDelete += (element, instance) =>
                ExportEventFileManager.ExportEvent(null, GetElementPrefix(element) + element + "." + instance, GumEventTypes.InstanceDeleted);


        }

        private string GetElementPrefix(ElementSave element)
        {
            if (element is ScreenSave)
            {
                return ElementReference.ScreenSubfolder + "/";
            }
            else if(element is ComponentSave)
            {
                return ElementReference.ComponentSubfolder + "/";
            }
            else if(element is StandardElementSave)
            {
                return ElementReference.StandardSubfolder + "/";
            }
            else
            {
                return null;
            }
        }
    }
}
