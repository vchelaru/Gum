# SpriteRenderer

### Introduction

The SpriteRenderer class is responsible for performing MonoGame/FNA-based rendering. It exposes an interface similar to SpriteBatch, but it provides additional functionality including storing a stack of states and providing information about state changes which can be useful for diagnostics.

### Accessing SpriteRenderer

The SpriteRenderer lives as an object in the Renderer class and can be accessed through the Renderer instance. For example, you can access the SpriteRenderer as shown in the following code:

```csharp
var spriteRenderer = SystemManagers.Default.Renderer.SpriteRenderer;
```
