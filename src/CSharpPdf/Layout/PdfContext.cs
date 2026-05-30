using CSharpPdf.Objects;
using Font = CSharpPdf.Text.Font;

namespace CSharpPdf.Layout;

/// <summary>
/// The low-level drawing surface handed to every component. It tracks the cursor
/// (the top-left position where the next content should be drawn) and exposes all
/// the PDF operations a component needs — text, rectangles (fill/stroke), and
/// images — so components never reach into the page or content stream directly.
/// Coordinates are PDF user space (y increases upward); "top" parameters are the
/// upper edge of a box.
/// </summary>
public sealed class PdfContext
{
    private int _imageSequence;

    internal PdfContext(PdfDocument document) => Document = document;

    public PdfDocument Document { get; }

    /// <summary>
    /// Outline entries collected during rendering (title + destination). The engine
    /// flushes these into <see cref="PdfDocument.SetOutline"/> at <c>Finish()</c>.
    /// </summary>
    internal List<(string Title, Objects.PdfArray Destination)> PendingBookmarks { get; } = new();

    /// <summary>The page currently being drawn into.</summary>
    public PdfPage Page { get; internal set; } = null!;

    /// <summary>1-based number of the current page.</summary>
    public int PageNumber { get; internal set; }

    /// <summary>Total page count, populated in the render phase of a two-phase save (0 in measure phase).</summary>
    public int TotalPages { get; internal set; }

    /// <summary>
    /// The current rendering phase. In <see cref="RenderMode.Measure"/> the drawing
    /// primitives are no-ops so the engine just paginates; <see cref="TotalPages"/>
    /// becomes valid in <see cref="RenderMode.Render"/>.
    /// </summary>
    public RenderMode Mode { get; internal set; } = RenderMode.Render;

    /// <summary>The top-left position where the next content should be drawn.</summary>
    public Point Cursor { get; set; }

    /// <summary>Draw a single line of text with its baseline at <paramref name="baselineY"/>.</summary>
    public void DrawText(Font font, double size, double x, double baselineY, string text, Color color)
    {
        if (Mode == RenderMode.Measure) return;
        Page.Content.Save().SetRgbFill(color.R, color.G, color.B);
        Page.DrawText(font, size, x, baselineY, text);
        Page.Content.Restore();
    }

    /// <summary>Fill a rectangle whose upper-left corner is (x, top).</summary>
    public void FillRectangle(double x, double top, double width, double height, Color color)
    {
        if (Mode == RenderMode.Measure) return;
        if (width <= 0 || height <= 0)
        {
            return;
        }
        Page.Content.Save().SetRgbFill(color.R, color.G, color.B)
            .Rectangle(x, top - height, width, height).Fill().Restore();
    }

    /// <summary>Stroke the outline of a rectangle whose upper-left corner is (x, top).</summary>
    public void StrokeRectangle(double x, double top, double width, double height, Color color, double lineWidth)
    {
        if (Mode == RenderMode.Measure) return;
        if (width <= 0 || height <= 0 || lineWidth <= 0)
        {
            return;
        }
        double half = lineWidth / 2;
        Page.Content.Save().SetRgbStroke(color.R, color.G, color.B).SetLineWidth(lineWidth)
            .Rectangle(x + half, top - height + half, width - lineWidth, height - lineWidth).Stroke().Restore();
    }

    /// <summary>Draw an image XObject into the box whose upper-left corner is (x, top).</summary>
    public void DrawImage(PdfReference image, double x, double top, double width, double height)
    {
        if (Mode == RenderMode.Measure) return;
        string name = $"LayImg{++_imageSequence}";
        Page.AddXObject(name, image);
        Page.Content.DrawImage(name, x, top - height, width, height);
    }

    /// <summary>Fill a rectangle with rounded corners. <paramref name="radius"/> is clamped to half the smaller side.</summary>
    public void FillRoundedRectangle(double x, double top, double width, double height, Color color, double radius)
    {
        if (Mode == RenderMode.Measure) return;
        if (width <= 0 || height <= 0) return;
        if (radius <= 0) { FillRectangle(x, top, width, height, color); return; }
        TraceRoundedRect(Page.Content.Save().SetRgbFill(color.R, color.G, color.B), x, top, width, height, radius)
            .Fill().Restore();
    }

    /// <summary>
    /// Stroke a rectangle outline with rounded corners and an optional dash pattern
    /// (lengths in points; null = solid).
    /// </summary>
    public void StrokeRoundedRectangle(double x, double top, double width, double height,
        Color color, double lineWidth, double radius, double[]? dash = null)
    {
        if (Mode == RenderMode.Measure) return;
        if (width <= 0 || height <= 0 || lineWidth <= 0) return;
        double half = lineWidth / 2;
        var cs = Page.Content.Save().SetRgbStroke(color.R, color.G, color.B).SetLineWidth(lineWidth);
        if (dash is { Length: > 0 }) cs.SetDash(dash);
        if (radius <= 0)
        {
            cs.Rectangle(x + half, top - height + half, width - lineWidth, height - lineWidth).Stroke().Restore();
        }
        else
        {
            TraceRoundedRect(cs, x + half, top - half, width - lineWidth, height - lineWidth, System.Math.Max(0, radius - half))
                .Stroke().Restore();
        }
    }

    // Traces a rounded rect on the content stream. (x, top) is the upper-left in PDF
    // coords; width/height are the outer extents; r is the corner radius (clamped).
    private static Content.ContentStream TraceRoundedRect(Content.ContentStream cs,
        double x, double top, double width, double height, double r)
    {
        r = System.Math.Min(r, System.Math.Min(width, height) / 2);
        const double K = 0.5522847498; // bezier ⇄ quarter-circle constant
        double c = r * K;
        double bottom = top - height;
        double right = x + width;
        // start at top edge after the top-left corner
        cs.MoveTo(x + r, top)
          .LineTo(right - r, top)
          .CurveTo(right - r + c, top, right, top - r + c, right, top - r)
          .LineTo(right, bottom + r)
          .CurveTo(right, bottom + r - c, right - r + c, bottom, right - r, bottom)
          .LineTo(x + r, bottom)
          .CurveTo(x + r - c, bottom, x, bottom + r - c, x, bottom + r)
          .LineTo(x, top - r)
          .CurveTo(x, top - r + c, x + r - c, top, x + r, top)
          .ClosePath();
        return cs;
    }
}
