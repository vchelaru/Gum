import sys, os
from pathlib import Path

import pythonnet
pythonnet.load("coreclr")  # or "mono" if you target Mono

import clr
from System import AppDomain, Enum
from System.Reflection import ReflectionTypeLoadException, BindingFlags
from System import FlagsAttribute

# -------- CONFIGURATION --------
here = os.path.dirname(os.path.abspath(__file__))

# Path to the DLLs we want to generate stubs for
clr_path = os.path.join(here, "..", "src", "gumui", "_clr", "net6.0")
DLL_DIR = Path(os.path.abspath(clr_path))

# *** CHANGED: write stubs inside the package under gumui/stubs ***
OUT_DIR = Path(os.path.join(here, "..", "typings"))

# Namespaces we want to skip
SKIP_ROOTS = {
    "System", "Microsoft", "MS", "Internal",
    "FxResources", "Python", "mscorlib", "Windows"
}
# --------------------------------

# make sure Python can load the DLLs
sys.path.insert(0, str(DLL_DIR))
for dll in DLL_DIR.glob("*.dll"):
    try:
        clr.AddReference(str(dll))
    except Exception:
        pass

# Create the output folder (gumui/stubs)
OUT_DIR.mkdir(parents=True, exist_ok=True)

HEADER = (
    "from __future__ import annotations\n"
    "import typing\n"
    "from typing import Any\n"
    "from enum import IntEnum, IntFlag\n\n"
)
ANY = "Any"

def safe_get_types(assembly):
    try:
        return assembly.GetTypes()
    except ReflectionTypeLoadException as ex:
        return [t for t in ex.Types if t is not None]

def collect_all_types():
    all_types = []
    for asm in AppDomain.CurrentDomain.GetAssemblies():
        for t in safe_get_types(asm):
            if t is not None and t.Namespace:
                all_types.append(t)
    return all_types

def is_flag_enum(t):
    return t.IsDefined(FlagsAttribute, False)

def group_by_namespace(types, roots):
    by_ns = {}
    for t in types:
        root = t.Namespace.split(".")[0]
        if root in roots:
            by_ns.setdefault(t.Namespace, []).append(t)
    return by_ns

def write_stub_for_namespace(ns: str, types):
    parts = ns.split(".")
    folder = OUT_DIR.joinpath(*parts)
    folder.mkdir(parents=True, exist_ok=True)
    stub_file = folder / "__init__.pyi"

    lines = [HEADER]

    for t in types:
        cls_name = t.Name

        # ---- ENUMS ----
        if t.IsEnum:
            base = "IntFlag" if is_flag_enum(t) else "IntEnum"
            lines.append(f"class {cls_name}({base}):")
            names = Enum.GetNames(t)
            values = Enum.GetValues(t)
            for n, v in zip(names, values):
                lines.append(f"    {n} = {int(v)}")
            lines.append("")  # blank line after each enum
            continue

        # ---- CLASSES / STRUCTS / INTERFACES ----
        lines.append(f"class {cls_name}(typing.Any):")

        # Properties
        for prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static):
            if prop.GetIndexParameters().Length != 0:
                continue
            lines.append("    @property")
            lines.append(f"    def {prop.Name}(self) -> {ANY}: ...")
            if prop.CanWrite:
                lines.append(f"    @{prop.Name}.setter")
                lines.append(f"    def {prop.Name}(self, value: {ANY}) -> None: ...")

        # Methods
        for m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static):
            if m.IsSpecialName:
                continue
            if m.IsStatic:
                lines.append("    @staticmethod")
                sig = f"    def {m.Name}(*args: Any, **kwargs: Any) -> Any: ..."
            else:
                sig = f"    def {m.Name}(self, *args: Any, **kwargs: Any) -> Any: ..."
            lines.append(sig)

        lines.append("")  # blank line after each class

    stub_file.write_text("\n".join(lines), encoding="utf-8")

def main():
    all_types = collect_all_types()
    roots = {t.Namespace.split(".")[0] for t in all_types} - SKIP_ROOTS
    print("Generating stubs for roots:", sorted(roots))
    by_ns = group_by_namespace(all_types, roots)
    for ns, types in by_ns.items():
        write_stub_for_namespace(ns, types)
    print("Stub generation complete. Stubs are in:", OUT_DIR)

if __name__ == "__main__":
    main()
