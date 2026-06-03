using PdfSpec.Objects;

namespace PdfSpec.Content;

/// <summary>
/// An ExtGState parameter dictionary (ISO 32000-1 §8.4.5) — a bundle of
/// graphic-state parameters that <c>gs</c> applies at once. Cover the
/// transparency / alpha entries (ca, CA, BM) plus a handful of stroke / overprint
/// parameters; extend as needed.
/// </summary>
public sealed class ExtGState
{
    internal PdfDictionary Dictionary { get; } = new();

    public ExtGState()
    {
        Dictionary["Type"] = new PdfName("ExtGState");
    }

    /// <summary>ca — non-stroking (fill) alpha, 0..1.</summary>
    public double? FillOpacity
    {
        set
        {
            if (value is null) Dictionary.Remove("ca");
            else Dictionary["ca"] = new PdfNumber(value.Value);
        }
    }

    /// <summary>CA — stroking alpha, 0..1.</summary>
    public double? StrokeOpacity
    {
        set
        {
            if (value is null) Dictionary.Remove("CA");
            else Dictionary["CA"] = new PdfNumber(value.Value);
        }
    }

    /// <summary>BM — blend mode name (Normal, Multiply, Screen, …).</summary>
    public string? BlendMode
    {
        set
        {
            if (value is null) Dictionary.Remove("BM");
            else Dictionary["BM"] = new PdfName(value);
        }
    }

    /// <summary>LW — line width.</summary>
    public double? LineWidth
    {
        set
        {
            if (value is null) Dictionary.Remove("LW");
            else Dictionary["LW"] = new PdfNumber(value.Value);
        }
    }

    /// <summary>OP / op — stroking / non-stroking overprint.</summary>
    public bool? StrokeOverprint
    {
        set
        {
            if (value is null) Dictionary.Remove("OP");
            else Dictionary["OP"] = new PdfBoolean(value.Value);
        }
    }

    public bool? FillOverprint
    {
        set
        {
            if (value is null) Dictionary.Remove("op");
            else Dictionary["op"] = new PdfBoolean(value.Value);
        }
    }

    /// <summary>Construct an ExtGState with only a fill-alpha entry.</summary>
    public static ExtGState ForFillOpacity(double alpha) => new() { FillOpacity = alpha };

    /// <summary>Construct an ExtGState with only a stroke-alpha entry.</summary>
    public static ExtGState ForStrokeOpacity(double alpha) => new() { StrokeOpacity = alpha };

    /// <summary>Construct an ExtGState with only a blend-mode entry.</summary>
    public static ExtGState ForBlendMode(string mode) => new() { BlendMode = mode };
}
