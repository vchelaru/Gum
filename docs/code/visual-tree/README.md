# Visual Tree

## Introduction

At runtime, every `GraphicalUiElement` (and the runtime classes that derive from it — `ContainerRuntime`, `TextRuntime`, `SpriteRuntime`, control visuals, and so on) lives in a parent-child hierarchy known as the **visual tree**. A screen, a Forms control, or any composite component you build is always a sub-tree of `GraphicalUiElement` instances rooted at a parent.

This section covers tasks that involve navigating or manipulating that tree from code: finding a specific child by name or type, walking ancestors, enumerating descendants, and similar runtime operations.
