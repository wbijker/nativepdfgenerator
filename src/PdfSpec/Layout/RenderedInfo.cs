using PdfSpec.Geometry;

namespace PdfSpec.Layout;

/// <summary>
/// Where a <see cref="Elements.BoxElement"/> ended up after the layout
/// engine placed it. Handed to <see cref="Elements.BoxElement.OnRendered"/>
/// once per render so callers (e.g. a link-button component) can wire
/// page-level annotations to the final on-page rectangle without ever
/// dealing with absolute coordinates in their own composition code.
/// </summary>
public sealed record RenderedInfo(PdfPage Page, PdfRectangle Bounds);
