using CSharpPdf.Content;
using CSharpPdf.Fluent;
using PdfSpec.Fonts;

using PdfSpec.Geometry;
namespace CSharpPdf.Layout;

/// <summary>
/// A custom <see cref="Element"/> that draws a titled frame and then renders an
/// inner content block inside it. The content can be supplied as either an
/// <see cref="IComponent"/> (typed, reusable) or an inline
/// <c>Action&lt;Container&gt;</c> (one-shot fluent block) — exercising both of
/// <see cref="PdfCanvas.Draw(double, double, IComponent)"/> and
/// <see cref="PdfCanvas.Draw(double, double, System.Action{Container})"/>.
///
/// Demonstrates the "Element renders a Component" direction.
/// </summary>
public sealed class FramedSection : Element
{
    public string Title { get; set; } = "";
    public Color FrameColor { get; set; } = Colors.DarkBlue;
    public double FrameHeight { get; set; } = 200;

    /// <summary>Inner content as a reusable component.</summary>
    public IComponent? Content { get; set; }

    /// <summary>Or — inner content as an ad-hoc fluent builder.</summary>
    public System.Action<Container>? Build { get; set; }

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        var rec = new SizeRect(available.Width, FrameHeight);
        return new SpaceDimension(
            new SizeRect(160, FrameHeight),
            rec,
            verticalBreakable: false);
    }

    protected override RenderResult RenderCore(PdfCanvas canvas, Size available)
    {
        Point start = canvas.Cursor;
        double w = available.Width;
        double h = FrameHeight;
        const double titleBand = 22;
        const double pad = 8;
        const double corner = 4;

        // Title band (filled) + outer frame stroke.
        canvas.FillRoundedRectangle(start.X, start.Y, w, titleBand + corner, FrameColor, corner);
        canvas.FillRectangle(start.X, start.Y - titleBand, w, corner, FrameColor);
        canvas.StrokeRoundedRectangle(start.X, start.Y, w, h, FrameColor, 1.25, corner);

        var titleFont = StandardFont.HelveticaBold;
        var tm = titleFont.GetVerticalMetrics(12);
        canvas.DrawText(titleFont, 12, start.X + 10, start.Y - 5 - tm.Ascent, Title, Colors.White);

        // Inner content: render with explicit width/height so the call works
        // whether or not the caller's canvas has its Width set (top-level
        // engine renders use the root canvas where Width is 0).
        double innerX = start.X + pad;
        double innerTopY = start.Y - titleBand - pad;
        double innerW = w - 2 * pad;
        double innerH = h - titleBand - 2 * pad;
        if (Content is { } component)
        {
            canvas.Draw(innerX, innerTopY, innerW, innerH, component);
        }
        else if (Build is { } builder)
        {
            canvas.Draw(innerX, innerTopY, innerW, innerH, builder);
        }

        return new RenderResult(null, new Point(start.X, start.Y - h));
    }
}
