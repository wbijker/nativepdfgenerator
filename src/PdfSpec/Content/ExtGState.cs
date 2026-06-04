using PdfSpec.Objects;

namespace PdfSpec.Content;

/// <summary>
/// An ExtGState parameter dictionary (ISO 32000-1 §8.4.5) — a bundle of
/// graphic-state parameters that <c>gs</c> applies at once. State is held
/// directly in the dictionary; the same instance can be passed to
/// <see cref="PdfPage.UseExtGState"/> as many times as needed (dedup is by
/// reference identity).
/// </summary>
public sealed class ExtGState
{
    internal PdfDictionary Dictionary { get; } = new();

    public ExtGState()
    {
        Dictionary.Add("Type", new PdfName("ExtGState"));
    }

    /// <summary>ca — non-stroking (fill) alpha, 0..1.</summary>
    public double? FillOpacity
    {
        set => Dictionary.Set("ca", value is null ? null : new PdfNumber(value.Value));
    }

    /// <summary>CA — stroking alpha, 0..1.</summary>
    public double? StrokeOpacity
    {
        set => Dictionary.Set("CA", value is null ? null : new PdfNumber(value.Value));
    }

    /// <summary>BM — blend mode name (Normal, Multiply, Screen, …).</summary>
    public string? BlendMode
    {
        set => Dictionary.Set("BM", value is null ? null : new PdfName(value));
    }

    /// <summary>LW — line width.</summary>
    public double? LineWidth
    {
        set => Dictionary.Set("LW", value is null ? null : new PdfNumber(value.Value));
    }

    /// <summary>OP — stroking overprint.</summary>
    public bool? StrokeOverprint
    {
        set => Dictionary.Set("OP", value is null ? null : new PdfBoolean(value.Value));
    }

    /// <summary>op — non-stroking overprint.</summary>
    public bool? FillOverprint
    {
        set => Dictionary.Set("op", value is null ? null : new PdfBoolean(value.Value));
    }

    /// <summary>Construct an ExtGState with only a fill-alpha entry.</summary>
    public static ExtGState ForFillOpacity(double alpha) => new() { FillOpacity = alpha };

    /// <summary>Construct an ExtGState with only a stroke-alpha entry.</summary>
    public static ExtGState ForStrokeOpacity(double alpha) => new() { StrokeOpacity = alpha };

    /// <summary>Construct an ExtGState with only a blend-mode entry.</summary>
    public static ExtGState ForBlendMode(string mode) => new() { BlendMode = mode };
}
