using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Gum.DataTypes;
using Gum.PropertyGridHelpers;
using Gum.PropertyGridHelpers.Converters;
using ToolsUtilities;

namespace Gum.Gui.Controls
{
    public partial class GuideListDisplay : UserControl
    {
        #region Fields

        GumProjectSave mGumProjectSave;



        #endregion

        #region Properties


        public GumProjectSave GumProjectSave
        {
            get { return mGumProjectSave; }
            set
            {
                mGumProjectSave = value;
                UpdateToChangedGumProjectSave();
            }
        }


        #endregion

        #region Events

        public event EventHandler PropertyGridChanged;

        public event EventHandler NewGuideAdded;
        #endregion

        public GuideListDisplay()
        {
            InitializeComponent();




        }

        void UpdateToChangedGumProjectSave()
        {
            PopulateComboBox();
        }

        private void PopulateComboBox()
        {
            int selectedIndex = this.GuidesComboBox.SelectedIndex;

            this.GuidesComboBox.Items.Clear();

            // This could be null if the property is set in the designer.
            if (mGumProjectSave != null)
            {
                List<string> availableGuides = AvailableGuidesTypeConverter.GetAvailableValues(
                mGumProjectSave, true, true);

                foreach (string value in availableGuides)
                {
                    this.GuidesComboBox.Items.Add(value);
                }

                if (selectedIndex != -1 && selectedIndex < GuidesComboBox.Items.Count)
                {
                    this.GuidesComboBox.SelectedIndex = selectedIndex;
                }

                // If it's -1, that means that this may be the first
                // time that this UI was shown.
                // If there are more than 2 items (None and New Guide)
                // then we should select the first one
                if (selectedIndex == -1 && GuidesComboBox.Items.Count > 2)
                {
                    this.GuidesComboBox.SelectedIndex = 0;
                }
            }
        }

        private void GuidesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            string text = this.GuidesComboBox.Text;

            if (text == AvailableGuidesTypeConverter.NewGuideString)
            {
                AddNewGuide();
            }
            else
            {
                GuideRectangle rectangle = GetRectangleByName(text);

                GuideRectanglePropertyGridDisplayer displayer = new GuideRectanglePropertyGridDisplayer();
                displayer.GuideRectangle = rectangle;
                this.propertyGrid1.SelectedObject = displayer;
            }
        }

        private GuideRectangle GetRectangleByName(string name)
        {
            foreach (GuideRectangle rectangle in GumProjectSave.Guides)
            {
                if (rectangle.Name == name)
                {
                    return rectangle;
                }
            }
            return null;
        }

        private void AddNewGuide()
        {
            GuideRectangle namedRectangle = new GuideRectangle();
            namedRectangle.Name = "Guide";

            while(GetRectangleByName(namedRectangle.Name) != null)
            {
                namedRectangle.Name = StringFunctions.IncrementNumberAtEnd(namedRectangle.Name);
            }


            GumProjectSave.Guides.Add(namedRectangle);

            PopulateComboBox();

            GuidesComboBox.SelectedItem = namedRectangle.Name;

            if (NewGuideAdded != null)
            {
                NewGuideAdded(namedRectangle, null);
            }
        }

        private void propertyGrid1_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            string changedProperty = e.ChangedItem.Label;

            if (changedProperty == "Name")
            {
                PopulateComboBox();
            }

            if (PropertyGridChanged != null)
            {
                PropertyGridChanged(s, e);
            }
        }
    }
}
