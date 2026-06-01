using CSharpPdf.Geometry;
using CSharpPdf.Objects;

namespace CSharpPdf.Content;

/// <summary>
/// Page-level orchestrator for the content stream. <c>IPdfCanvas</c> itself
/// holds no drawing methods — every state-mutating operator lives on
/// <see cref="PdfGraphics"/>, obtained only through <see cref="Graphics"/>,
/// so the q…Q wrap around drawing is a compile-time invariant rather than a
/// discipline.
///
/// What the canvas does expose are the things that exist outside the
/// graphics-state scope: entries to nested sub-states (graphics, text object,
/// marked content) and page-level operations the spec models separately from
/// the content stream (annotations).
///
/// Scope-entry methods (<see cref="Graphics"/>, <see cref="Text"/>) return a
/// disposable narrowed interface — use them with <c>using</c> so the matching
/// closer (Q, ET) is emitted automatically:
/// <code>
/// using var g = canvas.Graphics();
/// g.SetFillRgb(1, 0, 0);
/// g.DrawRectangle(0, 0, 50, 50, fill: Colors.Red);
/// </code>
///
/// Marked-content scopes still take a callback because their body is itself
/// an <c>IPdfCanvas</c> (recursive) and the closer is unambiguous.
///
/// Coordinates are PDF user space: origin bottom-left, Y increases upward;
/// arguments are in points unless noted.
/// </summary>
public interface IPdfCanvas
{
    /// <summary>q…Q — open a saved graphic state and return the drawing surface. Dispose emits Q. The only way to access drawing operators.</summary>
    PdfGraphics Graphics();

    /// <summary>BT…ET — open a text object and return the text-object surface. Dispose emits ET.</summary>
    PdfTextObject Text();

    // ===== Marked content / structure / optional content (§14.6) ===
    // BMC…EMC, BDC…EMC. The body is itself an IPdfCanvas — marked
    // content stays at the page-description level, where the canvas's
    // full surface (annotations, nested scopes, drawing via SavedState)
    // remains available. Marked content is intentionally not exposed on
    // PdfGraphics: it's a page-level structuring concern, not a drawing
    // concern. To wrap drawing in a marked sequence, compose
    // canvas.MarkedContent(c => c.SavedState(g => …)).

    /// <summary>BMC…EMC — wrap <paramref name="body"/> in a marked-content sequence tagged <paramref name="tag"/>.</summary>
    void MarkedContent(string tag, Action<IPdfCanvas> body);

    /// <summary>BDC…EMC — marked-content sequence with an associated property-list dictionary.</summary>
    void MarkedContent(string tag, PdfDictionary properties, Action<IPdfCanvas> body);

    /// <summary>BDC…EMC over an OCG/OCMD registered in the page's Properties — toggles visibility with the named optional content.</summary>
    void OptionalContent(string registeredPropertyName, Action<IPdfCanvas> body);

    /// <summary>BDC…EMC carrying a structure MCID — links page content to a structure element in the document's StructTreeRoot.</summary>
    void StructureContent(string tag, int mcid, Action<IPdfCanvas> body);

    /// <summary>BMC…EMC under the <c>Artifact</c> tag — content that isn't part of the logical structure (page numbers, rules, headers).</summary>
    void Artifact(Action<IPdfCanvas> body);

    /// <summary>MP — a single-point marker with the given tag.</summary>
    void MarkPoint(string tag);

    /// <summary>DP — a single-point marker with an associated property-list dictionary.</summary>
    void MarkPoint(string tag, PdfDictionary properties);

    // ===== Annotations (§12.5) =====================================
    // These edit the page's /Annots array; they don't touch the content
    // stream at all, so they live outside the drawing surface.

    /// <summary>Add a raw annotation dictionary to the page.</summary>
    PdfReference AddAnnotation(PdfDictionary annotation);

    /// <summary>Add a Link annotation triggering <paramref name="action"/> when clicked.</summary>
    PdfReference AddLink(PdfRectangle rect, PdfDictionary action);

    /// <summary>Add a Link annotation that opens <paramref name="url"/> in an external viewer.</summary>
    PdfReference AddUrlLink(PdfRectangle rect, string url);

    /// <summary>Add a Link annotation that jumps to an explicit in-document destination (e.g. <c>[pageRef /Fit]</c>).</summary>
    PdfReference AddGoToLink(PdfRectangle rect, PdfArray destination);

    /// <summary>Add a Link annotation that jumps to a destination registered in the Dests name tree.</summary>
    PdfReference AddGoToLink(PdfRectangle rect, string namedDestination);

    /// <summary>Add a sticky-note Text annotation with a paired Pop-up.</summary>
    void AddTextNote(PdfRectangle iconRect, string contents, string icon,
        PdfRectangle popupRect, bool open = true);
}
