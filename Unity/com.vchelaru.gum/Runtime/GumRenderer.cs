#nullable enable
using System;
using System.Runtime.InteropServices;
using SkiaGameRendering.Unity;
using SkiaSharp;
using UnityEngine;
using UnityEngine.Rendering;

namespace Gum.Unity
{
    /// <summary>
    /// Initializes <see cref="GumService.Default"/> and draws it every frame into a texture the size of
    /// the screen. On Windows Direct3D 11 Gum draws on the GPU through SkiaGameRendering's
    /// <see cref="SkiaUnityRenderTarget"/>; anywhere else it draws into a CPU raster surface that is
    /// uploaded to a <see cref="Texture2D"/> each frame. Either way <see cref="Texture"/> has Unity's
    /// orientation (row 0 at the bottom). Add <see cref="GumInput"/> next to it for mouse and keyboard.
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

        [Tooltip("Uses the CPU fallback even where the GPU path is available.")]
        [SerializeField] bool _forceCpu;

        SkiaUnityRenderTarget? _gpuTarget;
        SKSurface? _cpuSurface;
        Texture2D? _cpuTexture;
        byte[]? _cpuFlipBuffer;
        Material? _premultipliedMaterial;

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

        /// <summary>Uses the CPU fallback even where the GPU path is available. Read in <c>Awake</c>.</summary>
        public bool ForceCpu
        {
            get => _forceCpu;
            set => _forceCpu = value;
        }

        /// <summary>True when Gum draws on the GPU through SkiaGameRendering, false on the CPU fallback.</summary>
        public bool IsUsingGpu => _gpuTarget != null;

        /// <summary>The width in pixels of the canvas Gum draws into.</summary>
        public int CanvasPixelWidth { get; private set; }

        /// <summary>The height in pixels of the canvas Gum draws into.</summary>
        public int CanvasPixelHeight { get; private set; }

        /// <summary>
        /// What Gum drew this frame, alpha-premultiplied, with row 0 at the bottom: a
        /// <see cref="RenderTexture"/> on the GPU path, a <see cref="Texture2D"/> on the CPU fallback.
        /// </summary>
        public Texture? Texture => _gpuTarget != null ? _gpuTarget.Texture : _cpuTexture;

        void Awake()
        {
            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);

            CreateTarget(width, height);

            string? projectPath = null;
            if (!string.IsNullOrEmpty(_projectFile))
            {
                GumStreamingAssets streamingAssets = new GumStreamingAssets();
                streamingAssets.Install();
                projectPath = streamingAssets.GetProjectPath(_projectFile);
            }

            GumService gum = GumService.Default;
            if (_gpuTarget != null)
            {
                // The GPU canvas records only between Begin and End, so Initialize gets a canvas that
                // is replaced every frame in LateUpdate.
                _gpuTarget.Begin();
                gum.Initialize(_gpuTarget.Canvas, width, height, projectPath);
                _gpuTarget.End();
            }
            else
            {
                gum.Initialize(_cpuSurface!.Canvas, width, height, projectPath);
            }

            gum.UseClipboard(new GumUnityClipboard());

            Shader? shader = Resources.Load<Shader>("GumPremultiplied");
            if (shader != null)
            {
                _premultipliedMaterial = new Material(shader);
            }
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
            if (width != CanvasPixelWidth || height != CanvasPixelHeight)
            {
                DisposeTarget();
                CreateTarget(width, height);
                gum.HandleResize(width, height);
            }

            gum.Update(Time.unscaledTimeAsDouble);
        }

        void LateUpdate()
        {
            GumService gum = GumService.Default;
            if (!gum.IsInitialized)
            {
                return;
            }

            if (_gpuTarget != null)
            {
                _gpuTarget.Begin();
                SKCanvas canvas = _gpuTarget.Canvas;
                gum.SystemManagers.Canvas = canvas;
                canvas.Clear(SKColors.Transparent);
                gum.Draw();
                _gpuTarget.End();
            }
            else if (_cpuSurface != null && _cpuTexture != null)
            {
                SKCanvas canvas = _cpuSurface.Canvas;
                gum.SystemManagers.Canvas = canvas;
                canvas.Clear(SKColors.Transparent);
                gum.Draw();
                canvas.Flush();
                UploadCpuSurface();
            }
        }

        void OnGUI()
        {
            Texture? texture = Texture;
            if (!_drawToScreen || texture == null || Event.current.type != EventType.Repaint)
            {
                return;
            }

            // The texture's row 0 is its bottom row, which GUI drawing puts at the bottom of the rect.
            var rect = new Rect(0, 0, Screen.width, Screen.height);
            if (_premultipliedMaterial != null)
            {
                UnityEngine.Graphics.DrawTexture(rect, texture, _premultipliedMaterial);
            }
            else
            {
                GUI.DrawTexture(rect, texture);
            }
        }

        void OnDestroy()
        {
            GumService gum = GumService.Default;
            if (gum.IsInitialized)
            {
                gum.Uninitialize();
            }
            DisposeTarget();
            if (_premultipliedMaterial != null)
            {
                Destroy(_premultipliedMaterial);
            }
        }

        void CreateTarget(int width, int height)
        {
            CanvasPixelWidth = width;
            CanvasPixelHeight = height;

            if (!_forceCpu && SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11)
            {
                try
                {
                    _gpuTarget = new SkiaUnityRenderTarget(width, height);
                    return;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Gum: SkiaGameRendering could not create a GPU target, using the CPU fallback. {exception.Message}");
                }
            }

            _cpuSurface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
            _cpuTexture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: false)
            {
                name = "Gum",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point,
            };
            _cpuFlipBuffer = new byte[width * height * 4];
            if (GumService.Default.IsInitialized)
            {
                GumService.Default.SystemManagers.Canvas = _cpuSurface.Canvas;
            }
        }

        // Skia's row 0 is the top row and Unity's is the bottom, so rows are copied in reverse.
        void UploadCpuSurface()
        {
            using (SKPixmap pixmap = _cpuSurface!.PeekPixels())
            {
                IntPtr pixels = pixmap.GetPixels();
                int rowBytes = pixmap.RowBytes;
                int packedRowBytes = CanvasPixelWidth * 4;
                for (int row = 0; row < CanvasPixelHeight; row++)
                {
                    IntPtr source = pixels + row * rowBytes;
                    int destinationOffset = (CanvasPixelHeight - 1 - row) * packedRowBytes;
                    Marshal.Copy(source, _cpuFlipBuffer!, destinationOffset, packedRowBytes);
                }
            }
            _cpuTexture!.LoadRawTextureData(_cpuFlipBuffer!);
            _cpuTexture.Apply(updateMipmaps: false);
        }

        void DisposeTarget()
        {
            _gpuTarget?.Dispose();
            _gpuTarget = null;
            _cpuSurface?.Dispose();
            _cpuSurface = null;
            if (_cpuTexture != null)
            {
                Destroy(_cpuTexture);
                _cpuTexture = null;
            }
            _cpuFlipBuffer = null;
        }
    }
}
