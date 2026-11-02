using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RenderingLibrary.Content;
using SkiaSharp;

namespace Gum.Graphics;
/// <summary>
/// Taken from http://stackoverflow.com/questions/111345/getting-image-dimensions-without-reading-the-entire-file/111349
/// Minor improvements including supporting unsigned 16-bit integers when decoding Jfif and added logic
/// to load the image using new Bitmap if reading the headers fails
/// </summary>
public static class ImageHeader
{
    const string errorMessage = "Could not recognise image format.";

    private static Dictionary<byte[], Func<BinaryReader, Size>> imageFormatDecoders = new Dictionary<byte[], Func<BinaryReader, Size>>()
        {
            { new byte[] { 0x42, 0x4D }, DecodeBitmap },
            { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, DecodeGif },
            { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, DecodeGif },
            { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, DecodePng },
            { new byte[] { 0xff, 0xd8 }, DecodeJfif },
        };

    /// <summary>
    /// Gets the dimensions of an image file, asking the same sources the loader does: the decoders
    /// registered in <see cref="LoaderManager.ImageDecoders"/> first, then the built-in header
    /// table, then SkiaSharp. The file is opened once.
    /// </summary>
    /// <param name="path">The path of the image.</param>
    /// <returns>The dimensions, or null when the file does not exist or no source can read it.</returns>
    public static Size? GetDimensions(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        Stream stream = File.OpenRead(path);
        try
        {
            DecodedImage? decoded = LoaderManager.Self.ImageDecoders.TryDecode(path, ref stream);
            if (decoded != null)
            {
                return new Size(decoded.Width, decoded.Height);
            }

            // TryDecode leaves the stream where it found it; it may be a seekable copy.
            if (!stream.CanSeek)
            {
                return null;
            }

            long start = stream.Position;
            try
            {
                using (var binaryReader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                {
                    return GetDimensions(binaryReader);
                }
            }
            catch (Exception e) when (e is ArgumentException || e is EndOfStreamException)
            {
                // Header parsing failed: let a real decoder read the dimensions. SkiaSharp runs on
                // every OS the tool targets; System.Drawing.Bitmap does not.
                stream.Position = start;
                using (SKCodec? codec = SKCodec.Create(stream))
                {
                    return codec == null ? null : new Size(codec.Info.Width, codec.Info.Height);
                }
            }
        }
        finally
        {
            stream.Dispose();
        }
    }

    /// <summary>        
    /// Gets the dimensions of an image.        
    /// </summary>        
    /// <param name="path">The path of the image to get the dimensions of.</param>        
    /// <returns>The dimensions of the specified image.</returns>        
    /// <exception cref="ArgumentException">The image was of an unrecognised format.</exception>            
    public static Size GetDimensions(BinaryReader binaryReader)
    {
        int maxMagicBytesLength = imageFormatDecoders.Keys.OrderByDescending(x => x.Length).First().Length;
        byte[] magicBytes = new byte[maxMagicBytesLength];
        for (int i = 0; i < maxMagicBytesLength; i += 1)
        {
            magicBytes[i] = binaryReader.ReadByte();
            foreach (var kvPair in imageFormatDecoders)
            {
                if (StartsWith(magicBytes, kvPair.Key))
                {
                    return kvPair.Value(binaryReader);
                }
            }
        }

        throw new ArgumentException(errorMessage, "binaryReader");
    }

    private static bool StartsWith(byte[] thisBytes, byte[] thatBytes)
    {
        for (int i = 0; i < thatBytes.Length; i += 1)
        {
            if (thisBytes[i] != thatBytes[i])
            {
                return false;
            }
        }

        return true;
    }

    private static short ReadLittleEndianInt16(BinaryReader binaryReader)
    {
        byte[] bytes = new byte[sizeof(short)];

        for (int i = 0; i < sizeof(short); i += 1)
        {
            bytes[sizeof(short) - 1 - i] = binaryReader.ReadByte();
        }
        return BitConverter.ToInt16(bytes, 0);
    }

    private static ushort ReadLittleEndianUInt16(BinaryReader binaryReader)
    {
        byte[] bytes = new byte[sizeof(ushort)];

        for (int i = 0; i < sizeof(ushort); i += 1)
        {
            bytes[sizeof(ushort) - 1 - i] = binaryReader.ReadByte();
        }
        return BitConverter.ToUInt16(bytes, 0);
    }

    private static int ReadLittleEndianInt32(BinaryReader binaryReader)
    {
        byte[] bytes = new byte[sizeof(int)];
        for (int i = 0; i < sizeof(int); i += 1)
        {
            bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
        }
        return BitConverter.ToInt32(bytes, 0);
    }

    private static Size DecodeBitmap(BinaryReader binaryReader)
    {
        binaryReader.ReadBytes(16);
        int width = binaryReader.ReadInt32();
        int height = binaryReader.ReadInt32();
        return new Size(width, height);
    }

    private static Size DecodeGif(BinaryReader binaryReader)
    {
        int width = binaryReader.ReadInt16();
        int height = binaryReader.ReadInt16();
        return new Size(width, height);
    }

    private static Size DecodePng(BinaryReader binaryReader)
    {
        binaryReader.ReadBytes(8);
        int width = ReadLittleEndianInt32(binaryReader);
        int height = ReadLittleEndianInt32(binaryReader);
        return new Size(width, height);
    }

    private static Size DecodeJfif(BinaryReader binaryReader)
    {
        while (binaryReader.ReadByte() == 0xff)
        {
            byte marker = binaryReader.ReadByte();
            short chunkLength = ReadLittleEndianInt16(binaryReader);
            if (marker == 0xc0)
            {
                binaryReader.ReadByte();
                int height = ReadLittleEndianInt16(binaryReader);
                int width = ReadLittleEndianInt16(binaryReader);
                return new Size(width, height);
            }

            if (chunkLength < 0)
            {
                ushort uchunkLength = (ushort)chunkLength;
                binaryReader.ReadBytes(uchunkLength - 2);
            }
            else
            {
                binaryReader.ReadBytes(chunkLength - 2);
            }
        }

        throw new ArgumentException(errorMessage);
    }
}