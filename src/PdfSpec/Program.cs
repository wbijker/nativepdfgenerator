using PdfSpec.Content;
using PdfSpec.Geometry;
using PdfSpec.Fonts;
using PdfSpec.Layout;

namespace PdfSpec;

public enum AxisType
{
    Auto,
    Fixed,
    Relative
}

public class AxisSize
{
    public double Value { get; }
    public AxisType Type { get; }

    private AxisSize(double value, AxisType type)
    {
        Value = value;
        Type = type;
    }

    public static AxisSize Auto()
    {
        return new AxisSize(0, AxisType.Auto);
    }

    public static AxisSize Fixed(float value)
    {
        return new AxisSize(value, AxisType.Fixed);
    }

    public static AxisSize Relative(float value)
    {
        return new AxisSize(value, AxisType.Relative);
    }
}

public class AxisItem(AxisSize size, Element content)
{
    public AxisSize Size { get; } = size;
    public Element Content { get; } = content;
}

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

        double fixedSum = 0, relUnits = 0;
        foreach (var item in Items)
        {
            if (item.Size.Type == AxisType.Fixed) fixedSum += item.Size.Value;
            else if (item.Size.Type == AxisType.Relative) relUnits += item.Size.Value;
        }

        var remainingForAuto = Math.Max(0, available.Width - fixedSum);
        var widths = new double[Items.Length];
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
        throw new NotImplementedException();
    }
}

public class Rectangle(int size) : Element
{
    public override PdfSizeHint SizeHint(PdfSize available)
    {
        return PdfSizeHint.Fixed(size, size);
    }

    public override RenderResult Render(ContentStream cs, PdfSize available)
    {
        cs.SetFillColor(PdfColors.Blue(500));
        cs.Rectangle(0, 0, size, size);
        cs.Fill();
        cs.AddText()
            .SetFillColor(PdfColors.Black())
            .SetStrokeColor(PdfColors.Black())
            .SetTextMatrix(PdfMatrix.Translate(0, 0))
            .ShowText("Die hond blaf")
            .Build();

        return RenderResult.Done(size);
    }
}

internal static class Program
{
    private const double LabelX = 40;
    private const double DemoX = 130;

    public static void Main(string[] args)
    {
        var doc = new PdfDoc();
        doc.Info.Title = "PdfSpec Text Operators";
        doc.Info.Creator = "PdfSpec";
        doc.Info.Producer = "PdfSpec";
        doc.SetDefaultFont(Standard14Font.Helvetica, 10);

        var page = doc.AddPage(PageSizes.A4);
        var cs = page.Content;


        // here
        double y = 50;
        cs.Save();
        var box = cs.CreateSubStream(LabelX, y, 40, 40);
        new Rectangle(40).Render(box, new PdfSize(40, 40));
        box.Build();
        cs.Restore();


        // ===== Save =====
        var samplesDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../samples"));
        Directory.CreateDirectory(samplesDir);
        var output = Path.Combine(samplesDir, "spec-text-operators.pdf");
        doc.Save(output);

        Console.WriteLine($"Wrote {output}");
    }
}