using Font = CSharpPdf.Text.Font;
using Standard14Font = CSharpPdf.Text.Standard14Font;

namespace CSharpPdf.Layout;

/// <summary>
/// The base of every layout construct. It carries the styling common to all UI
/// elements (background, border, padding, horizontal/vertical alignment), applies
/// that styling centrally in <see cref="Render"/>, and exposes static helpers to
/// build the concrete elements (Text, Rows, Cols, Image, Unconstrained).
/// Subclasses implement <see cref="MeasureCore"/> and <see cref="RenderCore"/>.
/// </summary>
public abstract class UIElement
{
    internal Color? BackgroundFill;
    internal Color? BorderStroke;
    internal double BorderThickness;
    internal double PaddingAmount;
    internal HorizontalAlignment HAlign = HorizontalAlignment.Left;
    internal VerticalAlignment VAlign = VerticalAlignment.Top;
    internal bool ExtendWidth;

    /// <summary>The smallest space the element can render in (its floor).</summary>
    public abstract Size MinimalSpaceRequired { get; }

    /// <summary>The natural size given unlimited room (the auto-grow target).</summary>
    public abstract Size PreferredSize { get; }

    /// <summary>The minimum height needed to render *something* here (for break decisions).</summary>
    internal virtual double MinRenderHeight(Size available) => MinimalSpaceRequired.Height;

    /// <summary>The concrete size this element occupies for the given available space (incl. padding/border).</summary>
    public virtual Size Measure(Size available)
    {
        double inset = PaddingAmount + BorderThickness;
        var inner = MeasureCore(new Size(Max0(available.Width - 2 * inset), Max0(available.Height - 2 * inset)));
        double width = ExtendWidth ? available.Width : inner.Width + 2 * inset;
        return new Size(width, inner.Height + 2 * inset);
    }

    /// <summary>
    /// Draw the element at the context cursor: apply alignment, fill the background,
    /// stroke the border, inset by padding/border, render the content, and return
    /// the overflow (re-styled so a continuation keeps its look) plus the next
    /// position. If even the minimum cannot fit, defers untouched to the next page.
    /// </summary>
    /// <summary>
    /// Sub-point tolerance for the "does it fit" comparisons (defer / break / wrap).
    /// At PDF point precision (sub-pixel) this is invisible, but it absorbs the
    /// IEEE-754 noise that accumulates when a measured size is added to and then
    /// subtracted from a container's padding/border — without it a slightly larger
    /// minimum would defer forever on the same available space.
    /// </summary>
    internal const double FitTolerance = 1e-6;

    public virtual RenderResult Render(PdfContext context, Size available)
    {
        double inset = PaddingAmount + BorderThickness;
        var innerAvailable = new Size(Max0(available.Width - 2 * inset), Max0(available.Height - 2 * inset));
        if (innerAvailable.Height + FitTolerance < MinRenderHeight(innerAvailable))
        {
            return new RenderResult(this, context.Cursor); // can't start here — defer, drawing nothing
        }

        var measured = Measure(available);
        double contentWidth = ExtendWidth ? available.Width : System.Math.Min(measured.Width, available.Width);
        Point box = context.Cursor;
        double offsetX = HAlign switch
        {
            HorizontalAlignment.Center => (available.Width - contentWidth) / 2,
            HorizontalAlignment.Right => available.Width - contentWidth,
            _ => 0,
        };
        double drawX = box.X + Max0(offsetX);
        double boxHeight = System.Math.Min(measured.Height, available.Height);

        if (BackgroundFill is { } bg)
        {
            context.FillRectangle(drawX, box.Y, contentWidth, boxHeight, bg);
        }
        if (BorderStroke is { } border && BorderThickness > 0)
        {
            context.StrokeRectangle(drawX, box.Y, contentWidth, boxHeight, border, BorderThickness);
        }

        context.Cursor = new Point(drawX + inset, box.Y - inset);
        var result = RenderCore(context, new Size(Max0(contentWidth - 2 * inset), innerAvailable.Height));

        var next = new Point(box.X, result.Next.Y - inset);
        context.Cursor = next;
        if (result.Overflow is { } overflow)
        {
            CopyStyleTo(overflow);
        }
        return new RenderResult(result.Overflow, next);
    }

    protected abstract Size MeasureCore(Size available);
    protected abstract RenderResult RenderCore(PdfContext context, Size available);

    internal void CopyStyleTo(UIElement other)
    {
        other.BackgroundFill = BackgroundFill;
        other.BorderStroke = BorderStroke;
        other.BorderThickness = BorderThickness;
        other.PaddingAmount = PaddingAmount;
        other.HAlign = HAlign;
        other.VAlign = VAlign;
        other.ExtendWidth = ExtendWidth;
    }

    private protected static double Max0(double v) => v < 0 ? 0 : v;

    // ----- static construction helpers -----

    public static TextElement Text(string text) => new(text, Standard14Font.Helvetica, 12);
    public static TextElement Text(string text, Font font, double size) => new(text, font, size);
    /// <summary>Build a vertical stack with explicit slot sizing (Fixed/Auto/Relative).</summary>
    public static RowsElement Rows(System.Action<RowsBuilder> build)
    {
        var builder = new RowsBuilder();
        build(builder);
        return new RowsElement(builder.Slots);
    }
    /// <summary>Build a vertical stack from children (each becomes an Auto slot).</summary>
    public static RowsElement Rows(params UIElement[] children) => new RowsElement().Children(children);

    /// <summary>Build a horizontal stack with explicit slot sizing (Fixed/Auto/Relative).</summary>
    public static ColsElement Cols(System.Action<ColsBuilder> build)
    {
        var builder = new ColsBuilder();
        build(builder);
        return new ColsElement(builder.Slots);
    }
    /// <summary>Build a horizontal stack from children (each becomes an Auto slot).</summary>
    public static ColsElement Cols(params UIElement[] children) => new ColsElement().Children(children);
    public static ImageElement Image(byte[] rgb, int pixelWidth, int pixelHeight, double width, double height) =>
        new(rgb, pixelWidth, pixelHeight, width, height);
    public static UnconstrainedElement Unconstrained(UIElement child) => new(child);
    public static TableElement Table() => new();
    public static PageNumberElement PageNumber() => new(Standard14Font.Helvetica, 10);
    public static PageNumberElement PageNumber(Font font, double size) => new(font, size);
}

/// <summary>
/// Adds fluent, chainable styling that returns the concrete type, so
/// <c>UIElement.Rows(..).Background(..).Border(..).AlignCenter()</c> keeps working.
/// </summary>
public abstract class UIElement<TSelf> : UIElement
    where TSelf : UIElement<TSelf>
{
    public TSelf Background(Color color) { BackgroundFill = color; return (TSelf)this; }
    public TSelf Border(Color color, double thickness = 1) { BorderStroke = color; BorderThickness = thickness; return (TSelf)this; }
    public TSelf Padding(double padding) { PaddingAmount = padding; return (TSelf)this; }

    public TSelf AlignLeft() { HAlign = HorizontalAlignment.Left; return (TSelf)this; }
    public TSelf AlignCenter() { HAlign = HorizontalAlignment.Center; return (TSelf)this; }
    public TSelf AlignRight() { HAlign = HorizontalAlignment.Right; return (TSelf)this; }

    public TSelf AlignTop() { VAlign = VerticalAlignment.Top; return (TSelf)this; }
    public TSelf AlignMiddle() { VAlign = VerticalAlignment.Middle; return (TSelf)this; }
    public TSelf AlignBottom() { VAlign = VerticalAlignment.Bottom; return (TSelf)this; }

    /// <summary>Take the full available width (so a background/border fills it).</summary>
    public TSelf ExtendHorizontal() { ExtendWidth = true; return (TSelf)this; }
}
