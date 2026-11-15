using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gum.ProjectServices.CodeGeneration;

/// <summary>
/// Decides which elements are Debug Only (<see cref="CodeOutputElementSettings.DebugOnly"/>) and which
/// elements must be regenerated when that flag changes. An element is effectively debug only when it, or
/// any element it inherits from, is flagged: a class cannot exist in a release build without its base, so
/// the flag follows the BaseType chain.
/// </summary>
public class DebugOnlyElementLogic
{
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly Dictionary<ElementSave, bool> _cache = new Dictionary<ElementSave, bool>();

    public DebugOnlyElementLogic(CodeOutputElementSettingsManager elementSettingsManager)
    {
        _elementSettingsManager = elementSettingsManager;
    }

    /// <summary>Forgets what was read from disk. Called at the start of each generation, since the flag can change between runs.</summary>
    public void ClearCache() => _cache.Clear();

    /// <summary>
    /// Whether <paramref name="element"/> is effectively debug only. <paramref name="knownSettings"/> are the
    /// settings of <paramref name="element"/> itself when the caller has them in hand (they may be newer than the file).
    /// </summary>
    public bool IsDebugOnly(ElementSave? element, CodeOutputElementSettings? knownSettings = null)
    {
        HashSet<ElementSave> visited = new HashSet<ElementSave>();
        while (element != null && element is not StandardElementSave && visited.Add(element))
        {
            bool ownFlag;
            if (knownSettings != null)
            {
                ownFlag = knownSettings.DebugOnly;
                knownSettings = null;
            }
            else if (!_cache.TryGetValue(element, out ownFlag))
            {
                ownFlag = _elementSettingsManager.LoadOrCreateSettingsFor(element).DebugOnly;
                _cache[element] = ownFlag;
            }

            if (ownFlag)
            {
                return true;
            }
            element = string.IsNullOrEmpty(element.BaseType) ? null : ObjectFinder.Self.GetElementSave(element.BaseType);
        }
        return false;
    }

    public bool IsInstanceDebugOnly(InstanceSave? instance) =>
        instance != null && IsDebugOnly(ObjectFinder.Self.GetElementSave(instance));

    /// <summary>
    /// <paramref name="toggled"/> followed by every element whose generated code changes when its flag does:
    /// elements with an instance of it and elements derived from it, and in turn the elements that reference those.
    /// </summary>
    public IReadOnlyList<ElementSave> GetElementsToRegenerate(ElementSave toggled, GumProjectSave project)
    {
        List<ElementSave> result = new List<ElementSave> { toggled };
        HashSet<ElementSave> seen = new HashSet<ElementSave> { toggled };
        List<ElementSave> candidates = project.Screens.Cast<ElementSave>().Concat(project.Components).ToList();

        for (int i = 0; i < result.Count; i++)
        {
            ElementSave target = result[i];
            foreach (ElementSave candidate in candidates)
            {
                if (seen.Contains(candidate))
                {
                    continue;
                }
                bool derives = !string.IsNullOrEmpty(candidate.BaseType) &&
                    ObjectFinder.Self.GetElementSave(candidate.BaseType) == target;
                bool instantiates = candidate.Instances.Any(instance => ObjectFinder.Self.GetElementSave(instance) == target);
                if (derives || instantiates)
                {
                    seen.Add(candidate);
                    result.Add(candidate);
                }
            }
        }
        return result;
    }

    /// <summary>
    /// Writes <c>#if DEBUG</c> now when <paramref name="active"/>, and <c>#endif</c> on dispose; when nothing was
    /// written in between, the opening line is taken back so no empty guard is left behind.
    /// </summary>
    internal static DebugGuard Begin(StringBuilder stringBuilder, bool active) => new DebugGuard(stringBuilder, active);

    internal readonly struct DebugGuard : IDisposable
    {
        private static readonly string Open = "#if DEBUG" + Environment.NewLine;
        private readonly StringBuilder? _stringBuilder;
        private readonly int _lengthAfterOpen;

        public DebugGuard(StringBuilder stringBuilder, bool active)
        {
            if (active)
            {
                _stringBuilder = stringBuilder;
                stringBuilder.Append(Open);
                _lengthAfterOpen = stringBuilder.Length;
            }
            else
            {
                _stringBuilder = null;
                _lengthAfterOpen = 0;
            }
        }

        public void Dispose()
        {
            if (_stringBuilder == null)
            {
                return;
            }
            if (_stringBuilder.Length == _lengthAfterOpen)
            {
                _stringBuilder.Length -= Open.Length;
            }
            else
            {
                _stringBuilder.Append("#endif" + Environment.NewLine);
            }
        }
    }
}
