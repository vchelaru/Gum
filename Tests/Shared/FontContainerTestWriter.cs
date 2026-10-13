using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace GumTestSupport;

/// <summary>
/// Wraps an sfnt (.ttf) font in the other font containers (WOFF 1.0, TrueType Collection) so tests
/// can exercise those formats without committing binary fixtures. Compiled into the test projects
/// that need it.
/// </summary>
public static class FontContainerTestWriter
{
    /// <summary>
    /// Wraps <paramref name="sfnt"/> in a WOFF 1.0 container. Each table is zlib-compressed when
    /// that makes it smaller, the way WOFF producers do.
    /// </summary>
    public static byte[] ToWoff(byte[] sfnt)
    {
        int tableCount = BinaryPrimitives.ReadUInt16BigEndian(sfnt.AsSpan(4));
        List<(string Tag, byte[] Data)> tables = new();
        for (int i = 0; i < tableCount; i++)
        {
            int record = 12 + 16 * i;
            string tag = Encoding.ASCII.GetString(sfnt, record, 4);
            int offset = (int)BinaryPrimitives.ReadUInt32BigEndian(sfnt.AsSpan(record + 8));
            int length = (int)BinaryPrimitives.ReadUInt32BigEndian(sfnt.AsSpan(record + 12));
            tables.Add((tag, sfnt.AsSpan(offset, length).ToArray()));
        }

        tables = tables.OrderBy(t => t.Tag, StringComparer.Ordinal).ToList();

        List<byte[]> stored = new();
        foreach ((string Tag, byte[] Data) table in tables)
        {
            using MemoryStream compressed = new();
            using (ZLibStream zlib = new(compressed, CompressionLevel.Optimal, leaveOpen: true))
            {
                zlib.Write(table.Data);
            }

            byte[] candidate = compressed.ToArray();
            stored.Add(candidate.Length < table.Data.Length ? candidate : table.Data);
        }

        int directorySize = 44 + 20 * tables.Count;
        int totalSfntSize = 12 + 16 * tables.Count + tables.Sum(t => Pad4(t.Data.Length));
        int totalLength = directorySize + stored.Sum(s => Pad4(s.Length));

        using MemoryStream output = new();
        WriteUInt32(output, 0x774F4646); // 'wOFF'
        WriteUInt32(output, BinaryPrimitives.ReadUInt32BigEndian(sfnt.AsSpan(0)));
        WriteUInt32(output, (uint)totalLength);
        WriteUInt16(output, (ushort)tables.Count);
        WriteUInt16(output, 0);
        WriteUInt32(output, (uint)totalSfntSize);
        WriteUInt16(output, 1);
        WriteUInt16(output, 0);
        for (int i = 0; i < 5; i++)
        {
            WriteUInt32(output, 0); // no metadata or private data
        }

        int dataOffset = directorySize;
        for (int i = 0; i < tables.Count; i++)
        {
            output.Write(Encoding.ASCII.GetBytes(tables[i].Tag));
            WriteUInt32(output, (uint)dataOffset);
            WriteUInt32(output, (uint)stored[i].Length);
            WriteUInt32(output, (uint)tables[i].Data.Length);
            WriteUInt32(output, Checksum(tables[i].Data));
            dataOffset += Pad4(stored[i].Length);
        }

        foreach (byte[] data in stored)
        {
            output.Write(data);
            output.Write(new byte[Pad4(data.Length) - data.Length]);
        }

        return output.ToArray();
    }

    /// <summary>
    /// Wraps a single-font sfnt in a TrueType Collection: a 16-byte 'ttcf' header whose one entry
    /// points at the font's table directory, with every table offset shifted by that header.
    /// </summary>
    public static byte[] ToTtc(byte[] sfnt)
    {
        const int headerSize = 16;
        int tableCount = BinaryPrimitives.ReadUInt16BigEndian(sfnt.AsSpan(4));
        byte[] collection = new byte[headerSize + sfnt.Length];
        BinaryPrimitives.WriteUInt32BigEndian(collection, 0x74746366); // 'ttcf'
        BinaryPrimitives.WriteUInt16BigEndian(collection.AsSpan(4), 1);
        BinaryPrimitives.WriteUInt16BigEndian(collection.AsSpan(6), 0);
        BinaryPrimitives.WriteUInt32BigEndian(collection.AsSpan(8), 1);
        BinaryPrimitives.WriteUInt32BigEndian(collection.AsSpan(12), headerSize);
        sfnt.CopyTo(collection, headerSize);

        for (int i = 0; i < tableCount; i++)
        {
            Span<byte> offsetField = collection.AsSpan(headerSize + 12 + 16 * i + 8, 4);
            BinaryPrimitives.WriteUInt32BigEndian(offsetField, BinaryPrimitives.ReadUInt32BigEndian(offsetField) + headerSize);
        }

        return collection;
    }

    private static int Pad4(int value) => (value + 3) & ~3;

    private static uint Checksum(byte[] data)
    {
        uint sum = 0;
        for (int i = 0; i < data.Length; i += 4)
        {
            uint word = 0;
            for (int k = 0; k < 4; k++)
            {
                word = (word << 8) | (i + k < data.Length ? data[i + k] : (byte)0);
            }

            sum += word;
        }

        return sum;
    }

    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> buffer = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, value);
        stream.Write(buffer);
    }

    private static void WriteUInt16(Stream stream, ushort value)
    {
        Span<byte> buffer = stackalloc byte[2];
        BinaryPrimitives.WriteUInt16BigEndian(buffer, value);
        stream.Write(buffer);
    }
}
