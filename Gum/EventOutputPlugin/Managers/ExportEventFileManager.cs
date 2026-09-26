using EventOutputPlugin.Models;
using Gum;
using Gum.ToolStates;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Gum.Commands;
using Gum.Services;
using ToolsUtilities;

namespace EventOutputPlugin.Managers;

public class ExportEventFileManager
{
    private static readonly IFileCommands _fileCommands = Locator.GetRequiredService<IFileCommands>();
    private static readonly IRetryService _retryService = Locator.GetRequiredService<IRetryService>();
    const string masterFileName = "gum_events.json";
    static ExportedEventCollection? events;

    static string? EventExportDirectory
    {
        get
        {
            string? projectDirectory = Locator.GetRequiredService<IProjectState>().ProjectDirectory;
            if (!string.IsNullOrEmpty(projectDirectory))
            {
                return Path.Combine(projectDirectory, "EventExport");
            }
            else
            {
                return null;
            }
        }
    }

    static string? EventFileFullPath
    {
        get
        {
            string? eventExportDirectory = EventExportDirectory;
            if (!string.IsNullOrEmpty(eventExportDirectory))
            {
                return Path.Combine(eventExportDirectory, masterFileName);
            }
            else
            {
                return null;
            }
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


    static string GenerateConsistentHash(string input)
    {
        // Handle null inputs
        if (string.IsNullOrEmpty(input))
            return "";

        // Create SHA256 hash object
        using var sha256 = SHA256.Create();

        // Convert string to bytes using UTF8 encoding
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);

        // Compute hash
        byte[] hashBytes = sha256.ComputeHash(inputBytes);

        // Convert to lowercase hexadecimal string
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }

    public static void ExportEvent(string? newName, string? oldName, GumEventTypes eventType, string? elementType)
    {
        if(!string.IsNullOrWhiteSpace(EventExportDirectory))
        {
            var exportedEvent = new ExportedEvent();
            var username = GenerateConsistentHash(Environment.UserName);

            exportedEvent.NewName = newName;
            exportedEvent.OldName = oldName; 
            exportedEvent.ElementType = elementType;
            exportedEvent.EventType = eventType;
            exportedEvent.TimestampUtc = DateTime.UtcNow;

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
        var keys = Events.UserEvents.Keys.ToList();

        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
        foreach (var key in keys)
        {
            var list = Events.UserEvents[key];
            for (var i = list.Count - 1; i > -1; i--)
            {
                if (list[i].TimestampUtc < cutoff)
                {
                    list.RemoveAt(i);
                }
            }
        }
        SaveEventCollection();
    }

    static ExportedEventCollection GetOrCreateEventCollection()
    {
        string? eventFileFullPath = EventFileFullPath;
        if (eventFileFullPath != null && File.Exists(eventFileFullPath))
        {
            var text = File.ReadAllText(eventFileFullPath);
            return ExportedEventCollection.FromJson(text);
        }
        else
        {
            return new ExportedEventCollection();
        }
    }

    static void SaveEventCollection()
    {
        string? eventFileFullPath = EventFileFullPath;
        if (!string.IsNullOrEmpty(eventFileFullPath))
        {
            var file = new FilePath(eventFileFullPath);
            // using indented formatting results in "unminified" JSON. This is desired
            // to prevent merge conflicts.
            var serialized = JsonConvert.SerializeObject(Events, Formatting.Indented);
            _retryService.TryMultipleTimes(
                () =>
                {
                    // eventFileFullPath is a rooted file path, so it always has a containing directory.
                    if (file.GetDirectoryContainingThis() is { } directory)
                    {
                        System.IO.Directory.CreateDirectory(directory.FullPath);
                    }

                    _fileCommands.SaveIfDiffers(file, serialized);
                });
        }
    }

    
}
