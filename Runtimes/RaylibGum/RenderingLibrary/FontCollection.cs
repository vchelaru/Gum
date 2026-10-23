using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RenderingLibrary.Content;

/// <summary>
/// A font face in a font file: a plain .ttf/.otf has one (<see cref="FaceIndex"/> -1), a .ttc/.otc collection
/// holds several, picked by <see cref="FaceIndex"/>.
/// </summary>
internal readonly record struct FontFileFace(string Path, int FaceIndex = -1);

/// <summary>
/// Reads TrueType/OpenType font collections (.ttc/.otc, tag 'ttcf'), which hold several faces in one file and
/// which raylib cannot load (it reads the face at offset 0 of a single-face file). A face is cut out as a
/// standalone font that raylib loads from memory.
/// </summary>
internal static class FontCollection
{
    private const uint CollectionTag = 0x74746366; // 'ttcf'

    public static bool IsCollectionExtension(string path)
    {
        string extension = Path.GetExtension(path);
        return extension.Equals(".ttc", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".otc", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The offsets of the faces in <paramref name="file"/>: one per face of a collection, or just 0 for a plain
    /// font file. Empty when the file is truncated.
    /// </summary>
    public static IReadOnlyList<long> GetFaceOffsets(Stream file)
    {
        byte[] header = new byte[12];
        file.Position = 0;
        if (file.Length < header.Length || file.Read(header, 0, header.Length) < header.Length)
        {
            return Array.Empty<long>();
        }
        if (BinaryPrimitives.ReadUInt32BigEndian(header) != CollectionTag)
        {
            return new long[] { 0 };
        }

        int count = (int)System.Math.Min(BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(8)), 1024);
        List<long> offsets = new();
        byte[] entry = new byte[4];
        for (int i = 0; i < count; i++)
        {
            file.Position = 12 + 4 * i;
            if (file.Read(entry, 0, 4) < 4)
            {
                break;
            }
            offsets.Add(BinaryPrimitives.ReadUInt32BigEndian(entry));
        }
        return offsets;
    }

    /// <summary>
    /// Returns the face of a collection file at <paramref name="faceIndex"/> as a standalone single-face font
    /// (its tables copied next to a rebuilt table directory), or null when the file has no such face.
    /// </summary>
    public static byte[]? ExtractFace(string path, int faceIndex)
    {
        byte[] file = File.ReadAllBytes(path);
        using MemoryStream stream = new(file, writable: false);
        IReadOnlyList<long> offsets = GetFaceOffsets(stream);
        if (faceIndex < 0 || faceIndex >= offsets.Count || offsets[faceIndex] + 12 > file.Length)
        {
            return null;
        }

        int face = (int)offsets[faceIndex];
        int tableCount = BinaryPrimitives.ReadUInt16BigEndian(file.AsSpan(face + 4));
        if (face + 12 + tableCount * 16 > file.Length)
        {
            return null;
        }

        using MemoryStream output = new();
        byte[] directory = new byte[12 + tableCount * 16];
        Array.Copy(file, face, directory, 0, directory.Length);
        output.Write(directory, 0, directory.Length);
        for (int i = 0; i < tableCount; i++)
        {
            int record = face + 12 + i * 16;
            long offset = BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(record + 8));
            long length = BinaryPrimitives.ReadUInt32BigEndian(file.AsSpan(record + 12));
            if (offset + length > file.Length)
            {
                return null;
            }
            while (output.Position % 4 != 0)
            {
                output.WriteByte(0);
            }
            BinaryPrimitives.WriteUInt32BigEndian(directory.AsSpan(12 + i * 16 + 8), (uint)output.Position);
            output.Write(file, (int)offset, (int)length);
        }

        // The directory was written before the tables' new offsets were known; write it again with them.
        byte[] result = output.ToArray();
        Array.Copy(directory, result, directory.Length);
        return result;
    }
}
