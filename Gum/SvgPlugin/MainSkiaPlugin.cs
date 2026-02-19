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
    public class MainSkiaPlugin : PluginBase
    {
        #region Fields/Properties

        public override string FriendlyName => "Skia Plugin";

        public override Version Version => new Version(1, 1);
        
        private readonly ISelectedState _selectedState;
        private readonly WireframeCommands _wireframeCommands;
        private readonly IDialogService _dialogService;

        #endregion

        public MainSkiaPlugin()
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

            RegisterSkiaPropertyRedirect();

            DefaultStateManager.UpdateDisplayersForStandards();
        }

        private void RegisterSkiaPropertyRedirect()
        {
            Gum.Wireframe.CustomSetPropertyOnRenderable.AdditionalPropertyOnRenderable =
                (renderable, gue, propertyName, value) =>
                {
                    if (renderable is not SkiaTexturedRenderable skiaRenderable)
                        return false;

                    // If the wrapper itself has a writable property, let normal reflection handle it.
                    var wrapperProp = skiaRenderable.GetType().GetProperty(propertyName);
                    if (wrapperProp != null && wrapperProp.CanWrite)
                        return false;

                    // For shape adapters, target the underlying shape so reflection finds
                    // shape-specific properties (e.g. StartAngle, CornerRadius, IsFilled).
                    object drawableTarget = skiaRenderable.Drawable;
                    if (drawableTarget is RenderableShapeAdapter shapeAdapter)
                        drawableTarget = shapeAdapter.Shape;

                    var drawableProp = drawableTarget.GetType().GetProperty(propertyName);
                    if (drawableProp == null || !drawableProp.CanWrite)
                        return false;

                    var valueType = value.GetType();
                    if (valueType != drawableProp.PropertyType)
                    {
                        if (valueType == typeof(Gum.Managers.PositionUnitType) &&
                            drawableProp.PropertyType == typeof(Gum.Converters.GeneralUnitType))
                        {
                            value = Gum.Converters.UnitConverter.ConvertToGeneralUnit(
                                (Gum.Managers.PositionUnitType)value);
                        }
                        else
                        {
                            value = System.Convert.ChangeType(value, drawableProp.PropertyType);
                        }
                    }

                    drawableProp.SetValue(drawableTarget, value, null);
                    return true;
                };
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
                var projectState = Locator.GetRequiredService<IProjectState>();
                if(projectState.NeedsToSaveProject)
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
                case "Arc": return new SkiaTexturedRenderable(new RenderableShapeAdapter(new Arc()));
                case "Canvas": return new SkiaTexturedRenderable(new RenderableCanvas());

                case "ColoredCircle": return new SkiaTexturedRenderable(new RenderableShapeAdapter(new Circle()));
                case "LottieAnimation": return new SkiaTexturedRenderable(new RenderableLottieAnimation());
                case "RoundedRectangle": return new SkiaTexturedRenderable(new RenderableShapeAdapter(new RoundedRectangle()));
                case "Svg": return new SkiaTexturedRenderable(new RenderableSvg());
            }

            return null;
        }

        private StateSave HandleGetDefaultStateForType(string type)
        {
            switch(type)
            {
                case "Arc": return StandardElementsManager.GetArcState();
                case "Canvas": return DefaultStateManager.GetCanvasState();
                case "ColoredCircle": return StandardElementsManager.GetColoredCircleState();
                case "LottieAnimation": return DefaultStateManager.GetLottieAnimationState();
                case "RoundedRectangle": return StandardElementsManager.GetRoundedRectangleState();
                case "Svg": return DefaultStateManager.GetSvgState();
            }
            return null;
        }

    }
}
