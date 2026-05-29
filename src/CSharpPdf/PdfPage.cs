using CSharpPdf.Content;
using CSharpPdf.Geometry;
using CSharpPdf.Objects;

namespace CSharpPdf;

/// <summary>
/// A single page (a leaf <c>/Page</c> node in the page tree). Wraps the page
/// dictionary and offers a typed surface for the keys covered in Chapter 1:
/// the page boxes, rotation, user unit, resources, and content stream.
/// </summary>
public sealed class PdfPage
{
    private readonly PdfObjectStore _store;
    private readonly PdfDictionary _dictionary;
    private ContentStream? _content;

    internal PdfPage(PdfObjectStore store, PdfDictionary dictionary, PdfReference reference)
    {
        _store = store;
        _dictionary = dictionary;
        Reference = reference;
    }

    public PdfReference Reference { get; }
    internal PdfDictionary Dictionary => _dictionary;

    /// <summary>
    /// The page's content-stream builder. Created on first access; its operators
    /// are serialized into the page's /Contents when the document is saved.
    /// </summary>
    public ContentStream Content => _content ??= new ContentStream();

    /// <summary>Override the (possibly inherited) page size for this page.</summary>
    public void SetMediaBox(PdfRectangle box) => _dictionary["MediaBox"] = box.ToArray();

    /// <summary>Visible region of the page; pinned to the MediaBox by viewers.</summary>
    public void SetCropBox(PdfRectangle box) => _dictionary["CropBox"] = box.ToArray();

    /// <summary>Rotate the page clockwise; must be a multiple of 90.</summary>
    public void SetRotation(int degrees)
    {
        if (degrees % 90 != 0)
        {
            throw new ArgumentException("Rotation must be a multiple of 90.", nameof(degrees));
        }
        _dictionary["Rotate"] = new PdfNumber(degrees);
    }

    /// <summary>Scale the user unit (default is 1.0 == 72 units/inch).</summary>
    public void SetUserUnit(double userUnit) => _dictionary["UserUnit"] = new PdfNumber(userUnit);

    /// <summary>Set the page's content stream from raw content-stream operators.</summary>
    public void SetContent(string content)
    {
        var stream = new PdfStream(content);
        _dictionary["Contents"] = _store.Add(stream);
    }

    /// <summary>
    /// Register an XObject (image or form) in the page's resources under
    /// <paramref name="name"/>, paintable via the Do operator.
    /// </summary>
    public void AddXObject(string name, PdfReference xobject) =>
        AddResource("XObject", name, xobject);

    /// <summary>
    /// Register a font in the page's resources under <paramref name="name"/>,
    /// selectable via the Tf operator.
    /// </summary>
    public void AddFont(string name, PdfReference font) =>
        AddResource("Font", name, font);

    /// <summary>
    /// Register an ExtGState (graphic state parameter dictionary) in the page's
    /// resources under <paramref name="name"/>, invokable via the gs operator.
    /// </summary>
    public void AddExtGState(string name, PdfDictionary graphicState)
    {
        graphicState["Type"] = new PdfName("ExtGState");
        AddResource("ExtGState", name, _store.Add(graphicState));
    }

    /// <summary>
    /// Append an annotation to the page's /Annots array (added as an indirect
    /// object). Returns the annotation's reference.
    /// </summary>
    public PdfReference AddAnnotation(PdfDictionary annotation)
    {
        if (_dictionary.Get("Annots") is not PdfArray annots)
        {
            annots = new PdfArray();
            _dictionary["Annots"] = annots;
        }
        var reference = _store.Add(annotation);
        annots.Add(reference);
        return reference;
    }

    /// <summary>
    /// Add a Link annotation: a clickable rectangle bound to an action (Chapter 5).
    /// The border is suppressed by default.
    /// </summary>
    public PdfReference AddLinkAnnotation(PdfRectangle rect, PdfDictionary action)
    {
        var link = new PdfDictionary
        {
            ["Type"] = new PdfName("Annot"),
            ["Subtype"] = new PdfName("Link"),
            ["Rect"] = rect.ToArray(),
            ["Border"] = new PdfArray(new PdfNumber(0), new PdfNumber(0), new PdfNumber(0)),
            ["A"] = action,
        };
        return AddAnnotation(link);
    }

    /// <summary>
    /// Add a Text ("sticky note") annotation with an associated Pop-up holding
    /// its text (Chapter 6, "Text Annotations and Pop-ups"). The two annotations
    /// are cross-linked via Parent/Popup.
    /// </summary>
    public void AddTextNote(PdfRectangle iconRect, string contents, string icon, PdfRectangle popupRect, bool open = true)
    {
        var note = new PdfDictionary
        {
            ["Type"] = new PdfName("Annot"),
            ["Subtype"] = new PdfName("Text"),
            ["Rect"] = iconRect.ToArray(),
            ["Contents"] = new PdfString(contents),
            ["Name"] = new PdfName(icon),
        };
        var noteRef = AddAnnotation(note);

        var popup = new PdfDictionary
        {
            ["Type"] = new PdfName("Annot"),
            ["Subtype"] = new PdfName("Popup"),
            ["Rect"] = popupRect.ToArray(),
            ["Parent"] = noteRef,
            ["Open"] = new PdfBoolean(open),
        };
        note["Popup"] = AddAnnotation(popup);
    }

    /// <summary>
    /// If a fluent content builder was used, serialize it into the page's
    /// /Contents. Called by the document at save time.
    /// </summary>
    internal void FlushContent()
    {
        if (_content is not null)
        {
            var stream = new PdfStream(_content.ToBytes());
            _dictionary["Contents"] = _store.Add(stream);
        }
    }

    /// <summary>
    /// Add an entry to the page's resource dictionary, e.g.
    /// <c>AddResource("Font", "F1", fontRef)</c> builds
    /// <c>/Resources &lt;&lt; /Font &lt;&lt; /F1 fontRef &gt;&gt; &gt;&gt;</c>.
    /// </summary>
    public void AddResource(string category, string name, PdfObject value)
    {
        if (_dictionary.Get("Resources") is not PdfDictionary resources)
        {
            resources = new PdfDictionary();
            _dictionary["Resources"] = resources;
        }
        if (resources.Get(category) is not PdfDictionary group)
        {
            group = new PdfDictionary();
            resources[category] = group;
        }
        group[name] = value;
    }
}
