#nullable enable
using RenderingLibrary.Content;

namespace Gum;

public sealed partial class GumContext
{
    /// <summary>
    /// This service's content cache and content loader. Created on first use; the first live context
    /// adopts the process-wide <see cref="LoaderManager"/> the old static always held.
    /// </summary>
    internal LoaderManager? LoaderManager { get; set; }
}
