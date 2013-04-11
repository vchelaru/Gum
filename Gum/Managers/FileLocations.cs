using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolsUtilities;

namespace Gum.Managers
{
    public class FileLocations
    {
        static FileLocations mSelf;

        public static FileLocations Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new FileLocations();
                }
                return mSelf;
            }
        }

        public string ScreensFolder
        {
            get
            {
                return
                    FileManager.GetDirectory(ObjectFinder.Self.GumProjectSave.FullFileName) + "Screens\\";

            }
        }

        public string ComponentsFolder
        {
            get
            {
                return
                    FileManager.GetDirectory(ObjectFinder.Self.GumProjectSave.FullFileName) + "Components\\";
            }
        }


    }
}
