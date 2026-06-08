namespace PdfSpec.Geometry;

/// <summary>
/// Shared math helpers and tolerances for layout/measurement code.
/// </summary>
public static class PdfMath
{
    /// <summary>
    /// Tolerance for comparing widths and heights in user-space units. Two
    /// independent paths that "should" produce the same width often differ
    /// by a few ulps because they sum glyph widths (each scaled from 1000-
    /// unit glyph space) in different orders. <c>1e-6</c> is well below the
    /// finest meaningful PDF coordinate and well above the accumulated
    /// rounding error of summing dozens of glyph contributions.
    /// </summary>
    public const double Epsilon = 1e-6;

    /// <summary>Equality within <see cref="Epsilon"/>.</summary>
    public static bool ApproximatelyEqual(double a, double b) => Math.Abs(a - b) <= Epsilon;

    /// <summary>true if <paramref name="value"/> ≤ <paramref name="limit"/> within <see cref="Epsilon"/>.</summary>
    public static bool ApproximatelyLessOrEqual(double value, double limit) => value - limit <= Epsilon;
}
