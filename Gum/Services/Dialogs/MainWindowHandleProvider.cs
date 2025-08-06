using System;

namespace Gum.Services.Dialogs;

public interface IMainWindowHandleProvider
{
    IntPtr GetMainWindowHandle();
}

internal class MainFormWindowHandleProvider : IMainWindowHandleProvider
{
    public IntPtr GetMainWindowHandle() => Getter?.Invoke() ?? IntPtr.Zero;
    
    private Func<IntPtr>? Getter { get; set; }
    
    public void Initialize(Func<IntPtr> getMainWindowHandle)
    {
        Getter = getMainWindowHandle;
    }
}