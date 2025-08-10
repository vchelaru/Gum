import pythonnet; pythonnet.load("coreclr")
import sys, clr

#sys.path.append(r"C:\git\Gum\GumCommon\bin\Debug\net6.0")
sys.path.append(r"C:\git\Gum\python\PythonGum\src\gum_runtime\_clr")

try:
    clr.AddReference("GumCommon")
    print("GumCommon reference loaded successfully.")
except Exception as e:
    print("Error loading GumCommon:", e)

import System
from System import Reflection

try:
    assembly = Reflection.Assembly.Load("GumCommon")
    types = assembly.GetTypes()
    print("Types loaded successfully. Listing types:")
    for t in types:
        print(" -", t.FullName)
except Reflection.ReflectionTypeLoadException as rtle:
    print("ReflectionTypeLoadException occurred:")
    for ex in rtle.LoaderExceptions:
        print(" - LoaderException:", ex.Message)
except System.Exception as se:
    print("General .NET Exception occurred:", se)
