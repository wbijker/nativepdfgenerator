using CSharpPdf.Objects;

namespace CSharpPdf.ColorSpaces;

/// <summary>
/// Builds PDF function dictionaries (ISO 32000-1 §7.10). Functions map input
/// values to output values and are used by shadings (and elsewhere). This covers
/// type 2 (exponential interpolation) and type 3 (stitching) functions.
/// </summary>
public static class PdfFunction
{
    /// <summary>
    /// A type 2 exponential interpolation function over domain [0,1]:
    /// y = C0 + x^N * (C1 - C0). With N = 1 this is a linear blend from
    /// <paramref name="c0"/> (x=0) to <paramref name="c1"/> (x=1).
    /// </summary>
    public static PdfDictionary Exponential(double[] c0, double[] c1, double n = 1.0) => new()
    {
        ["FunctionType"] = new PdfNumber(2),
        ["Domain"] = Array(0, 1),
        ["C0"] = Array(c0),
        ["C1"] = Array(c1),
        ["N"] = new PdfNumber(n),
    };

    /// <summary>
    /// A type 3 stitching function over domain [0,1]: it splices several
    /// 1-in functions end to end. <paramref name="bounds"/> has k-1 split points
    /// for k subfunctions; <paramref name="encode"/> has 2k values remapping each
    /// subdomain (commonly [0 1 0 1 ...]).
    /// </summary>
    public static PdfDictionary Stitching(PdfObject[] functions, double[] bounds, double[] encode)
    {
        var fns = new PdfArray();
        foreach (var f in functions)
        {
            fns.Add(f);
        }
        return new PdfDictionary
        {
            ["FunctionType"] = new PdfNumber(3),
            ["Domain"] = Array(0, 1),
            ["Functions"] = fns,
            ["Bounds"] = Array(bounds),
            ["Encode"] = Array(encode),
        };
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
