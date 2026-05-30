namespace CSharpPdf.Layout;

internal enum SlotSizing { Auto, Fixed, Relative }

/// <summary>
/// A slot inside a Rows/Cols builder: carries its sizing intent (Fixed/Auto/Relative),
/// optional content, and the shared UI styling (background, border, padding). A slot
/// always fills the size its parent allocates (so a coloured background spans the full
/// allocation, not just the content), and is the unit the parent paginates on.
/// </summary>
public sealed class SlotElement : UIElement<SlotElement>
{
    internal SlotSizing Sizing = SlotSizing.Auto;
    internal double SizeValue = 1; // Fixed: size in points; Relative: weight; Auto: unused
    internal Unit SizeUnit = Unit.Px;
    internal UIElement? InnerContent;

    /// <summary>Set the slot's inner content (optional — an empty slot is a coloured band).</summary>
    public SlotElement Content(UIElement child) { InnerContent = child; return this; }

    public override Size MinimalSpaceRequired => InnerContent?.MinimalSpaceRequired ?? Size.Zero;
    public override Size PreferredSize => InnerContent?.PreferredSize ?? Size.Zero;

    internal override double MinRenderHeight(Size available)
    {
        double inset = PaddingAmount + BorderThickness;
        var inner = new Size(Max0(available.Width - 2 * inset), Max0(available.Height - 2 * inset));
        return Sizing switch
        {
            SlotSizing.Fixed => SizeValue,
            SlotSizing.Auto => (InnerContent?.MinRenderHeight(inner) ?? 0) + 2 * inset,
            SlotSizing.Relative => 2 * inset,
            _ => 0,
        };
    }

    /// <summary>
    /// A slot returns its content size including padding/border — used by Cols/Rows
    /// to size Auto slots from their natural content.
    /// </summary>
    public override Size Measure(Size available)
    {
        double inset = PaddingAmount + BorderThickness;
        if (InnerContent is null) return new Size(2 * inset, 2 * inset);
        var inner = InnerContent.Measure(new Size(Max0(available.Width - 2 * inset), Max0(available.Height - 2 * inset)));
        return new Size(inner.Width + 2 * inset, inner.Height + 2 * inset);
    }

    /// <summary>
    /// A slot fills the size its parent gives it (so background and border span the
    /// full allocation), advances by that full size, and reports any inner overflow
    /// as a new continuation slot that keeps the same sizing intent and styling.
    /// </summary>
    public override RenderResult Render(PdfContext context, Size available)
    {
        Point box = context.Cursor;
        if (BackgroundFill is { } bg)
        {
            context.FillRectangle(box.X, box.Y, available.Width, available.Height, bg);
        }
        if (BorderStroke is { } border && BorderThickness > 0)
        {
            context.StrokeRectangle(box.X, box.Y, available.Width, available.Height, border, BorderThickness);
        }

        var next = new Point(box.X, box.Y - available.Height);
        if (InnerContent is null)
        {
            context.Cursor = next;
            return new RenderResult(null, next);
        }

        double inset = PaddingAmount + BorderThickness;
        var inner = new Size(Max0(available.Width - 2 * inset), Max0(available.Height - 2 * inset));
        context.Cursor = new Point(box.X + inset, box.Y - inset);
        var result = InnerContent.Render(context, inner);
        context.Cursor = next;

        if (result.Overflow is { } overflow)
        {
            var rest = new SlotElement
            {
                Sizing = Sizing,
                SizeValue = SizeValue,
                SizeUnit = SizeUnit,
                InnerContent = overflow,
            };
            CopyStyleTo(rest);
            return new RenderResult(rest, next);
        }
        return new RenderResult(null, next);
    }

    // Unused — Render/Measure are overridden directly — but the base requires them.
    protected override Size MeasureCore(Size available) => Measure(available);
    protected override RenderResult RenderCore(PdfContext context, Size available) => Render(context, available);
}
