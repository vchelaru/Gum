"""Builds Icon.bmp from Gum/GumIcon.ico's 256x256 entry. Run from the repo root after changing
GumIcon.ico, then commit the result:

    python3 Tool/Gum.Avalonia/Packaging/make-icon-bmp.py

KNI and MonoGame's SDL window load an "Icon.bmp" embedded resource from the entry assembly and fall
back to their own logo without one; on macOS that icon is the Dock icon. Gum.Avalonia and GumPreview
embed this file. Standard library only: the .ico's 256 entry is a PNG, decoded here by hand.
"""

import os
import struct
import zlib

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
ICO = os.path.join(REPO_ROOT, 'Gum', 'GumIcon.ico')
BMP = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'Icon.bmp')


def png_entry(ico):
    count = struct.unpack('<H', ico[4:6])[0]
    for i in range(count):
        _, _, _, _, _, _, size, offset = struct.unpack('<BBBBHHII', ico[6 + 16 * i:22 + 16 * i])
        data = ico[offset:offset + size]
        if data.startswith(b'\x89PNG'):
            return data
    raise ValueError('GumIcon.ico has no PNG entry')


def decode_rgba_png(png):
    pos, idat = 8, b''
    while pos < len(png):
        length, kind = struct.unpack('>I4s', png[pos:pos + 8])
        body = png[pos + 8:pos + 8 + length]
        if kind == b'IHDR':
            width, height, depth, color, _, _, interlace = struct.unpack('>IIBBBBB', body)
            if (depth, color, interlace) != (8, 6, 0):
                raise ValueError('expected 8-bit non-interlaced RGBA')
        elif kind == b'IDAT':
            idat += body
        pos += 12 + length

    raw, stride, bpp = zlib.decompress(idat), width * 4, 4
    rows, prev = [], bytearray(stride)
    for y in range(height):
        start = y * (stride + 1)
        kind, line = raw[start], bytearray(raw[start + 1:start + 1 + stride])
        for x in range(stride):
            a = line[x - bpp] if x >= bpp else 0
            b = prev[x]
            c = prev[x - bpp] if x >= bpp else 0
            if kind == 1:
                line[x] = (line[x] + a) & 0xFF
            elif kind == 2:
                line[x] = (line[x] + b) & 0xFF
            elif kind == 3:
                line[x] = (line[x] + (a + b) // 2) & 0xFF
            elif kind == 4:
                p = a + b - c
                pa, pb, pc = abs(p - a), abs(p - b), abs(p - c)
                line[x] = (line[x] + (a if pa <= pb and pa <= pc else b if pb <= pc else c)) & 0xFF
        rows.append(bytes(line))
        prev = line
    return width, height, rows


def write_bmp(path, width, height, rows):
    # 32-bit BI_BITFIELDS with a BITMAPV4HEADER so the alpha mask survives SDL_LoadBMP.
    pixels = bytearray()
    for row in reversed(rows):  # BMP rows are bottom-up
        for x in range(0, len(row), 4):
            r, g, b, a = row[x:x + 4]
            pixels += bytes((b, g, r, a))
    header_size = 108
    offset = 14 + header_size
    info = struct.pack('<IiiHHIIiiII', header_size, width, height, 1, 32, 3, len(pixels), 2835, 2835, 0, 0)
    info += struct.pack('<IIII', 0x00FF0000, 0x0000FF00, 0x000000FF, 0xFF000000)
    info += b'BGRs' + bytes(36) + bytes(12)
    with open(path, 'wb') as f:
        f.write(b'BM' + struct.pack('<IHHI', offset + len(pixels), 0, 0, offset))
        f.write(info)
        f.write(pixels)


def main():
    with open(ICO, 'rb') as f:
        width, height, rows = decode_rgba_png(png_entry(f.read()))
    write_bmp(BMP, width, height, rows)
    print(f'wrote {BMP} ({width}x{height})')


if __name__ == '__main__':
    main()
