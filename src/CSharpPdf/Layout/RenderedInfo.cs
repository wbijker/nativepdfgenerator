using PdfSpec.Geometry;

namespace CSharpPdf.Layout;

/// <summary>
/// An axis-aligned rectangle in PDF user space. (<see cref="X"/>, <see cref="Y"/>)
/// is the upper-left corner; <see cref="Y"/> is the box's <i>top</i> edge in
/// PDF Y-up coordinates, so the rectangle spans (X, Y − Height) → (X + Width, Y).
/// </summary>
public readonly record struct Boundary(double X, double Y, double Width, double Height);

/// <summary>
/// Snapshot of where a <see cref="Element"/> ended up after being rendered.
/// Carried to the element's <see cref="Element.OnRendered"/> handler so the
/// caller can record element placements (for cross-page overlays, layout
/// inspectors, accessibility tagging, etc.).
/// </summary>
/// <param name="AbsolutePos">Top-left of the element in PDF absolute user space (Y is the top edge).</param>
/// <param name="Page">1-based page number the element was rendered on.</param>
/// <param name="Boundary">Full bounding box of the rendered element (same X/Y as <paramref name="AbsolutePos"/>, plus the rendered width and height).</param>
public readonly record struct RenderedInfo(Point AbsolutePos, int Page, Boundary Boundary);

/// <summary>
/// Context passed to a <see cref="DynamicContentElement"/>'s deferred
/// callback — carries the document-wide values that aren't known until the
/// layout pass is complete. Re-read on every deferred replay (one per page
/// the element landed on).
/// </summary>
/// <param name="Page">1-based page the dynamic block is being patched onto.</param>
/// <param name="TotalPages">Total page count, now final.</param>
public readonly record struct DynamicContext(int Page, int TotalPages);
