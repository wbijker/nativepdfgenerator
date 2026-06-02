namespace CSharpPdf.Layout;

/// <summary>
/// The outcome of rendering a <see cref="Element"/>: the part that did not fit
/// (<see cref="Overflow"/>, null when fully rendered) and <see cref="Next"/> — the
/// top-left position where following content should start (PDF coords).
/// </summary>
public sealed record RenderResult(Element? Overflow, Point Next)
{
    public bool IsComplete => Overflow is null;
}
