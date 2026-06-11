using PdfSpec.Geometry;

namespace PdfSpec.Layout;

/// <summary>
/// Where an <see cref="Element"/> ended up after the layout engine
/// placed it. Handed to <see cref="Element.OnRendered"/> once per
/// <see cref="Element.Render"/> call so callers (e.g. a link-button
/// component, an outline anchor) can wire page-level annotations to
/// the final on-page rectangle without ever dealing with absolute
/// coordinates in their own composition code.
/// </summary>
/// <param name="Page">The page the element landed on. Use this for
/// page-level annotation hosts (<c>AddLinkAnnotation</c>, …).</param>
/// <param name="PageNumber">1-based index of <paramref name="Page"/>
/// in the owning document's page list at the time the hook fires.</param>
/// <param name="Bounds">The element's bounding rectangle in PDF user
/// coordinates (bottom-left origin) on <paramref name="Page"/> —
/// directly usable as an annotation <c>Rect</c>.</param>
public sealed record RenderedData(PdfPage Page, int PageNumber, PdfRectangle Bounds);
