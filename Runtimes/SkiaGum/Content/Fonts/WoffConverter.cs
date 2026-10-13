using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;

namespace SkiaGum.Content.Fonts;

/// <summary>
/// Unwraps a WOFF 1.0 web font to the plain sfnt (TrueType/OpenType) bytes SkiaSharp can load.
/// SkiaSharp 4.x loads .ttf, .otf and .ttc but returns null for WOFF, so <see cref="GumFontMapper"/>
/// converts first and the tool's .woff fonts work on Skia the same as on the KernSmith runtimes.
/// WOFF 2.0 (Brotli plus glyph transforms) is not handled.
/// </summary>
public static class WoffConverter
{
    private const uint WoffSignature = 0x774F4646; // 'wOFF'
    private const int HeaderSize = 44;
    private const int TableDirectoryEntrySize = 20;
    private const int SfntHeaderSize = 12;
    private const int SfntTableRecordSize = 16;

    /// <summary>Whether <paramref name="data"/> starts with the WOFF 1.0 signature.</summary>
    public static bool IsWoff(byte[] data)
    {
        return data.Length >= HeaderSize && BinaryPrimitives.ReadUInt32BigEndian(data) == WoffSignature;
    }

    /// <summary>
    /// Returns the sfnt font held in a WOFF 1.0 file.
    /// </summary>
    /// <exception cref="InvalidDataException">The data is not a well-formed WOFF file.</exception>
    public static byte[] ToSfnt(byte[] woff)
    {
        if (!IsWoff(woff))
        {
            throw new InvalidDataException("The data is not a WOFF 1.0 font.");
        }

        uint flavor = BinaryPrimitives.ReadUInt32BigEndian(woff.AsSpan(4));
        int tableCount = BinaryPrimitives.ReadUInt16BigEndian(woff.AsSpan(12));

        if (woff.Length < HeaderSize + (long)tableCount * TableDirectoryEntrySize)
        {
            throw new InvalidDataException("The WOFF table directory is truncated.");
        }

        long totalSize = SfntHeaderSize + (long)tableCount * SfntTableRecordSize;
        byte[][] tables = new byte[tableCount][];
        uint[] tags = new uint[tableCount];
        uint[] checksums = new uint[tableCount];

        for (int i = 0; i < tableCount; i++)
        {
            ReadOnlySpan<byte> entry = woff.AsSpan(HeaderSize + i * TableDirectoryEntrySize, TableDirectoryEntrySize);
            tags[i] = BinaryPrimitives.ReadUInt32BigEndian(entry);
            long offset = BinaryPrimitives.ReadUInt32BigEndian(entry.Slice(4));
            long compressedLength = BinaryPrimitives.ReadUInt32BigEndian(entry.Slice(8));
            long originalLength = BinaryPrimitives.ReadUInt32BigEndian(entry.Slice(12));
            checksums[i] = BinaryPrimitives.ReadUInt32BigEndian(entry.Slice(16));

            if (offset + compressedLength > woff.Length || compressedLength > originalLength)
            {
                throw new InvalidDataException("A WOFF table lies outside the file.");
            }

            tables[i] = compressedLength == originalLength
                ? woff.AsSpan((int)offset, (int)originalLength).ToArray()
                : Inflate(woff, (int)offset, (int)compressedLength, (int)originalLength);

            totalSize += Pad4(tables[i].Length);
        }

        byte[] sfnt = new byte[totalSize];
        BinaryPrimitives.WriteUInt32BigEndian(sfnt, flavor);
        BinaryPrimitives.WriteUInt16BigEndian(sfnt.AsSpan(4), (ushort)tableCount);

        int maxPowerOfTwo = 1;
        int entrySelector = 0;
        while (maxPowerOfTwo * 2 <= tableCount)
        {
            maxPowerOfTwo *= 2;
            entrySelector++;
        }

        int searchRange = maxPowerOfTwo * 16;
        BinaryPrimitives.WriteUInt16BigEndian(sfnt.AsSpan(6), (ushort)searchRange);
        BinaryPrimitives.WriteUInt16BigEndian(sfnt.AsSpan(8), (ushort)entrySelector);
        BinaryPrimitives.WriteUInt16BigEndian(sfnt.AsSpan(10), (ushort)(tableCount * 16 - searchRange));

        int dataOffset = SfntHeaderSize + tableCount * SfntTableRecordSize;
        for (int i = 0; i < tableCount; i++)
        {
            Span<byte> record = sfnt.AsSpan(SfntHeaderSize + i * SfntTableRecordSize, SfntTableRecordSize);
            BinaryPrimitives.WriteUInt32BigEndian(record, tags[i]);
            BinaryPrimitives.WriteUInt32BigEndian(record.Slice(4), checksums[i]);
            BinaryPrimitives.WriteUInt32BigEndian(record.Slice(8), (uint)dataOffset);
            BinaryPrimitives.WriteUInt32BigEndian(record.Slice(12), (uint)tables[i].Length);
            tables[i].CopyTo(sfnt, dataOffset);
            dataOffset += Pad4(tables[i].Length);
        }

        return sfnt;
    }

    private static byte[] Inflate(byte[] source, int offset, int compressedLength, int originalLength)
    {
        // WOFF tables are zlib streams (RFC 1950): a 2-byte header, deflate data, then a checksum.
        // DeflateStream reads the raw deflate data, which is available on every target SkiaGum builds.
        if (compressedLength < 2)
        {
            throw new InvalidDataException("A compressed WOFF table is too short.");
        }

        byte[] result = new byte[originalLength];
        using MemoryStream compressed = new MemoryStream(source, offset + 2, compressedLength - 2);
        using DeflateStream inflater = new DeflateStream(compressed, CompressionMode.Decompress);

        int read = 0;
        while (read < originalLength)
        {
            int count = inflater.Read(result, read, originalLength - read);
            if (count == 0)
            {
                throw new InvalidDataException("A compressed WOFF table ended early.");
            }

            read += count;
        }

        return result;
    }

    private static int Pad4(int length) => (length + 3) & ~3;
}
