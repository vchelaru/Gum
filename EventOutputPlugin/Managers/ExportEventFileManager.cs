using EventOutputPlugin.Models;
using Gum;
using Gum.ToolStates;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace EventOutputPlugin.Managers
{
    public class ExportEventFileManager
    {
        static string EventExportDirectory
        {
            get
            {

                if (ProjectState.Self.GumProjectSave != null)
                {
                    return ProjectState.Self.ProjectDirectory + "EventExport/";
                }
                else
                {
                    return null;
                }
            }
        }

        public static void ExportEvent(string newName, string oldName, GumEventTypes eventType)
        {
            if(!string.IsNullOrWhiteSpace(EventExportDirectory))
            {
                var exportedEvent = new ExportedEvent();

                exportedEvent.NewName = newName;
                exportedEvent.OldName = oldName;
                exportedEvent.EventType = eventType;

                var serialized = JsonConvert.SerializeObject(exportedEvent);

                var file = new FilePath(EventExportDirectory +
                    Environment.UserName + "_" +
                    DateTime.UtcNow.Ticks + ".json");

                GumCommands.Self.TryMultipleTimes(
                    () =>
                    {
                        var directoryName = file.GetDirectoryContainingThis().FullPath;
                        // make the directory
                        System.IO.Directory.CreateDirectory(directoryName);
                        System.IO.File.WriteAllText(file.FullPath, serialized);
                    });
            }
        }

        public static void DeleteOldEventFiles()
        {
            const int daysToKeep = 14;

            if(!string.IsNullOrEmpty(EventExportDirectory) && System.IO.Directory.Exists(EventExportDirectory))
            {
                var filesInDirectory = System.IO.Directory.GetFiles(EventExportDirectory);

                var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);

                foreach(var file in filesInDirectory)
                {
                    var fileDate = GetFileDateTimeUtc(file);

                    if(fileDate < cutoff)
                    {
                        try
                        {
                            System.IO.File.Delete(file);
                        }
                        catch(Exception e)
                        {
                            // if we can't, oh well
                            GumCommands.Self.GuiCommands.PrintOutput(
                                $"Error attempting to delete event file:\n{e}");
                        }
                    }
                }
            }

        }

        private static DateTime GetFileDateTimeUtc(FilePath fileName)
        {
            var stripped = fileName.RemoveExtension().FileNameNoPath;

            var lastUnderscore = stripped.LastIndexOf("_");

            var ticks = long.Parse(stripped.Substring(lastUnderscore + 1));

            var dateUtc = new DateTime(ticks);

            return dateUtc;
        }

        
    }
}
