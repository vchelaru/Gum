"""Builds Gum.icns (the Gum.app bundle icon) from Gum/GumIcon.ico, so the Dock icon on macOS matches
the Windows app icon. Run from the repo root after changing GumIcon.ico, then commit the result:

    python Tool/Gum.Avalonia/Packaging/macOS/make-icns.py

Needs Pillow (pip install pillow). Writes the .icns by hand rather than through iconutil so it runs
on any OS; the .ico's own 16..256 entries are used as-is, no resampling.
"""

import io
import os
import struct

from PIL import Image

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..', '..'))
ICO = os.path.join(REPO_ROOT, 'Gum', 'GumIcon.ico')
ICNS = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'Gum.icns')

# icns entry type -> pixel size. Each type is a PNG payload; the @2x types share a size with a 1x
# type but are what Retina displays pick, so both are written.
ENTRIES = [
    (b'icp4', 16),   # 16x16
    (b'icp5', 32),   # 32x32
    (b'ic11', 32),   # 16x16@2x
    (b'icp6', 64),   # 64x64
    (b'ic12', 64),   # 32x32@2x
    (b'ic07', 128),  # 128x128
    (b'ic08', 256),  # 256x256
    (b'ic13', 256),  # 128x128@2x
]


def png_bytes(ico_path, size):
    image = Image.open(ico_path)
    image.size = (size, size)
    image.load()
    buffer = io.BytesIO()
    image.save(buffer, format='PNG')
    return buffer.getvalue()


def main():
    chunks = b''
    for entry_type, size in ENTRIES:
        payload = png_bytes(ICO, size)
        chunks += entry_type + struct.pack('>I', 8 + len(payload)) + payload
    with open(ICNS, 'wb') as f:
        f.write(b'icns' + struct.pack('>I', 8 + len(chunks)) + chunks)
    print(f'wrote {ICNS} ({8 + len(chunks)} bytes)')


if __name__ == '__main__':
    main()
