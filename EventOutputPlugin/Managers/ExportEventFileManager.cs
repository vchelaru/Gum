using EventOutputPlugin.Models;
using Gum;
using Gum.ToolStates;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace EventOutputPlugin.Managers
{
    public class ExportEventFileManager
    {
        const string masterFileName = "gum_events.json";
        static ExportedEventCollection events;

        static string EventExportDirectory
        {
            get
            {

                if (ProjectState.Self.GumProjectSave != null)
                {
                    return Path.Combine(ProjectState.Self.ProjectDirectory, "EventExport");
                }
                else
                {
                    return null;
                }
            }
        }

        static string EventFileFullPath
        {
            get
            {
                return Path.Combine(EventExportDirectory, masterFileName);
            }
        }

        static ExportedEventCollection Events
        {
            get
            {
                if(events == null)
                {
                    events = GetOrCreateEventCollection();
                }

                return events;
            }
        }



        public static void ExportEvent(string newName, string oldName, GumEventTypes eventType)
        {
            if(!string.IsNullOrWhiteSpace(EventExportDirectory))
            {
                var exportedEvent = new ExportedEvent();
                var username = Environment.UserName;

                exportedEvent.NewName = newName;
                exportedEvent.OldName = oldName;
                exportedEvent.EventType = eventType;
                exportedEvent.Timestamp = DateTime.UtcNow;

                if(!Events.UserEvents.ContainsKey(username))
                {
                    Events.UserEvents[username] = new List<ExportedEvent>();
                }

                Events.UserEvents[username].Add(exportedEvent);

                SaveEventCollection();
            }
        }

        public static void DeleteOldEventFiles()
        {
            const int daysToKeep = 14;
            var keys = Events?.UserEvents?.Keys?.ToList();

            // EARLY OUT: nothing to delete, probably a new file
            if (keys == null)
            {
                return;
            }

            var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
            foreach (var key in keys)
            {
                var list = Events.UserEvents[key];
                for (var i = list.Count - 1; i > -1; i--)
                {
                    if (list[i].Timestamp < cutoff)
                    {
                        list.RemoveAt(i);
                    }
                }
            }
            SaveEventCollection();
        }

        static ExportedEventCollection GetOrCreateEventCollection()
        {
            ExportedEventCollection collection;
            if(File.Exists(EventFileFullPath))
            {
                var text = File.ReadAllText(EventFileFullPath);
                collection = JsonConvert.DeserializeObject<ExportedEventCollection>(text);
            }
            else
            {
                collection = new ExportedEventCollection();
                collection.UserEvents = new Dictionary<string, List<ExportedEvent>>();
            }

            return collection;
        }

        static void SaveEventCollection()
        {
            var file = new FilePath(EventFileFullPath);
            // using indented formatting results in "unminified" JSON. This is desired
            // to prevent merge conflicts.
            var serialized = JsonConvert.SerializeObject(Events, Formatting.Indented);
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
}
