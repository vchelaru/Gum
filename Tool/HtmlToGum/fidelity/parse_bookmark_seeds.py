"""Parse Chrome bookmarks HTML → unique host seeds (first bookmark per host wins)."""
from __future__ import annotations

import json
from collections import OrderedDict
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import urlparse


class BookmarkParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__()
        self.hrefs: list[tuple[str, str]] = []
        self._href: str | None = None
        self._title_parts: list[str] = []
        self._in_a = False

    def handle_starttag(self, tag, attrs):
        if tag.lower() == "a":
            d = dict(attrs)
            self._href = d.get("HREF") or d.get("href")
            self._title_parts = []
            self._in_a = True

    def handle_endtag(self, tag):
        if tag.lower() == "a" and self._in_a:
            title = "".join(self._title_parts).strip()
            if self._href:
                self.hrefs.append((self._href, title))
            self._in_a = False

    def handle_data(self, data):
        if self._in_a:
            self._title_parts.append(data)


def host_key(href: str) -> str | None:
    u = urlparse(href)
    if u.scheme not in ("http", "https") or not u.netloc:
        return None
    # Collapse www. so www.example.com and example.com are one base site.
    host = u.netloc.lower()
    if host.startswith("www."):
        host = host[4:]
    return host


def parse_unique_seeds(bookmarks_path: Path) -> dict:
    parser = BookmarkParser()
    parser.feed(bookmarks_path.read_text(encoding="utf-8", errors="replace"))

    http_count = 0
    seen: OrderedDict[str, dict] = OrderedDict()
    skipped: list[dict] = []

    for href, title in parser.hrefs:
        if not href or not href.lower().startswith(("http://", "https://")):
            continue
        http_count += 1
        key = host_key(href)
        if not key:
            continue
        if key in seen:
            skipped.append({"url": href, "title": title, "kept": seen[key]["url"]})
            continue
        seen[key] = {"host": key, "url": href, "title": title}

    return {
        "bookmarksPath": str(bookmarks_path),
        "httpBookmarkCount": http_count,
        "uniqueHosts": len(seen),
        "skippedDuplicates": len(skipped),
        "seeds": list(seen.values()),
        "skipped": skipped,
    }


if __name__ == "__main__":
    import sys

    if len(sys.argv) < 2:
        print("Usage: python parse_bookmark_seeds.py <bookmarks.html> [seeds.json]", file=sys.stderr)
        sys.exit(2)
    path = Path(sys.argv[1])
    data = parse_unique_seeds(path)
    out = Path(sys.argv[2]) if len(sys.argv) > 2 else Path("seeds.json")
    out.write_text(json.dumps(data, indent=2), encoding="utf-8")
    print(f"http bookmarks: {data['httpBookmarkCount']}")
    print(f"unique hosts: {data['uniqueHosts']}")
    print(f"skipped dupes: {data['skippedDuplicates']}")
    for s in data["seeds"]:
        print(f"  {s['host']}\t{s['url']}")
    print(f"wrote {out}")
