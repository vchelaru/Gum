# Help

## Create Diagnostics File

{% hint style="info" %}
Available in December 2026, or now if building Gum from source.
{% endhint %}

**Help > Create diagnostics file...** saves a zip of the information a maintainer usually asks for when you report a problem. Nothing is uploaded. Attach the zip to a GitHub issue or post it in Discord yourself.

Optionally describe what went wrong, then choose what to add:

* Always included: Gum version, operating system, .NET runtime, CPU architecture, display scale, graphics card, recent logs, the current errors, installed plugins and tool settings.
* **Gum project**: the .gumx file and its screens, components and standards.
* **C# project**: the whole game project without build output, so a maintainer can run it. Disabled if Gum cannot find it.

Your user name and home folder path are removed from the logs, errors, plugins and settings. Your project files and description are saved as they are.

Save zip writes `GumDiagnostics-<date>.zip` to your Documents folder and shows it in your file manager. An existing file is never overwritten; a second zip on the same day ends in `-2`.
