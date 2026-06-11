using PdfSpec.Layout;

namespace PdfSpec.Elements;

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
