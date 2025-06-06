# Setup

## Introduction

This page shows how to set up Gum to be used in a Maui app using SkiaSharp. This page assumes that you have a functional Maui app.

## Adding Gum NuGet Package

1. Open your Maui project in your preferred IDE
2.  Add the `Gum.SkiaSharp.Maui` NuGet package. Note that this also adds SkiaSharp to your project.\
    \


    <figure><img src="../../../.gitbook/assets/image (200).png" alt=""><figcaption><p>Add Gum.SkiaSharp.Maui NuGet package</p></figcaption></figure>

## Initializing SkiaSharp

If you haven't already set up SkiaSharp for your project, add `.UseSkiaSharp()` to your Builder. For more information see the SkiaSharp setup: [https://learn.microsoft.com/en-us/samples/dotnet/maui-samples/skiasharpmaui-demos/](https://learn.microsoft.com/en-us/samples/dotnet/maui-samples/skiasharpmaui-demos/)

## Adding a SkiaGumCanvasView

You can add SkiaGumCanvasView instances to any page or component. SkiaGumCanvasView is a view which inherits from SKCanvasView, but allows adding of Gum runtime elements. To add a SkiaGumCanvasView:

1. Open the .xaml file for an existing view or component
2.  Add the following namespace to your page or view:\


    ```xml
    xmlns:SkiaGum="clr-namespace:SkiaGum.Maui;assembly=SkiaGum.Maui"
    ```
3.  Add the following to a container, such as to a Grid. Note that the xaml specifies a Name because all Gum objects are added in C#:\


    ```xml
    <SkiaGum:SkiaGumCanvasView 
        WidthRequest="200" 
        HeightRequest="200" 
        x:Name="SkiaGumCanvasView"/>
    ```
4.  Add the following in your C# (codebehind) for the given page or component:\


    ```csharp
    // Creates a circle:
    var circle = new ColoredCircleRuntime();
    circle.Color = SKColors.Red;
    circle.Width = 400;
    circle.Height = 400;
    // Adds it so it is drawn
    SkiaGumCanvasView.AddChild(circle);
    // Tells the canvas to refresh itself, so the circle appears
    SkiaGumCanvasView.InvalidateSurface();
    ```

You should now have a red circle on screen.

<figure><img src="../../../.gitbook/assets/image (201).png" alt=""><figcaption><p>Red ColoredCircleRuntime on screen</p></figcaption></figure>
