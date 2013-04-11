using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Gum.DataTypes;
using Gum.Managers;

namespace GumUnitTests
{
    [TestFixture]
    public class FindingObjectsUnitTest
    {
        ElementSaveTestCollection mTestCollection;

        [TestFixtureSetUp]
        public void Initialize()
        {
            mTestCollection = new ElementSaveTestCollection();
            mTestCollection.Initialize();
        }

        [Test]
        public void Test()
        {
            ElementSave textElementSave = ObjectFinder.Self.GetElementSave("Text");
            List<ElementSave> foundElements = ObjectFinder.Self.GetElementsReferencing(textElementSave);

            if (foundElements.Count == 0)
            {
                throw new Exception("Not finding elements that reference Text properly");
            }

            foundElements = ObjectFinder.Self.GetElementsReferencingRecursively(mTestCollection.Button);
            if (foundElements.Count == 0)
            {
                throw new Exception("Not finding elements that reference Button");
            }

            foundElements = ObjectFinder.Self.GetElementsReferencingRecursively(
                textElementSave);

            if (!foundElements.Contains(mTestCollection.Screen))
            {
                throw new Exception("Could not find the Screen as something that recursively reference Texts");
            }

        }
    }
}
