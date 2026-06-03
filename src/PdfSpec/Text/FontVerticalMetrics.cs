namespace PdfSpec.Text;

/// <summary>
/// A font's vertical metrics for a given size, in points (ISO 32000-1 §9.2.4 /
/// the AFM font dimensions). All distances are measured from the baseline, which
/// is the text origin (y = 0). Ascent and Descent are positive magnitudes above
/// and below the baseline; LineHeight and BaseLine are derived.
/// </summary>
public readonly record struct FontVerticalMetrics(
    double Ascent,
    double Descent,
    double LineGap,
    double CapHeight,
    double XHeight)
{
    public double LineHeight => Ascent + Descent + LineGap;
    public double BaseLine => Ascent + LineGap / 2.0;
}
