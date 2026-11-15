using Gum.Wireframe;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;
using System.Runtime.InteropServices;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Pixels edited in place on a bitmap Gum loaded (immutable, so its pixels are shared by every Sprite and
/// NineSlice over it, #7876). Every write route, with and without NotifyPixelsChanged, must show the edit
/// once the bitmap is assigned again. A GPU surface additionally needs the notification to see an edit:
/// the GPU texture cache is keyed by the image's unique id, which only NotifyPixelsChanged changes. That
/// cannot run in CI, so the unique id is asserted here instead.
/// </summary>
public class SharedTextureInvalidationTests : SharedTextureTestBase
{
    // Equal red, green and blue and full alpha, so the color reads the same whatever the bitmap's channel order.
    private static readonly SKColor Edited = new(250, 250, 250);

    public enum WriteRoute
    {
        Erase,
        CanvasClear,
        CanvasDraw,
        SetPixel,
        PixelSpan,
        PixelPointer,
    }

    public static TheoryData<string, WriteRoute, bool> Cases
    {
        get
        {
            TheoryData<string, WriteRoute, bool> data = new();
            foreach (string name in new[] { nameof(Sprite), nameof(NineSlice) })
            {
                foreach (WriteRoute route in Enum.GetValues<WriteRoute>())
                {
                    data.Add(name, route, false);
                    data.Add(name, route, true);
                }
            }
            return data;
        }
    }

    private static void EditEveryPixel(SKBitmap bitmap, WriteRoute route)
    {
        switch (route)
        {
            case WriteRoute.Erase:
                bitmap.Erase(Edited);
                break;
            case WriteRoute.CanvasClear:
                using (SKCanvas canvas = new(bitmap))
                {
                    canvas.Clear(Edited);
                }
                break;
            case WriteRoute.CanvasDraw:
                using (SKCanvas canvas = new(bitmap))
                using (SKPaint paint = new() { Color = Edited, BlendMode = SKBlendMode.Src })
                {
                    canvas.DrawRect(0, 0, bitmap.Width, bitmap.Height, paint);
                }
                break;
            case WriteRoute.SetPixel:
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        bitmap.SetPixel(x, y, Edited);
                    }
                }
                break;
            case WriteRoute.PixelSpan:
                Span<byte> span = bitmap.GetPixelSpan();
                for (int offset = 0; offset < span.Length; offset += 4)
                {
                    span[offset] = 250;
                    span[offset + 1] = 250;
                    span[offset + 2] = 250;
                    span[offset + 3] = 255;
                }
                break;
            case WriteRoute.PixelPointer:
                IntPtr pixels = bitmap.GetPixels();
                for (int offset = 0; offset < bitmap.ByteCount; offset += 4)
                {
                    Marshal.WriteInt32(pixels, offset, unchecked((int)0xFFFAFAFA));
                }
                break;
        }
    }

    // The center of the drawn 30x30 area at x=4, y=4: inside the NineSlice's center section and the Sprite.
    private static SKColor Center(GraphicalUiElement runtime) => ColorAt(4 + 15, 4 + 15, runtime);

    private static SKColor Corner(GraphicalUiElement runtime) => ColorAt(4 + 1, 4 + 1, runtime);

    [Theory]
    [MemberData(nameof(Cases))]
    public void WriteRoute_ThenTextureAssignedAgain_ShowsTheEditOnTheCpu(string name, WriteRoute route, bool notify)
    {
        SKBitmap sheet = Load(GridSize, seed: 0);
        sheet.IsImmutable.ShouldBeTrue();
        GraphicalUiElement runtime = CreateRuntime(name, sheet, x: 4);
        Center(runtime).ShouldNotBe(Edited);

        EditEveryPixel(sheet, route);
        if (notify)
        {
            sheet.NotifyPixelsChanged();
        }
        SetTexture(runtime, sheet);

        Center(runtime).ShouldBe(Edited);
        Corner(runtime).ShouldBe(Edited);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void WriteRoute_ThenTextureAssignedAgain_ShowsTheEditOnEveryRenderableOfTheSheet(string name, WriteRoute route, bool notify)
    {
        SKBitmap sheet = Load(GridSize, seed: 0);
        GraphicalUiElement reassigned = CreateRuntime(name, sheet, x: 4);
        GraphicalUiElement other = CreateRuntime(name, sheet, x: 44);

        EditEveryPixel(sheet, route);
        if (notify)
        {
            sheet.NotifyPixelsChanged();
        }
        SetTexture(reassigned, sheet);
        SetTexture(other, sheet);

        using SKBitmap drawn = RenderBitmap(reassigned, other);
        drawn.GetPixel(4 + 15, 4 + 15).ShouldBe(Edited);
        drawn.GetPixel(44 + 15, 4 + 15).ShouldBe(Edited);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void WriteRoute_WithoutAssigningTheTextureAgain_ShowsTheEditOnTheCpuBecauseThePixelsAreShared(string name, WriteRoute route, bool notify)
    {
        // The image shares the bitmap's pixel buffer, so a CPU surface reads the edit straight away. A GPU
        // surface uploads the pixels once per image id and keeps drawing the old upload until the id
        // changes (see ImageUniqueId tests); do not rely on this on a GPU.
        SKBitmap sheet = Load(GridSize, seed: 0);
        GraphicalUiElement runtime = CreateRuntime(name, sheet, x: 4);
        Center(runtime).ShouldNotBe(Edited);

        EditEveryPixel(sheet, route);
        if (notify)
        {
            sheet.NotifyPixelsChanged();
        }

        Center(runtime).ShouldBe(Edited);
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void CopyThenEditThenAssign_ShowsTheEditEverywhereItIsAssignedAndNowhereElse(string name)
    {
        // The documented-safe route for editing a loaded texture.
        SKBitmap sheet = Load(GridSize, seed: 0);
        GraphicalUiElement untouched = CreateRuntime(name, sheet, x: 4);
        GraphicalUiElement edited = CreateRuntime(name, sheet, x: 44);
        SKColor before = ColorAt(4 + 15, 4 + 15, untouched);

        using SKBitmap copy = sheet.Copy();
        copy.IsImmutable.ShouldBeFalse();
        copy.Erase(Edited);
        SetTexture(edited, copy);

        using SKBitmap drawn = RenderBitmap(untouched, edited);
        drawn.GetPixel(44 + 15, 4 + 15).ShouldBe(Edited);
        drawn.GetPixel(4 + 15, 4 + 15).ShouldBe(before);
        sheet.GetPixel(6, 6).ShouldBe(TexelColor(6, 6, seed: 0));
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void CopyThenEditThenAssign_EditedAgainWithoutReassigning_StaysAsAssigned(string name)
    {
        // A copy is a caller-owned mutable bitmap, so the renderable keeps a snapshot of it.
        SKBitmap sheet = Load(GridSize, seed: 0);
        using SKBitmap copy = sheet.Copy();
        GraphicalUiElement runtime = CreateRuntime(name, copy, x: 4);
        SKColor assigned = Center(runtime);

        copy.Erase(Edited);

        Center(runtime).ShouldBe(assigned);
        assigned.ShouldNotBe(Edited);
    }

    #region What a GPU cache is keyed by

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void ImagesOfOneUneditedSheet_HaveOneUniqueId_SoAGpuSurfaceUploadsItOnce(string name)
    {
        SKBitmap sheet = Load(GridSize, seed: 0);
        GraphicalUiElement first = CreateRuntime(name, sheet, x: 4);
        GraphicalUiElement second = CreateRuntime(name, sheet, x: 44);

        GetImage(second).UniqueId.ShouldBe(GetImage(first).UniqueId);
    }

    [Theory]
    [InlineData(WriteRoute.Erase)]
    [InlineData(WriteRoute.CanvasClear)]
    [InlineData(WriteRoute.CanvasDraw)]
    [InlineData(WriteRoute.SetPixel)]
    public void NotifyingWriteRoute_GivesTheNextImageANewUniqueId_SoAGpuSurfaceUploadsAgain(WriteRoute route)
    {
        SKBitmap sheet = Load(GridSize, seed: 0);
        GraphicalUiElement runtime = CreateRuntime(nameof(Sprite), sheet, x: 4);
        uint before = GetImage(runtime).UniqueId;

        EditEveryPixel(sheet, route);
        sheet.NotifyPixelsChanged();
        SetTexture(runtime, sheet);

        GetImage(runtime).UniqueId.ShouldNotBe(before);
    }

    [Theory]
    [InlineData(WriteRoute.PixelSpan)]
    [InlineData(WriteRoute.PixelPointer)]
    public void RawWriteWithoutNotify_KeepsTheUniqueId_SoAGpuSurfaceCannotSeeTheEdit(WriteRoute route)
    {
        // The contract: a raw write through GetPixelSpan or GetPixels must be followed by
        // NotifyPixelsChanged. Without it Skia has no signal that the pixels changed, the next image has the
        // old unique id, and a GPU surface keeps drawing its old upload even after Texture is assigned again
        // (the CPU shows the edit, see the tests above). Only the id can be asserted without a GPU.
        SKBitmap sheet = Load(GridSize, seed: 0);
        GraphicalUiElement runtime = CreateRuntime(nameof(Sprite), sheet, x: 4);
        uint before = GetImage(runtime).UniqueId;

        EditEveryPixel(sheet, route);
        SetTexture(runtime, sheet);

        GetImage(runtime).UniqueId.ShouldBe(before);
    }

    [Theory]
    [InlineData(WriteRoute.PixelSpan)]
    [InlineData(WriteRoute.PixelPointer)]
    public void RawWriteThenNotify_GivesTheNextImageANewUniqueId(WriteRoute route)
    {
        SKBitmap sheet = Load(GridSize, seed: 0);
        GraphicalUiElement runtime = CreateRuntime(nameof(Sprite), sheet, x: 4);
        uint before = GetImage(runtime).UniqueId;

        EditEveryPixel(sheet, route);
        sheet.NotifyPixelsChanged();
        SetTexture(runtime, sheet);

        GetImage(runtime).UniqueId.ShouldNotBe(before);
    }

    #endregion
}
