using PdfSpec.Content;
using PdfSpec.Layout;

namespace PdfSpec.Elements;

public class Rows : Element
{
    public AxisItem[] Items { get; }

    public Rows(params AxisItem[] items)
    {
        Items = items;
    }

    public override PdfSizeHint SizeHint(PdfSize available)
    {
        if (Items.Length == 0) return new PdfSizeHint(0, 0, null, null);

        var (widths, fixedSum, autoMaxSum, relUnits) = AllocateWidths(available);

        double minWidth = 0, minHeight = 0;
        double? maxHeight = 0;
        for (int i = 0; i < Items.Length; i++)
        {
            var hint = Items[i].Content.SizeHint(new PdfSize(widths[i], available.Height));
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
        if (Items.Length == 0) return RenderResult.Done(0);

        var (widths, _, _, _) = AllocateWidths(available);

        double x = 0, maxNextY = 0;
        for (int i = 0; i < Items.Length; i++)
        {
            var sub = cs.CreateSubStream(x, 0, widths[i], available.Height);
            var result = Items[i].Content.Render(sub, new PdfSize(widths[i], available.Height));
            sub.Build();
            maxNextY = Math.Max(maxNextY, result.NextY);
            x += widths[i];
        }

        return RenderResult.Done(maxNextY);
    }

    private (double[] Widths, double FixedSum, double AutoMaxSum, double RelativeUnits) AllocateWidths(PdfSize available)
    {
        var widths = new double[Items.Length];
        double fixedSum = 0, relUnits = 0;
        foreach (var item in Items)
        {
            if (item.Size.Type == AxisType.Fixed) fixedSum += item.Size.Value;
            else if (item.Size.Type == AxisType.Relative) relUnits += item.Size.Value;
        }

        double remainingForAuto = Math.Max(0, available.Width - fixedSum);
        double autoMinSum = 0, autoMaxSum = 0;
        for (int i = 0; i < Items.Length; i++)
        {
            if (Items[i].Size.Type != AxisType.Auto) continue;
            var hint = Items[i].Content.SizeHint(new PdfSize(remainingForAuto, available.Height));
            widths[i] = hint.MinWidth;
            autoMinSum += hint.MinWidth;
            autoMaxSum += hint.MaxWidth ?? hint.MinWidth;
        }

        double unitWidth = relUnits > 0 ? Math.Max(0, remainingForAuto - autoMinSum) / relUnits : 0;
        for (int i = 0; i < Items.Length; i++)
        {
            widths[i] = Items[i].Size.Type switch
            {
                AxisType.Fixed => Items[i].Size.Value,
                AxisType.Relative => Items[i].Size.Value * unitWidth,
                _ => widths[i],
            };
        }

        return (widths, fixedSum, autoMaxSum, relUnits);
    }
}
