using CSharpPdf.Content;
namespace CSharpPdf.Layout;

/// <summary>
/// A simple template component to copy when building your own UIElement.
/// Renders a rounded "card" with a title (bold) and a body line, stacked from
/// the top with a small inset. Demonstrates the two override points: the
/// sizing query (<see cref="SpaceHint"/>) and the draw
/// (<see cref="UIElement.RenderCore"/>).
/// </summary>
public sealed class TestComponent : UIElement
{
    public string Title { get; set; } = "TestComponent";
    public string Body { get; set; } = "Hello from a custom UIElement.";
    public Color Accent { get; set; } = Colors.DarkBlue;
    public Color Surface { get; set; } = Colors.PaleYellow;

    /// <summary>Card height in points. Width fills the parent's allocation.</summary>
    public double Height { get; set; } = 60;

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        // Width: floor 140 pt, prefers 360 pt or the available offer if narrower.
        // Height: fixed; the card is atomic and cannot break across pages.
        double recWidth = System.Math.Min(360, available.Width);
        return new SpaceDimension(
            new SizeRect(140, Height),
            new SizeRect(recWidth, Height),
            verticalBreakable: false);
    }

    protected override RenderResult RenderCore(PdfCanvas context, Size available)
    {
        Point start = context.Cursor;
        double width = available.Width;
        double height = Height;

        // Card body: rounded background + stroke. Use the engine's primitives so
        // the two-phase render handles measure mode automatically.
        context.FillRoundedRectangle(start.X, start.Y, width, height, Surface, 6);
        context.StrokeRoundedRectangle(start.X, start.Y, width, height, Accent, 1, 6);

        // Stack the two text lines from the top using font metrics, so different
        // font sizes still line up neatly.
        var titleFont = CSharpPdf.Text.Standard14Font.HelveticaBold;
        var bodyFont = CSharpPdf.Text.Standard14Font.Helvetica;
        var titleMetrics = titleFont.GetVerticalMetrics(14);
        var bodyMetrics = bodyFont.GetVerticalMetrics(11);

        const double padding = 10;
        double textX = start.X + padding;
        double y = start.Y - padding - titleMetrics.Ascent;
        context.DrawText(titleFont, 14, textX, y, Title, Accent);

        y -= (titleMetrics.LineHeight - titleMetrics.Ascent) + 4 + bodyMetrics.Ascent;
        context.DrawText(bodyFont, 11, textX, y, Body, Colors.Black);

        return new RenderResult(null, new Point(start.X, start.Y - height));
    }
}
