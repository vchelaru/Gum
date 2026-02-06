# async Programming

### Introduction

UI events may require interacting with systems that are async by default (return Tasks). MonoGame projects do not have a default synchronization context for keeping async code on the primary thread, but this can be added fairly easily.

This tutorial shows how to add and work with a synchronization context to allow async calls without leaving the primary thread. The MonoGameGumFromFile project shows how to add a synchronization context. You can view this project [here](https://github.com/vchelaru/Gum/tree/master/Samples/MonoGameGumFromFile).

### Why is a Synchronization Context Needed?

When a method makes an async call, its continuation is not guaranteed to be on the same thread as the code preceding the async call. For example, consider the following code:

```csharp
// Assume this is a click handler for a button:
async void HandleButtonClicked(object sender, EventArgs args)
{
  AnnounceButtonClicked();
  await Task.Delay(1000);
  // ------Remainder of method may not be on the primary thread
  Button.IsEnabled = false;
}
```

Unfortunatey, the code above may end up crashing or having other unexpected behaviors since the Button's IsEnabled is potentially being assigned on a non-primary thread. This is problematic because the UI may get updated mid-update, causing unexpected results.

Instead, we would like to keep all methods on the primary thread so that we can confidently interact with the UI without worrying about making changes mid-render.

### Adding the SingleThreadSynchronizationContext

The MonoGameGumFromFile sample includes a class called `SingleThreadSynchronizationContext` which provides a simple synchronization context for keeping all async call continuation on the primary thread. You can download the file and add it to your project from here:

{% embed url="https://github.com/vchelaru/Gum/blob/master/Samples/MonoGameGumFromFile/MonoGameGumFromFile/Managers/SingleThreadSynchronizationContext.cs" %}

Once you have added it to your project, you need to add an instance to your Game class, initialize it in your Initialize method, and call its Update method in the Game's Update, as shown in the following code snippet:

```csharp
public class YourGame : Game
{
    SingleThreadSynchronizationContext synchronizationContext;
    //...
    protected override void Initialize()
    {
        synchronizationContext = new SingleThreadSynchronizationContext();
        //...
    }
    
    protected override void Update(GameTime gameTime)
    {
        synchronizationContext.Update();
        //...
    }
    //...
```

After adding the SingleThreadSynchronizationContext class and after modifying your Game class as shown above, you can make async calls safely.

Keep in mind that the SingleThreadSynchronizationContext class can be modified for your own needs if you are comfortable making these changes.
