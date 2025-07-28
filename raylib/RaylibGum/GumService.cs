using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Renderables;
using Gum.Wireframe;
using Raylib_cs;
using RaylibGum.Input;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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
        InitializeInternal(
            gumProjectFile: null,
            systemManagers: SystemManagers.Default,
            defaultVisualsVersion: defaultVisualsVersion);
    }

    bool hasBeenInitialized = false;

    void InitializeInternal(string? gumProjectFile = null,
        SystemManagers? systemManagers = null,
        DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.V1)
    {
        if (hasBeenInitialized)
        {
            throw new InvalidOperationException("Initialize has already been called once. It cannot be called again");
        }
        hasBeenInitialized = true;

        //_game = game;
        // RegisterRuntimeTypesThroughReflection();

        SystemManagers.Default = new SystemManagers();
        ISystemManagers.Default = SystemManagers.Default;
        SystemManagers.Default.Initialize();

        FormsUtilities.InitializeDefaults(defaultVisualsVersion: defaultVisualsVersion);

        Root.Width = 0;
        Root.WidthUnits = DimensionUnitType.RelativeToParent;
        Root.Height = 0;
        Root.HeightUnits = DimensionUnitType.RelativeToParent;
        Root.Name = "Main Root";
        Root.HasEvents = false;

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
        SystemManagers.Default.Draw();
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

    public static void AddToRoot(this FrameworkElement element)
    {
        GumService.Default.Root.Children.Add(element.Visual);
    }
}

public static class ElementSaveExtensionMethods
{
    //public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave)
    //{
    //    return elementSave.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
    //}

}
