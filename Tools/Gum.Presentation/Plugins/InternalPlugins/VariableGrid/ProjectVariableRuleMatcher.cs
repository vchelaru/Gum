using Gum.Managers;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Decides whether an engine profile's <see cref="EngineProfile.VariableRules"/> hide a standard variable. The
/// last rule whose pattern matches wins; with no match <see cref="EngineProfile.HideVariablesByDefault"/> decides.
/// </summary>
internal class ProjectVariableRuleMatcher
{
    public bool IsHidden(EngineProfile profile, string? standardTypeName, string variableRootName)
    {
        bool hidden = profile.HideVariablesByDefault;
        string qualifiedName = standardTypeName + "." + variableRootName;

        foreach (VariableRule rule in profile.VariableRules)
        {
            string textToMatch = rule.Pattern.Contains('.') ? qualifiedName : variableRootName;
            if (standardTypeName == null && rule.Pattern.Contains('.'))
            {
                continue;
            }

            if (GetIfMatches(rule.Pattern, textToMatch))
            {
                hidden = rule.Action == VariableRuleAction.Hide;
            }
        }

        return hidden;
    }

    private static bool GetIfMatches(string pattern, string text)
    {
        int patternIndex = 0;
        int textIndex = 0;
        int starIndex = -1;
        int textIndexAtStar = 0;

        while (textIndex < text.Length)
        {
            if (patternIndex < pattern.Length && pattern[patternIndex] == '*')
            {
                starIndex = patternIndex;
                textIndexAtStar = textIndex;
                patternIndex++;
            }
            else if (patternIndex < pattern.Length && pattern[patternIndex] == text[textIndex])
            {
                patternIndex++;
                textIndex++;
            }
            else if (starIndex >= 0)
            {
                patternIndex = starIndex + 1;
                textIndexAtStar++;
                textIndex = textIndexAtStar;
            }
            else
            {
                return false;
            }
        }

        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == pattern.Length;
    }
}
