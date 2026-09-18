using System.Runtime.InteropServices;
using Shouldly;
using XnaAndWinforms;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The row-by-row readback copies behind every canvas frame: rows land at the destination stride
/// (which may be padded past the packed row size), padding bytes are left alone, and the BGRA
/// variant swaps the red and blue channels of every pixel.
/// </summary>
public class RawPixelBufferCopyTests
{
    private const int Width = 3;
    private const int Height = 2;
    private const int RowSize = Width * 4;
    private const int PaddedStride = RowSize + 4;
    private const byte Padding = 0xEE;

    private static byte[] MakeSource()
    {
        // Each byte encodes its own (row, pixel, channel) so a misplaced row or channel is visible.
        byte[] source = new byte[Width * Height * 4];
        for (int i = 0; i < source.Length; i++)
        {
            source[i] = (byte)(i + 1);
        }
        return source;
    }

    private static byte[] CopyThrough(Action<byte[], IntPtr> copy)
    {
        byte[] source = MakeSource();
        int destinationSize = PaddedStride * Height;
        IntPtr destination = Marshal.AllocHGlobal(destinationSize);
        try
        {
            byte[] prefill = new byte[destinationSize];
            Array.Fill(prefill, Padding);
            Marshal.Copy(prefill, 0, destination, destinationSize);

            copy(source, destination);

            byte[] result = new byte[destinationSize];
            Marshal.Copy(destination, result, 0, destinationSize);
            return result;
        }
        finally
        {
            Marshal.FreeHGlobal(destination);
        }
    }

    [Fact]
    public void CopyDirect_HonorsPaddedStrideAndLeavesPaddingUntouched()
    {
        byte[] result = CopyThrough((source, destination) =>
            RawPixelBufferCopy.CopyDirect(source, destination, PaddedStride, Width, Height));

        byte[] expected =
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, Padding, Padding, Padding, Padding,
            13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, Padding, Padding, Padding, Padding,
        };
        result.ShouldBe(expected);
    }

    [Fact]
    public void CopyAndConvertRgbaToBgra_SwapsRedAndBlueAndHonorsPaddedStride()
    {
        byte[] result = CopyThrough((source, destination) =>
            RawPixelBufferCopy.CopyAndConvertRgbaToBgra(source, destination, PaddedStride, Width, Height));

        byte[] expected =
        {
            3, 2, 1, 4, 7, 6, 5, 8, 11, 10, 9, 12, Padding, Padding, Padding, Padding,
            15, 14, 13, 16, 19, 18, 17, 20, 23, 22, 21, 24, Padding, Padding, Padding, Padding,
        };
        result.ShouldBe(expected);
    }
}
