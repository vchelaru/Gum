using Gum.Wireframe;
using SkiaGum.Renderables;

namespace SkiaGum.GueDeriving;

public class ContainerRuntime : BindableGue
{
    public ContainerRuntime(bool fullInstantiation = true)
    {
        if (fullInstantiation)
        {
            SetContainedObject(new RenderableShapeBase());
            Width = 150;
            Height = 150;
        }
    }

    // Gum codegen expects to override this on screens/components.
    public override void AfterFullCreation()
    {
        // Base hook intentionally empty.
        // Derived generated classes will assign named children + call CustomInitialize().
    }

    // Gum codegen expects this helper (or an extension providing it).
    public new GraphicalUiElement? GetGraphicalUiElementByName(string name)
        => FindByName(this, name);

    private static GraphicalUiElement? FindByName(GraphicalUiElement root, string name)
    {
        if (root.Name == name) return root;

        // Gum's GraphicalUiElement normally exposes Children; if differs,
        // adapt this one line and the rest stays clean.
        foreach (var child in root.Children)
        {
            var found = FindByName(child, name);
            if (found != null) return found;
        }

        return null;
    }

    public override GraphicalUiElement Clone()
        => (ContainerRuntime)base.Clone();
}
