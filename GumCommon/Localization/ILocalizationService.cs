using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Localization;
public interface ILocalizationService
{
    int CurrentLanguage { get; set; }
    IReadOnlyList<string> Languages { get; }

    void AddDatabase(Dictionary<string, string[]> entryDictionary, List<string> headerList);
    void Clear();
    string Translate(string stringId);
}
