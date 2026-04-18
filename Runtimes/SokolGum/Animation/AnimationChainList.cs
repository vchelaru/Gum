using Gum.Content.AnimationChain;
using RenderingLibrary.Content;
using ToolsUtilities;

namespace SokolGum.Animation;

/// <summary>
/// Collection of <see cref="AnimationChain"/>s with chain-name lookup.
/// Typically loaded from a <c>.achx</c> file via <see cref="FromAchxFile"/>
/// which parses the cross-backend <see cref="AnimationChainListSave"/>
/// schema and resolves every referenced texture through Gum's
/// <see cref="LoaderManager"/>.
/// </summary>
public sealed class AnimationChainList : List<AnimationChain>
{
    public string? Name { get; set; }

    /// <summary>Looks up a chain by its name (case-sensitive). Returns null when not found.</summary>
    public AnimationChain? this[string name]
    {
        get
        {
            for (int i = 0; i < Count; i++)
                if (this[i].Name == name) return this[i];
            return null;
        }
    }

    /// <summary>
    /// Loads and converts a <c>.achx</c> file in one shot: parses the XML
    /// via <see cref="AnimationChainListSave.FromFile"/>, resolves every
    /// frame's <c>TextureName</c> through the current
    /// <see cref="LoaderManager.ContentLoader"/>, and normalizes pixel
    /// coordinates to UV when the file uses <c>CoordinateType=Pixel</c> so
    /// the per-frame source rect stays resolution-independent.
    /// </summary>
    public static AnimationChainList FromAchxFile(string fileName)
    {
        var save = AnimationChainListSave.FromFile(fileName);
        return FromSave(save, fileName);
    }

    /// <summary>
    /// Same conversion as <see cref="FromAchxFile"/> but from an already-
    /// deserialized save. Exposed so the <see cref="ContentLoader"/>
    /// caching layer can hand in the parsed object directly.
    /// </summary>
    public static AnimationChainList FromSave(AnimationChainListSave save, string? sourceFile = null)
    {
        var list = new AnimationChainList { Name = sourceFile };
        if (save.AnimationChains is null) return list;

        // .achx files written with pixel coordinates store raw pixel values
        // in the Left/Right/Top/Bottom fields rather than UVs. We normalize
        // to UV after loading the texture so the AnimationFrame can resolve
        // back to a pixel rect at draw time using the texture's actual size.
        bool isPixelCoords = save.CoordinateType == TextureCoordinateType.Pixel;

        // .achx FrameLength is stored in either seconds or milliseconds
        // depending on TimeMeasurementUnit. Normalize to seconds.
        float timeDivisor = save.TimeMeasurementUnit == TimeMeasurementUnit.Millisecond ? 1000f : 1f;

        // When textures are stored file-relative to the .achx (the usual
        // case so achx files are portable), use the .achx's own directory
        // as the base. Falls back to FileManager.RelativeDirectory when
        // the .achx path is unknown or the flag is off.
        string? achxDirectory = null;
        if (save.FileRelativeTextures && !string.IsNullOrEmpty(sourceFile))
        {
            achxDirectory = Path.GetDirectoryName(sourceFile);
            if (!string.IsNullOrEmpty(achxDirectory)) achxDirectory += Path.DirectorySeparatorChar;
        }

        foreach (var chainSave in save.AnimationChains)
        {
            var chain = new AnimationChain { Name = chainSave.Name };
            if (chainSave.Frames is null)
            {
                list.Add(chain);
                continue;
            }

            foreach (var frameSave in chainSave.Frames)
            {
                var frame = new AnimationFrame
                {
                    TextureName    = frameSave.TextureName,
                    FrameLength    = frameSave.FrameLength / timeDivisor,
                    FlipHorizontal = frameSave.FlipHorizontal,
                    FlipVertical   = frameSave.FlipVertical,
                    RelativeX      = frameSave.RelativeX,
                    RelativeY      = frameSave.RelativeY,
                };

                if (!string.IsNullOrEmpty(frameSave.TextureName))
                {
                    var texturePath = frameSave.TextureName;
                    if (FileManager.IsRelative(texturePath))
                    {
                        // Prefer the .achx's own directory (file-relative),
                        // matching the behaviour described by AnimationChainListSave.FileRelativeTextures.
                        var basePath = achxDirectory ?? FileManager.RelativeDirectory;
                        texturePath = FileManager.RemoveDotDotSlash(basePath + texturePath);
                    }
                    try
                    {
                        frame.Texture = LoaderManager.Self.ContentLoader.LoadContent<Texture2D>(texturePath);
                    }
                    catch
                    {
                        if (Gum.Wireframe.GraphicalUiElement.MissingFileBehavior
                            == Gum.Wireframe.MissingFileBehavior.ThrowException) throw;
                        // Missing textures leave Texture=null; Sprite.Render skips the frame.
                    }
                }

                if (isPixelCoords && frame.Texture is not null)
                {
                    frame.LeftCoordinate   = frameSave.LeftCoordinate   / frame.Texture.Width;
                    frame.RightCoordinate  = frameSave.RightCoordinate  / frame.Texture.Width;
                    frame.TopCoordinate    = frameSave.TopCoordinate    / frame.Texture.Height;
                    frame.BottomCoordinate = frameSave.BottomCoordinate / frame.Texture.Height;
                }
                else
                {
                    frame.LeftCoordinate   = frameSave.LeftCoordinate;
                    frame.RightCoordinate  = frameSave.RightCoordinate;
                    frame.TopCoordinate    = frameSave.TopCoordinate;
                    frame.BottomCoordinate = frameSave.BottomCoordinate;
                }

                chain.Add(frame);
            }

            list.Add(chain);
        }

        return list;
    }
}
