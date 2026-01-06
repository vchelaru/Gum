namespace RenderingLibrary.Graphics;

/// <summary>
/// Interface for objects which have visibility, and which
/// can be part of a visibility hierarchy.
/// </summary>
public interface IVisible
{
    /// <summary>
    /// Gets or sets a value indicating whether this object is locally visible.
    /// This does not account for parent visibility; use <see cref="AbsoluteVisible"/> 
    /// to determine if the object is actually visible in the hierarchy.
    /// </summary>
    /// <value>
    /// <c>true</c> if this object is visible; otherwise, <c>false</c>.
    /// </value>
    bool Visible
    {
        get;
        set;
    }


    /// <summary>
    /// Gets the parent object in the visibility hierarchy.
    /// </summary>
    /// <value>
    /// The parent <see cref="IVisible"/> object, or <c>null</c> if this object has no parent.
    /// </value>
    IVisible? Parent
    {
        get;
    }

    /// <summary>
    /// Gets a value indicating whether this object is absolutely visible, 
    /// taking into account the visibility of all ancestors in the hierarchy.
    /// </summary>
    /// <value>
    /// <c>true</c> if this object and all of its ancestors are visible; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// This property recursively checks the visibility of the entire parent chain.
    /// An object is only absolutely visible if both its own <see cref="Visible"/> property 
    /// is <c>true</c> and all of its parents are absolutely visible.
    /// </remarks>
    bool AbsoluteVisible { get; }

    public bool GetAbsoluteVisible()
    {
        if (Parent == null)
        {
            return Visible;
        }
        else
        {
            return Visible && Parent.AbsoluteVisible;
        }
    }
}
