using CSharpPdf.Layout;
using Font = CSharpPdf.Text.Font;

namespace CSharpPdf.Fluent;

/// <summary>
/// The core fluent builder. Wraps a single <see cref="SlotElement"/> — every
/// styling method modifies the slot, every content method (Text, Image, Svg,
/// Rows, Cols, Layers, Table, Link, Stamp, …) sets <c>Slot.Content</c> to the
/// matching programmatic element. This is purely a builder DSL on top of the
/// existing UIElement layer; no new layout logic.
/// </summary>
public sealed class FluentContainer
{
    internal readonly SlotElement Slot;

    public FluentContainer() { Slot = new SlotElement(); }
    public FluentContainer(SlotElement slot) { Slot = slot; }

    // ----- styling -----

    public FluentContainer Padding(double v) { Slot.Padding = v; return this; }
    public FluentContainer Background(Color color) { Slot.Background = color; return this; }
    public FluentContainer Border(Color color, double width = 1) { Slot.BorderColor = color; Slot.BorderThickness = width; return this; }
    public FluentContainer BorderRadius(double radius) { Slot.BorderRadius = radius; return this; }
    public FluentContainer BorderDash(params double[] dash) { Slot.BorderDash = dash; return this; }
    public FluentContainer ExtendHorizontal() { Slot.ExtendHorizontal = true; return this; }

    public FluentContainer AlignLeft() { Slot.HAlign = HorizontalAlignment.Left; return this; }
    public FluentContainer AlignCenter() { Slot.HAlign = HorizontalAlignment.Center; return this; }
    public FluentContainer AlignRight() { Slot.HAlign = HorizontalAlignment.Right; return this; }
    public FluentContainer AlignTop() { Slot.VAlign = VerticalAlignment.Top; return this; }
    public FluentContainer AlignMiddle() { Slot.VAlign = VerticalAlignment.Middle; return this; }
    public FluentContainer AlignBottom() { Slot.VAlign = VerticalAlignment.Bottom; return this; }

    // ----- leaf content -----

    public TextBuilder Text(string text)
    {
        var t = new TextElement(text);
        Slot.Content = t;
        return new TextBuilder(t);
    }

    public ImageBuilder Image(byte[] rgb, int pixelWidth, int pixelHeight)
    {
        var img = new ImageElement(rgb, pixelWidth, pixelHeight, 0, 0);
        Slot.Content = img;
        return new ImageBuilder(img);
    }

    public FluentContainer Svg(string svgXml, double displayWidth, double displayHeight)
    {
        Slot.Content = new SvgElement(svgXml, displayWidth, displayHeight);
        return this;
    }

    public PageNumberBuilder PageNumber(string format = "{0}")
    {
        var p = new PageNumberElement { Format = format };
        Slot.Content = p;
        return new PageNumberBuilder(p);
    }

    // ----- composite content -----

    public FluentContainer Rows(System.Action<RowsBuilder> build)
    {
        var rows = new RowsElement();
        Slot.Content = rows;
        build(new RowsBuilder(rows));
        return this;
    }

    public FluentContainer Cols(System.Action<ColsBuilder> build)
    {
        var cols = new ColsElement();
        Slot.Content = cols;
        build(new ColsBuilder(cols));
        return this;
    }

    public FluentContainer Layers(double height, System.Action<LayersBuilder> build)
    {
        var layers = new LayersElement { Height = height };
        Slot.Content = layers;
        build(new LayersBuilder(layers));
        return this;
    }

    public TableBuilder Table()
    {
        var t = new TableElement();
        Slot.Content = t;
        return new TableBuilder(t);
    }

    // ----- flow / overlay / sentinel -----

    public void PageBreak() => Slot.Content = new PageBreakElement();

    public FluentContainer ShowAll(System.Action<FluentContainer> build)
    {
        var sa = new ShowAllElement();
        Slot.Content = sa;
        var inner = new FluentContainer();
        build(inner);
        sa.Content = inner.Slot.Content;
        return this;
    }

    public FluentContainer Transform(System.Action<TransformBuilder> build)
    {
        var t = new TransformElement();
        Slot.Content = t;
        build(new TransformBuilder(t));
        return this;
    }

    // ----- interactive -----

    public FluentContainer Link(string url, System.Action<FluentContainer> build) =>
        WrapLink(new LinkElement { Url = url }, build);

    public FluentContainer LinkInternal(string namedTarget, System.Action<FluentContainer> build) =>
        WrapLink(new LinkElement { Target = namedTarget }, build);

    private FluentContainer WrapLink(LinkElement link, System.Action<FluentContainer> build)
    {
        Slot.Content = link;
        var inner = new FluentContainer();
        build(inner);
        link.Content = inner.Slot.Content;
        return this;
    }

    public void Note(string text, string icon = "Note", double side = 18) =>
        Slot.Content = new TextNoteElement(text) { Icon = icon, Side = side };

    public void Stamp(string name, double width = 140, double height = 50, string? contents = null) =>
        Slot.Content = new StampElement(name, width, height) { Contents = contents };

    public void Bookmark(string title) => Slot.Content = new BookmarkElement(title);

    public void Anchor(string name) => Slot.Content = new NamedAnchorElement(name);

    /// <summary>Build a raw UIElement (escape hatch back to the programmatic layer).</summary>
    public FluentContainer Element(UIElement element)
    {
        Slot.Content = element;
        return this;
    }
}
