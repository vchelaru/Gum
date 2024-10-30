using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonoGameGum.Forms
{
    public class VisualTemplate
    {
        Func<object, GraphicalUiElement> creationFunc;


        public VisualTemplate(Type type)
        {
#if DEBUG
            if (typeof(GraphicalUiElement).IsAssignableFrom(type) == false)
            {
                throw new ArgumentException(
                    $"The type {type} must be derived from GraphicalUiElement (Gum Runtime usually)");
            }
#endif
            var constructor = type.GetConstructor(Type.EmptyTypes);

#if DEBUG
            if (constructor == null)
            {
                throw new ArgumentException(
                    $"The type {type} must have a constructor with no arguments");
            }
#endif

            Initialize((throwaway) => constructor.Invoke(null) as GraphicalUiElement);
        }

        public VisualTemplate(Func<GraphicalUiElement> creationFunc)
        {
            Initialize((throwaway) => creationFunc());
        }

        public VisualTemplate(Func<object, GraphicalUiElement> creationFunc)
        {
            Initialize(creationFunc);
        }


        private void Initialize(Func<object, GraphicalUiElement> creationFunc)
        {
            this.creationFunc = creationFunc;
        }

        public GraphicalUiElement CreateContent(object bindingContext)
        {
            return creationFunc(bindingContext);
        }
    }
}
