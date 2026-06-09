namespace PdfSpec.Geometry;

/// <summary>
/// Conversion helpers between <see cref="Unit"/> and PDF points — the
/// native user-space unit (1/72 inch). One call site for the conversion
/// constants so a tweak (e.g. a non-default DPI) propagates everywhere
/// at once.
/// </summary>
public static class Units
{
    /// <summary>
    /// DPI used when converting <see cref="Unit.Px"/> to points. Defaults
    /// to 96, the de-facto CSS reference. Settable so callers targeting
    /// e.g. print-resolution screens can override globally.
    /// </summary>
    public static double DefaultDpi { get; set; } = 96.0;

    /// <summary>
    /// Convert <paramref name="value"/> in <paramref name="unit"/> to PDF
    /// points. <paramref name="available"/> is the parent extent percentage
    /// lengths resolve against (ignored for non-percent units).
    /// </summary>
    public static double ToPoints(double value, Unit unit, double available = 0) => unit switch
    {
        Unit.Pt => value,
        Unit.Px => value * 72.0 / DefaultDpi,
        Unit.Mm => value * 72.0 / 25.4,
        Unit.Cm => value * 72.0 / 2.54,
        Unit.Inch => value * 72.0,
        Unit.Percent => available * value / 100.0,
        _ => value,
    };
}
