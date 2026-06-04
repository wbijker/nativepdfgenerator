using System.Runtime.CompilerServices;
using PdfSpec;

namespace CSharpPdf.Content;

/// <summary>
/// Extension API that adds a per-page <see cref="PdfCanvas"/> to
/// <see cref="PdfPage"/>. The canvas is lazily created and cached against the
/// page instance so repeated <c>page.Canvas()</c> calls return the same object.
/// </summary>
public static class PdfPageCanvasExtensions
{
    private static readonly ConditionalWeakTable<PdfPage, PdfCanvas> _cache = new();

    /// <summary>The unified <see cref="PdfCanvas"/> bound to this page.</summary>
    public static PdfCanvas Canvas(this PdfPage page) =>
        _cache.GetValue(page, p => new PdfCanvas(p, p.Document));
}
