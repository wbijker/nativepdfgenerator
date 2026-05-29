namespace CSharpPdf.Layout;

/// <summary>An RGB color with components in the range 0..1 (DeviceRGB).</summary>
public readonly record struct Color(double R, double G, double B);

/// <summary>A small palette of named colors for the fluent layout API.</summary>
public static class Colors
{
    public static readonly Color Black = new(0, 0, 0);
    public static readonly Color White = new(1, 1, 1);
    public static readonly Color Red = new(0.86, 0.15, 0.15);
    public static readonly Color Green = new(0.18, 0.55, 0.34);
    public static readonly Color Blue = new(0.13, 0.31, 0.78);
    public static readonly Color Yellow = new(0.98, 0.80, 0.08);
    public static readonly Color Orange = new(0.95, 0.55, 0.10);
    public static readonly Color Gray = new(0.50, 0.50, 0.50);
    public static readonly Color LightGray = new(0.88, 0.88, 0.88);
    public static readonly Color DarkBlue = new(0.10, 0.16, 0.40);
    public static readonly Color PaleGreen = new(0.85, 0.92, 0.85);
    public static readonly Color PaleBlue = new(0.85, 0.88, 0.96);
}
