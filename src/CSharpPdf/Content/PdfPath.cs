using CSharpPdf.Layout;

namespace CSharpPdf.Content;

/// <summary>
/// Path construction surface — the path-object state of a PDF content stream.
/// The only operators valid here are the path-construction ones (m, l, c, v,
/// y, h, re). Painting and clipping are intentionally absent: the enclosing
/// call (<c>PdfGraphics.StrokePath</c>, <c>FillPath</c>, <c>ClipPath</c>, …)
/// supplies the terminator automatically. State changes, transforms, and
/// colour are also absent — PDF forbids them between path construction and
/// the path's painter.
/// </summary>
public interface PdfPath
{
    /// <summary>m — begin a new subpath at (x, y).</summary>
    void MoveTo(double x, double y);

    /// <summary>l — append a straight line segment from the current point to (x, y).</summary>
    void LineTo(double x, double y);

    /// <summary>c — append a cubic Bézier with both control points given.</summary>
    void CurveTo(double x1, double y1, double x2, double y2, double x3, double y3);

    /// <summary>v — append a cubic Bézier using the current point as the first control point.</summary>
    void CurveToV(double x2, double y2, double x3, double y3);

    /// <summary>y — append a cubic Bézier using the endpoint as the second control point.</summary>
    void CurveToY(double x1, double y1, double x3, double y3);

    /// <summary>h — close the current subpath by drawing a line to its starting point.</summary>
    void ClosePath();

    /// <summary>re — append a closed rectangular subpath (origin bottom-left, in user space).</summary>
    void Rectangle(double x, double y, double width, double height);

    /// <summary>Append a closed circular subpath (Bézier-approximated).</summary>
    void Circle(double cx, double cy, double r);

    /// <summary>Append a closed elliptical subpath (Bézier-approximated).</summary>
    void Ellipse(double cx, double cy, double rx, double ry);

    /// <summary>Append a closed rounded-corner rectangular subpath (Bézier-approximated arcs).</summary>
    void RoundedRectangle(double x, double y, double width, double height, double radius);

    /// <summary>Append a closed polygonal subpath through the given vertices.</summary>
    void Polygon(ReadOnlySpan<Point> points);

    /// <summary>Append an open polyline subpath through the given vertices.</summary>
    void Polyline(ReadOnlySpan<Point> points);
}
