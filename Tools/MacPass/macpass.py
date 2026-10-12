#!/usr/bin/env python3
"""Replayable macOS pass for the packaged Gum.app. See Tools/MacPass/README.md.

Each scenario launches the app on a fresh copy of a sample project, drives it with real input
(gumdrv) and checks outcomes on disk, in the menu bar and in window titles. Screenshots are taken
only when a check fails, so a green run costs a reader nothing.

    python3 Tools/MacPass/macpass.py                  run everything
    python3 Tools/MacPass/macpass.py hotkeys menu     run scenarios whose name contains a word
    python3 Tools/MacPass/macpass.py --list
"""
import argparse
import os
import shutil
import subprocess
import sys
import tempfile
import time
import traceback
import xml.etree.ElementTree as ET
from pathlib import Path

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[1]
SAMPLE = REPO / "Samples/MonoGameGumFromFile/MonoGameGumFromFile/Content"
APP = Path(os.environ.get("GUM_APP", REPO / "publish/app/Gum.app"))
ARTIFACTS = Path(os.environ.get("MACPASS_OUT", REPO / "publish/macpass"))
CACHE = Path.home() / "Library/Caches/gum-macpass"


def build_driver() -> Path:
    src, exe = HERE / "gumdrv.swift", CACHE / "gumdrv"
    CACHE.mkdir(parents=True, exist_ok=True)
    if not exe.exists() or exe.stat().st_mtime < src.stat().st_mtime:
        subprocess.run(["swiftc", "-O", str(src), "-o", str(exe)], check=True,
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    return exe


DRV = None


def drv(*args, check=True) -> str:
    r = subprocess.run([str(DRV), *map(str, args)], capture_output=True, text=True, timeout=30)
    if check and r.returncode != 0:
        raise RuntimeError(f"gumdrv {args}: {r.stdout.strip()} {r.stderr.strip()}")
    return r.stdout.strip()


MODIFIER_BITS = {"Shift": 0x20000, "Control": 0x40000, "Option": 0x80000, "Command": 0x100000}


def held_modifiers() -> str:
    """The modifier keys the system currently believes are down, e.g. 'Command+Shift'; empty when none."""
    state = int(drv("flags").split()[0], 16)
    return "+".join(name for name, bit in MODIFIER_BITS.items() if state & bit)


def wait_until(pred, timeout=10.0, step=0.25):
    end = time.time() + timeout
    while time.time() < end:
        try:
            if pred():
                return True
        except Exception:
            pass
        time.sleep(step)
    return False


class Failure(Exception):
    pass


class Gum:
    """One running instance of the packaged app on a scratch copy of the sample project."""

    def __init__(self, work: Path, project="GumProject.gumx", args=(), copy_project=True, run=1):
        """run > 1 relaunches on the first run's project folder and user data (settings that persist)."""
        self.work = work
        self.dir = work / "proj"
        if copy_project and not self.dir.exists():
            shutil.copytree(SAMPLE, self.dir)
        self.userdata = work / "userdata"
        self.userdata.mkdir(exist_ok=True)
        self.log = open(work / f"gum{run}.log", "w")
        exe = os.environ.get("GUM_EXE") or str(APP / "Contents/MacOS/Gum")  # GUM_EXE: an unpackaged build
        cmd = [exe, "--user-data", str(self.userdata)]
        if project:
            cmd.append(str(self.dir / project))
        self.proc = subprocess.Popen(cmd + list(args), stdout=self.log, stderr=self.log)
        stem = Path(project).stem if project else "GumProject"  # a reopened last project
        if not wait_until(lambda: self.alive() and stem in self.title(), 45):
            raise Failure(f"main window titled '{stem}' never appeared (title='{self.title()}')")
        self.activate()
        if "--select" in args:
            # The Remove menu names the selected element, so it tells when `--select` has taken effect.
            element = args[list(args).index("--select") + 1].split("#")[0].split("/")[-1]
            if not wait_until(lambda: f"Edit>Remove>{element}" in self.menu_items(), 30):
                raise Failure(f"--select {element} never took effect")
        time.sleep(1.5)  # first frame and plugin load

    # -- process and windows
    @property
    def pid(self):
        return self.proc.pid

    def alive(self):
        return self.proc.poll() is None

    def activate(self):
        """Brings the app to the front and waits until it is active; input sent earlier is lost to the activation."""
        def front():
            drv("activate", self.pid)
            return drv("active", self.pid) == "true"
        if not wait_until(front, 10, 0.5):
            raise Failure("the app never became the active application")
        time.sleep(0.4)

    def windows(self):
        out = []
        for line in drv("windows", self.pid, check=False).splitlines():
            parts = line.split("|")
            if len(parts) == 3:
                x, y, w, h = (int(v) for v in parts[1].split())
                out.append({"title": parts[0], "x": x, "y": y, "w": w, "h": h, "subrole": parts[2]})
        return out

    def title(self):
        return drv("title", self.pid, check=False)

    def main_window(self):
        return next(w for w in self.windows() if w["subrole"] == "AXStandardWindow")

    # -- input
    def key(self, name, mods=""):
        drv("key", name, mods) if mods else drv("key", name)
        time.sleep(0.25)

    def type(self, text):
        drv("type", text)
        time.sleep(0.25)

    def has_sheet(self):
        return "AXSheet" in drv("tree", self.pid, 3, check=False)

    def wait_for_sheet(self, timeout=15):
        """Waits for a native open or save panel (a sheet on the main window), then lets it take focus."""
        if not wait_until(self.has_sheet, timeout):
            raise Failure("the native panel never appeared")
        time.sleep(1.0)

    def focus_tree(self):
        """Cmd+F then Escape puts keyboard focus in the tree and keeps the selection (clicking a row would not)."""
        self.key("f", "cmd")
        time.sleep(0.4)
        self.key("escape")
        time.sleep(0.4)

    def menu(self, *path):
        drv("menu", self.pid, *path)
        time.sleep(0.6)

    def click(self, rx, ry, how="left"):
        """Click at a point relative to the main window's top-left corner."""
        w = self.main_window()
        drv("click", w["x"] + rx, w["y"] + ry, how)
        time.sleep(0.4)

    def find(self, role, title):
        """Screen centre (x, y) of the first accessibility element with this role and title or value, or None."""
        out = drv("find", self.pid, role, title, check=False)
        return tuple(int(v) for v in out.split()) if out else None

    def has(self, role, title):
        return self.find(role, title) is not None

    def click_title(self, role, title, how="left"):
        """Clicks the centre of a control found through the accessibility tree, so no layout constant is needed."""
        pos = wait_until(lambda: self.find(role, title), 10) and self.find(role, title)
        if not pos:
            raise Failure(f"no {role} '{title}' in the accessibility tree")
        drv("click", *pos, how)
        time.sleep(0.5)

    def tree(self):
        return drv("tree", self.pid, 40, check=False).splitlines()

    def canvas(self):
        """Screen frame (x, y, w, h) in points of the canvas, the one large image in the editor tab."""
        for line in self.tree():
            if line.strip().startswith("AXImage"):
                x, y, w, h = (int(v) for v in line[line.index("[") + 1:-1].split())
                if w > 400 and h > 200:
                    return x, y, w, h
        raise Failure("the canvas is not in the accessibility tree")

    def zoom(self):
        """The toolbar's zoom percentage, read from the tree (camera zoom is never saved to disk)."""
        win = self.main_window()
        for line in self.tree():
            if line.strip().startswith("AXStaticText") and 'value="' in line:
                value = line.split('value="')[1].split('"')[0]
                y = int(line[line.index("[") + 1:-1].split()[1])
                if value.endswith("%") and value[:-1].isdigit() and y < win["y"] + 100:
                    return int(value[:-1])
        raise Failure("the zoom percentage is not in the accessibility tree")

    def scale(self):
        """Backing scale factor of the main display: a point of mouse travel is this many canvas pixels."""
        return float(drv("scale"))

    def find_color(self, rgb, tol=12):
        """Bounding box (x0, y0, x1, y1), in screen points, of the pixels near this colour in the canvas, or None.
        The canvas is a single image to Accessibility, so a selected element's rendered colour locates its
        edges and handles."""
        x, y, w, h = self.canvas()
        bmp = Path(tempfile.mkdtemp(prefix="macpass-bmp-")) / "canvas.bmp"
        subprocess.run(["screencapture", "-x", "-t", "bmp", f"-R{x},{y},{w},{h}", str(bmp)], check=True)
        data = bmp.read_bytes()
        offset = int.from_bytes(data[10:14], "little")
        width = int.from_bytes(data[18:22], "little")
        height = int.from_bytes(data[22:26], "little", signed=True)
        bpp = int.from_bytes(data[28:30], "little")
        if bpp not in (24, 32):
            raise Failure(f"unexpected screenshot format: {bpp} bpp")
        step = bpp // 8
        stride = (width * step + 3) // 4 * 4
        rows = abs(height)
        scale = width / w
        r, g, b = rgb
        x0 = y0 = 1 << 30
        x1 = y1 = -1
        for row in range(0, rows, 2):
            line = row * stride + offset
            px_y = rows - 1 - row if height > 0 else row  # a positive height is stored bottom-up
            seg = data[line:line + width * step]
            for col in range(0, width, 2):
                i = col * step
                if abs(seg[i + 2] - r) <= tol and abs(seg[i + 1] - g) <= tol and abs(seg[i] - b) <= tol:
                    x0, x1 = min(x0, col), max(x1, col)
                    y0, y1 = min(y0, px_y), max(y1, px_y)
        shutil.rmtree(bmp.parent, ignore_errors=True)
        if x1 < 0:
            return None
        return x + x0 / scale, y + y0 / scale, x + x1 / scale, y + y1 / scale

    def drag(self, rx1, ry1, rx2, ry2):
        w = self.main_window()
        drv("drag", w["x"] + rx1, w["y"] + ry1, w["x"] + rx2, w["y"] + ry2)
        time.sleep(0.8)

    def set_frame(self, x, y, w, h):
        drv("setframe", self.pid, x, y, w, h)
        time.sleep(0.8)

    def quit(self):
        self.key("q", "cmd")
        wait_until(lambda: not self.alive(), 10)

    def menu_items(self):
        items = {}
        for line in drv("menuinfo", self.pid).splitlines():
            path, enabled, cmd, mods = line.split("|")
            items[path] = {"enabled": enabled == "true", "cmd": cmd, "mods": mods}
        return items

    # -- evidence
    def shot(self, path: Path):
        path.parent.mkdir(parents=True, exist_ok=True)
        w = self.main_window()
        subprocess.run(["screencapture", "-x", f"-R{w['x']},{w['y']},{w['w']},{w['h']}", str(path)])

    def gucx(self, name):
        return self.dir / "Components" / f"{name}.gucx"

    def instance_count(self, element_file: Path):
        return len(ET.parse(element_file).getroot().findall(".//Instance"))

    def stop(self):
        if self.alive():
            self.proc.kill()
            self.proc.wait(10)
        self.log.close()


class Ctx:
    def __init__(self, name):
        self.name = name
        self.work = Path(tempfile.mkdtemp(prefix=f"macpass-{name}-"))
        self.gum = None
        self.failures = []

    def launch(self, **kw):
        if self.gum:
            self.gum.stop()
        self.runs = getattr(self, "runs", 0) + 1
        self.gum = Gum(self.work, run=self.runs, **kw)
        return self.gum

    def check(self, label, ok, detail=""):
        if not ok:
            self.failures.append(f"{label}{': ' + detail if detail else ''}")
        return ok

    def evidence(self):
        if self.gum and self.gum.alive():
            try:
                shot = ARTIFACTS / f"{self.name}-failure.png"
                self.gum.shot(shot)
                return str(shot)
            except Exception:
                pass
        return ""


# Window-relative points in the default 1280x720 layout of the sample project.
CHIP_CIRCLE = (54, 604)
CANVAS_DROP = (780, 330)

SCENARIOS = []


def scenario(name, known_bug=None):
    """known_bug: the issue number a failing scenario is expected to reproduce; it does not fail the run."""
    def wrap(fn):
        SCENARIOS.append((name, fn, known_bug))
        return fn
    return wrap


# --------------------------------------------------------------------------------------------
# Scenarios
# --------------------------------------------------------------------------------------------

@scenario("menu-bar")
def menu_bar(c: Ctx):
    g = c.launch(args=["--select", "ComponentWithState"])
    items = g.menu_items()
    (ARTIFACTS / "menus.txt").write_text("\n".join(
        f"{p}|{v['enabled']}|{v['cmd']}|{v['mods']}" for p, v in items.items()))
    for top in ("Gum", "File", "Edit", "View", "Content", "Plugins", "Help"):
        c.check(f"menu {top} exists", any(p == top or p.startswith(top + ">") for p in items) or top == "Gum"
                and any(p.startswith("Gum>") for p in items))
    c.check("About Gum is in the application menu", "Gum>About Gum" in items)
    c.check("Quit is in the application menu", "Gum>Quit" in items)
    undo, redo = items.get("Edit>Undo"), items.get("Edit>Redo")
    c.check("Edit>Undo is Cmd+Z", bool(undo) and undo["cmd"].upper() == "Z" and undo["mods"] == "0",
            str(undo))
    c.check("Edit>Redo is Shift+Cmd+Z", bool(redo) and redo["cmd"].upper() == "Z" and redo["mods"] == "1",
            str(redo))
    for item in ("File>New Project", "File>Load Project...", "File>Load Recent", "File>Save Project",
                 "File>Save All", "File>Export"):
        c.check(f"{item} exists", item in items)


@scenario("window-content-in-ax-tree")
def window_content_in_ax_tree(c: Ctx):
    g = c.launch(args=["--select", "ComponentWithState"])

    # The window nests its content more than 16 levels deep, so a shallower walk stops at empty groups.
    def walk():
        return drv("tree", g.pid, 40, check=False).splitlines()

    # Both queries are kept in the artifacts so a failure shows whether the content never appears,
    # or appears only after the window changes (a stale cache in Avalonia's bridge).
    first = walk()
    (ARTIFACTS / "ax-tree-first.txt").write_text("\n".join(first))
    g.activate()
    w = g.main_window()
    g.set_frame(w["x"], w["y"], w["w"] + 40, w["h"])
    after_resize = walk()
    (ARTIFACTS / "ax-tree-after-resize.txt").write_text("\n".join(after_resize))

    def exposes(lines):
        text = "\n".join(lines)
        return all(f'title="{name}"' in text for name in ("Project", "Variables", "Editor", "Preview in runtime"))

    # A tree row is static text whose value is its name. A row whose parent chain does not reach the
    # window reports the zero frame "0 982 0 0" (#5969).
    def rows_with_frames(lines):
        rows = [line for line in lines if "AXStaticText" in line
                and any(f'value="{name}"' in line for name in ("Screens", "Components", "ComponentWithState"))]
        return len(rows) >= 3 and all("[0 982 0 0]" not in row for row in rows)

    c.check("tabs and the Preview button are in the tree on the first query", exposes(first),
            f"{len(first)} nodes")
    c.check("they are there after a resize too", exposes(after_resize), f"{len(after_resize)} nodes")
    c.check("the project tree's rows are named and have a frame", rows_with_frames(first))
    c.check("they still do after a resize", rows_with_frames(after_resize))


@scenario("hotkeys-on-disk")
def hotkeys_on_disk(c: Ctx):
    g = c.launch(args=["--select", "ComponentWithState"])
    g.focus_tree()  # --select leaves keyboard focus off the tree, so hotkeys would do nothing
    # Cmd+D duplicates the selected component; the copy is written next to it.
    g.key("d", "cmd")
    c.check("Cmd+D wrote ComponentWithState1.gucx", wait_until(lambda: g.gucx("ComponentWithState1").exists(), 8))
    # Cmd+C then Cmd+V pastes a further copy of the (now selected) duplicate.
    g.key("c", "cmd")
    g.key("v", "cmd")
    c.check("Cmd+C, Cmd+V wrote ComponentWithState2.gucx", wait_until(lambda: g.gucx("ComponentWithState2").exists(), 8))
    # F2 renames the selected element; the file is renamed after the confirmation prompt.
    g.key("f2")
    time.sleep(0.8)
    g.type("MacRenamed")
    g.key("return")
    time.sleep(0.8)
    g.key("return")  # "Rename Object and File?" prompt, default answer
    c.check("F2 rename moved the file to MacRenamed.gucx", wait_until(lambda: g.gucx("MacRenamed").exists(), 8))
    c.check("F2 rename left no file under the old name", not g.gucx("ComponentWithState2").exists())
    # Delete asks what to do with the file; Y answers yes.
    g.key("delete")
    time.sleep(0.8)
    g.key("y")
    c.check("Delete then Y removed MacRenamed.gucx", wait_until(lambda: not g.gucx("MacRenamed").exists(), 8))


@scenario("new-project-cancel")
def new_project_cancel(c: Ctx):
    g = c.launch()
    g.menu("File", "New Project")
    c.check("New Project dialog opened", wait_until(lambda: any("New Project" in w["title"] for w in g.windows()), 8))
    g.key("escape")
    time.sleep(1.0)
    c.check("Cancelling New Project keeps the open project", "GumProject" in g.title(), f"title='{g.title()}'")


@scenario("open-project-native-panel")
def open_project_native_panel(c: Ctx):
    g = c.launch()
    second = c.work / "second"
    shutil.copytree(SAMPLE, second)
    (second / "GumProject.gumx").rename(second / "SecondProject.gumx")
    g.menu("File", "Load Project...")
    g.wait_for_sheet()
    g.key("g", "cmd,shift")  # Go to the folder
    time.sleep(0.8)
    g.type(str(second / "SecondProject.gumx"))
    g.key("return")
    time.sleep(1.0)
    g.key("return")  # Open
    c.check("Open panel loaded SecondProject", wait_until(lambda: "SecondProject" in g.title(), 15), f"title='{g.title()}'")


@scenario("export-image-on-disk")
def export_image_on_disk(c: Ctx):
    g = c.launch(args=["--select", "ComponentWithState"])
    out = c.work / "exported.png"
    g.menu("File", "Export", "Export as Image")
    g.wait_for_sheet()
    g.type(str(out))
    # A typed path first sets the panel's folder and name (one Return), then Save (another). Under load the
    # path sheet can swallow a Return, so press again until the file exists instead of counting on two.
    for _ in range(4):
        g.key("return")
        if wait_until(out.exists, 2.5):
            break
    c.check("Export as Image wrote a PNG", wait_until(lambda: out.exists() and out.stat().st_size > 100, 15))
    if out.exists():
        c.check("PNG has the PNG signature", out.read_bytes()[:8] == b"\x89PNG\r\n\x1a\n")


@scenario("about-copies-text")
def about_copies_text(c: Ctx):
    g = c.launch()
    subprocess.run(["pbcopy"], input=b"sentinel")
    g.menu("Gum", "About Gum")
    time.sleep(1.5)
    g.key("c", "cmd")
    copied = subprocess.run(["pbpaste"], capture_output=True, text=True).stdout
    c.check("Cmd+C in About copies its text", copied != "sentinel" and "Gum" in copied, f"clipboard='{copied[:60]}'")
    g.key("escape")


@scenario("quit-is-clean")
def quit_is_clean(c: Ctx):
    g = c.launch()
    before = set(Path.home().joinpath("Library/Logs/DiagnosticReports").glob("Gum*"))
    g.key("q", "cmd")
    c.check("Cmd+Q exits within 10 s", wait_until(lambda: not g.alive(), 10))
    c.check("Cmd+Q exits with code 0", g.proc.poll() == 0, f"exit={g.proc.poll()}")
    after = set(Path.home().joinpath("Library/Logs/DiagnosticReports").glob("Gum*"))
    c.check("Quit left no crash report", after == before, str(after - before))


@scenario("drag-standard-chip")
def drag_standard_chip(c: Ctx):
    g = c.launch(args=["--select", "ComponentWithState"])
    element = g.gucx("ComponentWithState")
    before = g.instance_count(element)
    g.drag(*CHIP_CIRCLE, *CANVAS_DROP)
    c.check("Dragging the Circle chip onto the canvas added an instance",
            wait_until(lambda: g.instance_count(element) == before + 1, 8),
            f"instances {before} -> {g.instance_count(element)}; modifiers held: '{held_modifiers() or 'none'}'")


@scenario("codegen-auto-setup-and-generate")
def codegen_auto_setup_and_generate(c: Ctx):
    # The sample ships a ProjectCodeSettings.codsj, which hides Auto. Start from a project with no setup,
    # a csproj above the .gumx folder (what Auto walks up to find) and ComponentWithState set to GenerateManually.
    proj = c.work / "proj"
    shutil.copytree(SAMPLE, proj)
    (proj / "ProjectCodeSettings.codsj").unlink()
    (proj / "Components/ComponentWithState.codsj").write_text('{"GenerationBehavior":1}')
    (c.work / "MonoGameGumFromFile.csproj").write_text('<Project Sdk="Microsoft.NET.Sdk" />')
    g = c.launch(args=["--select", "ComponentWithState"])
    g.click_title("AXRadioButton", "Code")
    c.check("the Code tab offers Auto and Manual",
            wait_until(lambda: g.has("AXButton", "Auto") and g.has("AXButton", "Manual"), 10))
    c.check("Generate is hidden until setup", not g.has("AXButton", "Generate"))
    g.click_title("AXButton", "Auto")
    settings = proj / "ProjectCodeSettings.codsj"
    c.check("Auto wrote ProjectCodeSettings.codsj", wait_until(settings.exists, 10))
    text = settings.read_text() if settings.exists() else ""
    squashed = text.replace(" ", "").replace("\\/", "/")
    c.check("Auto found the csproj above the project", '"CodeProjectRoot":"../"' in squashed, text[:300])
    c.check("Auto took the namespace from the csproj name", "MonoGameGumFromFile" in text, text[:300])
    c.check("Generate appears once set up", wait_until(lambda: g.has("AXButton", "Generate"), 10))
    g.click_title("AXButton", "Generate")
    generated = c.work / "Components/ComponentWithState.Generated.cs"
    custom = c.work / "Components/ComponentWithState.cs"
    c.check("Generate wrote ComponentWithState.Generated.cs", wait_until(generated.exists, 15))
    c.check("Generate wrote the ComponentWithState.cs stub", custom.exists())
    if generated.exists():
        code = generated.read_text()
        c.check("generated code is in the csproj's namespace", "namespace MonoGameGumFromFile.Components" in code)
        c.check("generated code is a partial ComponentWithState", "partial class ComponentWithState" in code)
    if custom.exists():
        c.check("the stub has the CustomInitialize hook", "partial void CustomInitialize()" in custom.read_text())
    c.check("the project folder got no generated files", not list((proj / "Components").glob("*.cs")))
    g.key("return")  # the "Generated code to ..." message


# -- canvas and Variables tab depth. The target is StartScreen#ColoredRectangleInstance: pixel X/Y/Width/Height
# (25, 39, 254, 106) with no parent, so every diff is an exact integer. Autosave writes the element on release.

RECT = "StartScreen#ColoredRectangleInstance"
RECT_RGB = (0, 20, 139)  # its Red, Green, Blue in StartScreen.gusx


def rect_vars(g, names=("X", "Y", "Width", "Height")):
    """The ColoredRectangleInstance variables in the saved StartScreen, as {name: float}."""
    root = ET.parse(g.dir / "Screens/StartScreen.gusx").getroot()
    out = {}
    for v in root.iter("Variable"):
        n, val = v.find("Name"), v.find("Value")
        if n is not None and val is not None and n.text.startswith("ColoredRectangleInstance."):
            out[n.text.split(".", 1)[1]] = float(val.text) if val.text.replace(".", "", 1).lstrip("-").isdigit() else val.text
    return out if names is None else {k: out.get(k) for k in names}


def frame_element(g):
    """Centres the selection (View > Focus on Selection works from any focus) and returns (centre x, centre y, zoom)."""
    g.menu("View", "Focus on Selection")
    time.sleep(0.8)
    cx, cy, w, h = g.canvas()
    return cx + w // 2, cy + h // 2, g.zoom()


@scenario("canvas-drag-move")
def canvas_drag_move(c: Ctx):
    g = c.launch(args=["--select", RECT])
    x, y, zoom = frame_element(g)
    s = g.scale()
    before = rect_vars(g)
    drv("drag", x, y, x + 40, y + 20)
    expected = int(40 * s * 100 / zoom), int(20 * s * 100 / zoom)
    c.check("the drag wrote X and Y (autosave on release)",
            wait_until(lambda: rect_vars(g)["X"] != before["X"], 8), str(rect_vars(g)))
    after = rect_vars(g)
    c.check("X moved right by the drag distance in world units", abs(after["X"] - before["X"] - expected[0]) <= 2,
            f"{before['X']} -> {after['X']}, expected +{expected[0]} (scale {s}, zoom {zoom}%)")
    c.check("Y moved down by the drag distance in world units", abs(after["Y"] - before["Y"] - expected[1]) <= 2,
            f"{before['Y']} -> {after['Y']}, expected +{expected[1]}")
    c.check("Width and Height are unchanged by a move",
            (after["Width"], after["Height"]) == (before["Width"], before["Height"]), str(after))


@scenario("canvas-resize-handle")
def canvas_resize_handle(c: Ctx):
    g = c.launch(args=["--select", RECT])
    frame_element(g)
    s, zoom = g.scale(), g.zoom()
    before = rect_vars(g)
    box = g.find_color(RECT_RGB)
    if not c.check("the selected rectangle is visible on the canvas", box is not None):
        return
    # Focus on Selection fills the view, so the corner handles sit on the view's edge. The bottom-middle
    # handle is centred on the rectangle's bottom edge and clear of every edge.
    hx, hy = int((box[0] + box[2]) / 2), int(box[3]) + 6  # the handle sits just outside the edge
    drv("drag", hx, hy, hx, hy + 20)
    expected = int(20 * s * 100 / zoom)
    c.check("the drag wrote Height (autosave on release)",
            wait_until(lambda: rect_vars(g)["Height"] != before["Height"], 8), str(rect_vars(g)))
    after = rect_vars(g)
    c.check("Height grew by the drag distance", abs(after["Height"] - before["Height"] - expected) <= 2,
            f"{before['Height']} -> {after['Height']}, expected +{expected} (scale {s}, zoom {zoom}%)")
    c.check("X, Y and Width are unchanged by a bottom-middle resize",
            (after["X"], after["Y"], after["Width"]) == (before["X"], before["Y"], before["Width"]), str(after))


@scenario("canvas-arrow-nudge")
def canvas_arrow_nudge(c: Ctx):
    g = c.launch(args=["--select", RECT])
    x, y, _ = frame_element(g)
    drv("click", x, y)  # a click on the selected rectangle focuses the canvas and keeps the selection
    time.sleep(0.6)
    before = rect_vars(g)
    g.key("right")
    g.key("down")
    g.key("right", "shift")
    g.key("up")
    time.sleep(1.0)
    after = rect_vars(g)
    c.check("Right, Down, Shift+Right, Up nudged X by 1+5", after["X"] == before["X"] + 6, f"{before['X']} -> {after['X']}")
    c.check("and Y by 1-1", after["Y"] == before["Y"], f"{before['Y']} -> {after['Y']}")
    file = g.dir / "Screens/StartScreen.gusx"
    bytes_before = file.read_bytes()
    g.key("right", "cmd")  # Cmd+arrow pans the camera, it must not move the element
    time.sleep(0.8)
    c.check("Cmd+Right panned the camera and left the element file alone", file.read_bytes() == bytes_before)


@scenario("canvas-zoom-shortcuts")
def canvas_zoom_shortcuts(c: Ctx):
    g = c.launch(args=["--select", RECT])
    file = g.dir / "Screens/StartScreen.gusx"
    bytes_before = file.read_bytes()
    # With focus in the tree the same keys change the app font size, not the camera.
    g.focus_tree()
    start = g.zoom()
    g.key("=", "cmd")
    c.check("Cmd+= with tree focus leaves the camera zoom alone", g.zoom() == start, f"{start}% -> {g.zoom()}%")
    g.key("-", "cmd")
    x, y, _ = frame_element(g)  # centres the rectangle so a click lands on it and focuses the canvas
    drv("click", x, y)
    time.sleep(0.6)
    focused = g.zoom()
    g.key("=", "cmd")
    zoomed_in = g.zoom()
    g.key("-", "cmd")
    zoomed_back = g.zoom()
    c.check("Cmd+= with canvas focus zooms in", zoomed_in > focused, f"{focused}% -> {zoomed_in}%")
    c.check("Cmd+- zooms back out", zoomed_back == focused, f"{zoomed_in}% -> {zoomed_back}%")
    c.check("zooming wrote nothing to the element file", file.read_bytes() == bytes_before)


@scenario("focus-on-selection-hotkey")
def focus_on_selection_hotkey(c: Ctx):
    file_bytes = None
    g = c.launch(args=["--select", RECT])
    file = g.dir / "Screens/StartScreen.gusx"
    file_bytes = file.read_bytes()
    items = g.menu_items()
    c.check("View > Focus on Selection exists", "View>Focus on Selection" in items, str([p for p in items if p.startswith("View")]))
    c.check("it shows a bare F", items.get("View>Focus on Selection", {}).get("cmd", "").upper() == "F", str(items.get("View>Focus on Selection")))
    start = g.zoom()
    # F from the tree (the focus the tool starts with after a selection)
    g.focus_tree()
    g.key("f")
    c.check("F with tree focus zooms to the selection", g.zoom() > start, f"{start}% -> {g.zoom()}%")
    g = c.launch(args=["--select", RECT])
    start = g.zoom()
    g.menu("View", "Focus on Selection")
    c.check("the View menu item zooms to the selection", g.zoom() > start, f"{start}% -> {g.zoom()}%")
    c.check("focusing wrote nothing to the element file", file.read_bytes() == file_bytes)


def alpha_slider_in_view(g):
    """Scrolls the Variables tab until the Alpha row's slider is inside the window; returns its centre and width."""
    win = g.main_window()
    for _ in range(20):
        lines = g.tree()
        for i, line in enumerate(lines):
            if 'AXStaticText value="Alpha"' in line:
                for nxt in lines[i + 1:i + 4]:
                    if nxt.strip().startswith("AXSlider"):
                        x, y, w, h = (int(v) for v in nxt[nxt.index("[") + 1:-1].split())
                        if win["y"] + 150 < y + h // 2 < win["y"] + win["h"] - 20:
                            return x + w // 2, y + h // 2, w
        drv("scroll", win["x"] + 400, win["y"] + 500, -5)
        time.sleep(0.4)
    raise Failure("the Alpha slider never scrolled into view")


@scenario("alpha-slider-right-click-then-click")
def alpha_slider_right_click_then_click(c: Ctx):
    g = c.launch(args=["--select", RECT])
    file = g.dir / "Screens/StartScreen.gusx"
    x, y, w = alpha_slider_in_view(g)
    bytes_before = file.read_bytes()
    drv("click", x, y, "right")
    time.sleep(0.8)
    g.key("escape")
    time.sleep(0.8)
    c.check("a right-click on the Alpha slider commits nothing", file.read_bytes() == bytes_before
            and "ColoredRectangleInstance.Alpha" not in file.read_text())
    x, y, w = alpha_slider_in_view(g)
    drv("click", x, y)  # the middle of the track, away from the 255 thumb
    c.check("a left click on the track commits Alpha to disk",
            wait_until(lambda: "Alpha" in rect_vars(g, None), 8), str(rect_vars(g, None)))
    alpha = rect_vars(g, None).get("Alpha")
    c.check("the middle of the track is about half", isinstance(alpha, float) and 100 <= alpha <= 155, str(alpha))
    x, y, w = alpha_slider_in_view(g)
    drv("click", x + w // 2 - 4, y)  # the far right end of the track
    c.check("a click at the right end commits a high Alpha",
            wait_until(lambda: isinstance(rect_vars(g, None).get("Alpha"), float) and rect_vars(g, None)["Alpha"] >= 240, 8),
            str(rect_vars(g, None).get("Alpha")))


def preview_pids():
    out = subprocess.run(["pgrep", "-x", "GumPreview"], capture_output=True, text=True).stdout
    return {int(v) for v in out.split()}


@scenario("preview-launches-and-exits")
def preview_launches_and_exits(c: Ctx):
    before = preview_pids()
    started = set()
    try:
        g = c.launch(args=["--select", "ComponentWithState"])
        g.click_title("AXButton", "Preview in runtime")
        # Native AOT host, plus a first-time JSON copy of the project: allow a slow start.
        c.check("Preview started a GumPreview process", wait_until(lambda: preview_pids() - before, 45))
        started = preview_pids() - before
        if not started:
            return
        pid = next(iter(started))
        args = subprocess.run(["ps", "-o", "args=", "-p", str(pid)], capture_output=True, text=True).stdout
        c.check("it was given the selected element", "--element ComponentWithState" in args, args[:200])
        c.check("Gum is still running", g.alive())
        c.check("the preview window is titled for the element",
                wait_until(lambda: "Gum Preview - ComponentWithState" in drv("title", pid, check=False), 20),
                drv("title", pid, check=False))
        # A second press reuses the running preview instead of starting another.
        g.activate()
        g.click_title("AXButton", "Preview in runtime")
        time.sleep(3)
        c.check("pressing Preview again starts no second process", preview_pids() - before == started,
                str(preview_pids() - before))
        reports = Path.home() / "Library/Logs/DiagnosticReports"
        crashes_before = set(reports.glob("GumPreview*"))
        drv("activate", pid)
        time.sleep(0.8)
        g.key("q", "cmd")
        c.check("Cmd+Q closes the preview", wait_until(lambda: pid not in preview_pids(), 10))
        c.check("Gum survives the preview closing", g.alive())
        c.check("the preview left no crash report", set(reports.glob("GumPreview*")) == crashes_before)
        # After the preview exits, the next press launches a fresh one.
        g.activate()
        time.sleep(1.0)  # the toolbar notices the exit on a 500 ms timer
        g.click_title("AXButton", "Preview in runtime")
        c.check("Preview launches again after the first one closed",
                wait_until(lambda: preview_pids() - before - {pid}, 45))
    finally:
        for pid in preview_pids() - before:
            subprocess.run(["kill", str(pid)])


@scenario("driver-releases-modifiers")
def driver_releases_modifiers(c: Ctx):
    """Pins the driver: a key press with modifiers must leave none held, or later drags become Command drags."""
    g = c.launch()
    for mods in ("cmd", "shift", "alt", "ctrl", "cmd,shift", "cmd,shift,alt,ctrl"):
        g.key("f12", mods)  # F12 is go to definition in Gum, harmless with nothing selected
        c.check(f"key f12 with {mods} left no modifier held", not held_modifiers(), held_modifiers())


@scenario("last-project-reopens")
def last_project_reopens(c: Ctx):
    g = c.launch()
    g.quit()
    g = c.launch(project=None, copy_project=False)  # no project argument: the saved last project
    c.check("Relaunch reopened the last project", "GumProject" in g.title(), f"title='{g.title()}'")
    items = g.menu_items()
    c.check("Load Recent lists the project", any(p.startswith("File>Load Recent>GumProject") for p in items),
            str([p for p in items if p.startswith("File>Load Recent")]))


@scenario("window-frame-persists")
def window_frame_persists(c: Ctx):
    g = c.launch()
    g.set_frame(200, 120, 1100, 700)
    wanted = (200, 120, 1100, 700)
    got = g.main_window()
    c.check("window accepted the new frame", (got["x"], got["y"], got["w"], got["h"]) == wanted, str(got))
    g.quit()
    g = c.launch(project=None, copy_project=False)
    got = g.main_window()
    c.check("relaunch restored the window frame", abs(got["w"] - 1100) <= 2 and abs(got["h"] - 700) <= 2
            and abs(got["x"] - 200) <= 2 and abs(got["y"] - 120) <= 2, str(got))


@scenario("view-menu-app-zoom")
def view_menu_app_zoom(c: Ctx):
    g = c.launch()
    items = g.menu_items()
    names = ("Zoom In", "Zoom Out", "Actual Size")
    c.check("the macOS View menu offers Zoom In, Zoom Out and Actual Size",
            all(f"View>{n}" in items for n in names), str([p for p in items if p.startswith("View")]))

    def text_center():
        # The tree's text follows the app's font size (the rows keep their height), so the centre of a
        # left-aligned label moves right as the text widens.
        return g.find("AXStaticText", "Components")[0]

    start = text_center()
    for _ in range(4):
        g.menu("View", "Zoom In")
    zoomed_in = text_center()
    c.check("Zoom In grows the whole app (tree text gets wider)", zoomed_in > start, f"{start} -> {zoomed_in}")
    for _ in range(8):
        g.menu("View", "Zoom Out")
    zoomed_out = text_center()
    c.check("Zoom Out shrinks it below where it started", zoomed_out < start, f"{start} -> {zoomed_out}")
    g.menu("View", "Actual Size")
    c.check("Actual Size returns to the original size", text_center() == start, f"{start} -> {text_center()}")


@scenario("full-screen-toggle")
def full_screen_toggle(c: Ctx):
    g = c.launch()
    before = g.main_window()
    g.menu("View", "Enter Full Screen")
    time.sleep(2.5)  # the full screen animation
    full = g.main_window()
    c.check("Enter Full Screen grew the window", full["w"] > before["w"] and full["h"] > before["h"], f"{before} -> {full}")
    c.check("the menu now offers Exit Full Screen", "View>Exit Full Screen" in g.menu_items())
    g.menu("View", "Exit Full Screen")
    time.sleep(2.5)
    back = g.main_window()
    c.check("Exit Full Screen restored the size", (back["w"], back["h"]) == (before["w"], before["h"]), f"{before} -> {back}")


# --------------------------------------------------------------------------------------------

def main():
    global DRV
    ap = argparse.ArgumentParser()
    ap.add_argument("names", nargs="*", help="run scenarios whose name contains any of these")
    ap.add_argument("--list", action="store_true")
    args = ap.parse_args()
    if args.list:
        for name, _, bug in SCENARIOS:
            print(name + (f"  (known bug #{bug})" if bug else ""))
        return 0
    if not (APP / "Contents/MacOS/Gum").exists():
        print(f"No packaged app at {APP}. Build it like the release workflow, or set GUM_APP.")
        return 2
    DRV = build_driver()
    ARTIFACTS.mkdir(parents=True, exist_ok=True)
    selected = [s for s in SCENARIOS if not args.names or any(n in s[0] for n in args.names)]
    failed, lines = 0, []
    for name, fn, bug in selected:
        ctx = Ctx(name)
        started = time.time()
        drv("clearflags")  # start from no modifier held, whatever an earlier run left
        try:
            held = held_modifiers()
            if held:
                raise Failure(f"{held} still held after clearing; is a key physically down?")
            fn(ctx)
        except Exception as e:  # a launch timeout, a missing menu item, a crash mid-scenario
            ctx.failures.append(f"{type(e).__name__}: {e}")
            if not isinstance(e, Failure):
                traceback.print_exc()
        held = held_modifiers()
        if held:
            ctx.failures.append(f"the scenario left {held} held down (a held Command turns drags into moves)")
        first_line = len(lines)
        evidence = ctx.evidence() if ctx.failures else ""
        if ctx.gum and not ctx.gum.alive() and ctx.failures:
            ctx.failures.append("app was no longer running")
        if ctx.gum:
            ctx.gum.stop()
        took = f"{time.time() - started:.0f}s"
        if not ctx.failures:
            status = "FIXED?" if bug else "PASS"
            lines.append(f"{status}  {name} ({took})" + (f"  known bug #{bug} no longer reproduces" if bug else ""))
        elif bug:
            lines.append(f"KNOWN  {name} ({took})  #{bug}: " + "; ".join(ctx.failures))
        else:
            failed += 1
            lines.append(f"FAIL  {name} ({took})")
            lines += [f"      - {f}" for f in ctx.failures]
            if evidence:
                lines.append(f"      screenshot: {evidence}")
        print("\n".join(lines[first_line:]))
        sys.stdout.flush()
        shutil.rmtree(ctx.work, ignore_errors=True)
    (ARTIFACTS / "report.txt").write_text("\n".join(lines) + "\n")
    print(f"\n{len(selected) - failed}/{len(selected)} ok; report: {ARTIFACTS / 'report.txt'}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
