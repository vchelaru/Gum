using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Gum.Wireframe;
using Gum.ToolStates;
using GumUnitTests.ToolStates;
using Gum.DataTypes;
using RenderingLibrary;

namespace GumUnitTests
{
    [TestFixture]
    public class WireframeUnitTests
    {

        ElementSaveTestCollection mElements;

        InstanceSave mTextInScreen;

        [TestFixtureSetUp]
        public void Initialize()
        {
            mElements = new ElementSaveTestCollection();
            mElements.Initialize();

            CreateTextInScreen();

            SelectedState.Self = new SelectedStateForTests();

        }

        private void CreateTextInScreen()
        {
            mTextInScreen = new InstanceSave();
            mTextInScreen.Name = mElements.Button.Instances[0].Name;
            mTextInScreen.BaseType = "Text";

            mElements.Screen.Instances.Add(mTextInScreen);
        }

        [Test]
        public void Test()
        {
            SelectedState.Self.SelectedElement = mElements.Screen;
            WireframeObjectManager.Self.RefreshAll(true,
                mElements.Screen);



            IPositionedSizedObject ipso =  WireframeObjectManager.Self.GetRepresentation(mTextInScreen);

            if (ipso == null)
            {
                throw new Exception("Could not find representation for the Text in Screen.");
            }

            if (ipso.Parent != null)
            {
                throw new Exception("The IPSO being returned is improper!  It shouldn't have a parent");
            }
        }
    }
}
