using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Renderables;
using Gum.Wireframe;
using GumRuntime;
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
using ToolsUtilities;

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

    public SystemManagers SystemManagers { get; private set; }


    public InteractiveGue Root { get; private set; } = new ContainerRuntime();

    public void Initialize(DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.V2)
    {
        InitializeInternal(
            gumProjectFile: null,
            systemManagers: SystemManagers.Default,
            defaultVisualsVersion: defaultVisualsVersion);
    }

    public GumProjectSave Initialize(string gumprojectFile)
    {
        return InitializeInternal(
            gumProjectFile: gumprojectFile,
            defaultVisualsVersion: DefaultVisualsVersion.V2)!;
    }

    bool hasBeenInitialized = false;

    GumProjectSave? InitializeInternal(string? gumProjectFile = null,
        SystemManagers? systemManagers = null,
        DefaultVisualsVersion defaultVisualsVersion = DefaultVisualsVersion.V2)
    {
        if (hasBeenInitialized)
        {
            throw new InvalidOperationException("Initialize has already been called once. It cannot be called again");
        }
        hasBeenInitialized = true;

        //_game = game;
        // RegisterRuntimeTypesThroughReflection();

        this.SystemManagers = systemManagers ?? new SystemManagers();

        if (systemManagers == null)
        {
            SystemManagers.Default = this.SystemManagers;
#if NET6_0_OR_GREATER
            ISystemManagers.Default = this.SystemManagers;
#endif
        }

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

        if (!string.IsNullOrEmpty(gumProjectFile))
        {

            gumProject = GumProjectSave.Load(gumProjectFile);
            ObjectFinder.Self.GumProjectSave = gumProject;
            gumProject.Initialize();
            Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();

            var absoluteFile = gumProjectFile;
            if (FileManager.IsRelative(absoluteFile))
            {
                absoluteFile = FileManager.MakeAbsolute(gumProjectFile);
            }

            var gumDirectory = FileManager.GetDirectory(absoluteFile);

            FileManager.RelativeDirectory = gumDirectory;

            ApplyStandardElementDefaults(gumProject);
        }

        return gumProject;
    }

    private void ApplyStandardElementDefaults(GumProjectSave gumProject)
    {
        var current = gumProject.StandardElements.Find(item => item.Name == "ColoredRectangle");
        ColoredRectangleRuntime.DefaultWidth = GetFloat("Width");
        ColoredRectangleRuntime.DefaultHeight = GetFloat("Height");

        current = gumProject.StandardElements.Find(item => item.Name == "NineSlice");

        // October 18, 2025 - this isn't functional in MonoGame either
        //NineSliceRuntime.DefaultSourceFile = GetString("SourceFile");

        float GetFloat(string variableName) => current.DefaultState.GetValueOrDefault<float>(variableName);
        string GetString(string varialbeName) => current.DefaultState.GetValueOrDefault<string>(varialbeName);
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
    public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave, SystemManagers? systemManagers = null)
    {
        return elementSave.ToGraphicalUiElement(systemManagers ?? SystemManagers.Default, addToManagers: false);
    }

}
