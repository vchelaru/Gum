using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms.DefaultVisuals;
using MonoGameGum.GueDeriving;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public  class ComboBoxTests : BaseTestClass
{
    [Fact]
    public void Visual_Assignment_ShouldSetVisualCorrectly()
    {
        var comboBox = new Gum.Forms.Controls.ComboBox();

        comboBox.Visual = new CGComboBox(tryCreateFormsObject:false);


    }



    public class CGComboBox : InteractiveGue
    {
        public DefaultListBoxRuntime ListBoxInstance;
        public RectangleRuntime FocusedIndicator { get; private set; }

        public CGComboBox(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                var background = new ColoredRectangleRuntime();
                background.Name = "Background";

                var TextInstance = new TextRuntime();
                TextInstance.Name = "TextInstance";

                ListBoxInstance = new DefaultListBoxRuntime(tryCreateFormsObject: false);
                ListBoxInstance.Name = "ListBoxInstance";


                background.Name = "Background";
                this.Children.Add(background);

                TextInstance.Text = "Selected Item";
                this.Children.Add(TextInstance);

                FocusedIndicator = new RectangleRuntime();
                FocusedIndicator.Name = "FocusedIndicator";
                this.Children.Add(FocusedIndicator);

                var rightSideText = new TextRuntime();
                rightSideText.Name = "DropdownIndicator";

                this.Children.Add(rightSideText);

                this.Children.Add(ListBoxInstance);
                ListBoxInstance.Visible = false;

                var comboBoxCategory = new StateSaveCategory();
                comboBoxCategory.Name = "ComboBoxCategory";
                this.AddCategory(comboBoxCategory);

                StateSave currentState;

                void AddState(string name)
                {
                    var state = new StateSave();
                    state.Name = name;
                    comboBoxCategory.States.Add(state);
                    currentState = state;
                }

                void AddVariable(string name, object value)
                {
                    currentState.Variables.Add(new VariableSave
                    {
                        Name = name,
                        Value = value
                    });
                }

                AddState(FrameworkElement.DisabledStateName);

                AddState(FrameworkElement.DisabledFocusedStateName);

                AddState(FrameworkElement.EnabledStateName);

                AddState(FrameworkElement.FocusedStateName);

                AddState(FrameworkElement.HighlightedStateName);

                AddState(FrameworkElement.HighlightedFocusedStateName);

                AddState(FrameworkElement.PushedStateName);

            }
            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new ComboBox(this);
            }
        }

        public ComboBox FormsControl => FormsControlAsObject as ComboBox;

    }
}





