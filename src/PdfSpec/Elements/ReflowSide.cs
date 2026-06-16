namespace PdfSpec.Elements;

/// <summary>
/// Which side of a <see cref="ReflowParagraph"/>'s available width a
/// floated element anchors against. <see cref="Left"/> places the float
/// at the left margin and lets text wrap to its right; <see cref="Right"/>
/// mirrors. Mid-line / centred floats are out of scope.
/// </summary>
public enum ReflowSide
{
    Left,
    Right,
}
