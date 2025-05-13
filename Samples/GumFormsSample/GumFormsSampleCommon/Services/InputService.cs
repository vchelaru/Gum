using Microsoft.Xna.Framework.Input;
using MonoGameGum.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumFormsSample.Services
{
    public class InputService
    {
        private KeyboardState _previousState;

        public int Update()
        {
            var keyboardState = Keyboard.GetState();

            for (int i = 0; i <= 5; i++)
            {
                var key = (Keys)((int)Keys.D0 + i);
                if (keyboardState.IsKeyDown(key) && _previousState.IsKeyUp(key))
                {
                    switch (key)
                    {
                        case Keys.D0: return 0;
                        case Keys.D1: return 1;
                        case Keys.D2: return 2;
                        case Keys.D3: return 3;
                        case Keys.D4: return 4;
                        case Keys.D5: return 5;
                    }
                }
            }

            var cursor = FormsUtilities.Cursor;
            if (cursor.PrimaryPush)
            {
                // Handle mouse input
            }

            if (cursor.WindowOver != null)
            {
                // Optional: Log cursor info
            }

            _previousState = keyboardState;
            return -1;
        }
    }
}
