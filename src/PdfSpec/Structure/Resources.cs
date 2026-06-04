using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// A page or form-XObject <c>/Resources</c> sub-dictionary (ISO 32000-1
/// §7.8.3): the named resources visible to the content stream — fonts,
/// XObjects (images, forms), ExtGStates, shadings, patterns, and properties
/// (e.g. OCGs). Each category is a lazily-created sub-dictionary attached to
/// the parent <see cref="Dictionary"/> on first use; subsequent adds with the
/// same name overwrite in place.
/// </summary>
public sealed class Resources
{
    internal PdfDictionary Dictionary { get; } = new();

    private PdfDictionary? _fonts;
    private PdfDictionary? _xobjects;
    private PdfDictionary? _shadings;
    private PdfDictionary? _patterns;
    private PdfDictionary? _extGStates;
    private PdfDictionary? _properties;

    public bool IsEmpty => Dictionary.Entries.Count == 0;

    public void AddFont(string name, PdfReference font) => Sub(ref _fonts, "Font").Add(name, font);
    public void AddXObject(string name, PdfReference xobject) => Sub(ref _xobjects, "XObject").Add(name, xobject);
    public void AddShading(string name, PdfReference shading) => Sub(ref _shadings, "Shading").Add(name, shading);
    public void AddPattern(string name, PdfReference pattern) => Sub(ref _patterns, "Pattern").Add(name, pattern);
    public void AddExtGState(string name, PdfReference extGState) => Sub(ref _extGStates, "ExtGState").Add(name, extGState);
    public void AddProperty(string name, PdfReference property) => Sub(ref _properties, "Properties").Add(name, property);

    private PdfDictionary Sub(ref PdfDictionary? cache, string category)
    {
        if (cache is null)
        {
            cache = new PdfDictionary();
            Dictionary.Add(category, cache);
        }
        return cache;
    }
}
