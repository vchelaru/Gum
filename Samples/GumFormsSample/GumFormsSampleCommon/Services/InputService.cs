using MonoGameGum.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.Services
{
    internal class InputService
    {
        public void Update()
        {
            var cursor = FormsUtilities.Cursor;
            if (cursor.PrimaryPush)
            {
                // Handle input
            }

            if (cursor.WindowOver != null)
            {
                // Optional: Log cursor info if needed
            }
        }
    }
}
