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
            get => Get<bool>(); 
            set => Set(value); 
        }

        public Color OutlineColor
        {
            get => Get<Color>();
            set => Set(value);
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

        public Color CheckerboardColor1
        {
            get => Get<Color>();
            set => Set(value);
        }

        public Color CheckerboardColor2
        {
            get => Get<Color>();
            set => Set(value);
        }

        public string LocalizationFile
        {
            get => Get<string>();
            set => Set(value);
        }

        public int LanguageIndex
        {
            get => Get<int>();
            set => Set(value);
        }

        public bool IsUpdatingFromModel { get; private set; }

        public ProjectPropertiesViewModel()
        {
            OutlineColor = Microsoft.Xna.Framework.Color.White;
        }

        public void BindTo(GeneralSettingsFile generalSettings, GumProjectSave gumProject)
        {
            IsUpdatingFromModel = true;
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

            CheckerboardColor1 = new Color(generalSettings.CheckerColor1R, generalSettings.CheckerColor1G, generalSettings.CheckerColor1B);
            CheckerboardColor2 = new Color(generalSettings.CheckerColor2R, generalSettings.CheckerColor2G, generalSettings.CheckerColor2B);

            OutlineColor = new Color(generalSettings.OutlineColorR, generalSettings.OutlineColorG, generalSettings.OutlineColorB);

            LocalizationFile = this.gumProject.LocalizationFile;
            LanguageIndex = this.gumProject.CurrentLanguageIndex;
            IsUpdatingFromModel = false;

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

            generalSettings.CheckerColor1R = CheckerboardColor1.R;
            generalSettings.CheckerColor1G = CheckerboardColor1.G;
            generalSettings.CheckerColor1B = CheckerboardColor1.B;

            generalSettings.CheckerColor2R = CheckerboardColor2.R;
            generalSettings.CheckerColor2G = CheckerboardColor2.G;
            generalSettings.CheckerColor2B = CheckerboardColor2.B;

            generalSettings.OutlineColorR = OutlineColor.R;
            generalSettings.OutlineColorG = OutlineColor.G;
            generalSettings.OutlineColorB = OutlineColor.B;


            this.gumProject.LocalizationFile = LocalizationFile;
            this.gumProject.CurrentLanguageIndex = LanguageIndex;
        }


    }
}
