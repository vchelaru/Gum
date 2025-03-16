using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.Forms;
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace MonoGameGum;

public class GumService
{
    #region Default
    static GumService _default;
    public static GumService Default
    {
        get
        {
            if(_default == null)
            {
                _default = new GumService();
            }
            return _default;
        }
    }

    #endregion

    public GameTime GameTime { get; private set; }

    public Cursor Cursor => FormsUtilities.Cursor;

    public Keyboard Keyboard => FormsUtilities.Keyboard;

    public GamePad[] Gamepads => FormsUtilities.Gamepads;

    public InteractiveGue Root { get; private set; } = new ContainerRuntime();

    #region Initialize

    public GumProjectSave? Initialize(Game game, string? gumProjectFile = null)
    {
        return InitializeInternal(game, game.GraphicsDevice, gumProjectFile);
    }

    [Obsolete("Initialize passing Game as the first parameter rather than GraphicsDevice. Using this method does not support non-(EN-US) keyboard layouts, and " +
        "does not support ALT+numeric key codes for accents in TextBoxes. This method will be removed in future versions of Gum")]
    public GumProjectSave? Initialize(GraphicsDevice graphicsDevice, string? gumProjectFile = null)
    {
        return InitializeInternal(null, graphicsDevice, gumProjectFile);
    }

    GumProjectSave? InitializeInternal(Game game, GraphicsDevice graphicsDevice, string? gumProjectFile = null)
    { 
        RegisterRuntimeTypesThroughReflection();
        SystemManagers.Default = new SystemManagers();
#if NET6_0_OR_GREATER
        ISystemManagers.Default = SystemManagers.Default;
#endif
        SystemManagers.Default.Initialize(graphicsDevice, fullInstantiation: true);
        FormsUtilities.InitializeDefaults();

        Root.Width = 0;
        Root.WidthUnits = DimensionUnitType.RelativeToParent;
        Root.Height = 0;
        Root.HeightUnits = DimensionUnitType.RelativeToParent;
        Root.Name = "Main Root";

        Root.AddToManagers();

        GumProjectSave? gumProject = null;

        if (!string.IsNullOrEmpty(gumProjectFile))
        {

            gumProject = GumProjectSave.Load(gumProjectFile);
            ObjectFinder.Self.GumProjectSave = gumProject;
            gumProject.Initialize();
            FormsUtilities.RegisterFromFileFormRuntimeDefaults();

            var gumDirectory = FileManager.GetDirectory(FileManager.MakeAbsolute(gumProjectFile));

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
        NineSliceRuntime.DefaultSourceFile = GetString("SourceFile");

        float GetFloat (string variableName) => current.DefaultState.GetValueOrDefault<float>(variableName);
        string GetString(string varialbeName) => current.DefaultState.GetValueOrDefault<string>(varialbeName);
    }

    // In December 31, 2024 we moved to using ModuleInitializer 
    // More info: https://github.com/vchelaru/Gum/issues/275
    // Therefore, this is no longer needed. However, old projects
    // may still use this. Not sure when we can remove this, sometime
    // in the future....
    private void RegisterRuntimeTypesThroughReflection()
    {
        // Get the currently executing assembly
        Assembly executingAssembly = Assembly.GetEntryAssembly();

        // Get all types in the assembly
        var types = executingAssembly?.GetTypes();

        if(types != null)
        {
            foreach (Type type in types)
            {
                var method = type.GetMethod("RegisterRuntimeType", BindingFlags.Static | BindingFlags.Public);

                if (method != null)
                {
                    method.Invoke(null, null);
                }
            }
        }
    }

    #endregion

    #region Update

    public void Update(Game game, GameTime gameTime)
    {
        FormsUtilities.SetDimensionsToCanvas(this.Root);
        Update(game, gameTime, this.Root);

    }

    public void Update(Game game, GameTime gameTime, Forms.Controls.FrameworkElement root) =>
        Update(game, gameTime, root.Visual);

    public void Update(Game game, GameTime gameTime, GraphicalUiElement root)
    {
        GameTime = gameTime;
        FormsUtilities.Update(game, gameTime, root);
        SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
    }

    public void Update(Game game, GameTime gameTime, IEnumerable<GraphicalUiElement> roots)
    {
        GameTime = gameTime;
        FormsUtilities.Update(game, gameTime, roots);
        SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
    }

    #endregion

    #region Draw

    public void Draw()
    {
        SystemManagers.Default.Draw();
    }

    #endregion
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
    public static GraphicalUiElement ToGraphicalUiElement(this ElementSave elementSave)
    {
        return elementSave.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
    }

}