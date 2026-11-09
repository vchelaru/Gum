using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Gum;

/// <summary>
/// Draws a GL texture (premultiplied alpha, row 0 at the top) over the default framebuffer with alpha blending,
/// so whatever the game already drew shows through where Gum drew nothing. One full-screen triangle, no buffers.
/// </summary>
internal sealed unsafe class GlBlit : IDisposable
{
    private const int GL_FRAMEBUFFER = 0x8D40, GL_TEXTURE_2D = 0x0DE1, GL_TEXTURE0 = 0x84C0, GL_TRIANGLES = 0x0004;
    private const int GL_VERTEX_SHADER = 0x8B31, GL_FRAGMENT_SHADER = 0x8B30;
    private const int GL_BLEND = 0x0BE2, GL_DEPTH_TEST = 0x0B71, GL_SCISSOR_TEST = 0x0C11, GL_CULL_FACE = 0x0B44, GL_STENCIL_TEST = 0x0B90;
    private const int GL_ONE = 1, GL_ONE_MINUS_SRC_ALPHA = 0x0303, GL_FUNC_ADD = 0x8006;
    private const int GL_CURRENT_PROGRAM = 0x8B8D, GL_VERTEX_ARRAY_BINDING = 0x85B5, GL_TEXTURE_BINDING_2D = 0x8069, GL_ACTIVE_TEXTURE = 0x84E0;
    private const int GL_VIEWPORT = 0x0BA2, GL_COLOR_WRITEMASK = 0x0C23;
    private const int GL_BLEND_SRC_RGB = 0x80C9, GL_BLEND_DST_RGB = 0x80C8, GL_BLEND_SRC_ALPHA = 0x80CB, GL_BLEND_DST_ALPHA = 0x80CA;
    private const int GL_BLEND_EQUATION_RGB = 0x8009, GL_BLEND_EQUATION_ALPHA = 0x883D;
    private const int GL_COMPILE_STATUS = 0x8B81, GL_LINK_STATUS = 0x8B82;

    private const string VertexShader =
        "#version 330 core\n" +
        "void main() {\n" +
        "    vec2 p = vec2((gl_VertexID << 1) & 2, gl_VertexID & 2);\n" +
        "    gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);\n" +
        "}\n";

    // texelFetch ignores the texture's filter state, which Skia owns. Row 0 of the texture is the top of the image.
    private const string FragmentShader =
        "#version 330 core\n" +
        "uniform sampler2D tex;\n" +
        "out vec4 color;\n" +
        "void main() {\n" +
        "    ivec2 size = textureSize(tex, 0);\n" +
        "    color = texelFetch(tex, ivec2(int(gl_FragCoord.x), size.y - 1 - int(gl_FragCoord.y)), 0);\n" +
        "}\n";

    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void BindFramebufferFn(int target, int id);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate int CreateShaderFn(int type);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void ShaderSourceFn(int shader, int count, ref IntPtr strings, ref int lengths);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void CompileShaderFn(int shader);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void GetIntFn(int id, int pname, out int value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate int CreateProgramFn();
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void AttachShaderFn(int program, int shader);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void LinkProgramFn(int program);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void DeleteObjectFn(int id);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void UseProgramFn(int program);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate int GetUniformLocationFn(int program, byte* name);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void Uniform1iFn(int location, int value);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void GenVertexArraysFn(int n, out int ids);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void DeleteVertexArraysFn(int n, ref int ids);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void BindVertexArrayFn(int id);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void ActiveTextureFn(int unit);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void BindTextureFn(int target, int id);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void EnableFn(int cap);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate byte IsEnabledFn(int cap);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void BlendFuncSeparateFn(int srcRgb, int dstRgb, int srcAlpha, int dstAlpha);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void BlendEquationSeparateFn(int rgb, int alpha);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void ViewportFn(int x, int y, int width, int height);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void GetIntArrayFn(int pname, int* values);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void ColorMaskFn(byte r, byte g, byte b, byte a);
    [UnmanagedFunctionPointer(CallingConvention.Winapi)] private delegate void DrawArraysFn(int mode, int first, int count);

    private readonly BindFramebufferFn _bindFramebuffer;
    private readonly UseProgramFn _useProgram;
    private readonly BindVertexArrayFn _bindVertexArray;
    private readonly ActiveTextureFn _activeTexture;
    private readonly BindTextureFn _bindTexture;
    private readonly EnableFn _enable;
    private readonly EnableFn _disable;
    private readonly IsEnabledFn _isEnabled;
    private readonly BlendFuncSeparateFn _blendFuncSeparate;
    private readonly BlendEquationSeparateFn _blendEquationSeparate;
    private readonly ViewportFn _viewport;
    private readonly GetIntArrayFn _getIntegerv;
    private readonly ColorMaskFn _colorMask;
    private readonly DrawArraysFn _drawArrays;
    private readonly DeleteObjectFn _deleteProgram;
    private readonly DeleteVertexArraysFn _deleteVertexArrays;
    private readonly int _texture;
    private int _program;
    private int _vao;

    public GlBlit(Func<string, IntPtr> getProc, int texture)
    {
        T Load<T>(string name) where T : Delegate => Marshal.GetDelegateForFunctionPointer<T>(getProc(name));
        _texture = texture;
        _bindFramebuffer = Load<BindFramebufferFn>("glBindFramebuffer");
        _useProgram = Load<UseProgramFn>("glUseProgram");
        _bindVertexArray = Load<BindVertexArrayFn>("glBindVertexArray");
        _activeTexture = Load<ActiveTextureFn>("glActiveTexture");
        _bindTexture = Load<BindTextureFn>("glBindTexture");
        _enable = Load<EnableFn>("glEnable");
        _disable = Load<EnableFn>("glDisable");
        _isEnabled = Load<IsEnabledFn>("glIsEnabled");
        _blendFuncSeparate = Load<BlendFuncSeparateFn>("glBlendFuncSeparate");
        _blendEquationSeparate = Load<BlendEquationSeparateFn>("glBlendEquationSeparate");
        _viewport = Load<ViewportFn>("glViewport");
        _getIntegerv = Load<GetIntArrayFn>("glGetIntegerv");
        _colorMask = Load<ColorMaskFn>("glColorMask");
        _drawArrays = Load<DrawArraysFn>("glDrawArrays");
        _deleteProgram = Load<DeleteObjectFn>("glDeleteProgram");
        _deleteVertexArrays = Load<DeleteVertexArraysFn>("glDeleteVertexArrays");

        _program = BuildProgram(Load<CreateShaderFn>("glCreateShader"), Load<ShaderSourceFn>("glShaderSource"),
            Load<CompileShaderFn>("glCompileShader"), Load<GetIntFn>("glGetShaderiv"), Load<CreateProgramFn>("glCreateProgram"),
            Load<AttachShaderFn>("glAttachShader"), Load<LinkProgramFn>("glLinkProgram"), Load<GetIntFn>("glGetProgramiv"),
            Load<DeleteObjectFn>("glDeleteShader"));
        Load<GenVertexArraysFn>("glGenVertexArrays")(1, out _vao);

        byte[] name = Encoding.ASCII.GetBytes("tex\0");
        int location;
        fixed (byte* p = name)
        {
            location = Load<GetUniformLocationFn>("glGetUniformLocation")(_program, p);
        }
        int previousProgram = GetInt(GL_CURRENT_PROGRAM);
        _useProgram(_program);
        Load<Uniform1iFn>("glUniform1i")(location, 0);
        _useProgram(previousProgram);
    }

    private static int BuildProgram(CreateShaderFn createShader, ShaderSourceFn shaderSource, CompileShaderFn compileShader,
        GetIntFn getShaderiv, CreateProgramFn createProgram, AttachShaderFn attachShader, LinkProgramFn linkProgram,
        GetIntFn getProgramiv, DeleteObjectFn deleteShader)
    {
        int Compile(int type, string source)
        {
            int shader = createShader(type);
            byte[] bytes = Encoding.ASCII.GetBytes(source);
            fixed (byte* p = bytes)
            {
                IntPtr pointer = (IntPtr)p;
                int length = bytes.Length;
                shaderSource(shader, 1, ref pointer, ref length);
            }
            compileShader(shader);
            getShaderiv(shader, GL_COMPILE_STATUS, out int ok);
            if (ok == 0)
            {
                throw new InvalidOperationException("Gum could not compile the shader that presents its frame.");
            }
            return shader;
        }

        int vertex = Compile(GL_VERTEX_SHADER, VertexShader);
        int fragment = Compile(GL_FRAGMENT_SHADER, FragmentShader);
        int program = createProgram();
        attachShader(program, vertex);
        attachShader(program, fragment);
        linkProgram(program);
        getProgramiv(program, GL_LINK_STATUS, out int linked);
        deleteShader(vertex);
        deleteShader(fragment);
        if (linked == 0)
        {
            throw new InvalidOperationException("Gum could not link the program that presents its frame.");
        }
        return program;
    }

    private int GetInt(int pname)
    {
        int value;
        _getIntegerv(pname, &value);
        return value;
    }

    private void SetEnabled(int cap, bool enabled)
    {
        if (enabled) { _enable(cap); } else { _disable(cap); }
    }

    /// <summary>
    /// Composites the texture over the default framebuffer, which keeps whatever the game drew. The GL state
    /// this changes is put back, so the game's own drawing after Gum is not affected.
    /// </summary>
    public void ToDefaultFramebuffer(int width, int height)
    {
        int program = GetInt(GL_CURRENT_PROGRAM);
        int vao = GetInt(GL_VERTEX_ARRAY_BINDING);
        int activeTexture = GetInt(GL_ACTIVE_TEXTURE);
        _activeTexture(GL_TEXTURE0);
        int texture = GetInt(GL_TEXTURE_BINDING_2D);
        int* viewport = stackalloc int[4];
        _getIntegerv(GL_VIEWPORT, viewport);
        int* colorMask = stackalloc int[4];
        _getIntegerv(GL_COLOR_WRITEMASK, colorMask);
        bool blend = _isEnabled(GL_BLEND) != 0, depth = _isEnabled(GL_DEPTH_TEST) != 0, scissor = _isEnabled(GL_SCISSOR_TEST) != 0;
        bool cull = _isEnabled(GL_CULL_FACE) != 0, stencil = _isEnabled(GL_STENCIL_TEST) != 0;
        int srcRgb = GetInt(GL_BLEND_SRC_RGB), dstRgb = GetInt(GL_BLEND_DST_RGB);
        int srcAlpha = GetInt(GL_BLEND_SRC_ALPHA), dstAlpha = GetInt(GL_BLEND_DST_ALPHA);
        int equationRgb = GetInt(GL_BLEND_EQUATION_RGB), equationAlpha = GetInt(GL_BLEND_EQUATION_ALPHA);

        _bindFramebuffer(GL_FRAMEBUFFER, 0);
        _viewport(0, 0, width, height);
        _colorMask(1, 1, 1, 1);
        SetEnabled(GL_DEPTH_TEST, false);
        SetEnabled(GL_SCISSOR_TEST, false);
        SetEnabled(GL_CULL_FACE, false);
        SetEnabled(GL_STENCIL_TEST, false);
        SetEnabled(GL_BLEND, true);
        _blendEquationSeparate(GL_FUNC_ADD, GL_FUNC_ADD);
        // Skia's target holds premultiplied alpha.
        _blendFuncSeparate(GL_ONE, GL_ONE_MINUS_SRC_ALPHA, GL_ONE, GL_ONE_MINUS_SRC_ALPHA);
        _useProgram(_program);
        _bindVertexArray(_vao);
        _bindTexture(GL_TEXTURE_2D, _texture);
        _drawArrays(GL_TRIANGLES, 0, 3);

        _bindTexture(GL_TEXTURE_2D, texture);
        _activeTexture(activeTexture);
        _bindVertexArray(vao);
        _useProgram(program);
        _viewport(viewport[0], viewport[1], viewport[2], viewport[3]);
        _colorMask((byte)colorMask[0], (byte)colorMask[1], (byte)colorMask[2], (byte)colorMask[3]);
        _blendEquationSeparate(equationRgb, equationAlpha);
        _blendFuncSeparate(srcRgb, dstRgb, srcAlpha, dstAlpha);
        SetEnabled(GL_BLEND, blend);
        SetEnabled(GL_DEPTH_TEST, depth);
        SetEnabled(GL_SCISSOR_TEST, scissor);
        SetEnabled(GL_CULL_FACE, cull);
        SetEnabled(GL_STENCIL_TEST, stencil);
    }

    public void Dispose()
    {
        if (_program != 0)
        {
            _deleteProgram(_program);
            _program = 0;
        }
        if (_vao != 0)
        {
            _deleteVertexArrays(1, ref _vao);
            _vao = 0;
        }
    }
}
