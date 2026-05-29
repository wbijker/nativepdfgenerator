namespace CSharpPdf.Layout;

/// <summary>
/// The outcome of rendering a <see cref="UIElement"/>: the part that did not fit
/// (<see cref="Overflow"/>, null when fully rendered) and <see cref="Next"/> — the
/// top-left position where following content should start (PDF coords).
/// </summary>
public sealed record RenderResult(UIElement? Overflow, Point Next)
{
    public bool IsComplete => Overflow is null;
}
