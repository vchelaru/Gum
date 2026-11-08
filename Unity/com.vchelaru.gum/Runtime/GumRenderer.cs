#nullable enable
using System;
using System.IO;
using SkiaGameRendering;
using SkiaGum.Helpers;
using SkiaSharp;
using UnityEngine;

namespace Gum.Unity
{
    /// <summary>
    /// Initializes <see cref="GumService.Default"/> and draws it every frame into a texture the size of
    /// the screen. Gum draws on the GPU through SkiaGameRendering's <see cref="SkiaRenderTarget2D"/>, and only
    /// there: on a graphics API SkiaGameRendering doesn't support (WebGL, for example) this component logs an
    /// error and disables itself. <see cref="Texture"/> has Unity's orientation (row 0 at the bottom). Add
    /// <see cref="GumInput"/> next to it for mouse and keyboard.
    ///
    /// Gum is initialized in <c>Awake</c>, so other scripts can build UI from <c>Start</c> on:
    /// <c>new Button().AddToRoot()</c>, or elements from the loaded project.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class GumRenderer : MonoBehaviour
    {
        [Tooltip("Optional .gumx/.gumj project, relative to StreamingAssets. Loaded through FileManager.CustomGetStreamFromFile.")]
        [SerializeField] string _projectFile = "";

        [Tooltip("Draws Gum over the whole screen in OnGUI. Turn off to show Texture yourself (a RawImage or a material).")]
        [SerializeField] bool _drawToScreen = true;

        SkiaTargetHolder<UnityTarget>? _target;

        /// <summary>The project file loaded on <c>Awake</c>, relative to StreamingAssets. Empty for none.</summary>
        public string ProjectFile
        {
            get => _projectFile;
            set => _projectFile = value;
        }

        /// <summary>Whether <c>OnGUI</c> draws <see cref="Texture"/> over the screen.</summary>
        public bool DrawToScreen
        {
            get => _drawToScreen;
            set => _drawToScreen = value;
        }

        /// <summary>The width in pixels of the canvas Gum draws into.</summary>
        public int CanvasPixelWidth { get; private set; }

        /// <summary>The height in pixels of the canvas Gum draws into.</summary>
        public int CanvasPixelHeight { get; private set; }

        /// <summary>What Gum drew this frame, alpha-premultiplied, with row 0 at the bottom. Null when the component disabled itself in <c>Awake</c>.</summary>
        public RenderTexture? Texture => _target?.Target?.Texture;

        void Awake()
        {
            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);

            GumService gum = GumService.Default;
            _target = new SkiaTargetHolder<UnityTarget>(
                (w, h) => new UnityTarget(new SkiaRenderTarget2D(w, h)),
                canvas => gum.SystemManagers.Canvas = canvas,
                gum.HandleResize);

            try
            {
                CreateTarget(width, height);
            }
            catch (NotSupportedException exception)
            {
                // SkiaGameRendering is GPU only. There is no CPU path to fall back to, so say so and stop.
                Debug.LogError($"Gum: SkiaGameRendering does not support the graphics API {SystemInfo.graphicsDeviceType}, so Gum cannot draw. {exception.Message}", this);
                enabled = false;
                return;
            }

            string? projectPath = null;
            if (!string.IsNullOrEmpty(_projectFile))
            {
                new GumStreamingAssets().Install();
                projectPath = Path.Combine(Application.streamingAssetsPath, _projectFile);
            }

            // The canvas records only between Begin and End, so Initialize gets a canvas that is
            // replaced every frame in LateUpdate.
            _target.Target!.Begin();
            gum.Initialize(_target.Canvas!, width, height, projectPath);
            _target.Target.End();

            gum.UseClipboard(new GumUnityClipboard());
        }

        void Update()
        {
            GumService gum = GumService.Default;
            if (!gum.IsInitialized)
            {
                return;
            }

            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            if (_target != null && _target.EnsureSize(width, height))
            {
                CanvasPixelWidth = width;
                CanvasPixelHeight = height;
            }

            gum.Update(Time.unscaledTimeAsDouble);
        }

        void LateUpdate()
        {
            GumService gum = GumService.Default;
            if (!gum.IsInitialized || _target == null)
            {
                return;
            }

            SKCanvas? canvas = _target.BeginFrame();
            if (canvas == null)
            {
                return;
            }
            canvas.Clear(SKColors.Transparent);
            gum.Draw();
            _target.Target!.End();
        }

        void OnGUI()
        {
            RenderTexture? texture = Texture;
            if (!_drawToScreen || texture == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            // The texture's row 0 is its bottom row, which GUI drawing puts at the bottom of the rect.
            var rect = new Rect(0, 0, Screen.width, Screen.height);
            UnityEngine.Graphics.DrawTexture(rect, texture, SkiaRenderTarget2D.PremultipliedGuiMaterial);
        }

        void OnDestroy()
        {
            GumService gum = GumService.Default;
            if (gum.IsInitialized)
            {
                gum.Uninitialize();
            }
            DisposeTarget();
        }

        void CreateTarget(int width, int height)
        {
            _target!.Create(width, height);
            CanvasPixelWidth = width;
            CanvasPixelHeight = height;
        }

        void DisposeTarget()
        {
            _target?.Dispose();
        }

        // SkiaRenderTarget2D has no End-without-drawing; ending a pass here only flushes into the texture.
        sealed class UnityTarget : ISkiaHostTarget
        {
            readonly SkiaRenderTarget2D _inner;

            public UnityTarget(SkiaRenderTarget2D inner) => _inner = inner;

            public SKCanvas Canvas => _inner.Canvas;
            public RenderTexture Texture => _inner.Texture;
            public void Begin() => _inner.Begin();
            public void End() => _inner.End();
            public void EndWithoutDrawing() => _inner.End();
            public void Dispose() => _inner.Dispose();
        }
    }
}
