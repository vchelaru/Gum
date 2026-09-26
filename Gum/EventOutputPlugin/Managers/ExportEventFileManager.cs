using EventOutputPlugin.Models;
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

/// <inheritdoc cref="IExportEventFileManager"/>
public class ExportEventFileManager : IExportEventFileManager
{
    private readonly IProjectState _projectState;
    private readonly IFileCommands _fileCommands;
    private readonly IRetryService _retryService;
    const string masterFileName = "gum_events.json";
    ExportedEventCollection? _events;
    string? _eventsFileFullPath;

    /// <summary>
    /// Creates the manager. The plugin constructs it from its injected services.
    /// </summary>
    public ExportEventFileManager(IProjectState projectState, IFileCommands fileCommands, IRetryService retryService)
    {
        _projectState = projectState;
        _fileCommands = fileCommands;
        _retryService = retryService;
    }

    string? EventExportDirectory
    {
        get
        {
            string? projectDirectory = _projectState.ProjectDirectory;
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

    string? EventFileFullPath
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

    // The cache belongs to one events file; switching projects must not carry it over (#5106).
    ExportedEventCollection Events
    {
        get
        {
            string? eventFileFullPath = EventFileFullPath;
            if(_events == null || _eventsFileFullPath != eventFileFullPath)
            {
                _events = GetOrCreateEventCollection(eventFileFullPath);
                _eventsFileFullPath = eventFileFullPath;
            }

            return _events;
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

    public void ExportEvent(string? newName, string? oldName, GumEventTypes eventType, string? elementType)
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

    public void DeleteOldEventFiles()
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

    ExportedEventCollection GetOrCreateEventCollection(string? eventFileFullPath)
    {
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

    void SaveEventCollection()
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
