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
        static string EventExportDirector =>
            ProjectState.Self.ProjectDirectory + "EventExport/";

        public static void ExportEvent(string newName, string oldName, GumEventTypes eventType)
        {
            var exportedEvent = new ExportedEvent();

            exportedEvent.NewName = newName;
            exportedEvent.OldName = oldName;
            exportedEvent.EventType = eventType;

            var serialized = JsonConvert.SerializeObject(exportedEvent);

            var file = new FilePath(EventExportDirector +
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

        public static void DeleteOldEventFiles()
        {
            const int daysToKeep = 14;

            if(System.IO.Directory.Exists(EventExportDirector))
            {
                var filesInDirectory = System.IO.Directory.GetFiles(EventExportDirector);

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
