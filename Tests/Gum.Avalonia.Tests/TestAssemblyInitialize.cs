// The views share one headless Application and its resources, and the canvas tests share one
// thread-affine graphics device, so test classes run one at a time, as in every Gum test project.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
