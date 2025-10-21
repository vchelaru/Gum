import os
import re
import sys
import hashlib
import concurrent.futures
from pathlib import Path
from urllib.parse import unquote

# pip install markdown-it-py
from markdown_it import MarkdownIt


# --- CONFIG ---
GUM_GIT = Path(r"c:/git/gum")
DOCS_DIR = GUM_GIT / "docs"
ASSETS_DIR = DOCS_DIR / ".gitbook" / "assets"
ASSET_EXTS = {".gif", ".svg", ".webp", ".png", ".mp4", ".jpg", ".jpeg", ".avif"}  # adjust as needed

# --- Helpers ---
# IMG_PATTERN = re.compile(
#     r"""
#     !\[[^\]]*\]\((?P<url1>[^)]+)\)   # Markdown image syntax
#     |<img[^>]+src=["'](?P<url2>[^"']+)["'][^>]*>  # HTML <img src="...">
#     """,
#     re.IGNORECASE | re.VERBOSE,
# )
IMG_PATTERN = re.compile(
    r"""
    !\[[^\]]*\]\(\s*
        (?:
            <(?P<url_angle>[^>]+)>            # ![]( <...> ) — allow ')' inside
          | (?P<url_plain>(?:\\\)|[^\)])+)    # ![]( ... ) — allow escaped '\)'
        )
    \s*\)
    |
    <img[^>]+src=["'](?P<html_src>[^"']+)["'][^>]*>   # HTML <img>
    """,
    re.IGNORECASE | re.VERBOSE,
)

def list_markdown_files(root: Path):
    for p in root.rglob("*.md"):
        # skip GitBook cache if present
        if ".git" in p.parts:
            continue
        yield p

def list_asset_files(root: Path, exts: set[str]):
    for p in root.iterdir():
        if p.is_file() and p.suffix.lower() in exts:
            yield p

# def extract_image_basenames(markdown_path: Path) -> set[str]:
#     try:
#         text = markdown_path.read_text(encoding="utf-8", errors="ignore")
#     except Exception:
#         return set()
#     names = set()
#     for m in IMG_PATTERN.finditer(text):
#         url = m.group("url1") or m.group("url2") or ""
#         url = unquote(url.strip())
#         # drop any query (# anchors, ?params), normalize path, keep basename
#         url = url.split("#", 1)[0].split("?", 1)[0]
#         base = os.path.basename(url)
#         if base:
#             names.add(base)
#     return names

# def extract_image_basenames(markdown_path: Path) -> set[str]:
#     try:
#         text = markdown_path.read_text(encoding="utf-8", errors="ignore")
#     except Exception:
#         return set()

#     names = set()
#     for m in IMG_PATTERN.finditer(text):
#         url = (m.group("url1") or m.group("url2") or "").strip()

#         # Drop anchors/query
#         url = url.split("#", 1)[0].split("?", 1)[0].strip()

#         # Remove Markdown wrappers like <...> and any surrounding quotes
#         if url.startswith("<") and url.endswith(">"):
#             url = url[1:-1].strip()
#         if (url.startswith('"') and url.endswith('"')) or (url.startswith("'") and url.endswith("'")):
#             url = url[1:-1].strip()

#         base = os.path.basename(url)
#         if base:
#             names.add(base)
#     return names

def extract_image_basenames(markdown_path: Path) -> set[str]:
    try:
        text = markdown_path.read_text(encoding="utf-8", errors="ignore")
    except Exception:
        return set()

    names = set()
    for m in IMG_PATTERN.finditer(text):
        url = (m.group("url_angle") or m.group("url_plain") or m.group("html_src") or "").strip()

        # Decode and normalize
        url = unquote(url)
        url = url.split("#", 1)[0].split("?", 1)[0].strip()

        # If someone still wrapped quotes inside <...>, strip them
        if (url.startswith('"') and url.endswith('"')) or (url.startswith("'") and url.endswith("'")):
            url = url[1:-1].strip()

        base = os.path.basename(url.replace("\\", "/"))
        if base:
            names.add(base)

    return names


def sha256_file(path: Path, bufsize: int = 1 << 20) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        while True:
            chunk = f.read(bufsize)
            if not chunk:
                break
            h.update(chunk)
    return h.hexdigest()

# --- Build asset index (once) ---
asset_files = {p.name: p for p in list_asset_files(ASSETS_DIR, ASSET_EXTS)}
asset_names = set(asset_files.keys())

# --- Parse all markdown once, build reverse index ---
md_files = list(list_markdown_files(DOCS_DIR))
image_to_pages: dict[str, set[Path]] = {name: set() for name in asset_names}
page_to_missing: dict[Path, set[str]] = {}

for md in md_files:
    names_in_page = extract_image_basenames(md)
    # Which of these are known assets?
    for name in names_in_page:
        if name in asset_names:
            image_to_pages[name].add(md)
    # Track missing images (referenced but not present in assets)
    missing = {n for n in names_in_page if n not in asset_names}
    if missing:
        page_to_missing[md] = missing

# --- Unused images ---
unused_images = sorted([name for name, pages in image_to_pages.items() if not pages])

# --- Duplicate detection by content hash (threaded) ---
dupe_map: dict[str, list[Path]] = {}
with concurrent.futures.ThreadPoolExecutor(max_workers=os.cpu_count() or 4) as ex:
    future_to_name = {ex.submit(sha256_file, path): name for name, path in asset_files.items()}
    for fut in concurrent.futures.as_completed(future_to_name):
        name = future_to_name[fut]
        try:
            digest = fut.result()
        except Exception:
            continue
        dupe_map.setdefault(digest, []).append(asset_files[name])

duplicates = [paths for paths in dupe_map.values() if len(paths) > 1]

# --- Output summaries ---
print(f"Total assets scanned: {len(asset_names)}")
print(f"Total markdown files: {len(md_files)}")
print(f"Total unused images: {len(unused_images)}")
if unused_images:
    print("Sample unused:", unused_images[:10])

print(f"Pages with missing images: {len(page_to_missing)}")
for i, (page, missing) in enumerate(page_to_missing.items()):
    if i >= 5:
        print("... (truncated)")
        break
    print(f"- {page.relative_to(DOCS_DIR)} missing {sorted(missing)}")

print(f"Duplicate image groups: {len(duplicates)}")
for i, group in enumerate(duplicates[:5], 1):
    print(f"[{i}]")
    for p in group:
        print("   ", p.name, "->", p)

print("Cleaning up unused images...")
DOCS_DIR = Path(DOCS_DIR).resolve()  # ensure absolute
for name in unused_images:
    p = Path(asset_files[name]).resolve()

    # Guard: only delete files under docs/
    try:
        p.relative_to(DOCS_DIR)
    except ValueError:
        print(f"Skipping: {p} (outside docs)")
        continue

    # (Optional tiny safeties)
    if not p.is_file() or p.is_symlink():
        print(f"Skipping: {p} (not a regular file)")
        continue

    if p.suffix.lower() not in {ext.lower() for ext in ASSET_EXTS}:
        print(f"Skipping: {p} (not an allowed image type)")
        continue

    try:
        p.unlink(missing_ok=True)  # Python 3.8+
        print(f"Deleted: {p}")
    except Exception as e:
        print(f"Failed to delete {p}: {e}")


print("Canonicalizing duplicate images...")
for group in duplicates:
    # Skip groups of size 1
    if len(group) < 2:
        continue

    # 1. Pick the shortest filename (tie-break alphabetically)
    names = [p.name for p in group]
    canonical = min(names, key=lambda n: (len(n), n.lower()))
    others = [n for n in names if n != canonical]

    # 2. Find every markdown file that uses any of the other names
    pages = set()
    for old in others:
        pages.update(image_to_pages.get(old, set()))

    if not pages:
        continue

    print(f"\nGroup: {canonical} <- {others}")
    # 3. Replace occurrences in each page
    for page in pages:
        text = page.read_text(encoding="utf-8", errors="ignore")
        new_text = text
        for old in others:
            new_text = new_text.replace(old, canonical)

        if new_text != text:
            print(f"  Updated {page.relative_to(DOCS_DIR)}")
            page.write_text(new_text, encoding="utf-8")

            # update in-memory map
            image_to_pages.setdefault(canonical, set()).add(page)
            image_to_pages[old].discard(page)

print("\nParsing all markdown files to check for syntax errors...")
md = MarkdownIt()
total_bad_markdown_files = 0
for path in DOCS_DIR.rglob("*.md"):
    try:
        text = path.read_text(encoding="utf-8")
        md.parse(text)  # just parsing — no rendering
    except Exception as e:
        total_bad_markdown_files += 1
        print(f"⚠️ Parse error in {path}: {e}")

if total_bad_markdown_files == 0:
    print("✅ All markdown files parsed successfully.")

BROKEN_IMAGE_RE = re.compile(r'!\[[^\]]*\]\(<[^>]*$')  # opens with < but never closes >

def find_broken_image_syntax(docs_dir: Path):
    bad_lines = []
    for md_path in docs_dir.rglob("*.md"):
        with md_path.open(encoding="utf-8", errors="ignore") as f:
            for i, line in enumerate(f, start=1):
                if BROKEN_IMAGE_RE.search(line):
                    bad_lines.append((md_path, i, line.strip()))
    return bad_lines

# Example usage:
issues = find_broken_image_syntax(Path(DOCS_DIR))
if not issues:
    print("✅ No broken image syntax found.")
else:
    print(f"⚠️ Found {len(issues)} possibly malformed image lines:")
    for path, line_num, line_text in issues:
        print(f"  {path.relative_to(DOCS_DIR)}:{line_num}: {line_text}")


link_re = re.compile(r'\[[^\]]*\]\(([^)]+)\)')
total_bad_links = 0
for path in DOCS_DIR.rglob("*.md"):
    for m in link_re.findall(path.read_text(encoding="utf-8")):
        if not m.startswith(("http://", "https://")) and m.endswith(".md"):
            target = (path.parent / m.split("#")[0]).resolve()
            if not target.exists():
                total_bad_links += 1
                print(f"❌ Broken link in {path}: {m}")

if total_bad_links == 0:
    print("✅ No broken links found.")


# Markdown: [text](url) and ![alt](url), supports <angle-wrapped> URLs
MD_URL = re.compile(
    r'!?\[[^\]]*\]\(\s*(?:<(?P<ang>[^>]+)>|(?P<plain>[^)]+))\s*\)',
    re.IGNORECASE,
)
# HTML (basic): <img src="..."> or <a href="...">
HTML_URL = re.compile(
    r'<(?:img|a)[^>]+(?:src|href)=["\'](?P<html>[^"\']+)["\'][^>]*>',
    re.IGNORECASE,
)

SKIP_SCHEMES = ("http://", "https://", "mailto:", "tel:")

FENCE = re.compile(r"^```")   # start/end of fenced block
INLINE_CODE = re.compile(r"`[^`]*`")  # inline code spans

def iter_links(md_path: Path):
    lines = md_path.read_text(encoding="utf-8", errors="ignore").splitlines()
    in_code_block = False
    for i, line in enumerate(lines, start=1):
        if FENCE.match(line.strip()):
            in_code_block = not in_code_block
            continue
        if in_code_block:
            continue  # skip entire fenced block

        # Strip inline code before scanning
        clean_line = INLINE_CODE.sub("", line)

        for m in MD_URL.finditer(clean_line):
            url = (m.group("ang") or m.group("plain") or "").strip()
            yield i, url
        for m in HTML_URL.finditer(clean_line):
            url = (m.group("html") or "").strip()
            yield i, url

# def iter_links(md_path: Path):
#     text = md_path.read_text(encoding="utf-8", errors="ignore").splitlines()
#     for i, line in enumerate(text, start=1):
#         for m in MD_URL.finditer(line):
#             url = (m.group("ang") or m.group("plain") or "").strip()
#             yield i, url
#         for m in HTML_URL.finditer(line):
#             url = (m.group("html") or "").strip()
#             yield i, url

def target_exists(src_file: Path, url: str) -> bool:
    # Drop query/anchor
    main = url.split("#", 1)[0].split("?", 1)[0].strip()
    if not main or main.startswith(SKIP_SCHEMES) or main.startswith("#"):
        return True  # external or anchor-only: skip
    # Absolute site-root URLs are not resolvable locally → mark missing
    if main.startswith("/"):
        return False

    # Resolve relative to the source file
    tgt = (src_file.parent / main).resolve()
    # Keep validation inside docs/
    try:
        tgt.relative_to(DOCS_DIR)
    except ValueError:
        return False

    if tgt.exists():
        return True
    # GitBook-style folder link → README.md
    if tgt.is_dir():
        for readme in ("README.md", "readme.md"):
            if (tgt / readme).exists():
                return True
    return False

def validate_relative_links():
    broken = []
    for md in DOCS_DIR.rglob("*.md"):
        for line_no, url in iter_links(md):
            if url.startswith(SKIP_SCHEMES) or url.startswith("#"):
                continue
            if not target_exists(md, url):
                broken.append((md.relative_to(DOCS_DIR), line_no, url))
    if not broken:
        print("✅ All relative links/images resolve.")
    else:
        print(f"❌ Broken relative links/images: {len(broken)}")
        for path, line, url in broken:
            print(f"  {path}:{line}: {url}")
    return broken

# Run it
validate_relative_links()