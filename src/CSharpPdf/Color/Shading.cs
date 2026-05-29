using CSharpPdf.Objects;

namespace CSharpPdf.Color;

/// <summary>
/// Builds shading dictionaries (ISO 32000-1 §8.7.4.5) — smooth colour gradients.
/// Axial (type 2) blends along a line; radial (type 3) blends between two
/// circles. A shading can be painted directly with the <c>sh</c> operator or
/// wrapped in a shading <see cref="Pattern"/> for filling paths and text.
/// </summary>
public static class Shading
{
    /// <summary>
    /// A type 2 (axial/linear) shading along the segment (x0,y0)→(x1,y1).
    /// <paramref name="function"/> maps the parametric t∈[0,1] to a colour in
    /// <paramref name="colorSpace"/> (e.g. /DeviceRGB).
    /// </summary>
    public static PdfDictionary Axial(PdfObject colorSpace,
        double x0, double y0, double x1, double y1,
        PdfObject function, bool extendStart = true, bool extendEnd = true) => new()
    {
        ["ShadingType"] = new PdfNumber(2),
        ["ColorSpace"] = colorSpace,
        ["Coords"] = Array(x0, y0, x1, y1),
        ["Function"] = function,
        ["Extend"] = new PdfArray(new PdfBoolean(extendStart), new PdfBoolean(extendEnd)),
    };

    /// <summary>
    /// A type 3 (radial) shading blending between circle (x0,y0,r0) and
    /// (x1,y1,r1). Useful for spotlights and spheres.
    /// </summary>
    public static PdfDictionary Radial(PdfObject colorSpace,
        double x0, double y0, double r0, double x1, double y1, double r1,
        PdfObject function, bool extendStart = true, bool extendEnd = true) => new()
    {
        ["ShadingType"] = new PdfNumber(3),
        ["ColorSpace"] = colorSpace,
        ["Coords"] = Array(x0, y0, r0, x1, y1, r1),
        ["Function"] = function,
        ["Extend"] = new PdfArray(new PdfBoolean(extendStart), new PdfBoolean(extendEnd)),
    };

    /// <summary>
    /// Wrap a shading in a shading pattern (PatternType 2) so it can be selected
    /// in the Pattern colour space and used to fill paths or text.
    /// </summary>
    public static PdfDictionary Pattern(PdfObject shading) => new()
    {
        ["Type"] = new PdfName("Pattern"),
        ["PatternType"] = new PdfNumber(2),
        ["Shading"] = shading,
    };

    private static PdfArray Array(params double[] values)
    {
        var array = new PdfArray();
        foreach (double v in values)
        {
            array.Add(new PdfNumber(v));
        }
        return array;
    }
}
