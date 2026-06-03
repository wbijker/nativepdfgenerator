namespace PdfSpec.Geometry;

/// <summary>A position in PDF user space (origin bottom-left, y increasing upward).</summary>
public readonly record struct Point(double X, double Y);
