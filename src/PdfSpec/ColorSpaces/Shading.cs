using PdfSpec.Objects;

namespace PdfSpec.ColorSpaces;

/// <summary>
/// Builds shading dictionaries (ISO 32000-1 §8.7.4.5) — smooth colour
/// gradients. Axial (type 2) blends along a line; radial (type 3) blends
/// between two circles.
/// </summary>
public static class Shading
{
    public static PdfDictionary Axial(PdfObject colorSpace,
        double x0, double y0, double x1, double y1,
        PdfObject function, bool extendStart = true, bool extendEnd = true)
    {
        var d = new PdfDictionary();
        d.SetInteger("ShadingType", 2);
        d.Add("ColorSpace", colorSpace);
        d.Add("Coords", Array(x0, y0, x1, y1));
        d.Add("Function", function);
        d.Add("Extend", new PdfArray(new PdfBoolean(extendStart), new PdfBoolean(extendEnd)));
        return d;
    }

    public static PdfDictionary Radial(PdfObject colorSpace,
        double x0, double y0, double r0, double x1, double y1, double r1,
        PdfObject function, bool extendStart = true, bool extendEnd = true)
    {
        var d = new PdfDictionary();
        d.SetInteger("ShadingType", 3);
        d.Add("ColorSpace", colorSpace);
        d.Add("Coords", Array(x0, y0, r0, x1, y1, r1));
        d.Add("Function", function);
        d.Add("Extend", new PdfArray(new PdfBoolean(extendStart), new PdfBoolean(extendEnd)));
        return d;
    }

    public static PdfDictionary Pattern(PdfObject shading)
    {
        var d = new PdfDictionary();
        d.SetName("Type", "Pattern");
        d.SetInteger("PatternType", 2);
        d.Add("Shading", shading);
        return d;
    }

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
