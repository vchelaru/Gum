using Gum.Collections;
using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.StateAnimation.Runtime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Wireframe
{
    public partial class GraphicalUiElement
    {
        #region IRenderable properties


        BlendState IRenderable.BlendState
        {
            get
            {
#if FULL_DIAGNOSTICS
                if (mContainedObjectAsIpso == null)
                {
                    throw new NullReferenceException("This GraphicalUiElemente has not had its visual set, so it does not have a blend operation. This can happen if a GraphicalUiElement was added as a child without its contained renderable having been set.");
                }
#endif
                return mContainedObjectAsIpso.BlendState;
            }
        }


        bool IRenderable.Wrap
        {
            get { return mContainedObjectAsIpso.Wrap; }
        }

        public virtual void Render(ISystemManagers managers)
        {
            mContainedObjectAsIpso.Render(managers);
        }

        public virtual string BatchKey => mContainedObjectAsIpso?.BatchKey ?? string.Empty;

        public virtual void StartBatch(ISystemManagers systemManagers) => mContainedObjectAsIpso?.StartBatch(systemManagers);
        public virtual void EndBatch(ISystemManagers systemManagers) => mContainedObjectAsIpso?.EndBatch(systemManagers);

        Layer? mLayer;

        public Layer? Layer => mLayer;

        #endregion
    }
}
