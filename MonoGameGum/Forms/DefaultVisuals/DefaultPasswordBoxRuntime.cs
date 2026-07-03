using Gum.Forms.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if FRB
namespace MonoGameGum.Forms.DefaultVisuals
#else
namespace Gum.Forms.DefaultVisuals
#endif
{
    [Obsolete("Legacy V1 default visual. Use the V3 visuals via DefaultVisualsVersion.V3/.Newest; the V1 default visuals are slated for removal in a future release.")]
    public class DefaultPasswordBoxRuntime : DefaultTextBoxBaseRuntime
    {
        protected override string CategoryName => "PasswordBoxCategory";

        public DefaultPasswordBoxRuntime(bool fullInstantiation = true, bool tryCreateFormsObject = true) : base(fullInstantiation, tryCreateFormsObject)
        {
            this.HasEvents = true;

            if (tryCreateFormsObject)
            {
                FormsControlAsObject = new PasswordBox(this);
            }
        }

        public PasswordBox FormsControl => FormsControlAsObject as PasswordBox;
    }
}
