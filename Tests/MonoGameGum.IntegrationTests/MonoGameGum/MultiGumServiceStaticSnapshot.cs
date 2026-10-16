#pragma warning disable CS0618 // DefaultFormsComponents is obsolete but still written by older registrations; the snapshot must carry it
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using ToolsUtilities;

namespace MonoGameGum.IntegrationTests.MonoGameGum;

/// <summary>
/// A hand-rolled copy of the process-wide state a service writes during Initialize, restorable
/// through today's public setters (spike for #6066). State with no setter is not captured.
/// </summary>
internal sealed class StaticSnapshot
{
    public SystemManagers? Managers { get; private set; }
    public ISystemManagers? InterfaceManagers { get; private set; }
    public IGumService? Service { get; private set; }
    public GumProjectSave? Project { get; private set; }
    public string RelativeDirectory { get; private set; } = "";
    public float CanvasWidth { get; private set; }
    public float CanvasHeight { get; private set; }
    public InteractiveGue? PopupRoot { get; private set; }
    public InteractiveGue? ModalRoot { get; private set; }
    public ICursor? Cursor { get; private set; }
    public IInputReceiverKeyboard? Keyboard { get; private set; }
    public Gum.Forms.DefaultVisuals.V3.Styling? Style { get; private set; }
    public IContentLoader? ContentLoader { get; private set; }
    public Func<string, ISystemManagers, IRenderable?>? CreateComponent { get; private set; }
    public Func<IRenderableIpso, GraphicalUiElement, string, object?, bool>? SetProperty { get; private set; }
    public Action<IRenderableIpso, ISystemManagers, Layer?>? AddRenderable { get; private set; }
    public Action<IRenderableIpso, ISystemManagers>? RemoveRenderable { get; private set; }
    public Action<IText, GraphicalUiElement>? UpdateFont { get; private set; }

    // Added for the Forms / theme spike.
    public Action<GraphicalUiElement>? InitialStateNotifier { get; private set; }
    public VisualOverBehavior VisualOver { get; private set; }
    public SpriteFont? DefaultTextFont { get; private set; }
    public BitmapFont? DefaultBitmap { get; private set; }
    public Texture2D? InvalidTexture { get; private set; }
    public List<IInputReceiverKeyboard> KeyboardsForUiControl { get; private set; } = new();
    public List<IGamePad> GamePadsForUiControl { get; private set; } = new();
    public Dictionary<Type, VisualTemplate> FormsTemplates { get; private set; } = new();
    public Dictionary<Type, Type> FormsComponents { get; private set; } = new();

    // The service this snapshot describes. Per-service statics are read and written with it current, because
    // "current" is whichever service was entered last.
    private Gum.GumService? _service;

    public static StaticSnapshot Capture(Gum.GumService? service = null)
    {
        // Reads the service's statics with it as the default, then puts the previous default back: a capture
        // must not change which service is current.
        IGumService? previousDefault = IGumService.Default;
        if (service != null) { global::RenderingLibrary.IGumService.Default = service; }
        try
        {
            return CaptureCurrent(service);
        }
        finally
        {
            if (service != null) { IGumService.Default = previousDefault; }
        }
    }

    private static StaticSnapshot CaptureCurrent(Gum.GumService? service)
    {
        return new StaticSnapshot
        {
            _service = service,
            Managers = SystemManagers.Default,
            InterfaceManagers = ISystemManagers.Default,
            Service = IGumService.Default,
            Project = Gum.Managers.ObjectFinder.Self.GumProjectSave,
            RelativeDirectory = FileManager.RelativeDirectory,
            CanvasWidth = GraphicalUiElement.CanvasWidth,
            CanvasHeight = GraphicalUiElement.CanvasHeight,
            PopupRoot = FrameworkElement.PopupRoot,
            ModalRoot = FrameworkElement.ModalRoot,
            Cursor = FormsUtilities.Cursor,
            Keyboard = FormsUtilities.Keyboard,
            Style = Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle,
            ContentLoader = LoaderManager.Self.ContentLoader,
            CreateComponent = ElementSaveExtensions.CustomCreateGraphicalComponentFunc,
            SetProperty = GraphicalUiElement.SetPropertyOnRenderable,
            AddRenderable = GraphicalUiElement.AddRenderableToManagers,
            RemoveRenderable = GraphicalUiElement.RemoveRenderableFromManagers,
            UpdateFont = GraphicalUiElement.UpdateFontFromProperties,
            InitialStateNotifier = ElementSaveExtensions.InitialStateAppliedNotifier,
            VisualOver = ICursor.VisualOverBehavior,
            DefaultTextFont = Text.DefaultFont,
            DefaultBitmap = Text.DefaultBitmapFont,
            InvalidTexture = Sprite.InvalidTexture,
            KeyboardsForUiControl = new List<IInputReceiverKeyboard>(FrameworkElement.KeyboardsForUiControl),
            GamePadsForUiControl = new List<IGamePad>(FrameworkElement.GamePadsForUiControl),
            FormsTemplates = new Dictionary<Type, VisualTemplate>(FrameworkElement.DefaultFormsTemplates),
            FormsComponents = new Dictionary<Type, Type>(FrameworkElement.DefaultFormsComponents),
        };
    }

    public void Restore()
    {
        if (_service != null) { global::RenderingLibrary.IGumService.Default = _service; }
        SystemManagers.Default = Managers!;
        ISystemManagers.Default = InterfaceManagers!;
        IGumService.Default = Service;
        Gum.Managers.ObjectFinder.Self.GumProjectSave = Project;
        FileManager.RelativeDirectory = RelativeDirectory;
        GraphicalUiElement.CanvasWidth = CanvasWidth;
        GraphicalUiElement.CanvasHeight = CanvasHeight;
        FrameworkElement.PopupRoot = PopupRoot!;
        FrameworkElement.ModalRoot = ModalRoot!;
        if (Cursor != null) { FormsUtilities.SetCursor(Cursor); }
        if (Keyboard != null) { FormsUtilities.SetKeyboard(Keyboard); }
        Gum.Forms.DefaultVisuals.V3.Styling.ActiveStyle = Style!;
        LoaderManager.Self.ContentLoader = ContentLoader!;
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = CreateComponent;
        GraphicalUiElement.SetPropertyOnRenderable = SetProperty!;
        GraphicalUiElement.AddRenderableToManagers = AddRenderable;
        GraphicalUiElement.RemoveRenderableFromManagers = RemoveRenderable;
        GraphicalUiElement.UpdateFontFromProperties = UpdateFont;

        ElementSaveExtensions.InitialStateAppliedNotifier = InitialStateNotifier;
        ICursor.VisualOverBehavior = VisualOver;
        Text.DefaultFont = DefaultTextFont;
        Text.DefaultBitmapFont = DefaultBitmap;
        Sprite.InvalidTexture = InvalidTexture;

        FrameworkElement.KeyboardsForUiControl.Clear();
        FrameworkElement.KeyboardsForUiControl.AddRange(KeyboardsForUiControl);
        FrameworkElement.GamePadsForUiControl.Clear();
        FrameworkElement.GamePadsForUiControl.AddRange(GamePadsForUiControl);

        // No setter; replace the contents in place.
        FrameworkElement.DefaultFormsTemplates.Clear();
        foreach (KeyValuePair<Type, VisualTemplate> pair in FormsTemplates)
        {
            FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value;
        }
        FrameworkElement.DefaultFormsComponents.Clear();
        foreach (KeyValuePair<Type, Type> pair in FormsComponents)
        {
            FrameworkElement.DefaultFormsComponents[pair.Key] = pair.Value;
        }
    }
}
