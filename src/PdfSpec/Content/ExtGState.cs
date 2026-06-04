using PdfSpec.Objects;

namespace PdfSpec.Content;

/// <summary>
/// An ExtGState parameter dictionary (ISO 32000-1 §8.4.5) — a bundle of
/// graphic-state parameters that <c>gs</c> applies at once. Holds typed
/// fields; the dictionary is built fresh on <see cref="Build"/>.
/// </summary>
public sealed class ExtGState
{
    /// <summary>ca — non-stroking (fill) alpha, 0..1.</summary>
    public double? FillOpacity { get; set; }

    /// <summary>CA — stroking alpha, 0..1.</summary>
    public double? StrokeOpacity { get; set; }

    /// <summary>BM — blend mode name (Normal, Multiply, Screen, …).</summary>
    public string? BlendMode { get; set; }

    /// <summary>LW — line width.</summary>
    public double? LineWidth { get; set; }

    /// <summary>OP — stroking overprint.</summary>
    public bool? StrokeOverprint { get; set; }

    /// <summary>op — non-stroking overprint.</summary>
    public bool? FillOverprint { get; set; }

    public PdfDictionary Build()
    {
        var d = new PdfDictionary
        {
            { "Type", new PdfName("ExtGState") },
        };
        if (FillOpacity is { } ca) d.Add("ca", new PdfNumber(ca));
        if (StrokeOpacity is { } CA) d.Add("CA", new PdfNumber(CA));
        if (BlendMode is not null) d.Add("BM", new PdfName(BlendMode));
        if (LineWidth is { } lw) d.Add("LW", new PdfNumber(lw));
        if (StrokeOverprint is { } so) d.Add("OP", new PdfBoolean(so));
        if (FillOverprint is { } fo) d.Add("op", new PdfBoolean(fo));
        return d;
    }

    /// <summary>Construct an ExtGState with only a fill-alpha entry.</summary>
    public static ExtGState ForFillOpacity(double alpha) => new() { FillOpacity = alpha };

    /// <summary>Construct an ExtGState with only a stroke-alpha entry.</summary>
    public static ExtGState ForStrokeOpacity(double alpha) => new() { StrokeOpacity = alpha };

    /// <summary>Construct an ExtGState with only a blend-mode entry.</summary>
    public static ExtGState ForBlendMode(string mode) => new() { BlendMode = mode };
}
