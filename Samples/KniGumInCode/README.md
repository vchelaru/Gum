# KniGumInCode: driving the browser build from Playwright

A Gum UI in the KNI browser build is one WebGL canvas, so Playwright can only click coordinates and compare screenshots. This sample adds a small **automation bridge** to the BlazorGL head so a test can find Gum elements by name, read and set their variables, and ask where an element is on screen. Clicks and typing still go through the real browser input path.

The bridge lives only in the sample. Gum ships no library code for it; copy `KniGumInCode.BlazorGL/Automation/GumAutomationBridge.cs` into your game and add the commands your game needs.

> **Warning:** the bridge can read and set any Gum object in the running game. Never ship it in a player build. It is compiled in only when you pass `-p:GumAutomation=true`; every normal build leaves it out.

## What is where

| Path | What |
|---|---|
| `KniGumInCode.BlazorGL/Automation/GumAutomationBridge.cs` | The bridge. The whole file sits inside `#if GUM_AUTOMATION`. |
| `KniGumInCode.BlazorGL/KniGumInCode.BlazorGL.csproj` | Defines `GUM_AUTOMATION` when `-p:GumAutomation=true`. |
| `KniGumInCode.BlazorGL/Pages/Index.razor.cs` | Calls `GumAutomationBridge.Install` under `#if GUM_AUTOMATION`. |
| `KniGumInCode.PlaywrightTests/` | C# Playwright tests that use the bridge. |
| `KniGumInCodeCommon/KniGumInCodeGame.cs` | The game. Elements the tests use are given a `Name`. |

## The page API

`Install` defines `window.gumAutomation`:

| Call | Returns |
|---|---|
| `ready()` | `true` once the game is running |
| `find(name)` | `{ name, type, visible }`; throws if there is no such element |
| `bounds(name)` | `{ left, top, width, height, centerX, centerY, visible }` in CSS pixels in the page viewport |
| `get(name, property)` | The variable's current value (enums come back as their name) |
| `set(name, property, value)` | `true`; `value` is a number, string, boolean or null |
| `call(command, args)` | What the other calls run on. `command` is `ready`, `find`, `bounds`, `get` or `set`; `args` is `{ name, property, value }` with the fields that command needs. Returns the command's result or throws. `call('bounds', ...)` is in canvas pixels; only `bounds()` converts to CSS pixels |

A name is an element's `Name`, or a dotted path such as `OptionsComboBox.TextInstance` for a name inside a component. Each segment is the shallowest descendant with that name under the previous one, so names that repeat in many components (`TextInstance`, `Background`) are reached through the named parent. The first segment is searched in `GumService.Default.Root`, then `PopupRoot` (open drop-downs), then `ModalRoot`.

`bounds` is what makes clicking work. C# reports the element in canvas pixels with the Gum camera zoom applied; the script scales that by `getBoundingClientRect().width / canvas.width` and adds the canvas's offset in the page. Browser zoom needs nothing extra because `getBoundingClientRect` already reports CSS pixels, which is what `page.Mouse` uses.

## Build and serve

From `Samples/KniGumInCode/KniGumInCode.BlazorGL`:

```
dotnet publish -c Release -p:GumAutomation=true -o <publish folder>
```

Pick a publish folder outside the repo. Then serve its `wwwroot` folder with any static server:

```
python -m http.server 5088 --directory <publish folder>/wwwroot
```

Without `-p:GumAutomation=true` the same project builds with no bridge: the type is not in `KniGumInCode.dll` and `window.gumAutomation` does not exist.

## Run the Playwright tests

The tests use the Chrome that is installed on the machine (`Channel = "chrome"`), so there is no browser download and no `playwright install`.

```
dotnet test Samples/KniGumInCode/KniGumInCode.PlaywrightTests
```

They expect the page at `http://localhost:5088/`; set `GUM_SAMPLE_URL` to use another address, and `GUM_HEADED=1` to watch the browser.

## Using it from a test

```csharp
// Where is the combo box? Ask Gum, then click it with the real mouse.
GumBounds box = await page.EvaluateAsync<GumBounds>("name => window.gumAutomation.bounds(name)", "OptionsComboBox");
await page.Mouse.ClickAsync((float)box.CenterX, (float)box.CenterY);

// Read and set variables by name.
await page.EvaluateAsync("() => window.gumAutomation.set('TitleText', 'Text', 'Hello')");
string? text = await page.EvaluateAsync<string?>("() => window.gumAutomation.get('TitleText', 'Text')");
```

`GumPageExtensions.cs` wraps these as `GumBoundsAsync`, `GumClickAsync`, `GumSetAsync` and `GumGetTextAsync`. Gum applies input and variable changes on the game's next frame, so poll for the result instead of reading straight after a click (see `WaitForAsync` in `KniGumInCodeBrowserTests.cs`).

## Limits

- Bounds ignore rotation; a rotated element reports its unrotated box.
- The tests are written against headless Chrome with software WebGL (`--enable-unsafe-swiftshader`); if your machine renders blank, run with `GUM_HEADED=1`.
