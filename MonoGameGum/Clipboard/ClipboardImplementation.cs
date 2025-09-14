using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if !IOS
using TextCopy;
#endif

namespace Gum.Clipboard;

public class ClipboardImplementation
{
    public static string GetText()
    {
#if IOS
        return string.Empty;
#else
        return ClipboardService.GetText();
#endif
    }

    public static void PushStringToClipboard(string text)
    {
#if IOS
        // todo - do we care to fix this?
#else

        ClipboardService.SetText(text);
#endif

    }
}
