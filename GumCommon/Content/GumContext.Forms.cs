#nullable enable
using Gum.Forms;
using Gum.Input;
using Gum.Wireframe;
using System;
using System.Collections.Generic;

namespace Gum;

// Forms state a service owns. FrameworkElement and FormsUtilities keep their static members as facades:
// the context that adopts the process statics reads and writes the old static fields, and every other
// context reads and writes the members below.
public sealed partial class GumContext
{
    internal InteractiveGue? PopupRoot { get; set; }

    internal InteractiveGue? ModalRoot { get; set; }

    internal ICursor? MainCursor { get; set; }

    internal IInputReceiverKeyboard? MainKeyboard { get; set; }

    internal List<IGamePad> GamePadsForUiControl { get; } = new List<IGamePad>();

    internal List<IInputReceiverKeyboard> KeyboardsForUiControl { get; } = new List<IInputReceiverKeyboard>();

    internal Dictionary<Type, VisualTemplate> DefaultFormsTemplates { get; } = new Dictionary<Type, VisualTemplate>();

    internal Dictionary<Type, Type> DefaultFormsComponents { get; } = new Dictionary<Type, Type>();

    internal List<(InteractiveGue popupRoot, InteractiveGue modalRoot)> AdditionalPopupRootPairs { get; } =
        new List<(InteractiveGue, InteractiveGue)>();

    // FormsUtilities.Cursor / Keyboard / Gamepads, which are separate from MainCursor / MainKeyboard.
    internal ICursor? FormsCursor { get; set; }

    internal IInputReceiverKeyboard? FormsKeyboard { get; set; }

    internal GamePad[] Gamepads { get; set; } = new GamePad[4];

    internal VisualTemplate? DefaultTooltipTemplate { get; set; }
}
