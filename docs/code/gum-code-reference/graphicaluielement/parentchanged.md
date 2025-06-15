# ParentChanged

## Introduction

The ParentChanged event is invoked whenever an object's direct parent is changed. This can happen when any of the following occur:

1. The Parent property is assigned to a null or non-null value
2. An object is added to a parent's Children collection
3. An object is added to root
4. A Forms object creates and adds a child Forms object

## Code Example: Handling Parent Changed

The following code raises the ParentChanged event when children are added to a parent using a variety of methods:

```csharp
var child= new ContainerRuntime();
child.ParentChanged += (_,_) => System.Diagnostics.Debug.WriteLine("parent changed");

var parent = new ContainerRuntime();

// the following code raises the event 4 times
parent.Children.Add(child);
parent.Children.Remove(child);

child.Parent = parent;
child.Parent = null;
```
