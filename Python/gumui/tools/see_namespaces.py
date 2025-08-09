import pythonnet; pythonnet.load("coreclr")
import sys, clr, os

# get directory of THIS script so we can find the clr files
here = os.path.dirname(os.path.abspath(__file__))
clr_path = os.path.join(here, "..", "src", "gumui", "_clr", "net6.0")
clr_path = os.path.abspath(clr_path)  # normalize to absolute
sys.path.append(clr_path)

try:
    clr.AddReference("GumCommon") # type: ignore
    print("GumCommon reference loaded successfully.")
except Exception as e:
    print("Error loading GumCommon:", e)


from Gum.Wireframe import GraphicalUiElement

# run after you've clr.AddReference'd all your DLLs
import System
from System.Reflection import ReflectionTypeLoadException

def safe_types(asm):
    try:
        return asm.GetTypes()
    except ReflectionTypeLoadException as ex:
        return [t for t in ex.Types if t]

all_ns = set()
for asm in System.AppDomain.CurrentDomain.GetAssemblies():
    for t in safe_types(asm):
        if t and t.Namespace:
            all_ns.add(t.Namespace)

ignore = {"System", "Microsoft", "mscorlib", "Mono"}  # noise
roots = sorted({ns.split(".")[0] for ns in all_ns if ns.split(".")[0] not in ignore})
print("Roots:", roots)

# (optional) see full namespaces under each root
by_root = {}
for ns in all_ns:
    root = ns.split(".")[0]
    if root in ignore: continue
    by_root.setdefault(root, set()).add(ns)
