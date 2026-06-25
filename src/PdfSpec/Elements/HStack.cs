using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

/// <summary>
/// Horizontal axis container. Each item carries an <see cref="AxisSize"/>
/// (Fixed / Auto / Relative) describing how its width is allocated; the
/// row's height is the tallest column's rendered NextY.
///
/// Inherits from <see cref="Element"/> so the row itself can carry
/// padding, background, per-side borders, optional explicit
/// <see cref="Element.Width"/> / <see cref="Element.Height"/>, and
/// horizontal / vertical alignment of the column band inside the chrome.
/// <see cref="Draw"/> only does column layout — chrome paint is
/// orchestrated by the base.
/// </summary>
public class HStack : Element
{
    private readonly List<HStackItem> _items = new();
    public IReadOnlyList<HStackItem> Items => _items;

    /// <summary>
    /// Fallback horizontal alignment for any column whose
    /// <see cref="HStackItem.HorizontalAlignment"/> is <c>null</c>. Applies
    /// when the column's content is naturally narrower than its allocated
    /// width — the slack is distributed by this alignment.
    /// </summary>
    public HorizontalAlignment DefaultHorizontalAlignment { get; set; } = HorizontalAlignment.Left;

    /// <summary>
    /// Fallback vertical alignment for any column whose
    /// <see cref="HStackItem.VerticalAlignment"/> is <c>null</c>. Applies
    /// to the slack between the column's rendered content height and the
    /// row's band height.
    /// </summary>
    public VerticalAlignment DefaultVerticalAlignment { get; set; } = VerticalAlignment.Top;

    /// <summary>
    /// Spacing between items, in points. For <see cref="GapMode.Between"/> /
    /// <see cref="GapMode.Around"/> this is the fixed gap inserted; for
    /// <see cref="GapMode.Evenly"/> it is ignored (free space is distributed).
    /// </summary>
    public double Gap { get; set; }

    /// <summary>How <see cref="Gap"/> / the row's free space is distributed across items. Defaults to <see cref="GapMode.Between"/>.</summary>
    public GapMode GapMode { get; set; } = GapMode.Between;

    /// <summary>Total fixed-gap width consumed by the current <see cref="GapMode"/> (zero for <see cref="GapMode.Evenly"/>, which uses slack instead).</summary>
    private double FixedGapTotal()
    {
        int n = _items.Count;
        if (n == 0 || Gap <= 0) return 0;
        return GapMode switch
        {
            GapMode.Between => (n - 1) * Gap,
            GapMode.Around  => (n + 1) * Gap,
            _ => 0,
        };
    }

    /// <summary>Leading offset and inter-item gap for the current mode, given the row and content widths.</summary>
    private (double Lead, double Between) ResolveGaps(double rowWidth, double contentWidth)
    {
        int n = _items.Count;
        switch (GapMode)
        {
            case GapMode.Around:
                return (Gap, Gap);
            case GapMode.Evenly:
                if (n == 0) return (0, 0);
                double unit = Math.Max(0, rowWidth - contentWidth) / (n + 1);
                return (unit, unit);
            default: // Between
                return (0, Gap);
        }
    }

    public HStack Add(
        AxisSize size,
        Element content,
        HorizontalAlignment? horizontalAlignment = null,
        VerticalAlignment? verticalAlignment = null)
    {
        _items.Add(new HStackItem(size, content, horizontalAlignment, verticalAlignment));
        return this;
    }

    public HStack Fixed(double width, Element content,
        HorizontalAlignment? horizontalAlignment = null,
        VerticalAlignment? verticalAlignment = null) =>
        Add(AxisSize.Fixed((float)width), content, horizontalAlignment, verticalAlignment);

    public HStack Auto(Element content,
        HorizontalAlignment? horizontalAlignment = null,
        VerticalAlignment? verticalAlignment = null) =>
        Add(AxisSize.Auto(), content, horizontalAlignment, verticalAlignment);

    public HStack Relative(double units, Element content,
        HorizontalAlignment? horizontalAlignment = null,
        VerticalAlignment? verticalAlignment = null) =>
        Add(AxisSize.Relative((float)units), content, horizontalAlignment, verticalAlignment);

    protected internal override void ResetRenderState()
    {
        foreach (var item in _items) item.Content.ResetRenderState();
        base.ResetRenderState();
    }

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        // Explicit Width / Height short-circuit the column measurement —
        // the row claims exactly the requested extent (Min and Max collapse
        // onto it). Width / Height are resolved into points against the
        // available extent first so percent units honour the parent.
        var explicitW = ResolveWidth(available.Width);
        var explicitH = ResolveHeight(available.Height);

        double chromeW = HorizontalChrome;
        double chromeH = VerticalChrome;
        if (_items.Count == 0)
        {
            return new PdfSizeHint(
                explicitW ?? chromeW,
                explicitH ?? chromeH,
                explicitW,
                explicitH);
        }

        // Fixed gaps consume row width that the columns can't use.
        double gapTotal = FixedGapTotal();
        var inner = new PdfSize(
            Math.Max(0, (explicitW ?? available.Width) - chromeW - gapTotal),
            Math.Max(0, (explicitH ?? available.Height) - chromeH));

        var (widths, fixedSum, autoMaxSum, relUnits) = AllocateWidths(inner);

        double minWidth = 0, minHeight = 0;
        double? maxHeight = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            var hint = _items[i].Content.SizeHint(new PdfSize(widths[i], inner.Height));
            minWidth += hint.MinWidth;
            minHeight = Math.Max(minHeight, hint.MinHeight);
            maxHeight = maxHeight is null || hint.MaxHeight is null
                ? null
                : Math.Max(maxHeight.Value, hint.MaxHeight.Value);
        }

        double maxWidth = relUnits > 0 ? inner.Width : fixedSum + autoMaxSum;
        return new PdfSizeHint(
            explicitW ?? minWidth + gapTotal + chromeW,
            explicitH ?? minHeight + chromeH,
            explicitW ?? (maxWidth + gapTotal + chromeW),
            explicitH ?? (maxHeight is null ? null : maxHeight.Value + chromeH));
    }

    protected override RenderResult Draw(ContentStream cs, PdfSize available)
    {
        if (_items.Count == 0) return RenderResult.Done(0);

        // Reserve fixed-gap width before allocating columns; Evenly reserves
        // nothing here and instead spreads the leftover below.
        double gapTotal = FixedGapTotal();
        var (widths, _, _, _) = AllocateWidths(
            new PdfSize(Math.Max(0, available.Width - gapTotal), available.Height));

        double contentWidth = 0;
        foreach (var w in widths) contentWidth += w;
        var (lead, gap) = ResolveGaps(available.Width, contentWidth);

        // Render every column into a deferred sub-stream (no Build yet) at
        // full column width and the row's available height. The child draws
        // at its natural size (Element no longer fills on alignment, so
        // a wrapping Element here shrinks). Record actual rendered
        // width / height so we can settle the row height and apply per-item
        // alignment before flushing each sub into its final position.
        var subs = new ContentStream[_items.Count];
        var heights = new double[_items.Count];
        var naturalWidths = new double[_items.Count];
        var positions = new double[_items.Count];
        double rowHeight = 0;
        double x = lead;
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            positions[i] = x;
            subs[i] = cs.CreateSubStream(x, 0, widths[i], available.Height);
            var result = item.Content.Render(subs[i], new PdfSize(widths[i], available.Height));
            heights[i] = result.NextY;

            // Natural width for horizontal alignment slack. SizeHint MaxWidth
            // tells us how wide the child wanted to draw; if narrower than
            // its allocated column width, the slack is distributed by the
            // item's HorizontalAlignment.
            var hint = item.Content.SizeHint(new PdfSize(widths[i], available.Height));
            naturalWidths[i] = Math.Min(widths[i], hint.MaxWidth ?? widths[i]);

            if (result.NextY > rowHeight) rowHeight = result.NextY;
            x += widths[i] + gap;
        }

        // Position pass: per-item alignment within (widths[i], rowHeight).
        // Horizontal slack = column width - natural width; vertical slack =
        // row height - rendered column height. Per-item override wins;
        // null falls back to the row's defaults.
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var hAlign = item.HorizontalAlignment ?? DefaultHorizontalAlignment;
            var vAlign = item.VerticalAlignment ?? DefaultVerticalAlignment;

            double hSlack = Math.Max(0, widths[i] - naturalWidths[i]);
            double xOffset = hAlign switch
            {
                HorizontalAlignment.Center => hSlack / 2,
                HorizontalAlignment.Right => hSlack,
                _ => 0,
            };

            double vSlack = Math.Max(0, rowHeight - heights[i]);
            double yOffset = vAlign switch
            {
                VerticalAlignment.Middle => vSlack / 2,
                VerticalAlignment.Bottom => vSlack,
                _ => 0,
            };

            subs[i].SetParentPosition(positions[i] + xOffset, yOffset);
            subs[i].Build();
        }

        return RenderResult.Done(rowHeight);
    }

    /// <summary>
    /// Distribute <paramref name="available"/>.Width across the columns. Auto
    /// columns have a base (MinWidth) and a desired (MaxWidth) width — they
    /// expand toward desired in expand mode and contract toward base in shrink
    /// mode. Relative columns have a base equal to the largest "MinWidth per
    /// unit" across all relative columns multiplied by their own unit count,
    /// and soak up any remainder when there's room to expand.
    /// </summary>
    private (double[] Widths, double FixedSum, double AutoDesiredSum, double RelativeUnits) AllocateWidths(PdfSize available)
    {
        var widths = new double[_items.Count];
        double fixedSum = 0, relUnits = 0;
        double autoDesiredSum = 0;
        double largestRelPerUnit = 0; // floor for "width per relative unit"
        var autoDesired = new double[_items.Count];

        // ----- Pass 1: classify, measure autos and relatives, place fixeds -----
        // Fixed columns lock in their width here. Autos & relatives only
        // gather measurements — their final widths depend on the mode picked
        // in pass 2. The measurement budget for both is the full available
        // box (we don't yet know how much each column will get, so let each
        // child report its own intrinsic min/desired against the whole row).
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            switch (item.Size.Type)
            {
                case AxisType.Fixed:
                    widths[i] = item.Size.Value;
                    fixedSum += item.Size.Value;
                    break;

                case AxisType.Auto:
                {
                    var hint = item.Content.SizeHint(new PdfSize(available.Width, available.Height));
                    // Base = MinWidth (the floor a column needs to render at
                    // all). Desired = MaxWidth where the child has one, else
                    // its base — there's nothing wider to ask for.
                    double desired = hint.MaxWidth ?? hint.MinWidth;
                    autoDesired[i] = desired;
                    autoDesiredSum += desired;
                    break;
                }

                case AxisType.Relative:
                {
                    relUnits += item.Size.Value;
                    // The relative's MinWidth, divided by its unit count,
                    // tells how wide each of its units must be to clear its
                    // own floor. The biggest such per-unit demand across the
                    // row becomes the per-unit floor for all relatives.
                    var hint = item.Content.SizeHint(new PdfSize(available.Width, available.Height));
                    if (item.Size.Value > 0)
                    {
                        double perUnit = hint.MinWidth / item.Size.Value;
                        if (perUnit > largestRelPerUnit) largestRelPerUnit = perUnit;
                    }
                    break;
                }
            }
        }

        // Each relative column's base = the largest-per-unit × its own units.
        double relativeBaseSum = largestRelPerUnit * relUnits;

        // ----- Pass 2: pick mode and resolve auto / relative widths -----
        // Total desired = what every column wants if nothing has to shrink.
        // Compare to available.Width to decide whether to shrink autos or
        // hand the slack to relatives.
        double totalDesired = fixedSum + autoDesiredSum + relativeBaseSum;

        if (totalDesired > available.Width)
        {
            // SHRINK MODE — autos overflowed the row. Pull width out of every
            // auto column proportional to its share of total auto desire, so
            // the wider columns give up more. Fixeds stay fixed; relatives
            // stay at their (already-minimal) base.
            double toShrink = totalDesired - available.Width;
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Size.Type == AxisType.Auto)
                {
                    double share = autoDesiredSum > 0 ? autoDesired[i] / autoDesiredSum : 0;
                    widths[i] = autoDesired[i] - share * toShrink;
                }
                else if (_items[i].Size.Type == AxisType.Relative)
                {
                    widths[i] = largestRelPerUnit * _items[i].Size.Value;
                }
            }
        }
        else
        {
            // EXPAND MODE — there's room to spare. Autos lock at desired;
            // relatives (if any) divide the leftover space by their units so
            // any unused width gets absorbed. If there are no relatives, the
            // leftover stays unused — the row is naturally narrower than
            // available, which the SizeHint MaxWidth advertises to callers.
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].Size.Type == AxisType.Auto)
                    widths[i] = autoDesired[i];
            }

            if (relUnits > 0)
            {
                double remainingForRelatives = available.Width - fixedSum - autoDesiredSum;
                double perUnit = remainingForRelatives / relUnits;
                for (int i = 0; i < _items.Count; i++)
                {
                    if (_items[i].Size.Type == AxisType.Relative)
                        widths[i] = perUnit * _items[i].Size.Value;
                }
            }
        }

        return (widths, fixedSum, autoDesiredSum, relUnits);
    }
}
