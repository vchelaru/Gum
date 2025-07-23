import pythonnet; pythonnet.load("coreclr")
import sys, clr


# sys.path.append(r"C:\git\Gum\GumCommon\bin\Debug\net6.0")
sys.path.append(r"C:\git\Gum\python\PythonGum\src\gum_runtime\_clr\net6.0")

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
