using PdfSpec.Geometry;

namespace PdfSpec.Svg;

/// <summary>
/// Parsed SVG tree (root group, intrinsic width/height, optional
/// viewBox). The <see cref="SvgRenderer"/> walks this tree once per
/// <see cref="Elements.SvgImage"/> render call — the model itself is
/// renderer-agnostic.
/// </summary>
internal sealed class SvgDocument
{
    public double IntrinsicWidth { get; init; }
    public double IntrinsicHeight { get; init; }

    /// <summary>viewBox = "minX minY width height" when present.</summary>
    public (double X, double Y, double Width, double Height)? ViewBox { get; init; }

    public SvgGroup Root { get; init; } = new();
}

/// <summary>Attributes shared by every node — paint, stroke width, opacity, transform. Null fields mean "inherit from parent".</summary>
internal sealed class SvgAttrs
{
    public SvgPaint? Fill;
    public SvgPaint? Stroke;
    public double? StrokeWidth;
    public double? Opacity;
    public double? FillOpacity;
    public double? StrokeOpacity;
    public SvgMatrix? Transform;
}

/// <summary>Paint spec: either explicitly <see cref="None"/> (no paint) or a colour.</summary>
internal sealed class SvgPaint
{
    public bool IsNone { get; }
    public PdfColor? Color { get; }

    private SvgPaint(bool isNone, PdfColor? color) { IsNone = isNone; Color = color; }

    public static readonly SvgPaint None = new(isNone: true, color: null);
    public static SvgPaint Of(PdfColor c) => new(isNone: false, color: c);
}

internal abstract class SvgNode
{
    public SvgAttrs Attrs { get; set; } = new();
}

internal sealed class SvgGroup : SvgNode
{
    public List<SvgNode> Children { get; } = new();
}

internal sealed class SvgRect : SvgNode
{
    public double X, Y, Width, Height;
    public double Rx, Ry;
}

internal sealed class SvgCircle  : SvgNode { public double Cx, Cy, R; }
internal sealed class SvgEllipse : SvgNode { public double Cx, Cy, Rx, Ry; }
internal sealed class SvgLine    : SvgNode { public double X1, Y1, X2, Y2; }

internal sealed class SvgPolyline : SvgNode
{
    public double[] Points { get; init; } = Array.Empty<double>();
    public bool Closed { get; init; }
}

internal sealed class SvgPath : SvgNode
{
    public string D { get; init; } = string.Empty;
}
