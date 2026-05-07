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

    /// <summary>
    /// Raised when <see cref="CurrentLanguage"/> changes. Subscribers (such as
    /// <c>GumService</c>) use this to walk the live visual tree and re-translate
    /// already-instantiated text without the caller having to rebuild it.
    /// </summary>
    event Action? CurrentLanguageChanged;

    void AddDatabase(Dictionary<string, string[]> entryDictionary, List<string> headerList);
    void Clear();
    string Translate(string stringId);
}
