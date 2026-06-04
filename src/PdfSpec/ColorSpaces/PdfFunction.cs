using PdfSpec.Objects;

namespace PdfSpec.ColorSpaces;

/// <summary>
/// Builds PDF function dictionaries (ISO 32000-1 §7.10). Covers type 2
/// (exponential interpolation) and type 3 (stitching) functions.
/// </summary>
public static class PdfFunction
{
    public static PdfDictionary Exponential(double[] c0, double[] c1, double n = 1.0) => new()
    {
        { "FunctionType", new PdfNumber(2) },
        { "Domain", Array(0, 1) },
        { "C0", Array(c0) },
        { "C1", Array(c1) },
        { "N", new PdfNumber(n) },
    };

    public static PdfDictionary Stitching(PdfObject[] functions, double[] bounds, double[] encode)
    {
        var fns = new PdfArray();
        foreach (var f in functions) fns.Add(f);
        return new PdfDictionary
        {
            { "FunctionType", new PdfNumber(3) },
            { "Domain", Array(0, 1) },
            { "Functions", fns },
            { "Bounds", Array(bounds) },
            { "Encode", Array(encode) },
        };
    }

    private static PdfArray Array(params double[] values)
    {
        var array = new PdfArray();
        foreach (double v in values) array.Add(new PdfNumber(v));
        return array;
    }
}
