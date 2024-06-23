using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TextCopy;

namespace MonoGameGum.Clipboard
{
    internal class ClipboardImplementation
    {
        internal static string GetText()
        {
            return ClipboardService.GetText();
        }

        internal static void PushStringToClipboard(string text)
        {
            ClipboardService.SetText(text);
        }
    }
}
