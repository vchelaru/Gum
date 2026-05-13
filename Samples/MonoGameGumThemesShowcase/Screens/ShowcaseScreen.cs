using System.Collections.Generic;
using Gum.Forms;
using Gum.Forms.Controls;
using MonoGameGum;

namespace MonoGameGumThemesShowcase.Screens;

public abstract class ShowcaseScreen
{
    private readonly List<FrameworkElement> _rootElements = new();

    public abstract void Build();

    protected T AddToScreenRoot<T>(T element) where T : FrameworkElement
    {
        element.AddToRoot();
        _rootElements.Add(element);
        return element;
    }

    public virtual void Destroy()
    {
        foreach (var e in _rootElements)
        {
            e.RemoveFromRoot();
        }
        _rootElements.Clear();
    }
}
