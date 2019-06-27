using Gum.DataTypes;
using Gum.Mvvm;
using Gum.Settings;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.PropertiesWindowPlugin
{
    public class ProjectPropertiesViewModel : ViewModel
    {

        GeneralSettingsFile generalSettings;
        GumProjectSave gumProject;

        public bool AutoSave
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public bool ShowOutlines
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public bool RestrictToUnitValues
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public int CanvasWidth
        {
            get { return Get<int>(); }
            set { Set(value); }
        }

        public int CanvasHeight
        {
            get { return Get<int>(); }
            set { Set(value); }
        }

        public bool RestrictFileNamesForAndroid
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public bool RenderTextCharacterByCharacter
        {
            get { return Get<bool>(); }
            set { Set(value); }
        }

        public void BindTo(GeneralSettingsFile generalSettings, GumProjectSave gumProject)
        {
            this.generalSettings = generalSettings;
            this.gumProject = gumProject;

            AutoSave = this.generalSettings.AutoSave;
            ShowOutlines = this.gumProject.ShowOutlines;
            RestrictToUnitValues = this.gumProject.RestrictToUnitValues;
            CanvasHeight = this.gumProject.DefaultCanvasHeight;
            CanvasWidth = this.gumProject.DefaultCanvasWidth;
            RestrictFileNamesForAndroid = this.gumProject.RestrictFileNamesForAndroid;
            RenderTextCharacterByCharacter = global::RenderingLibrary.Graphics.Text.TextRenderingMode ==
                global::RenderingLibrary.Graphics.TextRenderingMode.CharacterByCharacter;

        }

        public void ApplyToBoundObjects()
        {
            this.generalSettings.AutoSave = AutoSave;
            this.gumProject.ShowOutlines = ShowOutlines;
            GraphicalUiElement.ShowLineRectangles = ShowOutlines;
            GraphicalUiElement.CanvasWidth = CanvasWidth;
            GraphicalUiElement.CanvasHeight = CanvasHeight;


            this.gumProject.RestrictToUnitValues = RestrictToUnitValues;
            this.gumProject.DefaultCanvasHeight = CanvasHeight;
            this.gumProject.DefaultCanvasWidth = CanvasWidth;
            this.gumProject.RestrictFileNamesForAndroid = RestrictFileNamesForAndroid;
            if(RenderTextCharacterByCharacter)
            {
                global::RenderingLibrary.Graphics.Text.TextRenderingMode =
                    global::RenderingLibrary.Graphics.TextRenderingMode.CharacterByCharacter;
            }
            else
            {
                global::RenderingLibrary.Graphics.Text.TextRenderingMode =
                    global::RenderingLibrary.Graphics.TextRenderingMode.RenderTarget;
            }
        }


    }
}
