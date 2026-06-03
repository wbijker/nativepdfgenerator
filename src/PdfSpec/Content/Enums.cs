namespace PdfSpec.Content;

/// <summary>Line-cap style for stroked paths (ISO 32000-1 §8.4.3.3).</summary>
public enum LineCap
{
    Butt = 0,
    Round = 1,
    Square = 2,
}

/// <summary>Line-join style for stroked paths (ISO 32000-1 §8.4.3.4).</summary>
public enum LineJoin
{
    Miter = 0,
    Round = 1,
    Bevel = 2,
}

/// <summary>Filling rule for paths (ISO 32000-1 §8.5.3.3).</summary>
public enum FillRule
{
    NonZero,
    EvenOdd,
}

/// <summary>Colour rendering intent (ISO 32000-1 §8.6.5.8).</summary>
public enum RenderingIntent
{
    AbsoluteColorimetric,
    RelativeColorimetric,
    Saturation,
    Perceptual,
}

/// <summary>Tr operator values (ISO 32000-1 §9.3.6): combinations of fill / stroke / clip on glyphs.</summary>
public enum TextRenderMode
{
    Fill = 0,
    Stroke = 1,
    FillStroke = 2,
    Invisible = 3,
    FillClip = 4,
    StrokeClip = 5,
    FillStrokeClip = 6,
    Clip = 7,
}

/// <summary>Blend mode for the transparent imaging model (ISO 32000-1 §11.3.5).</summary>
public enum BlendMode
{
    Normal,
    Multiply,
    Screen,
    Overlay,
    Darken,
    Lighten,
    ColorDodge,
    ColorBurn,
    HardLight,
    SoftLight,
    Difference,
    Exclusion,
}
