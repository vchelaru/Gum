using CsvLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Managers
{
    static class LocalizationManager
    {

        public static ReadOnlyCollection<string> Languages
        {
            get;
            set;
        }

        static Dictionary<string, string[]> mStringDatabase = new Dictionary<string, string[]>();

        public static bool HasDatabase
        {
            get;
            private set;
        }

        public static int CurrentLanguage
        {
            get;
            set;
        }

        static LocalizationManager()
        {
            Languages = new ReadOnlyCollection<string>(new List<string>());
        }

        public static void Clear()
        {
            Languages = new ReadOnlyCollection<string>(new List<string>());
            mStringDatabase = new Dictionary<string, string[]>();
            HasDatabase = false;
        }

        public static void AddDatabase(string fileName, char delimiter)
        {

            RuntimeCsvRepresentation rcr;

            //char oldDelimiter = CsvFileManager.Delimiter;
            //CsvFileManager.Delimiter = delimiter;
            Dictionary<string, string[]> entryDictionary = new Dictionary<string, string[]>();

            CsvFileManager.CsvDeserializeDictionary<string, string[]>(fileName, entryDictionary, out rcr);
            //CsvFileManager.Delimiter = oldDelimiter;


            List<string> headerList = new List<string>();

            foreach (CsvHeader header in rcr.Headers)
            {
                headerList.Add(header.Name);
            }

            AddDatabase(entryDictionary, headerList);
        }

        public static void AddDatabase(Dictionary<string, string[]> entryDictionary, List<string> headerList)
        {
            Languages = new ReadOnlyCollection<string>(headerList);
            mStringDatabase = entryDictionary;
            HasDatabase = true;
        }

        public static string Translate(string stringID)
        {
            return TranslateForLanguage(stringID, CurrentLanguage);
        }

        public static string TranslateForLanguage(string stringID, int language)
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
                return stringID + " - UNTRANSLATED";
            }
        }

        private static bool ShouldExcludeFromTranslation(string stringID)
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
                stringID == "*" || stringID == "/" || stringID == "#" || stringID == ":")
            {
                return true;
            }
            return false;
        }

        private static bool IsTime(string stringID)
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

        private static bool IsPercentage(string stringID)
        {
            return stringID.CountOf('%') == 1 && stringID.Length > 1 && stringID.EndsWith("%") && StringFunctions.IsNumber(stringID.Substring(0, stringID.Length - 1));

        }

    }
}
