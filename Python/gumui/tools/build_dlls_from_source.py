import subprocess
from pathlib import Path

# Define paths
here = Path(__file__).resolve().parent
out = here.parent / "src" / "gumui" / "_clr"
cfg = "Debug"
tfms = ["net6.0"]

# Paths to projects
gum_common_proj = here.parent.parent.parent / "GumCommon" / "GumCommon.csproj"
helper_proj = here.parent.parent / "GumToPythonHelpers" / "GumToPythonHelpers.csproj"

def build_and_copy(proj_path: Path):
    for tfm in tfms:
        target_dir = out / tfm
        target_dir.mkdir(parents=True, exist_ok=True)

        subprocess.run([
            "dotnet", "build", str(proj_path),
            "-c", cfg,
            "-f", tfm,
            f"/p:CopyLocalLockFileAssemblies=true",
            f"/p:OutDir={target_dir}{'/' if not str(target_dir).endswith('/') else ''}"
        ], check=True)

# Build both projects
build_and_copy(gum_common_proj)
build_and_copy(helper_proj)

print("Build completed successfully.")
