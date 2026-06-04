using PdfSpec.Objects;

namespace PdfSpec.Structure;

/// <summary>
/// A page or form-XObject <c>/Resources</c> sub-dictionary (ISO 32000-1
/// §7.8.3): the named resources visible to the content stream — fonts,
/// XObjects (images, forms), ExtGStates, shadings, patterns, and properties
/// (e.g. OCGs). Per-category state is held in typed fields; the dictionary
/// is built fresh at write time. Re-adding the same name within a category
/// replaces the previous value.
/// </summary>
public sealed class Resources
{
    private List<KeyValuePair<string, PdfObject>>? _fonts;
    private List<KeyValuePair<string, PdfObject>>? _xobjects;
    private List<KeyValuePair<string, PdfObject>>? _shadings;
    private List<KeyValuePair<string, PdfObject>>? _patterns;
    private List<KeyValuePair<string, PdfObject>>? _extGStates;
    private List<KeyValuePair<string, PdfObject>>? _properties;

    public bool IsEmpty =>
        _fonts is null && _xobjects is null && _shadings is null
        && _patterns is null && _extGStates is null && _properties is null;

    public void AddFont(string name, PdfReference font) => AddTo(ref _fonts, name, font);
    public void AddXObject(string name, PdfReference xobject) => AddTo(ref _xobjects, name, xobject);
    public void AddShading(string name, PdfReference shading) => AddTo(ref _shadings, name, shading);
    public void AddPattern(string name, PdfReference pattern) => AddTo(ref _patterns, name, pattern);
    public void AddExtGState(string name, PdfReference extGState) => AddTo(ref _extGStates, name, extGState);
    public void AddProperty(string name, PdfReference property) => AddTo(ref _properties, name, property);

    public PdfDictionary Build()
    {
        var d = new PdfDictionary();
        Emit(d, "Font", _fonts);
        Emit(d, "XObject", _xobjects);
        Emit(d, "Shading", _shadings);
        Emit(d, "Pattern", _patterns);
        Emit(d, "ExtGState", _extGStates);
        Emit(d, "Properties", _properties);
        return d;
    }

    private static void AddTo(ref List<KeyValuePair<string, PdfObject>>? list, string name, PdfObject value)
    {
        list ??= new List<KeyValuePair<string, PdfObject>>();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Key == name)
            {
                list[i] = new KeyValuePair<string, PdfObject>(name, value);
                return;
            }
        }
        list.Add(new KeyValuePair<string, PdfObject>(name, value));
    }

    private static void Emit(PdfDictionary parent, string category, List<KeyValuePair<string, PdfObject>>? entries)
    {
        if (entries is null || entries.Count == 0) return;
        var sub = new PdfDictionary();
        foreach (var (name, value) in entries) sub.Add(name, value);
        parent.Add(category, sub);
    }
}
