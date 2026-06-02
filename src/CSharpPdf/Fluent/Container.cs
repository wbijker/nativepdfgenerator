using CSharpPdf.Layout;

namespace CSharpPdf.Fluent;

/// <summary>
/// The universal fluent surface. Every "slot" in the layout — a column item,
/// a row item, a table cell, a layer, a link/show-all body — is a
/// <see cref="Container"/>: it carries the slot's styling (padding, background,
/// border, alignment) and exposes the catalogue of content methods.
///
/// Styling methods return <c>this</c> so they chain. Content methods either
/// return a typed descriptor (so further styling can target the content
/// element specifically — e.g. <c>.Text("hi").Bold()</c>) or take a lambda
/// and return <c>void</c> (so composite content is configured inside the body).
/// </summary>
public sealed class Container
{
    internal readonly SlotElement Slot;

    public Container() { Slot = new SlotElement(); }
    internal Container(SlotElement slot) { Slot = slot; }

    // ===== Styling (chained — return this) ============================

    public Container Padding(double value) { Slot.Padding = value; return this; }

    public Container Background(Color color) { Slot.Background = color; return this; }
    public Container Border(Color color, double width = 1) { Slot.BorderColor = color; Slot.BorderThickness = width; return this; }
    public Container BorderRadius(double radius) { Slot.BorderRadius = radius; return this; }
    public Container BorderDash(params double[] dash) { Slot.BorderDash = dash; return this; }
    public Container ExtendHorizontal() { Slot.ExtendHorizontal = true; return this; }

    public Container AlignLeft() { Slot.HAlign = HorizontalAlignment.Left; return this; }
    public Container AlignCenter() { Slot.HAlign = HorizontalAlignment.Center; return this; }
    public Container AlignRight() { Slot.HAlign = HorizontalAlignment.Right; return this; }
    public Container AlignTop() { Slot.VAlign = VerticalAlignment.Top; return this; }
    public Container AlignMiddle() { Slot.VAlign = VerticalAlignment.Middle; return this; }
    public Container AlignBottom() { Slot.VAlign = VerticalAlignment.Bottom; return this; }

    /// <summary>
    /// Subscribe to the slot's <see cref="UIElement.OnRendered"/> hook —
    /// invoked once per render call after the slot's box has been placed, with
    /// the rendered page number plus absolute position + boundary.
    /// </summary>
    public Container OnRendered(System.Action<RenderedInfo> handler) { Slot.OnRendered = handler; return this; }

    // ===== Leaf content =============================================
    // Each sets the slot's content and returns a typed descriptor so the
    // caller can continue styling the content (rather than the slot).

    /// <summary>Place a single paragraph of text. Continue with <c>.Bold()</c>, <c>.FontSize(...)</c>, etc.</summary>
    public TextDescriptor Text(string text)
    {
        var element = new TextElement(text);
        Slot.Content = element;
        return new TextDescriptor(element);
    }

    /// <summary>Place a raw-RGB image. Continue with <c>.Size(w, h)</c>, <c>.Border(...)</c>.</summary>
    public ImageDescriptor Image(byte[] rgb, int pixelWidth, int pixelHeight)
    {
        var image = new ImageElement(rgb, pixelWidth, pixelHeight, 0, 0);
        Slot.Content = image;
        return new ImageDescriptor(image);
    }

    /// <summary>Place an inline SVG fragment at an explicit display size.</summary>
    public Container Svg(string svgXml, double displayWidth, double displayHeight)
    {
        Slot.Content = new SvgElement(svgXml, displayWidth, displayHeight);
        return this;
    }

    /// <summary>Place the current page number. Format takes <c>{0}</c> (current) and <c>{1}</c> (total).</summary>
    public PageNumberDescriptor PageNumber(string format = "{0}")
    {
        var element = new PageNumberElement { Format = format };
        Slot.Content = element;
        return new PageNumberDescriptor(element);
    }

    /// <summary>Place the page number of an earlier <c>Anchor</c> elsewhere in the document.</summary>
    public PageReferenceDescriptor PageReference(string anchor, string format = "{0}")
    {
        var element = new PageReferenceElement(anchor, format);
        Slot.Content = element;
        return new PageReferenceDescriptor(element);
    }

    // ===== Composite content (lambda body, void return) =============

    /// <summary>Stack items vertically (the underlying RowsElement).</summary>
    public void Column(System.Action<Column> build)
    {
        var rows = new RowsElement();
        Slot.Content = rows;
        build(new Column(rows));
    }

    /// <summary>Lay items horizontally (the underlying ColsElement).</summary>
    public void Row(System.Action<Row> build)
    {
        var cols = new ColsElement();
        Slot.Content = cols;
        build(new Row(cols));
    }

    /// <summary>Stack overlays bottom-to-top at the given block height.</summary>
    public void Layers(double height, System.Action<Layers> build)
    {
        var layers = new LayersElement { Height = height };
        Slot.Content = layers;
        build(new Layers(layers));
    }

    /// <summary>Construct a table — cells, header band, row borders, padding.</summary>
    public void Table(System.Action<Table> build)
    {
        var t = new TableElement();
        Slot.Content = t;
        build(new Table(t));
    }

    /// <summary>Apply a 2D transform (rotate / scale / pivot) around the wrapped content.</summary>
    public void Transform(System.Action<Transform> build)
    {
        var t = new TransformElement();
        Slot.Content = t;
        build(new Transform(t));
    }

    // ===== Wrappers ==================================================

    /// <summary>Wrap a sub-region in a Link annotation that opens an external URL.</summary>
    public void Link(string url, System.Action<Container> build) =>
        WrapLink(new LinkElement { Url = url }, build);

    /// <summary>Wrap a sub-region in a Link annotation that jumps to a named in-document anchor.</summary>
    public void LinkInternal(string anchor, System.Action<Container> build) =>
        WrapLink(new LinkElement { Target = anchor }, build);

    private void WrapLink(LinkElement link, System.Action<Container> build)
    {
        Slot.Content = link;
        var inner = new Container();
        build(inner);
        link.Content = inner.Slot.Content;
    }

    /// <summary>Render the body in one stretch (no pagination inside — atomic block).</summary>
    public void ShowAll(System.Action<Container> build)
    {
        var sa = new ShowAllElement();
        Slot.Content = sa;
        var inner = new Container();
        build(inner);
        sa.Content = inner.Slot.Content;
    }

    // ===== Sentinels / one-shots =====================================

    /// <summary>Force a page break at this position.</summary>
    public void PageBreak() => Slot.Content = new PageBreakElement();

    /// <summary>Register a named destination at the current position (pair with <see cref="PageReference"/>).</summary>
    public void Anchor(string name) => Slot.Content = new NamedAnchorElement(name);

    /// <summary>Add an outline (bookmark) entry pointing to the current position.</summary>
    public void Bookmark(string title) => Slot.Content = new BookmarkElement(title);

    /// <summary>Sticky-note text annotation (the icon must be one of the PDF-standard names).</summary>
    public void Note(string text, string icon = "Note", double side = 18) =>
        Slot.Content = new TextNoteElement(text) { Icon = icon, Side = side };

    /// <summary>Rubber-stamp annotation. The text is overlaid via an appearance stream.</summary>
    public void Stamp(string name, double width = 140, double height = 50, string? contents = null) =>
        Slot.Content = new StampElement(name, width, height) { Contents = contents };

    /// <summary>Escape hatch: place a raw UIElement.</summary>
    public Container Element(UIElement element)
    {
        Slot.Content = element;
        return this;
    }
}
