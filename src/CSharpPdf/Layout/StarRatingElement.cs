using CSharpPdf.Content;

namespace CSharpPdf.Layout;

/// <summary>
/// A tiny custom <see cref="Element"/> — five dots in a row, the first
/// <see cref="Filled"/> drawn in <see cref="FilledColor"/> and the rest in
/// <see cref="EmptyColor"/>. Width is intrinsic (computed from <see cref="DotSize"/>
/// and <see cref="Gap"/>); height equals <see cref="DotSize"/>. Atomic — never
/// breaks across pages.
/// </summary>
public sealed class StarRatingElement : Element
{
    public int Filled { get; set; } = 4;
    public int Total { get; set; } = 5;
    public Color FilledColor { get; set; } = Colors.Yellow;
    public Color EmptyColor { get; set; } = Colors.LightGray;
    public double DotSize { get; set; } = 11;
    public double Gap { get; set; } = 3;

    public override SpaceDimension SpaceHint(SizeRect available)
    {
        double width = Total * DotSize + System.Math.Max(0, Total - 1) * Gap;
        var size = new SizeRect(width, DotSize);
        return new SpaceDimension(size, size, verticalBreakable: false);
    }

    protected override RenderResult RenderCore(PdfCanvas canvas, Size available)
    {
        Point start = canvas.Cursor;
        double r = DotSize / 2;
        for (int i = 0; i < Total; i++)
        {
            double x = start.X + i * (DotSize + Gap);
            double topY = start.Y;
            var color = i < Filled ? FilledColor : EmptyColor;
            // A circle approximated as a max-radius rounded square.
            canvas.FillRoundedRectangle(x, topY, DotSize, DotSize, color, r);
        }
        return new RenderResult(null, new Point(start.X, start.Y - DotSize));
    }
}
