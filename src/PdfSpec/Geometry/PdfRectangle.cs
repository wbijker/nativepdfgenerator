using PdfSpec.Objects;

namespace PdfSpec.Geometry;

/// <summary>
/// A PDF rectangle, serialized as <c>[ llx lly urx ury ]</c> in user-space units
/// (lower-left and upper-right corners). The coordinate origin is the bottom-left.
/// </summary>
public readonly struct PdfRectangle
{
    public double Left { get; }
    public double Bottom { get; }
    public double Right { get; }
    public double Top { get; }

    public PdfRectangle(double left, double bottom, double right, double top)
    {
        Left = left;
        Bottom = bottom;
        Right = right;
        Top = top;
    }

    public double Width => Right - Left;
    public double Height => Top - Bottom;

    /// <summary>A rectangle anchored at the origin with the given size.</summary>
    public static PdfRectangle FromSize(double width, double height) =>
        new(0, 0, width, height);

    public PdfArray ToArray() => new(
        Number(Left), Number(Bottom), Number(Right), Number(Top));

    // Emit whole values as integers so output reads like [0 0 612 792].
    internal static PdfNumber Number(double value) =>
        value == Math.Floor(value) && !double.IsInfinity(value)
            ? new PdfNumber((long)value)
            : new PdfNumber(value);
}
