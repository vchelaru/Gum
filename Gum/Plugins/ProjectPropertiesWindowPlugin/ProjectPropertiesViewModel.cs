using Gum.DataTypes;
using Gum.Mvvm;
using Gum.Settings;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
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
            get => Get<bool>();
            set => Set(value); 
        }

        public int CanvasWidth
        {
            get => Get<int>();
            set => Set(value); 
        }

        public int CanvasHeight
        {
            get => Get<int>();
            set => Set(value);
        }

        public decimal DisplayDensity
        {
            get => Get<decimal>();
            set => Set(value);
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

        public Color CheckboardColor1
        {
            get => Get<Color>();
            set => Set(value);
        }

        public Color CheckboardColor2
        {
            get => Get<Color>();
            set => Set(value);
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

            CheckboardColor1 = new Color(generalSettings.CheckerColor1R, generalSettings.CheckerColor1G, generalSettings.CheckerColor1B);
            CheckboardColor2 = new Color(generalSettings.CheckerColor2R, generalSettings.CheckerColor2G, generalSettings.CheckerColor2B);



        }

        public void ApplyToModelObjects()
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

            generalSettings.CheckerColor1R = CheckboardColor1.R;
            generalSettings.CheckerColor1G = CheckboardColor1.G;
            generalSettings.CheckerColor1B = CheckboardColor1.B;

            generalSettings.CheckerColor2R = CheckboardColor2.R;
            generalSettings.CheckerColor2G = CheckboardColor2.G;
            generalSettings.CheckerColor2B = CheckboardColor2.B;

        }


    }
}
