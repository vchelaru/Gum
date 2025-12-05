using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace StateAnimationPlugin.Managers
{
    public class BitmapLoader : Singleton<BitmapLoader>
    {
        public BitmapFrame LoadImage(string resourceName)
        {
            Assembly thisassembly = Assembly.GetExecutingAssembly();

            string fullName = "StateAnimationPlugin.Resources." + resourceName;
            using (System.IO.Stream imageStream =
                thisassembly.GetManifestResourceStream(fullName))
            {
                return BitmapFrame.Create(imageStream);
            }
        }
    }
}
