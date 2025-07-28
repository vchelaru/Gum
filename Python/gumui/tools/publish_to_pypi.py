import sys
import subprocess
from pathlib import Path

HOME = Path.home()
PYPIRC = HOME / ".pypirc"

if not PYPIRC.exists():
    print("ERROR: Missing ~/.pypirc file.")
    print("Please create one with your PyPI token:")
    print("https://pypi.org/manage/account/token/")
    sys.exit(1)

print(".pypirc found. Uploading to PyPI...")
subprocess.run([sys.executable, "-m", "twine", "upload", "dist/*"], check=True)
