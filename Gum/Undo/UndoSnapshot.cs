using Gum.DataTypes;

namespace Gum.Undo
{
    public class UndoSnapshot
    {
        public ElementSave Element;
        public string CategoryName;
        public string StateName;

        public override string ToString()
        {
            // It would be nice to know what differed on this undo, but the way Gum works, it just takes a snapshot
            // of the entire element. This is lazy and performance isn't great, but it's also really easy to code against
            // and handles undos accurately. In the future we may add more info here if we want deeper diagnostics.
            var toReturn = $"{Element.Name} in {StateName ?? "<default>"}";

            if(!string.IsNullOrEmpty(CategoryName) )
            {
                toReturn += $" ({CategoryName})";
            }

            return toReturn;
        }
    }
}
