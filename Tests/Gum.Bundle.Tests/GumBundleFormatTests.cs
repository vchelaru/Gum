using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.Bundle;
using Shouldly;

namespace Gum.Bundle.Tests;

public class GumBundleFormatTests
{
    [Fact]
    public void Header_bytes_are_GUMP_then_0x01()
    {
        MemoryStream stream = new MemoryStream();
        GumBundleWriter.Write(stream, Array.Empty<(string, byte[])>());

        byte[] bytes = stream.ToArray();
        bytes.Length.ShouldBeGreaterThanOrEqualTo(5);
        bytes[0].ShouldBe((byte)0x47);
        bytes[1].ShouldBe((byte)0x55);
        bytes[2].ShouldBe((byte)0x4D);
        bytes[3].ShouldBe((byte)0x50);
        bytes[4].ShouldBe((byte)0x01);
    }

    [Fact]
    public void Read_returns_empty_bundle_for_writer_called_with_no_entries()
    {
        MemoryStream stream = new MemoryStream();
        GumBundleWriter.Write(stream, Array.Empty<(string, byte[])>());
        stream.Position = 0;

        GumBundle bundle = GumBundleReader.Read(stream);

        bundle.Version.ShouldBe((byte)0x01);
        bundle.Entries.Count.ShouldBe(0);
    }

    [Fact]
    public void Read_throws_when_magic_bytes_are_wrong()
    {
        byte[] bad = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01 };
        MemoryStream stream = new MemoryStream(bad);

        Should.Throw<GumBundleFormatException>(() => GumBundleReader.Read(stream));
    }

    [Fact]
    public void Read_throws_when_payload_is_truncated()
    {
        MemoryStream stream = new MemoryStream();
        GumBundleWriter.Write(stream, new (string, byte[])[]
        {
            ("a.txt", new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })
        });

        byte[] full = stream.ToArray();
        // Truncate the payload (keep header + a few bytes)
        byte[] truncated = full.Take(full.Length / 2).ToArray();
        MemoryStream truncatedStream = new MemoryStream(truncated);

        Should.Throw<GumBundleFormatException>(() => GumBundleReader.Read(truncatedStream));
    }

    [Fact]
    public void Read_throws_when_version_is_newer_than_supported()
    {
        byte[] futureHeader = new byte[] { 0x47, 0x55, 0x4D, 0x50, 0xFF };
        MemoryStream stream = new MemoryStream(futureHeader);

        Should.Throw<GumBundleFormatException>(() => GumBundleReader.Read(stream));
    }

    [Fact]
    public void Roundtrip_handles_binary_content_unchanged()
    {
        byte[] binary = new byte[] { 0x00, 0xFF, 0x89, 0x50, 0x4E, 0x47, 0x00, 0x00 };
        MemoryStream stream = new MemoryStream();
        GumBundleWriter.Write(stream, new (string, byte[])[] { ("image.png", binary) });
        stream.Position = 0;

        GumBundle bundle = GumBundleReader.Read(stream);

        bundle.Entries["image.png"].ShouldBe(binary);
    }

    [Fact]
    public void Roundtrip_handles_unicode_path()
    {
        string path = "Screens/メニュー.gusx";
        byte[] content = new byte[] { 1, 2, 3 };
        MemoryStream stream = new MemoryStream();
        GumBundleWriter.Write(stream, new (string, byte[])[] { (path, content) });
        stream.Position = 0;

        GumBundle bundle = GumBundleReader.Read(stream);

        bundle.Entries.ContainsKey(path).ShouldBeTrue();
        bundle.Entries[path].ShouldBe(content);
    }

    [Fact]
    public void Roundtrip_multiple_entries_preserves_all_paths_and_bytes()
    {
        (string, byte[])[] entries = new (string, byte[])[]
        {
            ("a.gumx", new byte[] { 1 }),
            ("Screens/Main.gusx", new byte[] { 2, 3 }),
            ("Components/Button.gucx", new byte[] { 4, 5, 6 }),
        };
        MemoryStream stream = new MemoryStream();
        GumBundleWriter.Write(stream, entries);
        stream.Position = 0;

        GumBundle bundle = GumBundleReader.Read(stream);

        bundle.Entries.Count.ShouldBe(3);
        bundle.Entries["a.gumx"].ShouldBe(new byte[] { 1 });
        bundle.Entries["Screens/Main.gusx"].ShouldBe(new byte[] { 2, 3 });
        bundle.Entries["Components/Button.gucx"].ShouldBe(new byte[] { 4, 5, 6 });
    }

    [Fact]
    public void Roundtrip_single_entry_preserves_path_and_bytes()
    {
        byte[] content = new byte[] { 10, 20, 30, 40 };
        MemoryStream stream = new MemoryStream();
        GumBundleWriter.Write(stream, new (string, byte[])[] { ("only.txt", content) });
        stream.Position = 0;

        GumBundle bundle = GumBundleReader.Read(stream);

        bundle.Entries.Count.ShouldBe(1);
        bundle.Entries["only.txt"].ShouldBe(content);
    }

    [Fact]
    public void Write_emits_entries_in_lexicographic_order()
    {
        (string, byte[])[] entries = new (string, byte[])[]
        {
            ("c.txt", new byte[] { 3 }),
            ("a.txt", new byte[] { 1 }),
            ("b.txt", new byte[] { 2 }),
        };
        MemoryStream stream = new MemoryStream();
        GumBundleWriter.Write(stream, entries);
        stream.Position = 0;

        GumBundle bundle = GumBundleReader.Read(stream);

        bundle.EntryPathsInOrder.ShouldBe(new[] { "a.txt", "b.txt", "c.txt" });
    }

    [Fact]
    public void Write_is_deterministic_for_identical_input()
    {
        (string, byte[])[] entries = new (string, byte[])[]
        {
            ("a.txt", new byte[] { 1, 2, 3 }),
            ("Screens/Main.gusx", new byte[] { 4, 5, 6 }),
        };
        MemoryStream first = new MemoryStream();
        MemoryStream second = new MemoryStream();

        GumBundleWriter.Write(first, entries);
        GumBundleWriter.Write(second, entries);

        first.ToArray().ShouldBe(second.ToArray());
    }
}
