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

    /// <summary>The page currently being drawn into.</summary>
    public PdfPage Page { get; internal set; } = null!;

    /// <summary>1-based number of the current page.</summary>
    public int PageNumber { get; internal set; }

    /// <summary>The top-left position where the next content should be drawn.</summary>
    public Point Cursor { get; set; }

    /// <summary>Draw a single line of text with its baseline at <paramref name="baselineY"/>.</summary>
    public void DrawText(Font font, double size, double x, double baselineY, string text, Color color)
    {
        Page.Content.Save().SetRgbFill(color.R, color.G, color.B);
        Page.DrawText(font, size, x, baselineY, text);
        Page.Content.Restore();
    }

    /// <summary>Fill a rectangle whose upper-left corner is (x, top).</summary>
    public void FillRectangle(double x, double top, double width, double height, Color color)
    {
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
        string name = $"LayImg{++_imageSequence}";
        Page.AddXObject(name, image);
        Page.Content.DrawImage(name, x, top - height, width, height);
    }
}
