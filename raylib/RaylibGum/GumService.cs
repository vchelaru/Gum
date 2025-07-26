using Gum.DataTypes;
using Gum.Wireframe;
using Gum.Renderables;
using Gum.GueDeriving;
using Raylib_cs;
using Gum.Forms;
using RaylibGum.Input;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaylibGum;
public class GumService
{
    #region Default
    static GumService _default;
    public static GumService Default
    {
        get
        {
            if (_default == null)
            {
                _default = new GumService();
            }
            return _default;
        }
    }

    #endregion

    public Cursor Cursor => FormsUtilities.Cursor;


    public InteractiveGue Root { get; private set; } = new ContainerRuntime();

    public void Initialize(DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.V2)
    {
        SystemManagers.Default = new SystemManagers();
        ISystemManagers.Default = SystemManagers.Default;
        SystemManagers.Default.Initialize();


        FormsUtilities.InitializeDefaults(defaultVisualsVersion: defaultVisualsVersion);

        Root.Width = 0;
        Root.WidthUnits = DimensionUnitType.RelativeToParent;
        Root.Height = 0;
        Root.HeightUnits = DimensionUnitType.RelativeToParent;
        Root.Name = "Main Root";

        Root.AddToManagers();

        GumProjectSave? gumProject = null;

        // todo - allow loading gum projects eventually
    }

    List<GraphicalUiElement> roots = new List<GraphicalUiElement>();
    public void Update(float seconds)
    {
        roots.Clear();
        roots.Add(Root);
        FormsUtilities.Update(seconds, roots);
    }

    public void Draw()
    {
        DrawGumRecursively(Root);
    }

    private static void DrawGumRecursively(GraphicalUiElement element)
    {
        var shouldDrawSelf = element.RenderableComponent is Sprite;

        element.Render(null);

        if(element.ClipsChildren)
        {
            var scissorX = (int)element.AbsoluteX;
            var scissorY = (int)element.AbsoluteY;
            var scissorWidth = (int)element.GetAbsoluteWidth();
            var scissorHeight = (int)element.GetAbsoluteHeight();
            Raylib.BeginScissorMode(scissorX, scissorY, scissorWidth, scissorHeight);
        }

        if (element.Children != null)
        {
            foreach (var child in element.Children)
            {
                if (child is GraphicalUiElement childGue)
                {
                    DrawGumRecursively(childGue);
                }
            }
        }

        if(element.ClipsChildren)
        {
            Raylib.EndScissorMode();
        }

    }
}

public static class GraphicalUiElementExtensionMethods
{
    public static void AddToRoot(this GraphicalUiElement element)
    {
        GumService.Default.Root.Children.Add(element);
    }

    public static void RemoveFromRoot(this GraphicalUiElement element)
    {
        element.Parent = null;
    }
}

public static class ElementSaveExtensionMethods
{
    //public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave)
    //{
    //    return elementSave.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
    //}

}
