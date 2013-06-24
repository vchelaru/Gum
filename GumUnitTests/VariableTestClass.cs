using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Gum.DataTypes;
using Gum.Managers;
using Gum.RenderingLibrary;
using RenderingLibrary;
using Gum.DataTypes.Variables;
using Gum.Reflection;

namespace GumUnitTests
{
    [TestFixture]
    public class VariableTestClass
    {

        ElementSaveTestCollection mElementSaveCollection;


        [TestFixtureSetUp]
        public void Initialize()
        {
            OverallInitializer.Initialize();

            mElementSaveCollection = new ElementSaveTestCollection();
            mElementSaveCollection.Initialize();       
        }

        [Test]
        public void Test()
        {
            mElementSaveCollection.Button.DefaultState.SetValue("X", 10.0f, null);
            object result = mElementSaveCollection.Screen.DefaultState.GetValueRecursive("ButtonInstance.X");
            if ((float)result != 10.0f)
            {
                throw new Exception("GetValueRecursively is not working properly");
            }



            mElementSaveCollection.Button.DefaultState.SetValue("TextInstance.X", 9.0f, null);
            result = mElementSaveCollection.Screen.DefaultState.GetValueRecursive("ButtonInstance.TextInstance.X");
            if ((float)result != 9.0f)
            {
                throw new Exception("GetValueRecursively is not working when going 2 deep");
            }


            ElementSave textElement = ObjectFinder.Self.GetElementSave("Text");
            string prefix = Gum.RenderingLibrary.IPositionedSizedObjectExtensionMethods.GetQualifiedPrefixWithDot(
                mElementSaveCollection.TextIpsoInScreen, textElement, mElementSaveCollection.Screen);
            if (prefix != "ButtonInstance.TextInstance.")
            {
                throw new Exception("Getting the prefix for variables in the element inside a standard element should be null but isn't");
            }


            prefix = Gum.RenderingLibrary.IPositionedSizedObjectExtensionMethods.GetQualifiedPrefixWithDot(
                mElementSaveCollection.TextIpsoInButton, textElement, mElementSaveCollection.Button);
            if (prefix != "TextInstance.")
            {
                throw new Exception("Getting the prefix is not working properly for a Text object in a Button");
            }

            prefix = Gum.RenderingLibrary.IPositionedSizedObjectExtensionMethods.GetQualifiedPrefixWithDot(
                mElementSaveCollection.TextIpsoInText, textElement, textElement);
            if (!string.IsNullOrEmpty(prefix))
            {
                throw new Exception("Getting the prefix for variables in the element inside a standard element should be null but isn't");
            }


            object value = mElementSaveCollection.ButtonInstanceInScreen.GetValueFromThisOrBase(
                new List<ElementSave>(){mElementSaveCollection.Screen}, "ButtonText");

            if (value == null)
            {
                throw new Exception("Varibles are not properly digging deep when encountering a null exposed variable");
            }

            //prefix = Gum.RenderingLibrary.IPositionedSizedObjectExtensionMethods.GetQualifiedPrefixWithDot(
            //    mTextIpsoInButtonContainer, textElement, mButton);
            //if (value == "Whatever")
            //{
            //    throw new Exception();
            //}
        }


        [Test]
        public void TestTypes()
        {
            Type type = TypeManager.Self.GetTypeFromString("DimensionUnitType");

            if (type == null)
            {
                throw new Exception("Couldn't find the type for DimensionUnitType in the TypeManager");
            }

        }



    }
}
