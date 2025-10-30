using System.Windows.Forms;

namespace Gum.Extensions;

public static class DragEventArgsExt
{
    public static T? GetData<T>(this DragEventArgs args) where T : class
    {
        return args.Data.GetData(typeof(T)) as T;
    }

    public static bool HasData<T>(this DragEventArgs args) where T : class
    {
        return args.Data.GetDataPresent(typeof(T));
    }
}