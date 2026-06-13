using PdfSpec.Elements;

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

    /// <summary>
    /// When true, nothing was rendered in the current slot — the element
    /// could not fit at all. The parent container should break to a fresh
    /// slot (next page / column) and retry <see cref="NextElement"/> from
    /// scratch. Set by <see cref="DoesNotFit"/>; <see cref="Partial"/>
    /// returns continuations that have already laid down some output.
    /// </summary>
    public bool RequiresBreak { get; set; }

    public static RenderResult Done(double nextY) => new RenderResult(nextY, null);
    public static RenderResult Partial(Element element) => new RenderResult(0, element);

    /// <summary>
    /// Signal that <paramref name="element"/> didn't fit the current slot
    /// at all (e.g. not enough room for a single line of text). The
    /// element is returned as the continuation with <see cref="RequiresBreak"/>
    /// set, so the layout engine can break to a fresh slot and try
    /// rendering the whole element again.
    /// </summary>
    public static RenderResult DoesNotFit(Element element) =>
        new RenderResult(0, element) { RequiresBreak = true };
}