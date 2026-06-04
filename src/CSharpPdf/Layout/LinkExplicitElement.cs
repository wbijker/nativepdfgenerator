using CSharpPdf.Content;
using PdfSpec.Geometry;
using PdfSpec.Navigation;
using PdfSpec.Objects;

namespace CSharpPdf.Layout;

/// <summary>
/// Wraps a child element and overlays a PDF link annotation whose target is an
/// <b>explicit</b> destination — a direct <c>[pageRef /XYZ … ]</c> array, not a
/// named-destination lookup via the <c>/Dests</c> name tree.
///
/// Explicit destinations work on every PDF reader (including the weak ones on
/// e-ink devices), avoid the cost of resolving through a name tree, and let
/// the document drop the name tree entirely if it's unused.
///
/// Set <see cref="TargetPageNumber"/> (1-based) for a direct page jump, or set
/// <see cref="TargetAnchor"/> to the name of a <see cref="NamedAnchorElement"/>
/// placed elsewhere in the document — the anchor's page is looked up after the
/// layout pass completes and substituted at finalize time.
/// </summary>
public sealed class LinkExplicitElement : Element
{
    public Element? Content { get; set; }

    /// <summary>1-based page number to jump to. Mutually exclusive with <see cref="TargetAnchor"/>.</summary>
    public int? TargetPageNumber { get; set; }

    /// <summary>
    /// Name of a <see cref="NamedAnchorElement"/> to resolve to a page number
    /// at finalize time. Mutually exclusive with <see cref="TargetPageNumber"/>.
    /// </summary>
    public string? TargetAnchor { get; set; }

    public LinkExplicitElement() { }

    public override SpaceDimension SpaceHint(SizeRect available) =>
        Content?.SpaceHint(available) ?? SpaceDimension.Empty;

    protected override RenderResult RenderCore(PdfCanvas context, Size available)
    {
        if (Content is null)
        {
            return new RenderResult(null, context.Cursor);
        }
        Point start = context.Cursor;
        var result = Content.Render(context, available);

        // Capture the rect in absolute coords before we lose the cursor position.
        double left = context.ToAbsoluteX(start.X);
        double top = context.ToAbsoluteY(start.Y);
        double bottom = System.Math.Min(top, context.ToAbsoluteY(result.Next.Y));
        double right = left + available.Width;
        var rect = new PdfRectangle(left, bottom, right, top);

        // Capture the source page; the annotation must be added to it.
        var sourcePage = context.Page;
        var targetPage = TargetPageNumber;
        var targetAnchor = TargetAnchor;

        // Defer until after layout: anchors register their pages during render,
        // so an anchor-based link can only resolve once every page is laid out.
        // We use a zero-size sub-canvas — we're not drawing into it, just
        // riding the deferred queue.
        context.Defer(0, 0, deferred =>
        {
            int? pageIndex0 = null;
            if (targetPage is { } p)
            {
                pageIndex0 = p - 1;
            }
            else if (targetAnchor is { } a)
            {
                // The captured value is the 1-based page number recorded by
                // NamedAnchorElement.
                int capturedPage = deferred.Lookup<int>(NamedAnchorElement.PageKey(a));
                if (capturedPage > 0) pageIndex0 = capturedPage - 1;
            }

            if (pageIndex0 is not { } idx || idx < 0 || idx >= deferred.Document.Pages.Count)
            {
                // Unresolved — skip the annotation. Better silent than broken.
                return;
            }

            var pageRef = deferred.Document.Pages[idx].Reference;
            // [pageRef /XYZ null null null] — jump to top-left of the target page,
            // keep the user's current zoom. Universally supported.
            var dest = new PdfArray(
                pageRef,
                new PdfName("XYZ"),
                PdfNull.Instance,
                PdfNull.Instance,
                PdfNull.Instance);
            sourcePage.AddLinkAnnotation(rect, PdfAction.GoTo(dest));
        });

        return result;
    }
}
