using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.RenderingLibrary;
using GumDataTypes.Variables;

using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.ComponentModel;
using ToolsUtilitiesStandard.Helpers;
using MathHelper = ToolsUtilitiesStandard.Helpers.MathHelper;
using Vector2 = System.Numerics.Vector2;
using Vector3 = System.Numerics.Vector3;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Matrix = System.Numerics.Matrix4x4;
using GumRuntime;
using Gum.Collections;


#if !FRB
using Gum.StateAnimation.Runtime;
#endif

namespace Gum.Wireframe;

#region Enums

public enum MissingFileBehavior
{
    ConsumeSilently,
    ThrowException
}

public enum Anchor
{
    TopLeft,
    Top,
    TopRight,
    Left,
    Center,
    Right,
    BottomLeft,
    Bottom,
    BottomRight
}

public enum Dock
{
    Top,
    Left,
    Fill,
    Right,
    Bottom,
    FillHorizontally,
    FillVertically,
    SizeToChildren
}

#endregion

/// <summary>
/// The base object for all Gum runtime objects. It contains functionality for
/// setting variables, states, and performing layout. The GraphicalUiElement can
/// wrap an underlying rendering object.
/// GraphicalUiElements are also considered "Visuals" for Forms objects such as Button and TextBox.
/// </summary>
public partial class GraphicalUiElement : IRenderableIpso, IVisible, INotifyPropertyChanged
{




}



