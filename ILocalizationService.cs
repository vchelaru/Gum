using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Intentionally frozen duplicate of GumCommon/Localization/ILocalizationService.cs, referenced only
// by GumCoreShared.projitems (FRB1's build). Do not sync this to the real interface's current shape:
// FRB1's own implementation (FlatRedBall repo, FRBDK/Glue/GumPlugin/GumPlugin/Embedded/LocalizationManagerWrapper.cs)
// only implements Translate and would fail to compile against CurrentLanguage/Languages/CurrentLanguageChanged/
// AddDatabase/Clear. Updating FRB1's wrapper to the full interface is separate, cross-repo work.
namespace Gum.Localization;
public interface ILocalizationService
{
    string Translate(string stringId);
}
