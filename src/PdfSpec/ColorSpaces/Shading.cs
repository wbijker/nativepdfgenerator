using PdfSpec.Objects;

namespace PdfSpec.ColorSpaces;

/// <summary>
/// Builds shading dictionaries (ISO 32000-1 §8.7.4.5) — smooth colour gradients.
/// Axial (type 2) blends along a line; radial (type 3) blends between two circles.
/// </summary>
public static class Shading
{
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
