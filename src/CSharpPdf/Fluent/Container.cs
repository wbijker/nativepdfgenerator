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

    /// <summary>
    /// Reserve a block whose contents are decided after the layout pass.
    /// <paramref name="initial"/> builds a Container whose styled slot is used
    /// purely for sizing — never drawn. <paramref name="deferred"/> is invoked
    /// once per page the block lands on (after every <see cref="UIElement.OnRendered"/>
    /// has fired and <see cref="PdfCanvas.TotalPages"/> is final) with a
    /// fresh Container plus a <see cref="DynamicContext"/> carrying the page
    /// number and total count. The deferred content is drawn into the
    /// reserved area at the size the initial measured.
    ///
    /// Pattern:
    /// <code>
    /// .DynamicContent(
    ///     init => init.Text("longest possible placeholder"),
    ///     (c, ctx) => c.Text($"Last item on page {ctx.Page} of {ctx.TotalPages}: " + state[ctx.Page]));
    /// </code>
    /// </summary>
    public void DynamicContent(
        System.Action<Container> initial,
        System.Action<Container, DynamicContext> deferred)
    {
        // Build the initial Container; its slot (with all the user's styling)
        // is what gets measured. We never draw it.
        var initContainer = new Container();
        initial(initContainer);

        // Wire the deferred callback: build a fresh Container per replay,
        // hand it to the user, then render that Container's slot into the
        // sub-canvas reserved by canvas.Defer.
        Slot.Content = new DynamicContentElement(initContainer.Slot, (sub, ctx) =>
        {
            var c = new Container();
            deferred(c, ctx);
            c.Slot.Render(sub, new Size(sub.Width, sub.Height));
        });
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

    /// <summary>
    /// Compose a reusable <see cref="IComponent"/> into this container. The
    /// component receives <c>this</c> and uses the fluent API to fill it —
    /// stylistic and content methods called inside <c>Compose</c> apply to
    /// the same slot this method was called on. Apply styling <i>before</i>
    /// <c>.Component(...)</c>; chaining styling after may be overwritten by
    /// the component's own styling calls.
    /// </summary>
    public Container Component(IComponent component)
    {
        component.Compose(this);
        return this;
    }
}
