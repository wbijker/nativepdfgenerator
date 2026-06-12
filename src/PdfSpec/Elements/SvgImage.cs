using PdfSpec.Content;
using PdfSpec.Layout;
using PdfSpec.Svg;

namespace PdfSpec.Elements;

/// <summary>
/// An <see cref="Element"/> that paints an SVG drawing into the slot
/// it's installed in. The SVG is parsed once on construction and held
/// as an internal tree; each render walks it and emits the equivalent
/// path / paint operators into the destination
/// <see cref="ContentStream"/>.
///
/// <para>
/// <b>Sizing.</b> If both <see cref="Width"/> and <see cref="Height"/>
/// are unset, the SVG's intrinsic <c>width</c>/<c>height</c> (or
/// <c>viewBox</c> dims as a fallback) drive the requested size and the
/// element is naturally that big. Setting either constrains the box to
/// that explicit dimension; the SVG is scaled uniformly to fit
/// (<c>preserveAspectRatio</c> = <c>xMidYMid meet</c>) and centred in
/// the resulting rect.
/// </para>
///
/// <para>
/// <b>Coverage.</b> Drawable shapes: <c>rect</c> (incl. rx/ry rounded
/// corners), <c>circle</c>, <c>ellipse</c>, <c>line</c>,
/// <c>polyline</c>, <c>polygon</c>, <c>path</c> (all commands incl.
/// arcs). Paint: <c>fill</c>, <c>stroke</c>, <c>stroke-width</c>,
/// <c>opacity</c>, <c>fill-opacity</c>, <c>stroke-opacity</c>;
/// transforms: <c>translate / scale / rotate / skewX / skewY / matrix</c>;
/// colours: <c>#RGB / #RRGGBB / rgb(...) / none</c> + a small CSS-named
/// subset. <i>Out of scope:</i> <c>text</c>, <c>image</c>, gradients,
/// patterns, filters, <c>&lt;style&gt;</c> blocks, embedded CSS.
/// </para>
/// </summary>
public sealed class SvgImage : Element
{
    private readonly SvgDocument _document;

    /// <summary>Explicit width override in points. <c>null</c> = use the SVG's intrinsic width.</summary>
    public double? Width { get; set; }

    /// <summary>Explicit height override in points. <c>null</c> = use the SVG's intrinsic height.</summary>
    public double? Height { get; set; }

    /// <summary>The SVG's natural width as declared on the root (or the viewBox width as fallback). Read-only — change <see cref="Width"/> to scale.</summary>
    public double IntrinsicWidth => _document.IntrinsicWidth;

    /// <summary>The SVG's natural height as declared on the root.</summary>
    public double IntrinsicHeight => _document.IntrinsicHeight;

    internal SvgImage(SvgDocument document) { _document = document; }

    /// <summary>Parse an SVG document from a string.</summary>
    public static SvgImage Parse(string svg) => new(SvgParser.Parse(svg));

    /// <summary>Parse an SVG document from a file on disk.</summary>
    public static SvgImage FromFile(string path) => Parse(File.ReadAllText(path));

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        double w = Math.Min(Width  ?? _document.IntrinsicWidth,  available.Width);
        double h = Math.Min(Height ?? _document.IntrinsicHeight, available.Height);
        if (w <= 0) w = available.Width;
        if (h <= 0) h = available.Height;
        return new PdfSizeHint(0, 0, w, h);
    }

    protected override RenderResult RenderCore(ContentStream cs, PdfSize available)
    {
        double w = Math.Min(Width  ?? _document.IntrinsicWidth,  available.Width);
        double h = Math.Min(Height ?? _document.IntrinsicHeight, available.Height);
        if (w <= 0) w = available.Width;
        if (h <= 0) h = available.Height;

        SvgRenderer.Render(cs, _document, w, h);
        return RenderResult.Done(h);
    }
}
