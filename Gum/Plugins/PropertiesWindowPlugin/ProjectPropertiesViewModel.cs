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

        bool autoSave;
        public bool AutoSave
        {
            get { return autoSave; }
            set { base.SetProperty(ref autoSave, value); }
        }

        bool showOutlines;
        public bool ShowOutlines
        {
            get { return showOutlines; }
            set { base.SetProperty(ref showOutlines, value); }
        }

        bool restrictToUnitValues;
        public bool RestrictToUnitValues
        {
            get { return restrictToUnitValues; }
            set { base.SetProperty(ref restrictToUnitValues, value); }
        }

        int canvasHeight;
        public int CanvasHeight
        {
            get { return canvasHeight; }
            set { base.SetProperty(ref canvasHeight, value); }
        }

        int canvasWidth;
        public int CanvasWidth
        {
            get { return canvasWidth; }
            set { base.SetProperty(ref canvasWidth, value); }
        }

        bool restrictFileNamesToAndroidAssets;
        public bool RestrictFileNamesForAndroid
        {
            get { return restrictFileNamesToAndroidAssets; }
            set { base.SetProperty(ref restrictFileNamesToAndroidAssets, value); }
        }

        bool renderTextCharacterByCharacter;
        public bool RenderTextCharacterByCharacter
        {
            get
            {
                return renderTextCharacterByCharacter;
            }
            set
            {
                base.SetProperty(ref renderTextCharacterByCharacter, value);
            }
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
