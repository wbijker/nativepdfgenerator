using PdfSpec.Geometry;
using PdfSpec.Objects;

namespace CSharpPdf.Content;

/// <summary>
/// Page-level orchestrator for the content stream. <c>IPdfCanvas</c> itself
/// holds no drawing methods — every state-mutating operator lives on
/// <see cref="PdfGraphics"/>, obtained only through <see cref="Graphics"/>,
/// so the q…Q wrap around drawing is a compile-time invariant rather than a
/// discipline.
/// </summary>
public interface IPdfCanvas
{
    /// <summary>q…Q — open a saved graphic state and return the drawing surface. Dispose emits Q.</summary>
    PdfGraphics Graphics();

    /// <summary>BT…ET — open a text object and return the text-object surface. Dispose emits ET.</summary>
    PdfTextObject Text();

    /// <summary>BMC…EMC — wrap <paramref name="body"/> in a marked-content sequence tagged <paramref name="tag"/>.</summary>
    void MarkedContent(string tag, Action<IPdfCanvas> body);

    /// <summary>BDC…EMC — marked-content sequence with an associated property-list dictionary.</summary>
    void MarkedContent(string tag, PdfDictionary properties, Action<IPdfCanvas> body);

    /// <summary>BDC…EMC over an OCG/OCMD registered in the page's Properties.</summary>
    void OptionalContent(string registeredPropertyName, Action<IPdfCanvas> body);

    /// <summary>BDC…EMC carrying a structure MCID — links page content to a structure element.</summary>
    void StructureContent(string tag, int mcid, Action<IPdfCanvas> body);

    /// <summary>BMC…EMC under the <c>Artifact</c> tag.</summary>
    void Artifact(Action<IPdfCanvas> body);

    /// <summary>MP — a single-point marker with the given tag.</summary>
    void MarkPoint(string tag);

    /// <summary>DP — a single-point marker with an associated property-list dictionary.</summary>
    void MarkPoint(string tag, PdfDictionary properties);

    /// <summary>Add a raw annotation dictionary to the page.</summary>
    PdfReference AddAnnotation(PdfDictionary annotation);

    /// <summary>Add a Link annotation triggering <paramref name="action"/> when clicked.</summary>
    PdfReference AddLink(PdfRectangle rect, PdfDictionary action);

    /// <summary>Add a Link annotation that opens <paramref name="url"/> in an external viewer.</summary>
    PdfReference AddUrlLink(PdfRectangle rect, string url);

    /// <summary>Add a Link annotation that jumps to an explicit in-document destination.</summary>
    PdfReference AddGoToLink(PdfRectangle rect, PdfArray destination);

    /// <summary>Add a Link annotation that jumps to a destination registered in the Dests name tree.</summary>
    PdfReference AddGoToLink(PdfRectangle rect, string namedDestination);

    /// <summary>Add a sticky-note Text annotation with a paired Pop-up.</summary>
    void AddTextNote(PdfRectangle iconRect, string contents, string icon,
        PdfRectangle popupRect, bool open = true);
}
