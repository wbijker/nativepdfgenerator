using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

public class Rows : Element
{
    private readonly List<AxisItem> _items = new();
    public IReadOnlyList<AxisItem> Items => _items;

    public Rows Add(AxisSize size, Element content)
    {
        _items.Add(new AxisItem(size, content));
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

        double x = 0, maxNextY = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            var sub = cs.CreateSubStream(x, 0, widths[i], available.Height);
            var result = _items[i].Content.Render(sub, new PdfSize(widths[i], available.Height));
            sub.Build();
            maxNextY = Math.Max(maxNextY, result.NextY);
            x += widths[i];
        }

        return RenderResult.Done(maxNextY);
    }

    private (double[] Widths, double FixedSum, double AutoMaxSum, double RelativeUnits) AllocateWidths(PdfSize available)
    {
        var widths = new double[_items.Count];
        double fixedSum = 0, relUnits = 0;
        foreach (var item in _items)
        {
            if (item.Size.Type == AxisType.Fixed) fixedSum += item.Size.Value;
            else if (item.Size.Type == AxisType.Relative) relUnits += item.Size.Value;
        }

        double remainingForAuto = Math.Max(0, available.Width - fixedSum);
        double autoMinSum = 0, autoMaxSum = 0;
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i].Size.Type != AxisType.Auto) continue;
            var hint = _items[i].Content.SizeHint(new PdfSize(remainingForAuto, available.Height));
            widths[i] = hint.MinWidth;
            autoMinSum += hint.MinWidth;
            autoMaxSum += hint.MaxWidth ?? hint.MinWidth;
        }

        double unitWidth = relUnits > 0 ? Math.Max(0, remainingForAuto - autoMinSum) / relUnits : 0;
        for (int i = 0; i < _items.Count; i++)
        {
            widths[i] = _items[i].Size.Type switch
            {
                AxisType.Fixed => _items[i].Size.Value,
                AxisType.Relative => _items[i].Size.Value * unitWidth,
                _ => widths[i],
            };
        }

        return (widths, fixedSum, autoMaxSum, relUnits);
    }
}
