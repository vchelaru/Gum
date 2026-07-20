using System.Windows.Forms;
using WinCursor = System.Windows.Forms.Cursor;

namespace InputLibrary
{
    /// <summary>
    /// Converts between <see cref="CursorKind"/> (Gum's platform-neutral cursor-icon enum) and
    /// <see cref="System.Windows.Forms.Cursor"/>. Kept separate from <see cref="IInputHostControl"/>
    /// so only the WinForms-backed pieces of InputLibrary (<see cref="ControlInputHostAdapter"/>,
    /// <see cref="Cursor"/>) need to know about the WinForms cursor type.
    /// </summary>
    internal static class CursorKindConverter
    {
        /// <summary>
        /// Maps a WinForms cursor to the closest <see cref="CursorKind"/>. Any cursor that isn't
        /// one of the kinds Gum's editor sets (e.g. <see cref="Cursors.Default"/>) falls back to
        /// <see cref="CursorKind.Arrow"/>.
        /// </summary>
        public static CursorKind ToCursorKind(WinCursor cursor)
        {
            if (cursor == Cursors.Cross)
            {
                return CursorKind.Cross;
            }
            if (cursor == Cursors.Hand)
            {
                return CursorKind.Hand;
            }
            if (cursor == Cursors.SizeAll)
            {
                return CursorKind.SizeAll;
            }
            if (cursor == Cursors.SizeNS)
            {
                return CursorKind.SizeNS;
            }
            if (cursor == Cursors.SizeWE)
            {
                return CursorKind.SizeWE;
            }
            if (cursor == Cursors.SizeNESW)
            {
                return CursorKind.SizeNESW;
            }
            if (cursor == Cursors.SizeNWSE)
            {
                return CursorKind.SizeNWSE;
            }
            return CursorKind.Arrow;
        }

        /// <summary>
        /// Maps a <see cref="CursorKind"/> to the WinForms cursor it represents.
        /// </summary>
        public static WinCursor ToWinCursor(CursorKind kind) => kind switch
        {
            CursorKind.Cross => Cursors.Cross,
            CursorKind.Hand => Cursors.Hand,
            CursorKind.SizeAll => Cursors.SizeAll,
            CursorKind.SizeNS => Cursors.SizeNS,
            CursorKind.SizeWE => Cursors.SizeWE,
            CursorKind.SizeNESW => Cursors.SizeNESW,
            CursorKind.SizeNWSE => Cursors.SizeNWSE,
            _ => Cursors.Arrow,
        };
    }
}
