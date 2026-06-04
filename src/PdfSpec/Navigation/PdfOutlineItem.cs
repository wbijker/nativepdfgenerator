using PdfSpec.Objects;

namespace PdfSpec.Navigation;

/// <summary>
/// A node in the document outline (ISO 32000-1 §12.3.3, "Bookmarks or
/// Outlines"). Each item has a title, an optional destination or action, and
/// child items.
/// </summary>
public sealed class PdfOutlineItem
{
    public string Title { get; }
    public PdfArray? Destination { get; set; }
    public PdfDictionary? Action { get; set; }

    /// <summary>Whether the item is displayed expanded (children visible).</summary>
    public bool Open { get; set; } = true;

    public List<PdfOutlineItem> Children { get; } = new();

    public PdfOutlineItem(string title, PdfArray? destination = null)
    {
        Title = title;
        Destination = destination;
    }

    /// <summary>Add a child item and return it for further nesting.</summary>
    public PdfOutlineItem AddChild(string title, PdfArray? destination = null)
    {
        var child = new PdfOutlineItem(title, destination);
        Children.Add(child);
        return child;
    }
}
