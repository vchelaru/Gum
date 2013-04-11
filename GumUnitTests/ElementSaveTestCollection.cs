using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.Managers;
using Gum.DataTypes.Variables;
using RenderingLibrary;

namespace GumUnitTests
{
    public class ElementSaveTestCollection
    {
        public ComponentSave Button
        {
            get;
            private set;
        }

        public ComponentSave ButtonContainer
        {
            get;
            private set;
        }

        public ScreenSave Screen
        {
            get;
            private set;
        }


        // These IPSOs represent the objects when the Screen is selected
        public IPositionedSizedObject TextIpsoInScreen { get; private set;}
        IPositionedSizedObject ButtonIpsoInScreen;
        public InstanceSave ButtonInstanceInScreen { get; private set; }

        // These IPSOs represent the objects when the container is selected (not the Screen)
        IPositionedSizedObject TextIpsoInButtonContainer;
        IPositionedSizedObject ButtonIpsoInButtonContainer;
        IPositionedSizedObject ButtonContainerIpsoInContainer;

        // These IPSOs represent the objects when the Button is selected (not the Screen)
        public IPositionedSizedObject TextIpsoInButton { get; private set; }
        IPositionedSizedObject ButtonIpsoInButton;

        // This IPSO represents the Text object when the Text is selected (the standard element type)
        public IPositionedSizedObject TextIpsoInText { get; private set; }


        public void Initialize()
        {
            CreateButtonComponent();

            CreateScreen();

            CreateButtonContainerComponent();

            AddContainersToProjectIfNotAlreadyAdded();

            CreateButtonIpsos();

            CreateContainerIpsos();

            CreateScreenIpsos();

        }

        private void AddContainersToProjectIfNotAlreadyAdded()
        {
            if (!ObjectFinder.Self.GumProjectSave.Screens.Contains(this.Screen))
            {
                ObjectFinder.Self.GumProjectSave.Screens.Add(Screen);

                ObjectFinder.Self.GumProjectSave.Components.Add(ButtonContainer);

                ObjectFinder.Self.GumProjectSave.Components.Add(Button);
            }
        }

        private void CreateScreenIpsos()
        {
            TextIpsoInScreen = new RenderingLibrary.Graphics.Text(null);
            TextIpsoInScreen.Name = "TextInstance";
            ButtonIpsoInScreen = new RenderingLibrary.Graphics.Sprite(null);
            ButtonIpsoInScreen.Name = "ButtonInstance";
            TextIpsoInScreen.Parent = ButtonIpsoInScreen;
        }

        private void CreateContainerIpsos()
        {
            ButtonContainerIpsoInContainer = new RenderingLibrary.Math.Geometry.LineRectangle();
            ButtonContainerIpsoInContainer.Name = "ButtonContainer";
            ButtonIpsoInButtonContainer = new RenderingLibrary.Graphics.Sprite(null);
            ButtonIpsoInButtonContainer.Name = "ButtonInstance";
            ButtonIpsoInButtonContainer.Parent = ButtonContainerIpsoInContainer;
            TextIpsoInButtonContainer = new RenderingLibrary.Graphics.Text(null);
            TextIpsoInButtonContainer.Name = "TextInstance";
            TextIpsoInButtonContainer.Parent = ButtonIpsoInButtonContainer;
        }

        private void CreateButtonIpsos()
        {
            // IPSOs in Button
            TextIpsoInButton = new RenderingLibrary.Graphics.Text(null);
            TextIpsoInButton.Name = "TextInstance";
            ButtonIpsoInButton = new RenderingLibrary.Graphics.Sprite(null);
            ButtonIpsoInButton.Name = "Button";
            TextIpsoInButton.Parent = ButtonIpsoInButton;

            TextIpsoInText = new RenderingLibrary.Graphics.Text(null);
            TextIpsoInText.Name = "Text";
        }
                


        private void CreateButtonComponent()
        {
            Button = new ComponentSave();
            Button.Name = "VariableTestButton";
            Button.BaseType = "Sprite";
            Button.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Sprite"));


            InstanceSave instance = new InstanceSave();
            instance.Name = "TextInstance";
            instance.BaseType = "Text";
            Button.Instances.Add(instance);

            // Set the varlue for the Text's Text property
            Button.DefaultState.SetValue(instance.Name + "." + "Text", "Hello");
            VariableSave variableSave = Button.DefaultState.GetVariableSave(instance.Name + "." + "Text");
            variableSave.ExposedAsName = "ButtonText";

        }

        private void CreateScreen()
        {
            Screen = new ScreenSave();
            Screen.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Screen"));
            Screen.Name = "VariableTestScreen";

            ButtonInstanceInScreen = new InstanceSave();
            ButtonInstanceInScreen.Name = "ButtonInstance";
            ButtonInstanceInScreen.BaseType = "VariableTestButton";
            Screen.Instances.Add(ButtonInstanceInScreen);

            Screen.DefaultState.SetValue("ButtonInstance.ButtonText", null);

        }



        private void CreateButtonContainerComponent()
        {
            ButtonContainer = new ComponentSave();
            ButtonContainer.Name = "ButtonContainer";
            ButtonContainer.BaseType = "Container";
            ButtonContainer.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Container"));

            InstanceSave buttonInContainer = new InstanceSave();
            buttonInContainer.Name = "ButtonInstance";
            buttonInContainer.BaseType = "Button";
            ButtonContainer.Instances.Add(buttonInContainer);


        }
    }
}
