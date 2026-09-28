using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;

namespace Gum.Managers;

/// <summary>
/// Fills settings a project file may lack with the tool's defaults when the project is opened or
/// created. A fill is not an edit: it is not saved on open and does not mark the project unsaved;
/// the next save of the project writes it (#5412). Called by <see cref="IProjectManager"/> before
/// plugins receive the loaded project.
/// </summary>
public interface IProjectLoadFills
{
    /// <summary>Fills what <paramref name="project"/> lacks. Returns whether anything was filled.</summary>
    bool Apply(GumProjectSave project);
}

/// <inheritdoc/>
public class ProjectLoadFills : IProjectLoadFills
{
    /// <summary>The canvas sizes a project without its own gets.</summary>
    public static IReadOnlyList<CustomCanvasSize> DefaultCanvasSizes { get; } = new CustomCanvasSize[]
    {
        new CustomCanvasSize { Width = null, Height = null, FriendlyName = "Project Default" },
        new CustomCanvasSize { Width = 640, Height = 480, FriendlyName = "480p" },
        new CustomCanvasSize { Width = 1280, Height = 720, FriendlyName = "720p" },
        new CustomCanvasSize { Width = 1280, Height = 800, FriendlyName = "Steam Deck" },
        new CustomCanvasSize { Width = 1920, Height = 1080, FriendlyName = "1080p" },
        new CustomCanvasSize { Width = 3840, Height = 2160, FriendlyName = "4k" },
    };

    /// <inheritdoc/>
    public bool Apply(GumProjectSave project)
    {
        if (project.CustomCanvasSizes is { Count: > 0 })
        {
            return false;
        }
        project.CustomCanvasSizes = DefaultCanvasSizes.ToList();
        return true;
    }
}
