using Gum.Converters;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;

using RenderingLibrary.Graphics;

using System.Linq;

using MonoGameGum.GueDeriving;
using System;
using KniGumFromFile;
partial class ScreenChangingComponentRuntime : ContainerRuntime
{
    partial void CustomInitialize()
    {
        ButtonScreen1.FormsControl.Click += (_, _) =>
            KniGumFromFileGame.Self.SwitchToScreen1Based(1);
        ButtonScreen2.FormsControl.Click += (_, _) =>
            KniGumFromFileGame.Self.SwitchToScreen1Based(2);
        ButtonScreen3.FormsControl.Click += (_, _) =>
            KniGumFromFileGame.Self.SwitchToScreen1Based(3);
        ButtonScreen4.FormsControl.Click += (_, _) =>
            KniGumFromFileGame.Self.SwitchToScreen1Based(4);
        ButtonScreen5.FormsControl.Click += (_, _) =>
            KniGumFromFileGame.Self.SwitchToScreen1Based(5);
        ButtonScreen6.FormsControl.Click += (_, _) =>
            KniGumFromFileGame.Self.SwitchToScreen1Based(6);
        ButtonScreen7.FormsControl.Click += (_, _) =>
            KniGumFromFileGame.Self.SwitchToScreen1Based(7);
        ButtonScreen8.FormsControl.Click += (_, _) =>
            KniGumFromFileGame.Self.SwitchToScreen1Based(8);
    }
}
