# tools/gen_stubs.py
import sys, pythonnet, clr
from pathlib import Path
from System.Reflection import ReflectionTypeLoadException

pythonnet.load("coreclr")

DLL_DIR = Path(r"C:\git\Gum\python\PythonGum\src\gum_runtime\_clr\net6.0")
sys.path.insert(0, str(DLL_DIR))
for p in DLL_DIR.glob("*.dll"):
    try: clr.AddReference(str(p))
    except Exception: pass

import System

SKIP = {"System", "Microsoft", "MS", "Internal", "FxResources", "Python", "mscorlib", "Windows"}
OUT  = Path("typings")
OUT.mkdir(parents=True, exist_ok=True)

def safe_types(asm):
    try: return asm.GetTypes()
    except ReflectionTypeLoadException as ex: return [t for t in ex.Types if t]

# discover roots automatically
all_types = [t for asm in System.AppDomain.CurrentDomain.GetAssemblies()
             for t in safe_types(asm) if t and t.Namespace]

roots = {t.Namespace.split(".")[0] for t in all_types} - SKIP
print("Generating stubs for roots:", sorted(roots))

# group by namespace
by_ns = {}
for t in all_types:
    root = t.Namespace.split(".")[0]
    if root in roots:
        by_ns.setdefault(t.Namespace, []).append(t)

HEADER = ("from __future__ import annotations\n"
          "import typing\n"
          "from typing import Any\n\n")
ANY = "typing.Any"

for ns, types in by_ns.items():
    parts = ns.split(".")
    folder = OUT.joinpath(*parts)
    folder.mkdir(parents=True, exist_ok=True)
    stub_file = folder / "__init__.pyi"

    lines = [HEADER]
    for t in types:
        name = t.Name
        if t.IsEnum:
            lines.append(f"class {name}(int): ...\n")
            continue
        lines.append(f"class {name}(typing.Any):")
        # properties
        for prop in t.GetProperties():
            lines.append(f"    {prop.Name}: {ANY}")
        # methods
        for m in t.GetMethods():
            if m.IsSpecialName:  # skip get_/set_ etc
                continue
            lines.append(f"    def {m.Name}(self, *args: {ANY}, **kwargs: {ANY}) -> {ANY}: ...")
        lines.append("")
    stub_file.write_text("\n".join(lines), encoding="utf-8")

print("Stub generation done ->", OUT)
