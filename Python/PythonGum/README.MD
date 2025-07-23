# Setup for Development

## Requirements

1. VSCode
1. Python 3 Installed
1. GUM source checked out (This project!) [We utilize the GumCommon.dll built]
1. Dotnet installed
1. (Windows for doing the DLL build since it's a PowerShell script, but you can manually do that if you want)


## Building GumCommon.DLL and it's dependencies

1. Open VSCode
1. In VSCode, open the Gum/Python/PythonGum/ folder
1. Go to Terminal > Run Task
![alt text](image.png)
1. Select Bootstrap
![alt text](image-1.png)
    1. This will Setup the Python Virtual Environment
    1. Update PIP (Package manager for Python)
    1. Install the required packages from (requirements.txt)
    1. Build the GumCommon.dll and pull in all required DLLs and put them in src\gum_runtime\_clr\net6.0\
    1. Create Intellisense PyLance Stubs
1. Now you Can open loader.py and click Play to test it.
![alt text](image-2.png)


