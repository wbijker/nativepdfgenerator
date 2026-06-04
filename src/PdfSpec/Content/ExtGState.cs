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
    public PdfDictionary Dictionary { get; } = new();

    public ExtGState()
    {
        Dictionary.SetName("Type", "ExtGState");
    }

    /// <summary>ca — non-stroking (fill) alpha, 0..1.</summary>
    public double? FillOpacity { set => Dictionary.SetNumber("ca", value); }

    /// <summary>CA — stroking alpha, 0..1.</summary>
    public double? StrokeOpacity { set => Dictionary.SetNumber("CA", value); }

    /// <summary>BM — blend mode (Normal, Multiply, Screen, …).</summary>
    public BlendMode? BlendMode { set => Dictionary.SetName("BM", value?.ToString()); }

    /// <summary>LW — line width.</summary>
    public double? LineWidth { set => Dictionary.SetNumber("LW", value); }

    /// <summary>OP — stroking overprint.</summary>
    public bool? StrokeOverprint { set => Dictionary.SetBoolean("OP", value); }

    /// <summary>op — non-stroking overprint.</summary>
    public bool? FillOverprint { set => Dictionary.SetBoolean("op", value); }

    /// <summary>Construct an ExtGState with only a fill-alpha entry.</summary>
    public static ExtGState ForFillOpacity(double alpha) => new() { FillOpacity = alpha };

    /// <summary>Construct an ExtGState with only a stroke-alpha entry.</summary>
    public static ExtGState ForStrokeOpacity(double alpha) => new() { StrokeOpacity = alpha };

    /// <summary>Construct an ExtGState with only a blend-mode entry.</summary>
    public static ExtGState ForBlendMode(BlendMode mode) => new() { BlendMode = mode };
}
