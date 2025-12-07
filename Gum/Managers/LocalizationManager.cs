using CsvLibrary;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ToolsUtilities;

namespace Gum.Managers;

public class LocalizationManager
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

    public LocalizationManager()
    {
        Languages = new ReadOnlyCollection<string>(new List<string>());
    }

    public void Clear()
    {
        Languages = new ReadOnlyCollection<string>(new List<string>());
        mStringDatabase = new Dictionary<string, string[]>();
        HasDatabase = false;
    }

    public void AddDatabase(string fileName, char delimiter)
    {

        RuntimeCsvRepresentation rcr;

        //char oldDelimiter = CsvFileManager.Delimiter;
        //CsvFileManager.Delimiter = delimiter;
        Dictionary<string, string[]> entryDictionary = new Dictionary<string, string[]>();

        CsvFileManager.CsvDeserializeDictionary<string, string[]>(fileName, 
            entryDictionary, 
            // FRB supports multiple lines of text per single string ID. We don't support this in Gum (yet?), so just use the first:
            DuplicateDictionaryEntryBehavior.PreserveFirst,
            out rcr);
        //CsvFileManager.Delimiter = oldDelimiter;
        var keys = entryDictionary.Keys.ToArray();
        foreach(var key in keys)
        {
            if(key?.Trim().StartsWith("//") == true)
            {
                entryDictionary.Remove(key);
            }
        }

        List<string> headerList = new List<string>();

        foreach (CsvHeader header in rcr.Headers)
        {
            headerList.Add(header.Name);
        }

        AddDatabase(entryDictionary, headerList);
    }

    public void AddDatabase(Dictionary<string, string[]> entryDictionary, List<string> headerList)
    {
        Languages = new ReadOnlyCollection<string>(headerList);
        mStringDatabase = entryDictionary;
        HasDatabase = true;
    }

    public string Translate(string stringID)
    {
        return TranslateForLanguage(stringID, CurrentLanguage);
    }

    public string TranslateForLanguage(string stringID, int language)
    {

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
        if (string.IsNullOrEmpty(stringID))
        {
            return true;
        }
        else if (StringFunctions.IsNumber(stringID))
        {
            return true;
        }
        else if (IsPercentage(stringID))
        {
            return true;
        }
        else if (IsTime(stringID))
        {
            return true;
        }
        else if (stringID == "!" || stringID == "+" || stringID == "-" ||
            stringID == "*" || stringID == "/" || stringID == "#" || stringID == ":" ||
            stringID == "<" || stringID == ">")
        {
            return true;
        }
        return false;
    }

    private bool IsTime(string stringID)
    {
        for (int i = 0; i < stringID.Length; i++)
        {
            char cAti = stringID[i];
            if (char.IsDigit(cAti) == false && (cAti == ':') == false && (cAti == '.') == false)
            {
                return false;
            }
        }
        return true;
    }

    private bool IsPercentage(string stringID)
    {
        return stringID.CountOf('%') == 1 && stringID.Length > 1 && stringID.EndsWith("%") && StringFunctions.IsNumber(stringID.Substring(0, stringID.Length - 1));

    }

}
