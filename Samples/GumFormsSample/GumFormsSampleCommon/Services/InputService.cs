using Microsoft.Xna.Framework.Input;
using MonoGameGum;
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
        public int Update()
        {
            var keyboard = GumService.Default.Keyboard;
            if (keyboard.KeyPushed(Keys.D0)) return 0;
            if (keyboard.KeyPushed(Keys.D1)) return 1;
            if (keyboard.KeyPushed(Keys.D2)) return 2;
            if (keyboard.KeyPushed(Keys.D3)) return 3;
            if (keyboard.KeyPushed(Keys.D4)) return 4;
            if (keyboard.KeyPushed(Keys.D5)) return 5;

            return -1;
        }
    }
}
