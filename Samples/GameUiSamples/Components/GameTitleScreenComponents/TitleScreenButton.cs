using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using MonoGameGum.Forms.Controls;
using MonoGameGum.Forms;
using MonoGameGum;
namespace GameUiSamples.Components;

partial class TitleScreenButton
{
    partial void CustomInitialize()
    {
        Click += (_, _) => IsFocused = true;

    }

}
