namespace CSharpPdf.Layout;

/// <summary>How a slot's length (height in Rows, width in Cols) is determined.</summary>
public enum Sizing
{
    /// <summary>Use the content's natural length.</summary>
    Auto,
    /// <summary>Use a fixed length (see <c>SlotElement.Length</c>).</summary>
    Fixed,
    /// <summary>Share the remaining length with other relative slots by weight (<c>SlotElement.Length</c>).</summary>
    Relative,
}
