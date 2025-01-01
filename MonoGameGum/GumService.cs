using Gum.DataTypes;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum.Forms;
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

    public GumProjectSave? Initialize(GraphicsDevice graphicsDevice, string? gumProjectFile = null)
    {
        RegisterRuntimeTypesThroughReflection();
        SystemManagers.Default = new SystemManagers();
#if NET6_0_OR_GREATER
        ISystemManagers.Default = SystemManagers.Default;
#endif
        SystemManagers.Default.Initialize(graphicsDevice, fullInstantiation: true);
        FormsUtilities.InitializeDefaults();

        GumProjectSave gumProject = null;

        if (!string.IsNullOrEmpty(gumProjectFile))
        {

            gumProject = GumProjectSave.Load(gumProjectFile);
            ObjectFinder.Self.GumProjectSave = gumProject;
            gumProject.Initialize();
            FormsUtilities.RegisterFromFileFormRuntimeDefaults();

            var gumDirectory = FileManager.GetDirectory(FileManager.MakeAbsolute(gumProjectFile));

            FileManager.RelativeDirectory = gumDirectory;
        }

        return gumProject;
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

    public void Update(Game game, GameTime gameTime)
    {
        FormsUtilities.Update(game, gameTime, (GraphicalUiElement)null);
        SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
    }

    public void Update(Game game, GameTime gameTime, GraphicalUiElement root)
    {
        FormsUtilities.Update(game, gameTime, root);
        SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
    }

    public void Update(Game game, GameTime gameTime, IEnumerable<GraphicalUiElement> roots)
    {
        FormsUtilities.Update(game, gameTime, roots);
        SystemManagers.Default.Activity(gameTime.TotalGameTime.TotalSeconds);
    }

    public void Draw()
    {
        SystemManagers.Default.Draw();
    }
}
