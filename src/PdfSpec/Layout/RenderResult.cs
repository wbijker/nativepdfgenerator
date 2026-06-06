namespace PdfSpec.Layout;

public class RenderResult(double nextY, Element? nextElement)
{
    /// <summary>
    /// What is the start for the next available block to render?
    /// </summary>
    public double NextY { get; set; } = nextY;

    /// <summary>
    /// The next element to render for the next available space.
    /// Use null if the current element fits completely.
    /// </summary>
    public Element? NextElement { get; set; } = nextElement;

    public static RenderResult Done(double nextY) => new RenderResult(nextY, null);
    public static RenderResult Partial(Element element) => new RenderResult(0, element);
}