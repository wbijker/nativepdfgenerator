using PdfSpec.Objects;

namespace PdfSpec.Navigation;

/// <summary>
/// Builds explicit destination arrays (ISO 32000-1 §12.3.2.2, "Explicit
/// Destinations"). A null coordinate means "leave this value unchanged when
/// the destination is invoked".
/// </summary>
public static class PdfDestination
{
    /// <summary>[page /Fit] — fit the whole page in the window.</summary>
    public static PdfArray Fit(PdfReference page) => new(page, new PdfName("Fit"));

    /// <summary>[page /FitH top].</summary>
    public static PdfArray FitH(PdfReference page, double top) =>
        new(page, new PdfName("FitH"), Num(top));

    /// <summary>[page /FitV left].</summary>
    public static PdfArray FitV(PdfReference page, double left) =>
        new(page, new PdfName("FitV"), Num(left));

    /// <summary>[page /XYZ left top zoom].</summary>
    public static PdfArray XYZ(PdfReference page, double? left, double? top, double? zoom) =>
        new(page, new PdfName("XYZ"), Coord(left), Coord(top), Coord(zoom));

    private static PdfObject Coord(double? value) =>
        value is { } v ? Num(v) : PdfNull.Instance;

    private static PdfNumber Num(double value) =>
        value == Math.Floor(value) && !double.IsInfinity(value)
            ? new PdfNumber((long)value)
            : new PdfNumber(value);
}
