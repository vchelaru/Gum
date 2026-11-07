using System;
using System.Collections.Generic;
using System.Linq;
using Gum.DataTypes;
using ToolsUtilities;

namespace Gum.Managers;

/// <inheritdoc cref="IProjectRestrictions"/>
public class ProjectRestrictions : IProjectRestrictions
{
    private readonly ProjectVariableRuleMatcher _matcher = new ProjectVariableRuleMatcher();
    private readonly IEngineProfileLoader _loader;
    private readonly IOutputManager _outputManager;

    // The profile the cache was built for; a different project, reference or file name reloads it.
    private GumProjectSave? _cachedProject;
    private string? _cachedReference;
    private string? _cachedProjectFileName;
    private readonly EngineProfile _unrestricted = new EngineProfile();
    private EngineProfile _cachedProfile = new EngineProfile();
    private FilePath? _cachedFilePath;

    private EngineProfile? _inMemoryProfile;

    public ProjectRestrictions(IEngineProfileLoader loader, IOutputManager outputManager)
    {
        _loader = loader;
        _outputManager = outputManager;
    }

    /// <inheritdoc/>
    public event EventHandler? Changed;

    private EngineProfile Profile
    {
        get
        {
            if (_inMemoryProfile != null)
            {
                return _inMemoryProfile;
            }

            GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
            if (project == null)
            {
                return _unrestricted;
            }

            if (!ReferenceEquals(project, _cachedProject) ||
                project.EngineProfile != _cachedReference ||
                project.FullFileName != _cachedProjectFileName)
            {
                LoadFor(project);
            }
            return _cachedProfile;
        }
    }

    private void LoadFor(GumProjectSave project)
    {
        EngineProfileLoadResult result = _loader.Load(project.FullFileName, project.EngineProfile);
        _cachedProject = project;
        _cachedReference = project.EngineProfile;
        _cachedProjectFileName = project.FullFileName;
        _cachedProfile = result.Profile;
        _cachedFilePath = result.FilePath;
        if (result.Warning != null)
        {
            _outputManager.AddOutput(result.Warning);
        }
    }

    /// <summary>
    /// Replaces the profile the project's reference resolves to with <paramref name="profile"/>, without touching
    /// disk. Pass null to go back to the project's own reference.
    /// </summary>
    public void UseProfile(EngineProfile? profile)
    {
        _inMemoryProfile = profile;
    }

    /// <inheritdoc/>
    public FilePath? ProfileFilePath
    {
        get
        {
            _ = Profile;
            return _cachedFilePath;
        }
    }

    /// <inheritdoc/>
    public void Reload()
    {
        _cachedProject = null;
        _ = Profile;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public bool AreComponentsDisabled => Profile.DisableComponents;

    /// <inheritdoc/>
    public bool AreBehaviorsDisabled => Profile.DisableBehaviors;

    /// <inheritdoc/>
    public IReadOnlyList<string> SupportedStandardTypes => Profile.SupportedStandardTypes;

    /// <inheritdoc/>
    public bool HasVariableRules
    {
        get
        {
            EngineProfile profile = Profile;
            return profile.HideVariablesByDefault || profile.VariableRules.Count > 0;
        }
    }

    /// <inheritdoc/>
    public bool IsVariableHidden(string? standardTypeName, string variableRootName)
    {
        return _matcher.IsHiddenByProfile(Profile, standardTypeName, variableRootName);
    }

    /// <inheritdoc/>
    public bool IsVariableHidden(InstanceSave instance, string variableRootName) =>
        IsVariableHidden(ObjectFinder.Self.GetRootStandardElementSave(instance)?.Name, variableRootName);

    /// <inheritdoc/>
    public bool IsVariableHidden(ElementSave element, string variableRootName) =>
        IsVariableHidden(ObjectFinder.Self.GetRootStandardElementSave(element)?.Name, variableRootName);

    /// <inheritdoc/>
    public bool IsEveryVariableHidden(ElementSave element, params string[] variableRootNames) =>
        variableRootNames.All(name => IsVariableHidden(element, name));

    /// <inheritdoc/>
    public bool IsInstanceTypeBlocked(ElementSave elementToAdd)
    {
        if (elementToAdd is StandardElementSave)
        {
            return IsStandardTypeBlocked(elementToAdd.Name);
        }
        if (elementToAdd is ComponentSave)
        {
            return AreComponentsDisabled;
        }
        return false;
    }

    /// <inheritdoc/>
    public bool IsStandardTypeBlocked(string standardTypeName)
    {
        List<string> supported = Profile.SupportedStandardTypes;
        return supported.Count > 0 && !supported.Contains(standardTypeName);
    }

    /// <inheritdoc/>
    public bool IsHierarchyBlockedFor(ElementSave element) => IsVariableHidden(element, "Parent");

    /// <inheritdoc/>
    public bool IsHierarchyBlocked
    {
        get
        {
            GumProjectSave? project = ObjectFinder.Self.GumProjectSave;
            if (project == null)
            {
                return false;
            }
            List<string> supported = Profile.SupportedStandardTypes;
            List<string> typeNames = supported.Count > 0
                ? supported
                : project.StandardElements.Select(item => item.Name).ToList();
            return typeNames.Count > 0 && typeNames.All(name => IsVariableHidden(name, "Parent"));
        }
    }
}
