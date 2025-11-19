# LastFrameDrawStates

### Introduction

LastFrameDrawStates is an IEnumerable for the draw states used in the previous draw call. This can be used to find performance problems and isolate what may be causing state changes.

### Viewing State Changes

The following code can be used to output state changes to the Output window in Visual Studio

```csharp
// It's best to use some condition rather than reporting
// this ever frame to avoid spamming the output window:
if(someCondition)
{
    foreach (var item in SystemManagers.Default.Renderer.SpriteRenderer.LastFrameDrawStates)
    {
        foreach (var changeRecord in item.ChangeRecord)
        {
                System.Diagnostics.Debug.Output.Write(
                        "Texture {changeRecord.Texture} by {changeRecord.ObjectRequestingChange}");
        }
    }
}

```
