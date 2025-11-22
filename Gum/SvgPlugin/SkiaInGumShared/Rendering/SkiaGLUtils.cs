using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;
using System.Reflection;
using System.Runtime.InteropServices;
using static SkiaMonoGameRendering.GlConstants;
using static SkiaMonoGameRendering.GlWrapper;

namespace SkiaMonoGameRendering
{
    internal class GlConstants
    {
        public const int SDL_GL_SHARE_WITH_CURRENT_CONTEXT = 22;
        public const int GL_ALL_ATTRIB_BITS = 0xfffff;
        public const int GL_SAMPLES = 0x80a9;
        public const int GL_TEXTURE_BINDING_2D = 0x8069;

        internal enum RenderbufferTarget
        {
            Renderbuffer = 0x8D41,
            RenderbufferExt = 0x8D41,
        }

        internal enum FramebufferTarget
        {
            Framebuffer = 0x8D40,
            FramebufferExt = 0x8D40,
            ReadFramebuffer = 0x8CA8,
        }

        internal enum RenderbufferStorage
        {
            Rgba8 = 0x8058,
            DepthComponent16 = 0x81a5,
            DepthComponent24 = 0x81a6,
            Depth24Stencil8 = 0x88F0,
            // GLES Values
            DepthComponent24Oes = 0x81A6,
            Depth24Stencil8Oes = 0x88F0,
            StencilIndex8 = 0x8D48,
        }

        internal enum FramebufferAttachment
        {
            ColorAttachment0 = 0x8CE0,
            ColorAttachment0Ext = 0x8CE0,
            DepthAttachment = 0x8D00,
            StencilAttachment = 0x8D20,
            ColorAttachmentExt = 0x1800,
            DepthAttachementExt = 0x1801,
            StencilAttachmentExt = 0x1802,
        }

        internal enum TextureTarget
        {
            Texture2D = 0x0DE1,
            Texture3D = 0x806F,
            TextureCubeMap = 0x8513,
            TextureCubeMapPositiveX = 0x8515,
            TextureCubeMapPositiveY = 0x8517,
            TextureCubeMapPositiveZ = 0x8519,
            TextureCubeMapNegativeX = 0x8516,
            TextureCubeMapNegativeY = 0x8518,
            TextureCubeMapNegativeZ = 0x851A,
        }

        internal enum FramebufferErrorCode
        {
            FramebufferUndefined = 0x8219,
            FramebufferComplete = 0x8CD5,
            FramebufferCompleteExt = 0x8CD5,
            FramebufferIncompleteAttachment = 0x8CD6,
            FramebufferIncompleteAttachmentExt = 0x8CD6,
            FramebufferIncompleteMissingAttachment = 0x8CD7,
            FramebufferIncompleteMissingAttachmentExt = 0x8CD7,
            FramebufferIncompleteDimensionsExt = 0x8CD9,
            FramebufferIncompleteFormatsExt = 0x8CDA,
            FramebufferIncompleteDrawBuffer = 0x8CDB,
            FramebufferIncompleteDrawBufferExt = 0x8CDB,
            FramebufferIncompleteReadBuffer = 0x8CDC,
            FramebufferIncompleteReadBufferExt = 0x8CDC,
            FramebufferUnsupported = 0x8CDD,
            FramebufferUnsupportedExt = 0x8CDD,
            FramebufferIncompleteMultisample = 0x8D56,
            FramebufferIncompleteLayerTargets = 0x8DA8,
            FramebufferIncompleteLayerCount = 0x8DA9,
        }

        internal enum ErrorCode
        {
            NoError = 0,
        }
    }

    internal static class GlWrapper
    {
        private const CallingConvention callingConvention = CallingConvention.Winapi;

        // Native function attribute ported from MonoGame source
        [AttributeUsage(AttributeTargets.Delegate)]
        internal sealed class NativeFunctionWrapper : Attribute { }

        static FieldInfo _winHandleField;
        static PropertyInfo _contextProperty;

        static object _sdl_GL_GetCurrentContextValue;
        static MethodInfo _sdl_GL_GetCurrentContextMethod;

        static object _sdl_GL_CreateContextValue;
        static MethodInfo _sdl_GL_CreateContextMethod;

        static object _sdl_GL_SetAttributeValue;
        static MethodInfo _sdl_GL_SetAttributeMethod;

        static object _makeCurrentValue;
        static MethodInfo _makeCurrentMethod;

        static MethodInfo _loadFunctionMethod;

        static GlWrapper()
        {
            var monoGameAssembly = typeof(Texture2D).Assembly;
            var sdlGlType = monoGameAssembly.GetType("Sdl").GetNestedType("GL");
            var mgGlType = monoGameAssembly.GetType("MonoGame.OpenGL.GL");

            _winHandleField = monoGameAssembly.GetType("MonoGame.OpenGL.GraphicsContext").GetField("_winHandle", BindingFlags.NonPublic | BindingFlags.Instance);
            _contextProperty = monoGameAssembly.GetType("Microsoft.Xna.Framework.Graphics.GraphicsDevice").GetProperty("Context", BindingFlags.Instance | BindingFlags.NonPublic);

            var sdl_GL_GetCurrentContextField = sdlGlType.GetField("SDL_GL_GetCurrentContext", BindingFlags.NonPublic | BindingFlags.Static);
            _sdl_GL_GetCurrentContextValue = sdl_GL_GetCurrentContextField.GetValue(null);
            _sdl_GL_GetCurrentContextMethod = _sdl_GL_GetCurrentContextValue.GetType().GetMethod("Invoke");

            var sdl_GL_CreateContextField = sdlGlType.GetField("SDL_GL_CreateContext", BindingFlags.NonPublic | BindingFlags.Static);
            _sdl_GL_CreateContextValue = sdl_GL_CreateContextField.GetValue(null);
            _sdl_GL_CreateContextMethod = _sdl_GL_CreateContextValue.GetType().GetMethod("Invoke");

            var sdl_GL_SetAttributeField = sdlGlType.GetField("SDL_GL_SetAttribute", BindingFlags.NonPublic | BindingFlags.Static);
            _sdl_GL_SetAttributeValue = sdl_GL_SetAttributeField.GetValue(null);
            _sdl_GL_SetAttributeMethod = _sdl_GL_SetAttributeValue.GetType().GetMethod("Invoke");

            var makeCurrentField = sdlGlType.GetField("MakeCurrent", BindingFlags.Public | BindingFlags.Static);
            _makeCurrentValue = makeCurrentField.GetValue(null);
            _makeCurrentMethod = _makeCurrentValue.GetType().GetMethod("Invoke");

            _loadFunctionMethod = mgGlType.GetMethod("LoadFunction", BindingFlags.NonPublic | BindingFlags.Static);
        }

        internal static IntPtr GetMgWindowId()
        {
            var context = _contextProperty.GetValue(SkiaGlManager.GraphicsDevice);
            return (IntPtr)_winHandleField.GetValue(context);
        }

        internal static IntPtr SDL_GL_GetCurrentContext()
        {
            return (IntPtr)_sdl_GL_GetCurrentContextMethod.Invoke(_sdl_GL_GetCurrentContextValue, null);
        }

        internal static IntPtr SDL_GL_CreateContext(IntPtr window)
        {
            return (IntPtr)_sdl_GL_CreateContextMethod.Invoke(_sdl_GL_CreateContextValue, new object[] { window });
        }

        internal static int SDL_GL_SetAttribute(int attribute, int value)
        {
            return (int)_sdl_GL_SetAttributeMethod.Invoke(_sdl_GL_SetAttributeValue, new object[] { attribute, value });
        }

        // This allocates a little, we can make it a little quieter by reusing this object array:
        static object[] makeCurrentArray = new object[2];
        internal static int MakeCurrent(IntPtr window, IntPtr context)
        {
            makeCurrentArray[0] = window;
            makeCurrentArray[1] = context;
            return (int)_makeCurrentMethod.Invoke(_makeCurrentValue, makeCurrentArray);
        }

        internal static T LoadFunction<T>(string nativeMethodName)
        {
            var method = _loadFunctionMethod.MakeGenericMethod(new Type[] { typeof(T) });
            return (T)method.Invoke(null, new object[] { nativeMethodName, false });
        }

        /// <summary>
        /// OpenGL functions wrapper for the MonoGame context.
        /// </summary>
        internal static class MgGlFunctions
        {
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [UnmanagedFunctionPointer(callingConvention)]
            [NativeFunctionWrapper]
            internal unsafe delegate void GetIntegerDelegate(int param, [Out] int* data);
            internal static GetIntegerDelegate GetIntegerv;

            internal static void LoadFunctions()
            {
                GetIntegerv = LoadFunction<GetIntegerDelegate>("glGetIntegerv");
            }

            internal unsafe static void GetInteger(int name, out int value)
            {
                fixed (int* ptr = &value)
                {
                    GetIntegerv(name, ptr);
                }
            }
        }

        /// <summary>
        /// OpenGL functions wrapper for the Skia context.
        /// </summary>
        internal static class SkGlFunctions
        {
            [System.Security.SuppressUnmanagedCodeSecurity()]
            [UnmanagedFunctionPointer(callingConvention)]
            [NativeFunctionWrapper]
            internal delegate void GenRenderbuffersDelegate(int count, [Out] out int buffer);
            internal static GenRenderbuffersDelegate GenRenderbuffers;

            [System.Security.SuppressUnmanagedCodeSecurity()]
            [UnmanagedFunctionPointer(callingConvention)]
            [NativeFunctionWrapper]
            internal delegate void BindRenderbufferDelegate(RenderbufferTarget target, int buffer);
            internal static BindRenderbufferDelegate BindRenderbuffer;

            [System.Security.SuppressUnmanagedCodeSecurity()]
            [UnmanagedFunctionPointer(callingConvention)]
            [NativeFunctionWrapper]
            internal delegate void DeleteRenderbuffersDelegate(int count, [In][Out] ref int buffer);
            internal static DeleteRenderbuffersDelegate DeleteRenderbuffers;

            [System.Security.SuppressUnmanagedCodeSecurity()]
            [UnmanagedFunctionPointer(callingConvention)]
            [NativeFunctionWrapper]
            internal delegate void GenFramebuffersDelegate(int count, out int buffer);
            internal static GenFramebuffersDelegate GenFramebuffers;

            [System.Security.SuppressUnmanagedCodeSecurity()]
            [UnmanagedFunctionPointer(callingConvention)]
            [NativeFunctionWrapper]
            internal delegate void BindFramebufferDelegate(FramebufferTarget target, int buffer);
            internal static BindFramebufferDelegate BindFramebuffer;

            [System.Security.SuppressUnmanagedCodeSecurity()]
            [UnmanagedFunctionPointer(callingConvention)]
            [NativeFunctionWrapper]
            internal delegate void DeleteFramebuffersDelegate(int count, ref int buffer);
            internal static DeleteFramebuffersDelegate DeleteFramebuffers;

            [System.Security.SuppressUnmanagedCodeSecurity()]
            [UnmanagedFunctionPointer(callingConvention)]
            [NativeFunctionWrapper]
            public delegate void InvalidateFramebufferDelegate(FramebufferTarget target, int numAttachments, FramebufferAttachment[] attachments);
            public static InvalidateFramebufferDelegate InvalidateFramebuffer;

            [System.Security.SuppressUnmanagedCodeSecurity()]
            [UnmanagedFunctionPointer(callingConvention)]
            [NativeFunctionWrapper]
            internal delegate void FramebufferTexture2DDelegate(FramebufferTarget target, FramebufferAttachment attachement,
                TextureTarget textureTarget, int texture, int level);
            internal static FramebufferTexture2DDelegate FramebufferTexture2D;

            [System.Security.SuppressUnmanagedCodeSecurity()]
            [UnmanagedFunctionPointer(callingConvention)]
            [NativeFunctionWrapper]
            internal delegate void FramebufferRenderbufferDelegate(FramebufferTarget target, FramebufferAttachment attachement,
                RenderbufferTarget renderBufferTarget, int buffer);
            internal static FramebufferRenderbufferDelegate FramebufferRenderbuffer;

            [System.Security.SuppressUnmanagedCodeSecurity()]
            [UnmanagedFunctionPointer(callingConvention)]
            [NativeFunctionWrapper]
            public delegate void RenderbufferStorageDelegate(RenderbufferTarget target, RenderbufferStorage storage, int width, int hegiht);
            public static RenderbufferStorageDelegate RenderbufferStorage;

            [System.Security.SuppressUnmanagedCodeSecurity()]
            [UnmanagedFunctionPointer(callingConvention)]
            [NativeFunctionWrapper]
            internal delegate FramebufferErrorCode CheckFramebufferStatusDelegate(FramebufferTarget target);
            internal static CheckFramebufferStatusDelegate CheckFramebufferStatus;

            [System.Security.SuppressUnmanagedCodeSecurity()]
            [UnmanagedFunctionPointer(callingConvention)]
            [NativeFunctionWrapper]
            internal unsafe delegate void GetIntegerDelegate(int param, [Out] int* data);
            internal static GetIntegerDelegate GetIntegerv;

            internal static readonly FramebufferAttachment[] FramebufferAttachements = {
                FramebufferAttachment.ColorAttachment0,
                FramebufferAttachment.DepthAttachment,
                FramebufferAttachment.StencilAttachment,
            };

            internal static void LoadFunctions()
            {
                GenRenderbuffers = LoadFunction<GenRenderbuffersDelegate>("glGenRenderbuffers");
                BindRenderbuffer = LoadFunction<BindRenderbufferDelegate>("glBindRenderbuffer");
                DeleteRenderbuffers = LoadFunction<DeleteRenderbuffersDelegate>("glDeleteRenderbuffers");
                GenFramebuffers = LoadFunction<GenFramebuffersDelegate>("glGenFramebuffers");
                BindFramebuffer = LoadFunction<BindFramebufferDelegate>("glBindFramebuffer");
                DeleteFramebuffers = LoadFunction<DeleteFramebuffersDelegate>("glDeleteFramebuffers");
                InvalidateFramebuffer = LoadFunction<InvalidateFramebufferDelegate>("glInvalidateFramebuffer");
                FramebufferTexture2D = LoadFunction<FramebufferTexture2DDelegate>("glFramebufferTexture2D");
                FramebufferRenderbuffer = LoadFunction<FramebufferRenderbufferDelegate>("glFramebufferRenderbuffer");
                RenderbufferStorage = LoadFunction<RenderbufferStorageDelegate>("glRenderbufferStorage");
                CheckFramebufferStatus = LoadFunction<CheckFramebufferStatusDelegate>("glCheckFramebufferStatus");

                GetIntegerv = LoadFunction<GetIntegerDelegate>("glGetIntegerv");
            }

            internal unsafe static void GetInteger(int name, out int value)
            {
                fixed (int* ptr = &value)
                {
                    GetIntegerv(name, ptr);
                }
            }
        }
    }

    /// <summary>
    /// Manages contexts and loads all the needed functions for Skia drawing. You should 
    /// call Initialize() once when the game is created passing a valid GraphicsDevice.
    /// </summary>
    public static class SkiaGlManager
    {
        internal static GraphicsDevice GraphicsDevice { get; private set; }

        static bool _initialized;
        static IntPtr _windowId;
        static IntPtr _mgContextId;
        static IntPtr _skContextId;

        internal static GRContext SkiaGrContext { get; private set; }

        public static void Initialize(GraphicsDevice graphicsDevice)
        {
            if (_initialized)
                throw new InvalidOperationException("SkiaGlManager is already initialized.");

            GraphicsDevice = graphicsDevice;
            GraphicsDevice.DeviceReset += GraphicsDevice_DeviceReset;

            // Get the SDL window ID. We need it to create new contexts.
            _windowId = GetMgWindowId();

            // The MonoGame context is already created by the MG library. Here we get the ID.
            _mgContextId = SDL_GL_GetCurrentContext();

            // Load the MonoGame context functions that we will use
            MgGlFunctions.LoadFunctions();

            // This will tell OpenGL that the next context created will share objects with the main context
            var setAttributeResult = SDL_GL_SetAttribute(SDL_GL_SHARE_WITH_CURRENT_CONTEXT, 1);

            if (setAttributeResult < 0)
                throw new Exception("SDL_GL_SetAttribute failed.");

            // Create the alternate context for Skia
            _skContextId = SDL_GL_CreateContext(_windowId);

            if (_skContextId == IntPtr.Zero)
                throw new Exception("SDL_GL_CreateContext failed.");

            // Set the Skia context as current
            SetSkiaContextAsCurrent();

            // Load the Skia context functions that we will use
            SkGlFunctions.LoadFunctions();

            // Create the Skia context object that will be using the alternate OpenGL context
            SkiaGrContext = GRContext.CreateGl();

            // Now that everything has been set up make the default context current again so MonoGame runs normally
            SetMonoGameContextAsCurrent();

            _initialized = true;
        }

        private static void SetContextAsCurrent(IntPtr contextId)
        {
            var makeCurrentResult = MakeCurrent(_windowId, contextId);

            if (makeCurrentResult < 0)
                throw new Exception("SDL_GL_MakeCurrent failed.");
        }

        internal static void SetMonoGameContextAsCurrent()
        {
            SetContextAsCurrent(_mgContextId);
        }

        internal static void SetSkiaContextAsCurrent()
        {
            SetContextAsCurrent(_skContextId);
        }

        private static void GraphicsDevice_DeviceReset(object sender, EventArgs e)
        {
            // TODO: Is there something that needs to be done when the device is reset?
        }
    }
}
