using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Managers
{
    public class GraphicalUiElementManager
    {
        List<GraphicalUiElement> GraphicalUiElements;

        public GraphicalUiElementManager()
        {
            GraphicalUiElements = new List<GraphicalUiElement>();
        }

        public void Add(GraphicalUiElement graphicalUiElement)
        {
            GraphicalUiElements.Add(graphicalUiElement);
        }

        public void Remove(GraphicalUiElement graphicalUiElement)
        {
            GraphicalUiElements.Add(graphicalUiElement);
        }

        public void Activity()
        {
            var count = GraphicalUiElements.Count;

            for(int i = 0; i < count; i++)
            {
                GraphicalUiElements[i].AnimateSelf();
            }
        }
    }
}
