﻿using Gum.Converters;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;
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
        ComboBox comboBox = new ();

        comboBox.Visual = new CGComboBox(tryCreateFormsObject:false);
    }

    [Fact]
    public void Clicking_ShouldOpenComboBox_IfInBounds()
    {
        ComboBox comboBox = new ComboBox();

        comboBox.AddToRoot();

        comboBox.IsDropDownOpen.ShouldBe(false);

        Mock<ICursor> cursor = new();
        // combo boxes open on a push
        cursor.Setup(x => x.PrimaryPush).Returns(true);
        cursor.Setup(x => x.WindowPushed).Returns(comboBox.Visual);
        cursor.Setup(x => x.WindowOver).Returns(comboBox.Visual);
        Gum.Forms.FormsUtilities.SetCursor(cursor.Object);

        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        comboBox.IsDropDownOpen.ShouldBe(true);

        // Set it up so it's inside the window, but outside the bounds of the combo box
        cursor.Setup(x => x.X).Returns((int)(GraphicalUiElement.CanvasWidth-1));
        cursor.Setup(x => x.XRespectingGumZoomAndBounds()).Returns((int)(GraphicalUiElement.CanvasWidth - 1));
        cursor.Setup(x => x.WindowOver).Returns((InteractiveGue?)null);
        GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        comboBox.IsDropDownOpen.ShouldBe(false);

    }


    public class CGComboBox : InteractiveGue
    {
        public MonoGameGum.Forms.DefaultVisuals.DefaultListBoxRuntime ListBoxInstance;
        public RectangleRuntime FocusedIndicator { get; private set; }

        public CGComboBox(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(new InvisibleRenderable())
        {
            if (fullInstantiation)
            {
                var background = new ColoredRectangleRuntime();
                background.Name = "Background";

                var TextInstance = new TextRuntime();
                TextInstance.Name = "TextInstance";

                ListBoxInstance = new MonoGameGum.Forms.DefaultVisuals.DefaultListBoxRuntime(tryCreateFormsObject: false);
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