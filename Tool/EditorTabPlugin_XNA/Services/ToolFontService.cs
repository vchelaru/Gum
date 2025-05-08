using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Managers
{
    /// <summary>
    /// Manages fonts used by the app, as opposed to fonts used by the loaded project
    /// </summary>
    public class ToolFontService : Singleton<ToolFontService>
        // todo - eventually move to a service locator or full DI
    {
        BitmapFont _toolFont;
        public BitmapFont ToolFont
        {
            get => _toolFont;
        }
        public void Initialize()
        {
            // This is the plugin, which doesn't have the font...
            //var executingPath = Assembly.GetExecutingAssembly().Location;
            // Instead use Gum which is the entry assembly
            var executingPath = Assembly.GetEntryAssembly().Location;
            var directory = Path.GetDirectoryName(executingPath);

            var fntFilePath = Path.Combine(directory, "Content/Fonts/Font18Arial_o1.fnt");
            var font = new BitmapFont(fntFilePath, SystemManagers.Default);

            _toolFont = font;
        }
    }
}
