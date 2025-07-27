using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
using Gum.Reflection;
using Gum.ToolStates;
using RenderingLibrary.Graphics;
using SkiaGum.Renderables;
using SkiaPlugin.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.Commands;
using Gum.Services;
using Gum.Services.Dialogs;
using ToolsUtilities;

namespace SkiaPlugin
{
    [Export(typeof(PluginBase))]
    public class MainSvgPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "Skia Plugin";

        public override Version Version => new Version(1, 1);
        
        private readonly ISelectedState _selectedState;
        private readonly WireframeCommands _wireframeCommands;
        private readonly IDialogService _dialogService;

        #endregion

        public MainSvgPlugin()
        {
            _selectedState = Locator.GetRequiredService<ISelectedState>();
            _wireframeCommands = Locator.GetRequiredService<WireframeCommands>();
            _dialogService = Locator.GetRequiredService<IDialogService>();
        }
        
        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        public override void StartUp()
        {
            AssignEvents();

            AddMenuItems();

            RegisterEnumTypes();
        }

        private void RegisterEnumTypes()
        {
            TypeManager.Self.AddType(typeof(GradientType));
        }

        private void AddMenuItems()
        {
            var item = this.AddMenuItem(new List<string>() { "Plugins", "Add Skia Standard Elements" });
            item.Click += (not, used) =>
            {
                if(GumState.Self.ProjectState.NeedsToSaveProject)
                {
                    _dialogService.ShowMessage("You must first save your project before adding Skia Standard Elements");
                }
                else
                {
                    StandardAdder.AddAllStandards();
                    _guiCommands.RefreshElementTreeView();
                }
            };
        }

        private void AssignEvents()
        {
            GetDefaultStateForType += HandleGetDefaultStateForType;
            CreateRenderableForType += HandleCreateRenderbleFor;
            VariableExcluded += DefaultStateManager.GetIfVariableIsExcluded;
            VariableSet += DefaultStateManager.HandleVariableSet;
            ReactToFileChanged += HandleFileChanged;
            IsExtensionValid += HandleIsExtensionValid;
        }

        private void HandleFileChanged(FilePath filePath)
        {
            var isSvg = filePath.Extension == "svg";
            var currentElement = _selectedState.SelectedElement;

            ///////////////////Early Out///////////////////////
            if(!isSvg || currentElement == null)
            {
                return;
            }

            /////////////////End Early Out/////////////////////

            var referencedFiles = ObjectFinder.Self
                .GetFilesReferencedBy(currentElement)
                .Select(item => new FilePath(item))
                .ToList();

            if (referencedFiles.Contains(filePath))
            {
                _wireframeCommands.Refresh(true, true);
            }
        }

        private bool HandleIsExtensionValid(string arg1, ElementSave arg2, InstanceSave arg3, string arg4)
        {
            // for now blindly support .svg and .json
            return arg1 == "svg" || arg1 == "json";
        }

        private IRenderableIpso HandleCreateRenderbleFor(string type)
        {
            switch (type)
            {
                case "Arc": return new RenderableArc();
                case "Canvas": return new RenderableCanvas();

                case "ColoredCircle": return new RenderableCircle();
                case "LottieAnimation": return new RenderableLottieAnimation();
                case "RoundedRectangle": return new RenderableRoundedRectangle();
                case "Svg": return new RenderableSvg();
            }

            return null;
        }

        private StateSave HandleGetDefaultStateForType(string type)
        {
            switch(type)
            {
                case "Arc": return DefaultStateManager.GetArcState();
                case "Canvas": return DefaultStateManager.GetCanvasState();
                case "ColoredCircle": return DefaultStateManager.GetColoredCircleState();
                case "LottieAnimation": return DefaultStateManager.GetLottieAnimationState();
                case "RoundedRectangle": return DefaultStateManager.GetRoundedRectangleState();
                case "Svg": return DefaultStateManager.GetSvgState();
            }
            return null;
        }

    }
}
