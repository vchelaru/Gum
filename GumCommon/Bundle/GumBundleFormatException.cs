using System;

namespace Gum.Bundle;

/// <summary>
/// Thrown when a `.gumpkg` bundle stream is malformed: bad magic, unsupported version,
/// or truncated/corrupt compressed payload.
/// </summary>
public class GumBundleFormatException : Exception
{
    /// <summary>Initializes a new <see cref="GumBundleFormatException"/> with no message.</summary>
    public GumBundleFormatException() { }

    /// <summary>Initializes a new <see cref="GumBundleFormatException"/> with the supplied message.</summary>
    public GumBundleFormatException(string message) : base(message) { }

    /// <summary>Initializes a new <see cref="GumBundleFormatException"/> wrapping an inner exception.</summary>
    public GumBundleFormatException(string message, Exception innerException) : base(message, innerException) { }
}
