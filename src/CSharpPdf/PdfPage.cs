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

    internal PdfPage(PdfObjectStore store, PdfDictionary dictionary, PdfReference reference)
    {
        _store = store;
        _dictionary = dictionary;
        Reference = reference;
    }

    public PdfReference Reference { get; }
    internal PdfDictionary Dictionary => _dictionary;

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
