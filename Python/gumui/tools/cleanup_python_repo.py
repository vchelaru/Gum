import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
paths_to_clean = [
    ROOT / "stage",
    ROOT / "src" / "gumui" / "_clr",
    ROOT / "dist"
]

def remove_all_in_directory(path: Path):
    if path.exists() and path.is_dir():
        for item in path.iterdir():
            if item.is_file() or item.is_symlink():
                item.unlink()
            elif item.is_dir():
                shutil.rmtree(item)
        print(f"Cleaned: {path}")
    else:
        print(f"Skipped (not found): {path}")

def main():
    # Accept `--yes` to skip confirmation
    auto_confirm = "--yes" in sys.argv

    if not auto_confirm:
        answer = input("This will remove the 'stage', 'src/gumui/_clr', and 'dist' directories. Continue? (y/n): ").strip().lower()
        if answer != 'y':
            print("Aborting. Run with --yes to skip prompt.")
            return

    for path in paths_to_clean:
        remove_all_in_directory(path)

if __name__ == "__main__":
    main()
