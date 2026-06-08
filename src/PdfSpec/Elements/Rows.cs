using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

public class Rows : Element
{
    private readonly List<AxisItem> _items = new();
    public IReadOnlyList<AxisItem> Items => _items;

    /// <summary>
    /// Fallback vertical alignment for any column whose
    /// <see cref="AxisItem.VerticalAlign"/> is <c>null</c>.
    /// </summary>
    public VerticalAlign DefaultVerticalAlign { get; set; } = VerticalAlign.Top;

    public Rows Add(AxisSize size, Element content, VerticalAlign? verticalAlign = null)
    {
        _items.Add(new AxisItem(size, content, verticalAlign));
        return this;
    }

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        if (_items.Count == 0) return new PdfSizeHint(0, 0, null, null);

        var (widths, fixedSum, autoMaxSum, relUnits) = AllocateWidths(available);

        double minWidth = 0, minHeight = 0;
        double? maxHeight = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            var hint = _items[i].Content.SizeHint(new PdfSize(widths[i], available.Height));
            minWidth += hint.MinWidth;
            minHeight = Math.Max(minHeight, hint.MinHeight);
            maxHeight = maxHeight is null || hint.MaxHeight is null
                ? null
                : Math.Max(maxHeight.Value, hint.MaxHeight.Value);
        }

        double maxWidth = relUnits > 0 ? available.Width : fixedSum + autoMaxSum;
        return new PdfSizeHint(minWidth, minHeight, maxWidth, maxHeight);
    }

    public override RenderResult Render(ContentStream cs, PdfSize available)
    {
        if (_items.Count == 0) return RenderResult.Done(0);

        var (widths, _, _, _) = AllocateWidths(available);

        // Pass 1: render each column into a deferred sub-stream (no Build yet)
        // so we can learn each column's actual content height before deciding
        // how the row stacks vertically.
        var subs = new ContentStream[_items.Count];
        var heights = new double[_items.Count];
        var positions = new double[_items.Count];
        double rowHeight = 0;
        double x = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            positions[i] = x;
            subs[i] = cs.CreateSubStream(x, 0, widths[i], available.Height);
            var result = _items[i].Content.Render(subs[i], new PdfSize(widths[i], available.Height));
            heights[i] = result.NextY;
            if (result.NextY > rowHeight) rowHeight = result.NextY;
            x += widths[i];
        }

        // Pass 2: with rowHeight known, slide each sub down to the alignment
        // offset within the row's content band, then flush. Top is the existing
        // y=0 placement; Middle/Bottom shift the sub down by the slack between
        // its own content height and the row's tallest column.
        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var align = item.VerticalAlign ?? DefaultVerticalAlign;
            double slack = Math.Max(0, rowHeight - heights[i]);
            double yOffset = align switch
            {
                VerticalAlign.Middle => slack / 2,
                VerticalAlign.Bottom => slack,
                _ => 0,
            };
            subs[i].SetParentPosition(positions[i], yOffset);
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
