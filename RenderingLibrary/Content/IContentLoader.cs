using System;
using System.Collections.Generic;

namespace RenderingLibrary.Content;

/// <summary>
/// Strategy for turning a content name (typically a file path) into a loaded asset such as a
/// texture or font. Each rendering backend (MonoGame/KNI/FNA, Raylib, Skia, and so on) ships its
/// own implementation.
/// </summary>
/// <remarks>
/// The active loader is held by <see cref="LoaderManager.ContentLoader"/>; assign your own
/// implementation there to customize how Gum resolves assets — for example, to load from a custom
/// asset store, or to hand back an asset your game engine has already loaded so the same texture is
/// not loaded into memory twice. A custom loader typically wraps (delegates to) the built-in loader
/// for any content name it does not handle, so default loading and caching are preserved.
/// <para>
/// Implementations are responsible for their own caching. See the remarks on
/// <see cref="LoaderManager.LoadContent{T}"/> for why caching lives in the loader rather than in
/// <see cref="LoaderManager"/>.
/// </para>
/// </remarks>
public interface IContentLoader
{
    /// <summary>
    /// Loads content of the requested type by name, throwing if the type is unsupported or the
    /// content cannot be loaded.
    /// </summary>
    /// <typeparam name="T">The asset type to load, such as a texture.</typeparam>
    /// <param name="contentName">
    /// The content name, typically a file path resolved relative to
    /// <see cref="ToolsUtilities.FileManager.RelativeDirectory"/>.
    /// </param>
    /// <returns>The loaded asset.</returns>
    T LoadContent<T>(string contentName);

    /// <summary>
    /// Attempts to load content of the requested type by name, returning the type's default value
    /// (for example <c>null</c> for reference types) instead of throwing when the content is
    /// missing or cannot be loaded.
    /// </summary>
    /// <typeparam name="T">The asset type to load, such as a texture.</typeparam>
    /// <param name="contentName">
    /// The content name, typically a file path resolved relative to
    /// <see cref="ToolsUtilities.FileManager.RelativeDirectory"/>.
    /// </param>
    /// <returns>The loaded asset, or <c>default(T)</c> if it could not be loaded.</returns>
    T TryLoadContent<T>(string contentName);
}
