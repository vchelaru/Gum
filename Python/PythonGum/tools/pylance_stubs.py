# tools/gen_stubs.py
import sys, pythonnet, clr, textwrap
from pathlib import Path
pythonnet.load("coreclr")

DLL_DIR = Path(r"C:\git\Gum\python\PythonGum\src\gum_runtime\_clr\net6.0")
sys.path.insert(0, str(DLL_DIR))
for p in DLL_DIR.glob("*.dll"):
    try: clr.AddReference(str(p))
    except Exception: pass

import System
from System.Reflection import ReflectionTypeLoadException
out_root = Path("typings") / "Gum"  # where stubs go
out_root.mkdir(parents=True, exist_ok=True)

def safe_types(a):
    try: return a.GetTypes()
    except ReflectionTypeLoadException as ex: return [t for t in ex.Types if t]

type_map = [t for asm in System.AppDomain.CurrentDomain.GetAssemblies()
            for t in safe_types(asm) if t and t.Namespace and t.Namespace.startswith("Gum")]

by_ns = {}
for t in type_map:
    by_ns.setdefault(t.Namespace, []).append(t)

ANY = "typing.Any"
header = "from __future__ import annotations\nimport typing\n" \
         "from typing import Any\n\n"

for ns, types in by_ns.items():
    rel = ns.split(".")[1:]  # drop root "Gum"
    folder = out_root.joinpath(*rel)
    folder.mkdir(parents=True, exist_ok=True)
    stub = folder / "__init__.pyi"
    lines = [header]
    for t in types:
        name = t.Name
        if t.IsEnum:
            lines.append(f"class {name}(int): ...\n")
            continue
        bases = "typing.Any"
        lines.append(f"class {name}({bases}):")
        # props
        for p in t.GetProperties():
            lines.append(f"    {p.Name}: {ANY}  # property")
        # methods (skip special names)
        for m in t.GetMethods():
            if m.Name.startswith("_") or m.IsSpecialName: continue
            lines.append(f"    def {m.Name}(self, *args: {ANY}, **kw: {ANY}) -> {ANY}: ...")
        lines.append("")
    stub.write_text("\n".join(lines), encoding="utf-8")
print("Stub generation done ->", out_root)
