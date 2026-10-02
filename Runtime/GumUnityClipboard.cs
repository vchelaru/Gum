#nullable enable
using System;
using Gum.Forms.Controls;
using UnityEngine;

namespace Gum.Unity
{
    /// <summary>
    /// TextBox copy and paste through Unity's <see cref="GUIUtility.systemCopyBuffer"/>, the OS clipboard.
    /// </summary>
    public sealed class GumUnityClipboard : IGumClipboard
    {
        /// <inheritdoc/>
        public string? GetText(Action? callback) => GUIUtility.systemCopyBuffer;

        /// <inheritdoc/>
        public void SetText(string text) => GUIUtility.systemCopyBuffer = text;
    }
}
