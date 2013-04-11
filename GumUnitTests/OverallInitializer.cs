using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.Managers;
using Gum.Reflection;

namespace GumUnitTests
{
    public class OverallInitializer
    {
        public static void Initialize()
        {
            if (ObjectFinder.Self.GumProjectSave == null)
            {
                ObjectFinder.Self.GumProjectSave = new Gum.DataTypes.GumProjectSave();
                TypeManager.Self.Initialize();
                
                StandardElementsManager.Self.Initialize();
                StandardElementsManager.Self.PopulateProjectWithDefaultStandards(ObjectFinder.Self.GumProjectSave);
            }
        }
    }
}
