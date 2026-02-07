using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Gum.Localization;

namespace Gum.Localization;

/// <summary>
/// Manages localization strings and language support for the application.
/// Implements ILocalizationService for portability across different contexts.
/// </summary>
public class LocalizationService : ILocalizationService
{
    public ReadOnlyCollection<string> Languages
    {
        get;
        set;
    }

    Dictionary<string, string[]> mStringDatabase = new Dictionary<string, string[]>();

    string[] emptyStringArray = new string[0];
    public IEnumerable<string> Keys => mStringDatabase?.Keys.ToArray() ?? emptyStringArray;

    public bool HasDatabase
    {
        
        get;
        private set;
    }

    public int CurrentLanguage
    {
        get;
        set;
    }

    public LocalizationService()
    {
        Languages = new ReadOnlyCollection<string>(new List<string>());
    }

    public void Clear()
    {
        Languages = new ReadOnlyCollection<string>(new List<string>());
        mStringDatabase = new Dictionary<string, string[]>();
        HasDatabase = false;
    }

    /// <summary>
    /// Adds a localization database from a dictionary of string IDs to translations.
    /// </summary>
    /// <param name="entryDictionary">Dictionary mapping string IDs to arrays of translations (one per language).</param>
    /// <param name="headerList">List of language names corresponding to the translation arrays.</param>
    public void AddDatabase(Dictionary<string, string[]> entryDictionary, List<string> headerList)
    {
        Languages = new ReadOnlyCollection<string>(headerList);
        mStringDatabase = entryDictionary;
        HasDatabase = true;
    }

    /// <summary>
    /// Translates a string ID to the current language.
    /// </summary>
    /// <param name="stringId">The string ID to translate.</param>
    /// <returns>The translated string, or the original string with "(loc)" suffix if not found.</returns>
    public string Translate(string stringId)
    {
        return TranslateForLanguage(stringId, CurrentLanguage);
    }

    /// <summary>
    /// Translates a string ID to a specific language.
    /// </summary>
    /// <param name="stringID">The string ID to translate.</param>
    /// <param name="language">The language index to translate to.</param>
    /// <returns>The translated string, or the original string with "(loc)" suffix if not found.</returns>
    public string TranslateForLanguage(string stringID, int language)
    {
        if (this.mStringDatabase.Count == 0) return stringID;

        if (stringID == null)
        {
            return "NULL STRING";
        }
        else if (mStringDatabase.ContainsKey(stringID))
        {
            return mStringDatabase[stringID][language];
        }
        else if (ShouldExcludeFromTranslation(stringID))
        {
            return stringID;
        }
        else
        {
            return stringID + "(loc)";
        }
    }

    private bool ShouldExcludeFromTranslation(string stringID)
    {
        if(string.IsNullOrEmpty(stringID))
        {
            return true;
        }
        foreach(var character in stringID)
        {
            if(char.IsLetter(character))
            {
                return false;
            }
        }

        // If we got here, it has no letters, so we should exclude it:
        return true;
    }
}
