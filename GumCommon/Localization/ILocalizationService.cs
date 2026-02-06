using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Localization;
public interface ILocalizationService
{
    int CurrentLanguage { get; set; }

    void AddDatabase(Dictionary<string, string[]> entryDictionary, List<string> headerList);
    string Translate(string stringId);
}
