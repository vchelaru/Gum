using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Gum.DataTypes.Variables;
using Gum.DataTypes;

namespace GumUnitTests
{
    


    [TestFixture]
    public class StateSaveTests
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
            StateSave defaultState = mElementSaveCollection.Screen.DefaultState;

            object value = defaultState.GetValueRecursive("ButtonInstance.X");
            if (value == null)
            {
                throw new Exception("StateSave.GetValueRecursive is not properly returning values");
            }

            VariableSave variableSave = defaultState.GetVariableRecursive("ButtonInstance.X");

            if (variableSave == null)
            {
                throw new Exception("StateSave.GetVariableSaveRecursively is not finding values");
            }
        }
    }
}
