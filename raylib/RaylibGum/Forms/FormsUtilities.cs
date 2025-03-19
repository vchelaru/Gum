using Gum.Wireframe;
using RaylibGum.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum.Forms;
public class FormsUtilities
{
    static Cursor cursor;
    public static Cursor Cursor => cursor;


    static List<GraphicalUiElement> innerList = new List<GraphicalUiElement>();

    internal static void InitializeDefaults()
    {
        cursor = new Cursor();

        //FrameworkElement.MainCursor = cursor;


        //FrameworkElement.PopupRoot = CreateFullscreenContainer(nameof(FrameworkElement.PopupRoot));
        //FrameworkElement.ModalRoot = CreateFullscreenContainer(nameof(FrameworkElement.ModalRoot));
    }


    public static void Update(float gameTime, IEnumerable<GraphicalUiElement> roots)
    {
        cursor.Activity(gameTime);

        innerList.Clear();
        innerList.AddRange(roots);

        GueInteractiveExtensionMethods.DoUiActivityRecursively(innerList, 
            cursor, 
            (IInputReceiverKeyboard)null, 
            gameTime);
    }
}
