using System.Windows.Input;
using WpfCursor = System.Windows.Input.Cursor;

namespace InputLibrary
{
    /// <summary>
    /// Converts between <see cref="CursorKind"/> (Gum's platform-neutral cursor-icon enum) and
    /// <see cref="System.Windows.Input.Cursor"/>. The WPF counterpart to
    /// <see cref="CursorKindConverter"/>; kept separate so only the WPF-backed pieces of InputLibrary
    /// (<see cref="WpfInputHostAdapter"/>) need to know about the WPF cursor type.
    /// </summary>
    internal static class WpfCursorKindConverter
    {
        /// <summary>
        /// Maps a WPF cursor to the closest <see cref="CursorKind"/>. Any cursor that isn't one of
        /// the kinds Gum's editor sets (e.g. <see cref="Cursors.Wait"/>) falls back to
        /// <see cref="CursorKind.Arrow"/>.
        /// </summary>
        public static CursorKind ToCursorKind(WpfCursor cursor)
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
        /// Maps a <see cref="CursorKind"/> to the WPF cursor it represents.
        /// </summary>
        public static WpfCursor ToWpfCursor(CursorKind kind) => kind switch
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
