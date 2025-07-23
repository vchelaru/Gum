# tools/fetch_nuget.py
import io, json, zipfile, urllib.request, pathlib, sys

PKG = "FlatRedBall.GumCommon"
TFM = "net6.0"   # good universal target
OUT = pathlib.Path("src/gum_runtime/_clr")  # where DLLs will live in your package

def download(url: str) -> bytes:
    with urllib.request.urlopen(url) as r:
        return r.read()

def latest_version(pkg: str) -> str:
    idx_url = f"https://api.nuget.org/v3-flatcontainer/{pkg.lower()}/index.json"
    data = json.loads(download(idx_url))
    return data["versions"][-1]

def extract_dlls(nupkg_bytes: bytes, tfm: str, out: pathlib.Path):
    out.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(io.BytesIO(nupkg_bytes)) as z:
        for name in z.namelist():
            if name.startswith(f"lib/{tfm}/") and name.endswith(".dll"):
                z.extract(name, out)
                (out / name).rename(out / pathlib.Path(name).name)

def main(ver: str | None = None):
    ver = ver or latest_version(PKG)
    base = "https://api.nuget.org/v3-flatcontainer"
    url = f"{base}/{PKG.lower()}/{ver}/{PKG.lower()}.{ver}.nupkg"
    nupkg = download(url)
    extract_dlls(nupkg, TFM, OUT)
    print(f"Fetched {PKG} {ver} -> {OUT}")

if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else None)
