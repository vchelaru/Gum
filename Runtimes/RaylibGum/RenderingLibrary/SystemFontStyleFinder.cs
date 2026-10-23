using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RenderingLibrary.Content;

/// <summary>
/// Finds the file of a bold and/or italic cut of a font family in a folder of font files by reading each file's
/// family name (name table, ID 1) and its bold/italic bits (OS/2 fsSelection, or head macStyle when there is no
/// OS/2 table), so a family name picks the same cut a system font manager (Skia's) would, whatever the files are
/// called. Each folder is read once.
/// </summary>
internal static class SystemFontStyleFinder
{
    private readonly record struct FontFace(string Family, bool IsBold, bool IsItalic, string Path);

    // Lazy so threads that ask for one folder at the same time share a single scan.
    private static readonly ConcurrentDictionary<string, Lazy<IReadOnlyList<FontFace>>> facesByDirectory =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the path of the cut of <paramref name="family"/> in <paramref name="directory"/> (searched
    /// recursively) whose bold and italic bits equal the requested ones, or null when there is none.
    /// </summary>
    public static string? Find(string directory, string family, bool isBold, bool isItalic)
    {
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return null;
        }

        foreach (FontFace face in facesByDirectory.GetOrAdd(directory,
            folder => new Lazy<IReadOnlyList<FontFace>>(() => ReadFaces(folder))).Value)
        {
            if (face.IsBold == isBold && face.IsItalic == isItalic
                && string.Equals(face.Family, family, StringComparison.OrdinalIgnoreCase))
            {
                return face.Path;
            }
        }
        return null;
    }

    private static IReadOnlyList<FontFace> ReadFaces(string directory)
    {
        List<FontFace> faces = new();
        EnumerationOptions options = new() { RecurseSubdirectories = true, IgnoreInaccessible = true };
        foreach (string file in Directory.EnumerateFiles(directory, "*", options))
        {
            string extension = Path.GetExtension(file);
            if (!extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase)
                && !extension.Equals(".otf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                using FileStream stream = File.OpenRead(file);
                if (TryReadFace(stream, file, out FontFace face))
                {
                    faces.Add(face);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // An unreadable file is not a candidate.
            }
        }
        return faces;
    }

    /// <summary>
    /// Reads only the table directory and the name, OS/2 and head tables, not the whole file.
    /// </summary>
    private static bool TryReadFace(Stream stream, string path, out FontFace face)
    {
        face = default;
        byte[]? header = ReadAt(stream, 0, 12);
        if (header == null)
        {
            return false;
        }

        int tableCount = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(4));
        byte[]? directory = ReadAt(stream, 12, tableCount * 16);
        if (directory == null)
        {
            return false;
        }

        byte[]? name = null, os2 = null, head = null;
        for (int i = 0; i < tableCount; i++)
        {
            int record = i * 16;
            string tag = Encoding.ASCII.GetString(directory, record, 4);
            long offset = BinaryPrimitives.ReadUInt32BigEndian(directory.AsSpan(record + 8));
            int length = (int)System.Math.Min(BinaryPrimitives.ReadUInt32BigEndian(directory.AsSpan(record + 12)), 1 << 20);
            switch (tag)
            {
                case "name": name = ReadAt(stream, offset, length); break;
                case "OS/2": os2 = ReadAt(stream, offset, length); break;
                case "head": head = ReadAt(stream, offset, length); break;
            }
        }

        string? family = name != null ? ReadFamilyName(name) : null;
        if (family == null)
        {
            return false;
        }

        bool isBold, isItalic;
        if (os2 != null && os2.Length >= 64)
        {
            ushort fsSelection = BinaryPrimitives.ReadUInt16BigEndian(os2.AsSpan(62));
            isItalic = (fsSelection & 0x01) != 0;
            isBold = (fsSelection & 0x20) != 0;
        }
        else if (head != null && head.Length >= 46)
        {
            ushort macStyle = BinaryPrimitives.ReadUInt16BigEndian(head.AsSpan(44));
            isBold = (macStyle & 0x01) != 0;
            isItalic = (macStyle & 0x02) != 0;
        }
        else
        {
            return false;
        }

        face = new FontFace(family, isBold, isItalic, path);
        return true;
    }

    private static byte[]? ReadAt(Stream stream, long offset, int length)
    {
        if (length <= 0 || offset < 0 || offset + length > stream.Length)
        {
            return null;
        }
        stream.Position = offset;
        byte[] buffer = new byte[length];
        stream.ReadExactly(buffer);
        return buffer;
    }

    /// <summary>
    /// The family name (ID 1), preferring the Windows Unicode record and falling back to the Macintosh Roman one.
    /// </summary>
    private static string? ReadFamilyName(byte[] data)
    {
        if (data.Length < 6)
        {
            return null;
        }
        int count = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(2));
        int stringsOffset = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(4));
        string? macName = null;
        for (int i = 0; i < count; i++)
        {
            int record = 6 + i * 12;
            if (record + 12 > data.Length)
            {
                break;
            }
            ushort platform = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(record));
            ushort encoding = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(record + 2));
            ushort nameId = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(record + 6));
            int length = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(record + 8));
            int start = stringsOffset + BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(record + 10));
            if (nameId != 1 || start + length > data.Length)
            {
                continue;
            }
            if (platform == 3 && (encoding == 1 || encoding == 0))
            {
                return Encoding.BigEndianUnicode.GetString(data, start, length);
            }
            if (platform == 1 && encoding == 0)
            {
                macName ??= Encoding.Latin1.GetString(data, start, length);
            }
        }
        return macName;
    }
}
