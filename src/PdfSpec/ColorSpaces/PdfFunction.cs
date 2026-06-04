using PdfSpec.Objects;

namespace PdfSpec.ColorSpaces;

/// <summary>
/// Builds PDF function dictionaries (ISO 32000-1 §7.10). Covers type 2
/// (exponential interpolation) and type 3 (stitching) functions.
/// </summary>
public static class PdfFunction
{
    public static PdfDictionary Exponential(double[] c0, double[] c1, double n = 1.0)
    {
        var d = new PdfDictionary();
        d.SetInteger("FunctionType", 2);
        d.Add("Domain", Array(0, 1));
        d.Add("C0", Array(c0));
        d.Add("C1", Array(c1));
        d.SetNumber("N", n);
        return d;
    }

    public static PdfDictionary Stitching(PdfObject[] functions, double[] bounds, double[] encode)
    {
        var fns = new PdfArray();
        foreach (var f in functions) fns.Add(f);
        var d = new PdfDictionary();
        d.SetInteger("FunctionType", 3);
        d.Add("Domain", Array(0, 1));
        d.Add("Functions", fns);
        d.Add("Bounds", Array(bounds));
        d.Add("Encode", Array(encode));
        return d;
    }

    private static PdfArray Array(params double[] values)
    {
        var array = new PdfArray();
        foreach (double v in values) array.Add(new PdfNumber(v));
        return array;
    }
}
