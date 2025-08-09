# tools/build_gumui.py

import os
import sys
import shutil
import subprocess
from pathlib import Path

HERE = Path(__file__).parent.resolve()
ROOT = HERE.parent
SRC = ROOT / "src" / "gumui"
TYPINGS = ROOT / "typings"
STAGE = ROOT / "stage"
DIST = ROOT / "dist"
STAGE_SRC = STAGE / "src" / "gumui"
PYPROJECT = ROOT / "pyproject.toml"
EXAMPLES = ROOT / "examples"
EXAMPLES_SRC = STAGE / "examples"

def run_stub_generator():
    print("Running stub generator...")
    subprocess.run(["python", str(HERE / "pylance_stubs.py")], check=True, cwd=HERE)

def prepare_stage():
    print("Preparing stage directory...")
    if STAGE.exists():
        shutil.rmtree(STAGE)
    STAGE_SRC.mkdir(parents=True, exist_ok=True)

    # Copy main source
    shutil.copytree(SRC, STAGE_SRC, dirs_exist_ok=True)

    # Copy stubs
    print("Copying stubs into stage...")
    for item in TYPINGS.iterdir():
        dest = STAGE_SRC / item.name
        if item.is_dir():
            shutil.copytree(item, dest, dirs_exist_ok=True)
        else:
            shutil.copy2(item, dest)

    # Copy the examples
    shutil.copytree(EXAMPLES, EXAMPLES_SRC, dirs_exist_ok=True)

    shutil.copy2(ROOT / "README_PACKAGED.md", STAGE / "README.md")
    shutil.copy2(ROOT / "LICENSE", STAGE / "LICENSE")

    # Add py.typed
    py_typed = STAGE_SRC / "py.typed"
    if not py_typed.exists():
        py_typed.write_text("")

    # Copy pyproject.toml
    if PYPROJECT.exists():
        shutil.copy2(PYPROJECT, STAGE / "pyproject.toml")
    else:
        print("ERROR: Missing pyproject.toml in root.")
        sys.exit(1)

def build_package():
    print("Building wheel...")
    subprocess.run(
        [sys.executable, "-m", "build", "--wheel", "--sdist", "--outdir", str(DIST)],
        check=True,
        cwd=STAGE
    )

def main():
    run_stub_generator()
    prepare_stage()
    build_package()
    print("Build complete.")

if __name__ == "__main__":
    main()
