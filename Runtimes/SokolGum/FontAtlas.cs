using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Sokol.Fontstash;
using static Sokol.SG;
using static Sokol.SGP;

namespace SokolGum;

/// <summary>
/// A fontstash context whose render callbacks emit into sokol_gp instead of
/// sokol_gl. This way text glyphs flow through the same batched pipeline as
/// rectangles, sprites, and nine-slices — giving us natural Z ordering with
/// respect to scene-graph draw order (same as MonoGameGum/FnaGum/RaylibGum,
/// where SpriteBatch / raylib's batcher handle both sprites and text).
///
/// The atlas itself is single-channel (fontstash's internal format). We
/// expand it to RGBA8 on upload so sokol_gp's default textured pipeline
/// renders it correctly: R=G=B=255, A=glyph_alpha. Sampled * vertex color
/// gives correctly-tinted alpha-blended text.
/// </summary>
public sealed unsafe class FontAtlas : IDisposable
{
    // FONSflags
    private const byte FONS_ZERO_TOPLEFT = 1;

    // FONSparams layout matches fontstash.h's struct exactly.
    // On 64-bit with natural alignment: int+int+byte+(7 pad)+5*ptr = 16 + 8 + 40 = 64 bytes.
    [StructLayout(LayoutKind.Sequential)]
    private struct FONSparams
    {
        public int width;
        public int height;
        public byte flags;
        // auto padding to pointer alignment
        public IntPtr userPtr;
        public IntPtr renderCreate;
        public IntPtr renderResize;
        public IntPtr renderUpdate;
        public IntPtr renderDraw;
        public IntPtr renderDelete;
    }

    private GCHandle _selfHandle;
    private IntPtr _stash;
    private sg_image _image;
    private sg_view _view;
    private sg_sampler _sampler;
    private byte[] _rgbaStaging = [];
    private int _width;
    private int _height;
    private sgp_vertex[] _vertexScratch = new sgp_vertex[256];
    private bool _disposed;

    // TTF byte buffers handed to fontstash via fonsAddFontMem. Fontstash
    // keeps the raw pointer alive for the font's lifetime within the
    // context, so these must outlive every Font but can be freed safely
    // once fonsDeleteInternal has torn down the context.
    private readonly List<IntPtr> _fontDataBuffers = [];

    // sokol_gfx permits exactly one sg_update_image per image per frame.
    // fontstash may call renderUpdate multiple times in a frame (one per
    // fonsDrawText that rasterizes new glyphs). We accumulate the union
    // dirty region and upload once via FlushPendingUpload, called from
    // Renderer.EndFrame. Trade-off: new glyphs render blank for one frame
    // before the atlas is uploaded, then appear on the next frame.
    private bool _hasPendingUpload;

    public IntPtr Stash => _stash;

    public FontAtlas(int initialWidth, int initialHeight, sg_sampler sampler)
    {
        _sampler = sampler;
        _selfHandle = GCHandle.Alloc(this);

        var p = new FONSparams
        {
            width = initialWidth,
            height = initialHeight,
            flags = FONS_ZERO_TOPLEFT,
            userPtr = GCHandle.ToIntPtr(_selfHandle),
            renderCreate = (IntPtr)(delegate* unmanaged[Cdecl]<void*, int, int, int>)&RenderCreateStatic,
            renderResize = (IntPtr)(delegate* unmanaged[Cdecl]<void*, int, int, int>)&RenderResizeStatic,
            renderUpdate = (IntPtr)(delegate* unmanaged[Cdecl]<void*, int*, byte*, void>)&RenderUpdateStatic,
            renderDraw   = (IntPtr)(delegate* unmanaged[Cdecl]<void*, float*, float*, uint*, int, void>)&RenderDrawStatic,
            renderDelete = (IntPtr)(delegate* unmanaged[Cdecl]<void*, void>)&RenderDeleteStatic,
        };

        _stash = fonsCreateInternal((IntPtr)(&p));
        if (_stash == IntPtr.Zero)
            throw new InvalidOperationException("fonsCreateInternal returned null");
    }

    /// <summary>
    /// Registers a native TTF byte buffer that fontstash took a pointer to
    /// via <c>fonsAddFontMem(..., freeData: 0)</c>. The atlas owns the
    /// buffer's lifetime and frees it in <see cref="Dispose"/> once the
    /// fontstash context is torn down, so no Font instance can dangle.
    /// </summary>
    internal void TrackFontData(IntPtr nativeData)
    {
        if (nativeData != IntPtr.Zero)
            _fontDataBuffers.Add(nativeData);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // fontstash calls our renderDelete callback which cleans up sg resources.
        if (_stash != IntPtr.Zero)
        {
            fonsDeleteInternal(_stash);
            _stash = IntPtr.Zero;
        }

        // Free TTF buffers *after* fonsDeleteInternal so fontstash is no
        // longer reading from them. Up until this point every Font that
        // references the context is still valid — freeing these earlier
        // would leave fonsRasterizeGlyph pointing at reclaimed memory.
        foreach (var buf in _fontDataBuffers)
            Marshal.FreeHGlobal(buf);
        _fontDataBuffers.Clear();

        if (_selfHandle.IsAllocated)
            _selfHandle.Free();
    }

    // ====== Instance handlers (called from the static callbacks) ======

    private int RenderCreate(int w, int h)
    {
        _width = w;
        _height = h;
        _rgbaStaging = new byte[w * h * 4];
        // Start with R=G=B=255, A=0 so the atlas samples as "white, transparent" everywhere.
        for (int i = 0; i < _rgbaStaging.Length; i += 4)
        {
            _rgbaStaging[i + 0] = 255;
            _rgbaStaging[i + 1] = 255;
            _rgbaStaging[i + 2] = 255;
            _rgbaStaging[i + 3] = 0;
        }

        var desc = new sg_image_desc
        {
            width = w,
            height = h,
            pixel_format = sg_pixel_format.SG_PIXELFORMAT_RGBA8,
            // stream_update (not dynamic_update) because fontstash may call
            // renderUpdate multiple times per frame when rasterizing several
            // glyphs across different sizes/fonts — sokol_gfx's dynamic_update
            // enforces one update per frame and panics otherwise.
            usage = { stream_update = true },
            label = "SokolGum.FontAtlas",
        };
        _image = sg_make_image(desc);
        _view = sgp_make_texture_view_from_image(_image, "SokolGum.FontAtlas.View");
        return 1; // success
    }

    private int RenderResize(int w, int h)
    {
        // Destroy old resources and create new ones at the new size.
        // fontstash will redraw glyphs into the new atlas.
        if (_view.id != 0) sg_destroy_view(_view);
        if (_image.id != 0) sg_destroy_image(_image);
        return RenderCreate(w, h);
    }

    private void RenderUpdate(int* rect, byte* data)
    {
        // rect = [x0, y0, x1, y1]; data = pointer to the full fontstash atlas
        // (w*h single-channel alpha). Expand the dirty region into our RGBA8
        // staging buffer (R=G=B=255, A=alpha) but DO NOT upload yet — flushing
        // is deferred to FlushPendingUpload / Renderer.EndFrame so we stay
        // within sokol_gfx's one-update-per-frame rule.
        int x0 = rect[0], y0 = rect[1], x1 = rect[2], y1 = rect[3];
        for (int y = y0; y < y1; y++)
        {
            int srcIdx = y * _width + x0;
            int dstIdx = (y * _width + x0) * 4;
            for (int x = x0; x < x1; x++)
            {
                _rgbaStaging[dstIdx + 0] = 255;
                _rgbaStaging[dstIdx + 1] = 255;
                _rgbaStaging[dstIdx + 2] = 255;
                _rgbaStaging[dstIdx + 3] = data[srcIdx];
                srcIdx++;
                dstIdx += 4;
            }
        }
        _hasPendingUpload = true;
    }

    /// <summary>
    /// Upload any accumulated glyph atlas changes. Must be called at most
    /// once per frame per image. Renderer.EndFrame drives this.
    /// </summary>
    public void FlushPendingUpload()
    {
        if (!_hasPendingUpload || _image.id == 0) return;

        fixed (byte* p = _rgbaStaging)
        {
            var imgData = new sg_image_data();
            imgData.mip_levels[0] = new sg_range { ptr = p, size = (nuint)_rgbaStaging.Length };
            sg_update_image(_image, imgData);
        }
        _hasPendingUpload = false;
    }

    private void RenderDraw(float* verts, float* tcoords, uint* colors, int nverts)
    {
        // Grow the scratch buffer if needed (glyph-heavy frames may exceed 256 verts).
        if (_vertexScratch.Length < nverts)
            _vertexScratch = new sgp_vertex[Math.Max(nverts, _vertexScratch.Length * 2)];

        // fontstash colors are packed uint32 with byte0=R, byte1=G, byte2=B, byte3=A.
        for (int i = 0; i < nverts; i++)
        {
            uint c = colors[i];
            _vertexScratch[i] = new sgp_vertex
            {
                position = new sgp_vec2 { x = verts[i * 2 + 0], y = verts[i * 2 + 1] },
                texcoord = new sgp_vec2 { x = tcoords[i * 2 + 0], y = tcoords[i * 2 + 1] },
                color = new sgp_color_ub4
                {
                    r = (byte)(c & 0xFF),
                    g = (byte)((c >> 8) & 0xFF),
                    b = (byte)((c >> 16) & 0xFF),
                    a = (byte)((c >> 24) & 0xFF),
                },
            };
        }

        sgp_set_view(0, _view);
        sgp_set_sampler(0, _sampler);
        fixed (sgp_vertex* v = _vertexScratch)
        {
            // SG_PRIMITIVETYPE_TRIANGLES (fontstash emits 3 verts per triangle).
            sgp_draw(sg_primitive_type.SG_PRIMITIVETYPE_TRIANGLES, Unsafe.AsRef<sgp_vertex>(v), (uint)nverts);
        }
        sgp_reset_view(0);
        sgp_reset_sampler(0);
    }

    private void RenderDelete()
    {
        if (_view.id != 0) { sg_destroy_view(_view); _view = default; }
        if (_image.id != 0) { sg_destroy_image(_image); _image = default; }
    }

    // ====== Static callbacks (what fontstash actually calls) ======

    private static FontAtlas FromUserPtr(void* uptr)
        => (FontAtlas)GCHandle.FromIntPtr((IntPtr)uptr).Target!;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int RenderCreateStatic(void* uptr, int w, int h)
        => FromUserPtr(uptr).RenderCreate(w, h);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int RenderResizeStatic(void* uptr, int w, int h)
        => FromUserPtr(uptr).RenderResize(w, h);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void RenderUpdateStatic(void* uptr, int* rect, byte* data)
        => FromUserPtr(uptr).RenderUpdate(rect, data);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void RenderDrawStatic(void* uptr, float* verts, float* tcoords, uint* colors, int nverts)
        => FromUserPtr(uptr).RenderDraw(verts, tcoords, colors, nverts);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void RenderDeleteStatic(void* uptr)
        => FromUserPtr(uptr).RenderDelete();
}
