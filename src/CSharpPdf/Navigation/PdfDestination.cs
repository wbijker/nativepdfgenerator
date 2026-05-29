using CSharpPdf.Objects;

namespace CSharpPdf.Navigation;

/// <summary>
/// Builds explicit destination arrays (Chapter 5, "Explicit Destinations"): a
/// page reference followed by a zoom/fit mode and its parameters. A null
/// coordinate means "leave this value unchanged when the destination is invoked".
/// </summary>
public static class PdfDestination
{
    /// <summary>[page /Fit] — fit the whole page in the window.</summary>
    public static PdfArray Fit(PdfReference page) =>
        new(page, new PdfName("Fit"));

    /// <summary>[page /FitH top] — fit the page width, positioned at <paramref name="top"/>.</summary>
    public static PdfArray FitH(PdfReference page, double top) =>
        new(page, new PdfName("FitH"), Num(top));

    /// <summary>[page /FitV left] — fit the page height, positioned at <paramref name="left"/>.</summary>
    public static PdfArray FitV(PdfReference page, double left) =>
        new(page, new PdfName("FitV"), Num(left));

    /// <summary>[page /XYZ left top zoom] — position at (left, top) with an explicit zoom.</summary>
    public static PdfArray XYZ(PdfReference page, double? left, double? top, double? zoom) =>
        new(page, new PdfName("XYZ"), Coord(left), Coord(top), Coord(zoom));

    private static PdfObject Coord(double? value) =>
        value is { } v ? Num(v) : PdfNull.Instance;

    private static PdfNumber Num(double value) =>
        value == Math.Floor(value) && !double.IsInfinity(value)
            ? new PdfNumber((long)value)
            : new PdfNumber(value);
}
